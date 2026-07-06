import { getDevToken } from './dev-auth'
import { emitAuthEvent } from '@/features/auth/authEvents'
import { IS_DEV_AUTH_MODE } from '@/features/auth/authMode'
import { oidcUserManager } from '@/features/auth/oidcUserManager'
import {
  classifyAuthResponse,
  type AuthClassification,
  type ProblemDetails,
} from '@/features/session-continuity/authErrorClassifier'
import { persistFailureClassEvent } from '@/features/session-continuity/deferredTelemetryBuffer'
import {
  renewSessionForExpiredToken,
  RenewalError,
  type RenewalFailureCause,
} from '@/features/session-continuity/sessionRenewal'
import {
  buildSessionContinuityEvent,
  emitSessionContinuityEvent,
  type SessionContinuityEventName,
  type SessionContinuityIdentity,
} from '@/features/session-continuity/sessionTelemetry'

const API_BASE = ''

interface AccessTokenResolution {
  token: string
  user: SessionContinuityIdentity | null
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public problem: ProblemDetails | null,
  ) {
    super(problem?.title ?? `HTTP ${status}`)
    this.name = 'ApiError'
  }

  get code(): string | undefined {
    return this.problem?.code
  }

  get traceId(): string | undefined {
    return this.problem?.traceId
  }
}

export class MutationRetryRequiredError extends ApiError {
  constructor(public endpointRoute: string, method: string) {
    super(409, {
      type: 'https://nebula.local/problems/session-continuity/mutation-retry-required',
      title: 'Session renewed. Retry the change.',
      status: 409,
      detail: `${method} ${endpointRoute} was not replayed automatically after session renewal.`,
      code: 'mutation_retry_required',
    })
    this.name = 'MutationRetryRequiredError'
  }
}

async function resolveToken(
  method: string,
  endpointRoute: string,
): Promise<AccessTokenResolution> {
  if (!IS_DEV_AUTH_MODE) {
    const user = await oidcUserManager.getUser()
    if (user?.access_token && !user.expired) {
      return { token: user.access_token, user }
    }

    if (user?.access_token && user.expired) {
      try {
        const renewal = await renewSessionForExpiredToken()
        if (!isReadMethod(method)) {
          emitAuthEvent('mutation_retry_required', { endpointRoute, method })
          throw new MutationRetryRequiredError(endpointRoute, method)
        }

        return {
          token: renewal.accessToken,
          user: (await oidcUserManager.getUser().catch(() => null)) ?? user,
        }
      } catch (error) {
        if (error instanceof MutationRetryRequiredError) {
          throw error
        }

        const cause = renewalFailureCause(error)
        recordFailureClassEvent(user, 'silent-renewal-fail', { cause })
        beginForcedReauth(cause, method, endpointRoute, user)
        return new Promise<AccessTokenResolution>(() => {})
      }
    }

    emitAuthEvent('session_expired')
    return new Promise<AccessTokenResolution>(() => {})
  }

  return { token: await getDevToken(), user: null }
}

function resolveApiUrl(path: string): string {
  if (/^https?:\/\//.test(path)) {
    return path
  }

  if (typeof window !== 'undefined' && window.location?.origin) {
    return new URL(`${API_BASE}${path}`, window.location.origin).toString()
  }

  return `${API_BASE}${path}`
}

async function fetchApi<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await requestApi(path, options, true)
  return response.json()
}

async function fetchApiNoBody(path: string, options?: RequestInit): Promise<void> {
  await requestApi(path, options, true)
}

async function fetchBlob(path: string, options?: RequestInit): Promise<Blob> {
  const response = await requestApi(path, options, false)
  return response.blob()
}

async function requestApi(path: string, options?: RequestInit, jsonContent = true): Promise<Response> {
  const method = resolveMethod(options)
  const endpointRoute = endpointRouteFromPath(path)
  const auth = await resolveToken(method, endpointRoute)
  const response = await sendRequest(path, options, jsonContent, auth.token)

  if (response.ok) {
    return response
  }

  return handleProblemResponse({
    response,
    path,
    options,
    jsonContent,
    auth,
    method,
    endpointRoute,
    allowRenewalRetry: true,
    allowLocalDevTokenRetry: true,
  })
}

