import { expect, test, type Page, type Route } from '@playwright/test'

const accountId = '22222222-2222-2222-2222-222222222222'
const policyId = '33333333-3333-3333-3333-333333333333'
const caseId = '44444444-4444-4444-4444-444444444444'
const ownerId = '11111111-1111-1111-1111-111111111111'
const communicationId = '55555555-5555-5555-5555-555555555555'

test.beforeEach(async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.addInitScript(() => {
    localStorage.setItem('nebula-theme', 'dark')
  })
  await mockF0024Apis(page)
})

test('F0024 workspace smoke: list, filters, and workspace create modal render', async ({ page }, testInfo) => {
  await page.goto('/service-cases')
  await page.waitForLoadState('networkidle')

  await expect(page.getByRole('main').getByRole('heading', { name: 'Service Cases' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'SC-2026-000123' })).toBeVisible()
  await expect(page.getByText('Acme Manufacturing')).toBeVisible()
  await expect(page.getByText('PKG-1001')).toBeVisible()
  await expect(page.getByText('Sarah Chen')).toBeVisible()
  await expect(page.getByText('Linked')).toBeVisible()

  await page.getByRole('button', { name: 'Filters' }).click()
  await page.getByLabel('Due').selectOption('next7')
  await page.getByPlaceholder('Search case, account, policy, claim, or summary').fill('carrier')
  await expect(page.getByText('Include closed')).toBeVisible()

  await page.getByRole('button', { name: 'New Case' }).click()
  const createDialog = page.getByRole('dialog', { name: 'New service case' })
  await expect(createDialog).toBeVisible()
  await createDialog.getByLabel('Account').selectOption(accountId)
  await createDialog.getByLabel('Summary').fill('Smoke test service case')
  await createDialog.getByLabel('Due date').fill('2026-07-10')
  await createDialog.getByPlaceholder('Search by name or email...').fill('Sarah')
  await expect(createDialog.getByText('sarah.chen@nebula.local')).toBeVisible()
  await createDialog.getByText('Sarah Chen').click()
  await createDialog.getByRole('button', { name: 'Create' }).click()

  await expect(page.getByRole('dialog', { name: 'New service case' })).toBeHidden()
  await testInfo.attach('f0024-workspace-smoke', {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png',
  })
})

test('F0024 detail smoke: work management, status, communication link, and history render', async ({ page }, testInfo) => {
  await page.goto(`/service-cases/${caseId}`)
  await page.waitForLoadState('networkidle')

  await expect(page.getByRole('heading', { name: 'SC-2026-000123' })).toBeVisible()
  await expect(page.getByText('Case Details')).toBeVisible()
  await expect(page.getByText('Work Management')).toBeVisible()
  await expect(page.getByText('History')).toBeVisible()

  await page.getByLabel('Priority').selectOption('Urgent')
  await page.getByLabel('Follow-up summary').fill('Smoke follow-up summary')
  await page.getByRole('button', { name: 'Save Work' }).click()
  await expect(page.getByLabel('Priority')).toHaveValue('Urgent')

  await page.getByRole('button', { name: 'Link Communication' }).click()
  const communicationDialog = page.getByRole('dialog', { name: 'Link communication' })
  await expect(communicationDialog).toBeVisible()
  await communicationDialog.getByLabel('Communication event ID').fill(communicationId)
  await communicationDialog.getByLabel('Link type').selectOption('Evidence')
  await communicationDialog.getByRole('button', { name: 'Save', exact: true }).click()
  await expect(page.getByText('Communication linked · Evidence')).toBeVisible()

  await page.locator('select').filter({ hasText: 'Select next status' }).selectOption('Waiting')
  await page.getByLabel('Waiting reason').fill('Awaiting carrier response')
  await page.getByRole('button', { name: 'Save Status' }).click()
  await expect(page.getByText('InProgress → Waiting')).toBeVisible()

  await testInfo.attach('f0024-detail-smoke', {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png',
  })
})

