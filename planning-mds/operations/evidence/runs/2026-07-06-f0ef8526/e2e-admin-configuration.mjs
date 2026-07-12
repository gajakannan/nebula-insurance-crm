const baseUrl = process.env.F0032_E2E_BASE_URL ?? 'http://127.0.0.1:5174';
const domainKey = process.env.F0032_E2E_DOMAIN ?? 'template-metadata';
const runId = `f0032-e2e-${Date.now()}`;

function b64(obj) {
  return Buffer.from(JSON.stringify(obj)).toString('base64url');
}

function token(roles) {
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64({
    iss: 'http://localhost:9000/application/o/nebula/',
    sub: roles.includes('Admin') ? 'dev-user-001' : 'dev-user-002',
    name: roles.includes('Admin') ? 'Sarah Chen' : 'Non Admin Tester',
    nebula_roles: roles,
    regions: ['West', 'Central', 'East', 'South'],
    exp: Math.floor(Date.now() / 1000) + 86400,
  })}.dev`;
}

const adminToken = token(['Admin', 'DistributionManager']);
const nonAdminToken = token(['DistributionManager']);
const steps = [];

async function request(method, path, { body, auth = adminToken, headers = {} } = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      Authorization: `Bearer ${auth}`,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...headers,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { status: response.status, contentType: response.headers.get('content-type'), data };
}

function pass(name, detail = {}) {
  steps.push({ name, result: 'PASS', ...detail });
}

function fail(name, detail = {}) {
  steps.push({ name, result: 'FAIL', ...detail });
  throw new Error(`${name} failed: ${JSON.stringify(detail)}`);
}

function expectStatus(name, actual, expected, detail = {}) {
  if (actual !== expected) fail(name, { expected, actual, ...detail });
  pass(name, { status: actual, ...detail });
}

async function getDetail() {
  const detail = await request('GET', `/admin/configuration-domains/${domainKey}`);
  expectStatus('domain detail loads', detail.status, 200);
  return detail.data;
}

async function getOrCreateDraft(reason) {
  const detail = await getDetail();
  if (detail.activeDraft) {
    pass('reuse existing active draft', { draftId: detail.activeDraft.id, status: detail.activeDraft.status });
    return detail.activeDraft;
  }
  const created = await request('POST', `/admin/configuration-domains/${domainKey}/drafts`, { body: { reason } });
  expectStatus('create draft', created.status, 201);
  return created.data;
}

async function updateDraft(draft, marker, reason) {
  const payload = {
    ...(typeof draft.payload === 'object' && draft.payload !== null ? draft.payload : {}),
    e2eMarker: {
      runId,
      marker,
      updatedAt: new Date().toISOString(),
    },
  };
  const updated = await request('PATCH', `/admin/configuration-drafts/${draft.id}`, {
    body: { payload, reason },
    headers: { 'If-Match': String(draft.rowVersion) },
  });
  expectStatus(`update draft ${marker}`, updated.status, 200);
  return updated.data;
}

async function validateComparePublish(draft, marker) {
  const validation = await request('POST', `/admin/configuration-drafts/${draft.id}/validation`, { body: {} });
  expectStatus(`validate draft ${marker}`, validation.status, 200, { validationStatus: validation.data?.status });
  if (validation.data?.status !== 'Passed') fail(`validation status ${marker}`, { validation: validation.data });

  const comparison = await request('GET', `/admin/configuration-drafts/${draft.id}/comparison`);
  expectStatus(`compare draft ${marker}`, comparison.status, 200, { changes: comparison.data?.compareSummary?.length ?? 0 });

  const published = await request('POST', `/admin/configuration-drafts/${draft.id}/publish`, {
    body: { reason: `${runId} publish ${marker}` },
  });
  expectStatus(`publish draft ${marker}`, published.status, 200, { publishedVersion: published.data?.publishedVersion });
  return published.data;
}

const health = await fetch('http://localhost:5113/healthz').then(async (response) => ({
  status: response.status,
  text: await response.text(),
}));
expectStatus('api health', health.status, 200, { text: health.text });

const unauthenticatedProxy = await request('GET', '/admin/configuration-domains', { auth: 'invalid' });
expectStatus('unauthenticated admin proxy returns API problem', unauthenticatedProxy.status, 401, {
  contentType: unauthenticatedProxy.contentType,
});

const nonAdmin = await request('GET', '/admin/configuration-domains', { auth: nonAdminToken });
expectStatus('non-admin blocked from catalog', nonAdmin.status, 403);

const domains = await request('GET', '/admin/configuration-domains');
expectStatus('admin catalog loads', domains.status, 200, { count: Array.isArray(domains.data) ? domains.data.length : null });
for (const expected of ['queue-routing', 'workflow-sla-thresholds', 'search-report-defaults', 'template-metadata']) {
  if (!domains.data?.some((domain) => domain.domainKey === expected)) fail(`catalog contains ${expected}`);
  pass(`catalog contains ${expected}`);
}

let draft = await getOrCreateDraft(`${runId} create first draft`);

const blankReason = await request('PATCH', `/admin/configuration-drafts/${draft.id}`, {
  body: { payload: draft.payload, reason: '' },
  headers: { 'If-Match': String(draft.rowVersion) },
});
expectStatus('blank draft update reason rejected', blankReason.status, 400);

const missingIfMatch = await request('PATCH', `/admin/configuration-drafts/${draft.id}`, {
  body: { payload: draft.payload, reason: `${runId} missing if-match` },
});
expectStatus('missing If-Match rejected', missingIfMatch.status, 428);

draft = await updateDraft(draft, 'first', `${runId} update first draft`);
const firstPublish = await validateComparePublish(draft, 'first');

draft = await getOrCreateDraft(`${runId} create second draft`);
draft = await updateDraft(draft, 'second', `${runId} update second draft`);
const secondPublish = await validateComparePublish(draft, 'second');

draft = await getOrCreateDraft(`${runId} create stale-validation draft`);
let staleDraft = await updateDraft(draft, 'stale-before-validation', `${runId} update stale before validation`);
const staleValidation = await request('POST', `/admin/configuration-drafts/${staleDraft.id}/validation`, { body: {} });
expectStatus('validate stale draft before mutation', staleValidation.status, 200, { validationStatus: staleValidation.data?.status });
staleDraft = (await getDetail()).activeDraft;
if (!staleDraft) fail('stale draft remains active after validation');
staleDraft = await updateDraft(staleDraft, 'stale-after-validation', `${runId} mutate after validation`);
const stalePublish = await request('POST', `/admin/configuration-drafts/${staleDraft.id}/publish`, {
  body: { reason: `${runId} stale publish attempt` },
});
expectStatus('stale validation publish rejected', stalePublish.status, 409, { code: stalePublish.data?.code });
await validateComparePublish(staleDraft, 'after-stale-revalidation');

const rollback = await request('POST', `/admin/configuration-domains/${domainKey}/rollback`, {
  body: {
    targetPublishedVersion: firstPublish.publishedVersion,
    reason: `${runId} rollback to first published version`,
  },
});
expectStatus('rollback publishes new version', rollback.status, 200, {
  targetPublishedVersion: firstPublish.publishedVersion,
  rollbackPublishedVersion: rollback.data?.publishedVersion,
});
if (!(rollback.data?.publishedVersion > secondPublish.publishedVersion)) {
  fail('rollback version is append-only', {
    secondPublishedVersion: secondPublish.publishedVersion,
    rollbackPublishedVersion: rollback.data?.publishedVersion,
  });
}
pass('rollback version is append-only', {
  secondPublishedVersion: secondPublish.publishedVersion,
  rollbackPublishedVersion: rollback.data.publishedVersion,
});

for (const action of ['DraftCreated', 'DraftUpdated', 'ValidationPassed', 'Published', 'RollbackPublished']) {
  const audit = await request('GET', `/admin/configuration-audit-events?domainKey=${domainKey}&action=${action}&pageSize=25`);
  expectStatus(`audit filter ${action}`, audit.status, 200, { totalCount: audit.data?.totalCount });
  if ((audit.data?.items?.length ?? 0) === 0) fail(`audit has ${action}`);
  pass(`audit has ${action}`, { count: audit.data.items.length });
}

const finalDetail = await getDetail();
pass('final domain detail includes published history', {
  currentPublishedVersion: finalDetail.currentPublishedSet?.publishedVersion ?? null,
  publishedSetCount: finalDetail.publishedSets?.length ?? 0,
  refreshStatuses: finalDetail.refreshStatuses?.map((status) => status.status) ?? [],
});

console.log(JSON.stringify({
  result: 'PASS',
  runId,
  baseUrl,
  domainKey,
  firstPublishedVersion: firstPublish.publishedVersion,
  secondPublishedVersion: secondPublish.publishedVersion,
  rollbackPublishedVersion: rollback.data.publishedVersion,
  steps,
}, null, 2));