async function sendRequest(
  path: string,
  options: RequestInit | undefined,
  jsonContent: boolean,
  token: string,
): Promise<Response> {
  const headers = new Headers(options?.headers)
  if (jsonContent && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  headers.set('Authorization', `Bearer ${token}`)

  return fetch(resolveApiUrl(path), {
    ...options,
    headers,
    credentials: 'include',
  })
}

interface ProblemResponseContext {
  response: Response
  path: string
  options: RequestInit | undefined
  jsonContent: boolean
  auth: AccessTokenResolution
  method: string
  endpointRoute: string
  allowRenewalRetry: boolean
  allowLocalDevTokenRetry: boolean
}

async function handleProblemResponse(
  context: ProblemResponseContext,
): Promise<Response> {
  const { response, auth, method, endpointRoute } = context
  const problem = await response.json().catch(() => null) as ProblemDetails | null

  if (response.status === 403 && problem?.code === 'broker_scope_unresolvable') {
    emitAuthEvent('broker_scope_unresolvable')
    return navigationInFlight()
  }

  if (response.status !== 401 && response.status !== 403) {
    throw new ApiError(response.status, problem)
  }

  const classification = classifyAuthResponse(
    response,
    problem,
    endpointRoute,
  )
  recordClassificationTelemetry(classification, response.status, problem, auth.user)

  if (classification.kind === 'authz_forbidden') {
    throw new ApiError(response.status, problem)
  }

  if (classification.kind === 'auth_token_expired') {
    return handleExpiredToken(context)
  }

  if (
    classification.kind === 'auth_token_invalid' ||
    classification.kind === 'auth_session_revoked'
  ) {
    if (classification.kind === 'auth_token_invalid' && shouldRetryWithLocalDevToken(context)) {
      return retryWithLocalDevToken(context)
    }

    beginForcedReauth(classification.kind, method, endpointRoute, auth.user)
    return navigationInFlight()
  }

  beginForcedReauth('auth_unknown', method, endpointRoute, auth.user)
  return navigationInFlight()
}

async function handleExpiredToken(
  context: ProblemResponseContext,
): Promise<Response> {
  const { path, options, jsonContent, method, endpointRoute } = context

  try {
    const renewal = await renewSessionForExpiredToken()
    if (!isReadMethod(method)) {
      emitAuthEvent('mutation_retry_required', { endpointRoute, method })
      throw new MutationRetryRequiredError(endpointRoute, method)
    }

    const retry = await sendRequest(
      path,
      options,
      jsonContent,
      renewal.accessToken,
    )
    if (retry.ok) {
      return retry
    }

    if (!context.allowRenewalRetry) {
      throw new ApiError(retry.status, await retry.json().catch(() => null))
    }

    return handleProblemResponse({
      ...context,
      response: retry,
      allowRenewalRetry: false,
    })
  } catch (error) {
    if (
      error instanceof MutationRetryRequiredError ||
      error instanceof ApiError
    ) {
      throw error
    }

    const cause = renewalFailureCause(error)

    recordFailureClassEvent(context.auth.user, 'silent-renewal-fail', {
      cause,
    })
    beginForcedReauth(
      cause,
      method,
      endpointRoute,
      context.auth.user,
    )
    return navigationInFlight()
  }
}

function shouldRetryWithLocalDevToken(context: ProblemResponseContext): boolean {
  return import.meta.env.DEV &&
    context.allowLocalDevTokenRetry &&
    context.endpointRoute.startsWith('/carrier-markets')
}

async function retryWithLocalDevToken(
  context: ProblemResponseContext,
): Promise<Response> {
  const retry = await sendRequest(
    context.path,
    context.options,
    context.jsonContent,
    await getDevToken(),
  )

  if (retry.ok) {
    return retry
  }

  return handleProblemResponse({
    ...context,
    response: retry,
    allowLocalDevTokenRetry: false,
  })
}

function beginForcedReauth(
  cause:
    | 'auth_token_invalid'
    | 'auth_session_revoked'
    | 'auth_unknown'
    | RenewalFailureCause,
  method: string,
  endpointRoute: string,
  user: SessionContinuityIdentity | null,
): void {
  recordFailureClassEvent(user, 'forced-redirect', {
    cause,
    route_at_redirect: currentRouteWithoutQuery(),
  })
  emitAuthEvent('forced_reauth', {
    cause,
    method,
    endpointRoute,
    returnTo: currentRouteWithQuery(),
  })
}

function renewalFailureCause(error: unknown): RenewalFailureCause {
  return error instanceof RenewalError ? error.cause : 'idp_unreachable'
}

function recordClassificationTelemetry(
  classification: AuthClassification,
  responseStatus: number,
  problem: ProblemDetails | null,
  user: SessionContinuityIdentity | null,
): void {
  if (classification.conflict) {
    recordFailureClassEvent(user, 'auth-classifier-conflict', {
      endpoint_route: classification.endpointRoute,
      www_authenticate_class: classification.wwwAuthenticateClass ?? 'unknown',
      problem_details_type: problem?.type ?? 'unknown',
    })
  }

  if (classification.kind === 'auth_unknown') {
    recordFailureClassEvent(user, 'auth-classifier-fallback', {
      endpoint_route: classification.endpointRoute,
      response_status: responseStatus,
    })
  }
}

function recordFailureClassEvent(
  user: SessionContinuityIdentity | null,
  eventName: SessionContinuityEventName,
  payload: Record<string, unknown>,
): void {
  const event = buildSessionContinuityEvent(user, eventName, payload)
  if (!event) {
    return
  }

  persistFailureClassEvent(event)
  emitSessionContinuityEvent(event)
}

function navigationInFlight(): Promise<Response> {
  return new Promise<Response>(() => {})
}

function resolveMethod(options?: RequestInit): string {
  return (options?.method ?? 'GET').toUpperCase()
}

function isReadMethod(method: string): boolean {
  return method === 'GET' || method === 'HEAD'
}

function endpointRouteFromPath(path: string): string {
  try {
    const origin = typeof window === 'undefined'
      ? 'http://localhost'
      : window.location.origin
    return new URL(path, origin).pathname
  } catch {
    return path.split('?')[0] || path
  }
}

function currentRouteWithQuery(): string {
  return typeof window === 'undefined'
    ? '/'
    : `${window.location.pathname}${window.location.search}`
}

function currentRouteWithoutQuery(): string {
  return typeof window === 'undefined' ? '/' : window.location.pathname
}

export const api = {
  get: <T>(path: string) => fetchApi<T>(path),
  post: <T>(path: string, body: unknown, headers?: Record<string, string>) =>
    fetchApi<T>(path, { method: 'POST', body: JSON.stringify(body), headers }),
  put: <T>(path: string, body: unknown, headers?: Record<string, string>) =>
    fetchApi<T>(path, { method: 'PUT', body: JSON.stringify(body), headers }),
  patch: <T>(path: string, body: unknown, headers?: Record<string, string>) =>
    fetchApi<T>(path, { method: 'PATCH', body: JSON.stringify(body), headers }),
  postMultipart: <T>(path: string, body: FormData, headers?: Record<string, string>) =>
    requestApi(path, { method: 'POST', body, headers }, false).then((response) => response.json() as Promise<T>),
  putMultipart: <T>(path: string, body: FormData, headers?: Record<string, string>) =>
    requestApi(path, { method: 'PUT', body, headers }, false).then((response) => response.json() as Promise<T>),
  downloadBlob: (path: string, headers?: Record<string, string>) =>
    fetchBlob(path, { method: 'GET', headers }),
  delete: (path: string, headers?: Record<string, string>) =>
    fetchApiNoBody(path, { method: 'DELETE', headers }),
}
