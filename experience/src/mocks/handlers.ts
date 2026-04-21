import { http, HttpResponse } from 'msw'
import {
  API_ORIGIN,
  accountReferenceFixture,
  buildBrokerListResponse,
  buildOpportunityBreakdownFixture,
  buildOpportunityItemsFixture,
  createSubmission,
  createRenewal,
  getSubmission,
  getSubmissionTimeline,
  getRenewal,
  getRenewalTimeline,
  dashboardKpisFixture,
  dashboardNudgesFixture,
  dashboardOpportunitiesFixture,
  opportunityOutcomesFixture,
  opportunityAgingFixture,
  programReferenceFixture,
  renewalAgingFixture,
  renewalFlowFixture,
  renewalOutcomesFixture,
  listRenewals,
  searchUsers,
  transitionSubmission,
  transitionRenewal,
  updateSubmission,
  assignSubmission,
  assignRenewal,
  listSubmissions,
  submissionFlowFixture,
  taskFixture,
  timelineFixture,
} from './data'
import './submissions'

function apiUrl(path: string): string {
  return new URL(path, API_ORIGIN).toString()
}

export const handlers = [
  http.get(apiUrl('/dashboard/kpis'), () => HttpResponse.json(dashboardKpisFixture)),

  http.get(apiUrl('/dashboard/nudges'), () => HttpResponse.json(dashboardNudgesFixture)),

  http.get(apiUrl('/dashboard/opportunities'), () => {
    return HttpResponse.json(dashboardOpportunitiesFixture)
  }),

  http.get(apiUrl('/dashboard/opportunities/flow'), ({ request }) => {
    const entityType = new URL(request.url).searchParams.get('entityType')
    return HttpResponse.json(entityType === 'renewal' ? renewalFlowFixture : submissionFlowFixture)
  }),

  http.get(apiUrl('/dashboard/opportunities/outcomes'), ({ request }) => {
    const entityTypes = new URL(request.url).searchParams.get('entityTypes')
    if (entityTypes === 'renewal') {
      return HttpResponse.json(renewalOutcomesFixture)
    }

    if (entityTypes === 'submission') {
      return HttpResponse.json(opportunityOutcomesFixture)
    }

    return HttpResponse.json(opportunityOutcomesFixture)
  }),

  http.get(apiUrl('/dashboard/opportunities/aging'), ({ request }) => {
    const entityType = new URL(request.url).searchParams.get('entityType')
    return HttpResponse.json(entityType === 'renewal' ? renewalAgingFixture : opportunityAgingFixture)
  }),

  http.get(apiUrl('/dashboard/opportunities/:entityType/:status/breakdown'), ({ params, request }) => {
    const url = new URL(request.url)
    const groupBy = url.searchParams.get('groupBy')
    const periodDays = Number(url.searchParams.get('periodDays') ?? '180')

    if (
      typeof params.entityType !== 'string'
      || typeof params.status !== 'string'
      || !groupBy
    ) {
      return HttpResponse.json({ detail: 'Invalid breakdown request' }, { status: 400 })
    }

    return HttpResponse.json(
      buildOpportunityBreakdownFixture(
        params.entityType as 'submission' | 'renewal',
        decodeURIComponent(params.status),
        groupBy as Parameters<typeof buildOpportunityBreakdownFixture>[2],
        periodDays,
      ),
    )
  }),

  http.get(apiUrl('/dashboard/opportunities/:entityType/:status/items'), () => {
    return HttpResponse.json(buildOpportunityItemsFixture())
  }),

  http.get(apiUrl('/dashboard/opportunities/outcomes/:outcomeKey/items'), () => {
    return HttpResponse.json(buildOpportunityItemsFixture())
  }),

  http.get(apiUrl('/my/tasks'), () => HttpResponse.json(taskFixture)),

  http.get(apiUrl('/timeline/events'), () => {
    return HttpResponse.json({
      data: timelineFixture,
      page: 1,
      pageSize: 12,
      totalCount: timelineFixture.length,
      totalPages: 1,
    })
  }),

  http.get(apiUrl('/brokers'), ({ request }) => {
    const url = new URL(request.url)
    return HttpResponse.json(buildBrokerListResponse(url.searchParams))
  }),

  http.get(apiUrl('/accounts'), () => HttpResponse.json(accountReferenceFixture)),

  http.get(apiUrl('/programs'), () => HttpResponse.json(programReferenceFixture)),

  http.get(apiUrl('/users'), ({ request }) => {
    const url = new URL(request.url)
    const query = url.searchParams.get('q') ?? ''

    if (query.length < 2) {
      return HttpResponse.json({
        title: 'Validation error',
        status: 400,
        code: 'validation_error',
        errors: { q: ['Search query must be at least 2 characters.'] },
      }, { status: 400 })
    }

    return HttpResponse.json(searchUsers(query))
  }),

  http.get(apiUrl('/submissions'), ({ request }) => {
    const url = new URL(request.url)
    return HttpResponse.json(listSubmissions(url.searchParams))
  }),

  http.post(apiUrl('/submissions'), async ({ request }) => {
    const result = createSubmission(await request.json() as never)

    if ('code' in result) {
      return HttpResponse.json({
        title: 'Create failed',
        status: 400,
        code: result.code,
        detail: result.detail,
      }, { status: 400 })
    }

    return HttpResponse.json(result, { status: 201 })
  }),

  http.get(apiUrl('/submissions/:submissionId'), ({ params }) => {
    const result = getSubmission(String(params.submissionId))
    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    return HttpResponse.json(result)
  }),

  http.put(apiUrl('/submissions/:submissionId'), async ({ params, request }) => {
    const result = updateSubmission(
      String(params.submissionId),
      parseRowVersion(request.headers.get('if-match')),
      await request.json() as never,
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    if ('code' in result) {
      return HttpResponse.json({
        title: 'Update failed',
        status: result.code === 'precondition_failed' ? 412 : 400,
        code: result.code,
        detail: result.detail,
      }, { status: result.code === 'precondition_failed' ? 412 : 400 })
    }

    return HttpResponse.json(result)
  }),

  http.put(apiUrl('/submissions/:submissionId/assignment'), async ({ params, request }) => {
    const result = assignSubmission(
      String(params.submissionId),
      parseRowVersion(request.headers.get('if-match')),
      await request.json() as never,
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    if ('code' in result) {
      const status = result.code === 'precondition_failed' ? 412 : 400
      return HttpResponse.json({
        title: 'Assignment failed',
        status,
        code: result.code,
        detail: result.detail,
      }, { status })
    }

    return HttpResponse.json(result)
  }),

  http.post(apiUrl('/submissions/:submissionId/transitions'), async ({ params, request }) => {
    const result = transitionSubmission(
      String(params.submissionId),
      parseRowVersion(request.headers.get('if-match')),
      await request.json() as never,
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    if ('code' in result) {
      const status = result.code === 'precondition_failed'
        ? 412
        : result.code === 'invalid_transition' || result.code === 'missing_transition_prerequisite'
          ? 409
          : 400

      return HttpResponse.json({
        title: 'Transition failed',
        status,
        code: result.code,
        detail: result.detail,
      }, { status })
    }

    return HttpResponse.json(result, { status: 201 })
  }),

  http.get(apiUrl('/submissions/:submissionId/timeline'), ({ params, request }) => {
    const url = new URL(request.url)
    const result = getSubmissionTimeline(
      String(params.submissionId),
      Number(url.searchParams.get('page') ?? '1'),
      Number(url.searchParams.get('pageSize') ?? '20'),
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    return HttpResponse.json(result)
  }),

  http.get(apiUrl('/renewals'), ({ request }) => {
    const url = new URL(request.url)
    return HttpResponse.json(listRenewals(url.searchParams))
  }),

  http.post(apiUrl('/renewals'), async ({ request }) => {
    const result = createRenewal(await request.json() as never)

    if ('code' in result) {
      const status = result.code === 'duplicate_renewal'
        ? 409
        : result.code === 'not_found'
          ? 404
          : 400

      return HttpResponse.json({
        title: 'Create failed',
        status,
        code: result.code,
        detail: result.detail,
        errors: result.errors,
      }, { status })
    }

    return HttpResponse.json(result, { status: 201 })
  }),

  http.get(apiUrl('/renewals/:renewalId'), ({ params }) => {
    const result = getRenewal(String(params.renewalId))
    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    return HttpResponse.json(result)
  }),

  http.put(apiUrl('/renewals/:renewalId/assignment'), async ({ params, request }) => {
    const result = assignRenewal(
      String(params.renewalId),
      parseRowVersion(request.headers.get('if-match')),
      await request.json() as never,
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    if ('code' in result) {
      const status = result.code === 'precondition_failed'
        ? 412
        : result.code === 'assignment_not_allowed_in_terminal_state'
          ? 409
          : 400

      return HttpResponse.json({
        title: 'Assignment failed',
        status,
        code: result.code,
        detail: result.detail,
        errors: result.errors,
      }, { status })
    }

    return HttpResponse.json(result)
  }),

  http.post(apiUrl('/renewals/:renewalId/transitions'), async ({ params, request }) => {
    const result = transitionRenewal(
      String(params.renewalId),
      parseRowVersion(request.headers.get('if-match')),
      await request.json() as never,
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    if ('code' in result) {
      const status = result.code === 'precondition_failed'
        ? 412
        : result.code === 'invalid_transition' || result.code === 'missing_transition_prerequisite'
          ? 409
          : 400

      return HttpResponse.json({
        title: 'Transition failed',
        status,
        code: result.code,
        detail: result.detail,
        errors: result.errors,
      }, { status })
    }

    return HttpResponse.json(result, { status: 201 })
  }),

  http.get(apiUrl('/renewals/:renewalId/timeline'), ({ params, request }) => {
    const url = new URL(request.url)
    const result = getRenewalTimeline(
      String(params.renewalId),
      Number(url.searchParams.get('page') ?? '1'),
      Number(url.searchParams.get('pageSize') ?? '20'),
    )

    if (!result) {
      return HttpResponse.json({ title: 'Not found', status: 404, code: 'not_found' }, { status: 404 })
    }

    return HttpResponse.json(result)
  }),
]

function parseRowVersion(ifMatch: string | null) {
  return ifMatch?.replace(/"/g, '') ?? null
}