async function mockF0024Apis(page: Page) {
  let detailOverrides: Record<string, unknown> = {}

  await page.route('**/service-cases?**', async (route) => {
    if (route.request().resourceType() === 'document' || new URL(route.request().url()).pathname !== '/service-cases') {
      await route.fallback()
      return
    }

    await fulfillServiceCaseCollection(route)
  })

  await page.route('**/service-cases', async (route) => {
    if (route.request().resourceType() === 'document' || new URL(route.request().url()).pathname !== '/service-cases') {
      await route.fallback()
      return
    }

    await fulfillServiceCaseCollection(route)
  })

  await page.route(`**/service-cases/${caseId}`, async (route) => {
    if (route.request().resourceType() === 'document' || new URL(route.request().url()).pathname !== `/service-cases/${caseId}`) {
      await route.fallback()
      return
    }

    if (route.request().method() === 'PATCH') {
      detailOverrides = { ...detailOverrides, priority: 'Urgent', followUpSummary: 'Smoke follow-up summary' }
    }
    await fulfillJson(route, serviceCaseDetail(detailOverrides))
  })

  await page.route(`**/service-cases/${caseId}/communication-links`, async (route) => {
    detailOverrides = {
      ...detailOverrides,
      communicationLinks: [{
        communicationEventId: communicationId,
        linkType: 'Evidence',
        createdByUserId: ownerId,
        createdAt: '2026-07-03T12:00:00Z',
      }],
    }
    await fulfillJson(route, serviceCaseDetail(detailOverrides))
  })

  await page.route(`**/service-cases/${caseId}/transition`, async (route) => {
    detailOverrides = {
      ...detailOverrides,
      status: 'Waiting',
      transitions: [
        {
          fromStatus: null,
          toStatus: 'Intake',
          actorUserId: ownerId,
          occurredAt: '2026-07-03T10:00:00Z',
          reasonCode: null,
          note: null,
        },
        {
          fromStatus: 'Intake',
          toStatus: 'InProgress',
          actorUserId: ownerId,
          occurredAt: '2026-07-03T11:00:00Z',
          reasonCode: null,
          note: 'Started support',
        },
        {
          fromStatus: 'InProgress',
          toStatus: 'Waiting',
          actorUserId: ownerId,
          occurredAt: '2026-07-03T12:15:00Z',
          reasonCode: null,
          note: 'Awaiting carrier response',
        },
      ],
    }
    await fulfillJson(route, serviceCaseDetail(detailOverrides))
  })

  async function fulfillServiceCaseCollection(route: Route) {
    if (route.request().method() === 'POST') {
      await fulfillJson(route, serviceCaseDetail({ summary: 'Smoke test service case' }), 201)
      return
    }

    await fulfillJson(route, {
      data: [serviceCaseDetail()],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    })
  }

  await page.route('**/accounts?**', async (route) => {
    await fulfillJson(route, {
      data: [{
        id: accountId,
        displayName: 'Acme Manufacturing',
        legalName: 'Acme Manufacturing LLC',
        taxId: null,
        status: 'Active',
        brokerOfRecordId: null,
        brokerOfRecordName: null,
        territoryCode: 'WEST',
        region: 'West',
        primaryLineOfBusiness: 'Commercial Package',
        lastActivityAt: '2026-07-03T12:00:00Z',
        activePolicyCount: 1,
        openSubmissionCount: 0,
        renewalDueCount: 0,
        rowVersion: '1',
      }],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    })
  })

  await page.route('**/users?**', async (route) => {
    await fulfillJson(route, {
      users: [{
        userId: ownerId,
        displayName: 'Sarah Chen',
        email: 'sarah.chen@nebula.local',
        roles: ['Admin'],
        isActive: true,
      }],
    })
  })

  await page.route('**/dashboard/**', async (route) => {
    if (!new URL(route.request().url()).pathname.startsWith('/dashboard/')) {
      await route.fallback()
      return
    }
    await fulfillJson(route, {})
  })
  await page.route('**/timeline**', async (route) => {
    if (!new URL(route.request().url()).pathname.startsWith('/timeline')) {
      await route.fallback()
      return
    }
    await fulfillJson(route, { data: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })
  })
}

function serviceCaseDetail(overrides: Record<string, unknown> = {}) {
  return {
    id: caseId,
    caseNumber: 'SC-2026-000123',
    accountId,
    policyId,
    summary: 'Carrier claim follow-up',
    description: 'Coordinate claim context and carrier response.',
    type: 'ClaimSupport',
    status: 'InProgress',
    priority: 'High',
    ownerUserId: ownerId,
    ownerDisplayName: 'Sarah Chen',
    accountDisplayName: 'Acme Manufacturing',
    policyNumber: 'PKG-1001',
    dueDate: '2026-07-10',
    followUpSummary: 'Call carrier desk',
    hasClaimReference: true,
    lastActivityAt: '2026-07-03T12:00:00Z',
    claimReference: {
      carrierClaimNumber: 'CLM-123',
      dateOfLoss: '2026-07-01',
      claimantDisplayName: 'Jane Claimant',
      lossSummary: 'Water damage at insured premises.',
      carrierContactReference: 'carrier-desk',
      updatedByUserId: ownerId,
      updatedAt: '2026-07-03T12:00:00Z',
    },
    communicationLinks: [],
    taskLinks: [{
      taskId: '66666666-6666-6666-6666-666666666666',
      relationship: 'FollowUp',
      createdByUserId: ownerId,
      createdAt: '2026-07-03T11:30:00Z',
    }],
    transitions: [
      {
        fromStatus: null,
        toStatus: 'Intake',
        actorUserId: ownerId,
        occurredAt: '2026-07-03T10:00:00Z',
        reasonCode: null,
        note: null,
      },
      {
        fromStatus: 'Intake',
        toStatus: 'InProgress',
        actorUserId: ownerId,
        occurredAt: '2026-07-03T11:00:00Z',
        reasonCode: null,
        note: 'Started support',
      },
    ],
    resolvedAt: null,
    closedAt: null,
    resolutionSummary: null,
    createdByUserId: ownerId,
    createdAt: '2026-07-03T10:00:00Z',
    updatedAt: '2026-07-03T12:00:00Z',
    rowVersion: 1,
    ...overrides,
  }
}

async function fulfillJson(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}
