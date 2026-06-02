/**
 * Standalone simulation of the transient-retry control flow added to
 * sessionRenewal.ts. The real vitest suite cannot execute in this environment
 * (pnpm/WSL linker blocker — see d4-env-blocker.md), so this mirrors the exact
 * logic (mapRenewalError classification + renewWithTransientRetry loop +
 * bounded budget) and asserts behavior deterministically with plain Node.
 *
 * This validates the ALGORITHM; the committed vitest regression tests validate
 * the real module and should run green in CI / a Linux-native install.
 */
const MAX_TRANSIENT_RENEWAL_RETRIES = 1
const RENEWAL_TRANSIENT_RETRY_DELAY_MS = 0 // no real wait needed for the sim

// Faithful copy of mapRenewalError's cause classification.
function mapCause(message) {
  const n = String(message).toLowerCase()
  if (n.includes('revoked')) return 'refresh_revoked'
  if (n.includes('invalid_grant') || n.includes('login_required') || n.includes('expired'))
    return 'refresh_expired'
  return 'idp_unreachable'
}

const delay = (ms) => new Promise((r) => setTimeout(r, ms))

// Faithful copy of renewWithTransientRetry + performSilentRenewal.
async function renewWithTransientRetry(signinSilent) {
  for (let attempt = 0; ; attempt += 1) {
    try {
      const user = await signinSilent()
      if (!user?.access_token) {
        const e = new Error('no token'); e.cause = 'refresh_expired'; throw e
      }
      return { accessToken: user.access_token }
    } catch (error) {
      const cause = error.cause ?? mapCause(error.message)
      if (cause !== 'idp_unreachable' || attempt >= MAX_TRANSIENT_RENEWAL_RETRIES) {
        const e = new Error(cause); e.cause = cause; throw e
      }
      await delay(RENEWAL_TRANSIENT_RETRY_DELAY_MS)
    }
  }
}

function makeSignin(behaviors) {
  let i = 0
  const fn = () => {
    fn.calls += 1
    const b = behaviors[Math.min(i, behaviors.length - 1)]
    i += 1
    return b === 'ok'
      ? Promise.resolve({ access_token: 'renewed-token' })
      : Promise.reject(new Error(b))
  }
  fn.calls = 0
  return fn
}

let failures = 0
function check(name, cond) {
  console.log((cond ? 'PASS' : 'FAIL') + '  ' + name)
  if (!cond) failures += 1
}

const run = async () => {
  // 1. transient then success -> resolves, 2 calls
  {
    const s = makeSignin(['network request failed', 'ok'])
    const res = await renewWithTransientRetry(s).then((r) => r, (e) => e)
    check('transient-then-success resolves with token', res.accessToken === 'renewed-token')
    check('transient-then-success retries exactly once (2 calls)', s.calls === 2)
  }
  // 2. terminal revoked -> rejects fast, 1 call, no retry
  {
    const s = makeSignin(['refresh token revoked'])
    const res = await renewWithTransientRetry(s).then((r) => r, (e) => e)
    check('terminal revoked rejects with refresh_revoked', res.cause === 'refresh_revoked')
    check('terminal revoked does NOT retry (1 call)', s.calls === 1)
  }
  // 3. transient exhausted -> rejects idp_unreachable, 2 calls
  {
    const s = makeSignin(['network down'])
    const res = await renewWithTransientRetry(s).then((r) => r, (e) => e)
    check('exhausted retry rejects with idp_unreachable', res.cause === 'idp_unreachable')
    check('exhausted retry stops after the single retry (2 calls)', s.calls === 2)
  }
  // 4. terminal expired (invalid_grant) -> rejects fast, 1 call
  {
    const s = makeSignin(['invalid_grant'])
    const res = await renewWithTransientRetry(s).then((r) => r, (e) => e)
    check('terminal invalid_grant maps to refresh_expired', res.cause === 'refresh_expired')
    check('terminal invalid_grant does NOT retry (1 call)', s.calls === 1)
  }

  console.log('\nTOTAL_FAILURES=' + failures)
  process.exit(failures === 0 ? 0 : 1)
}

run()
