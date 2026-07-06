import { expect, request, test, type APIRequestContext, type Page } from '@playwright/test'
import fs from 'node:fs/promises'
import path from 'node:path'

const API_BASE = 'http://127.0.0.1:8080'
const SCREENSHOTS_DIR = process.env.F0021_E2E_SCREENSHOTS_DIR
  ?? path.resolve(process.cwd(), '../planning-mds/operations/evidence/runs/2026-07-02-ddeb8492/artifacts/screenshots')

type EntityType = 'Account' | 'Broker' | 'Policy' | 'Submission' | 'Renewal'

interface EntityFixture {
  entityType: EntityType
  listPath: string
  detailPath: string
  id?: string
  label?: string
}

interface CommunicationResponse {
  id: string
  summary: string
  links: Array<{ entityType: string; entityId: string; isPrimary: boolean }>
  participants: Array<{ displayName: string; email: string | null }>
  followUpTaskIds: string[]
  isRedacted: boolean
}

test.describe.serial('F0021 communications end-to-end', () => {
  let api: APIRequestContext
  let account: EntityFixture
  let fixtures: EntityFixture[]

  test.beforeAll(async () => {
    await fs.mkdir(SCREENSHOTS_DIR, { recursive: true })
    api = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: {
        Authorization: `Bearer ${devToken(['DistributionManager'])}`,
      },
    })

    account = await loadEntity(api, 'Account', '/accounts?page=1&pageSize=1', '/accounts')
    fixtures = [
      account,
      await loadEntity(api, 'Broker', '/brokers?page=1&pageSize=1', '/brokers'),
      await loadEntity(api, 'Policy', '/policies?page=1&pageSize=1', '/policies'),
      await loadEntity(api, 'Submission', '/submissions?page=1&pageSize=1', '/submissions'),
      await loadEntity(api, 'Renewal', '/renewals?page=1&pageSize=1', '/renewals'),
    ]

    const userResponse = await api.get('/users?q=Sarah')
    expect(userResponse.ok()).toBeTruthy()
    const users = await userResponse.json()
    expect(users.users[0].userId).toBeTruthy()
  })

  test.afterAll(async () => {
    await api?.dispose()
  })

  test('rejects unauthenticated direct communication history requests', async ({ request }) => {
    const response = await request.get(`${API_BASE}/communications?entityType=Account&entityId=${account.id}&page=1&pageSize=20`)
    expect(response.status()).toBe(401)
    expect(response.headers()['content-type']).toContain('application/problem+json')
  })

  test('renders empty history without blocking the host record page', async ({ page }) => {
    const isolatedAccount = await loadEntity(api, 'Account', '/accounts?page=2&pageSize=1', '/accounts')
    await openRecord(page, isolatedAccount)
    await openCommunicationsTabIfPresent(page)

    await expect(page.getByText(/Unable to load communications/i)).toHaveCount(0)
    await expect(page.getByText('No communication activity captured for this record yet.')).toBeVisible()
    await expect(page.getByRole('button', { name: /^Add$/ })).toBeVisible()
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0021-empty-state.png'), fullPage: true })
  })

  test('captures a communication with participant and follow-up through the UI', async ({ page }) => {
    const summary = `F0021 UI capture ${Date.now()}`
    await openRecord(page, account)
    await openCommunicationsTabIfPresent(page)

    await page.getByRole('button', { name: /^Add$/ }).click()
    const dialog = page.getByRole('dialog', { name: /Add Communication/i })
    await expect(dialog).toBeVisible()
    await dialog.getByLabel(/^Summary$/i).fill(summary)
    await dialog.getByLabel(/Notes/i).fill('E2E communication body with participant and follow-up evidence.')
    await dialog.getByLabel(/^Participant$/i).fill('Jane Broker E2E')
    await dialog.getByLabel(/Participant email/i).fill('jane.e2e@example.local')
    await dialog.getByLabel(/Create follow-up task/i).check()
    await dialog.getByLabel(/Task title/i).fill(`Follow up ${summary}`)
    await dialog.getByRole('combobox', { name: /Assignee/i }).fill('Sarah')
    await page.getByRole('option', { name: /Sarah Chen/i }).click()
    await dialog.getByRole('button', { name: /Save/i }).click()

    await expect(dialog).toBeHidden()
    await expect(page.getByText(summary)).toBeVisible()
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0021-created-with-follow-up.png'), fullPage: true })

    const history = await getHistory(api, account)
    const created = history.data.find((item: CommunicationResponse) => item.summary === summary)
    expect(created).toBeTruthy()
    expect(created.links).toContainEqual(expect.objectContaining({ entityType: 'Account', entityId: account.id, isPrimary: true }))
    expect(created.participants).toContainEqual(expect.objectContaining({ displayName: 'Jane Broker E2E', email: 'jane.e2e@example.local' }))
    expect(created.followUpTaskIds.length).toBeGreaterThan(0)

    const tasks = await api.get('/my/tasks?limit=25')
    expect(tasks.ok()).toBeTruthy()
    expect(await tasks.text()).toContain(`Follow up ${summary}`)
  })

  test('renders communication history on all F0021 contextual surfaces', async ({ page }) => {
    for (const fixture of fixtures) {
      const summary = `F0021 ${fixture.entityType} seeded ${Date.now()}`
      await createCommunication(api, fixture, summary)
      await openRecord(page, fixture)
      await openCommunicationsTabIfPresent(page)
      await expect(page.getByText(/Unable to load communications/i)).toHaveCount(0)
      await expect(page.getByText(summary).first()).toBeVisible()
    }

    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0021-contextual-history.png'), fullPage: true })
  })

  test('corrects and redacts communication content with audit-safe read behavior', async ({ page }) => {
    const summary = `F0021 redact source ${Date.now()}`
    const created = await createCommunication(api, account, summary, 'Sensitive content to redact')

    const correctedSummary = `F0021 corrected ${Date.now()}`
    const correction = await api.post(`/communications/${created.id}/corrections`, {
      data: {
        action: 'Correct',
        reason: 'E2E correction reason',
        summary: correctedSummary,
        body: 'Corrected body before redaction',
      },
    })
    expect(correction.ok(), await correction.text()).toBeTruthy()

    const adminApi = await request.newContext({
      baseURL: API_BASE,
      extraHTTPHeaders: {
        Authorization: `Bearer ${devToken(['Admin'])}`,
      },
    })
    const redaction = await adminApi.post(`/communications/${created.id}/corrections`, {
      data: {
        action: 'Redact',
        reason: 'E2E redaction reason',
      },
    })
    const redactionBody = await redaction.text()
    await adminApi.dispose()
    expect(redaction.ok(), redactionBody).toBeTruthy()

    await openRecord(page, account)
    await openCommunicationsTabIfPresent(page)
    await expect(page.getByText('[Redacted]').first()).toBeVisible()
    await expect(page.getByLabel('Redacted').first()).toBeVisible()
    await expect(page.getByText(correctedSummary)).toHaveCount(0)
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0021-redacted-state.png'), fullPage: true })
  })
})

async function loadEntity(api: APIRequestContext, entityType: EntityType, listPath: string, routeRoot: string): Promise<EntityFixture> {
  const response = await api.get(listPath)
  expect(response.ok()).toBeTruthy()
  const body = await response.json()
  const item = body.data[0]
  expect(item?.id).toBeTruthy()
  return {
    entityType,
    listPath,
    detailPath: `${routeRoot}/${item.id}`,
    id: item.id,
    label: item.displayName ?? item.legalName ?? item.policyNumber ?? item.accountDisplayName ?? item.accountName ?? item.brokerName ?? item.id,
  }
}

async function openRecord(page: Page, fixture: EntityFixture) {
  await page.goto('/')
  await page.waitForLoadState('networkidle')
  await page.evaluate((detailPath) => {
    history.pushState({}, '', detailPath)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, fixture.detailPath)
  await page.waitForLoadState('networkidle')
  await expect(page.locator('body')).toContainText(fixture.label ?? fixture.id ?? fixture.entityType, { timeout: 15_000 })
}

async function openCommunicationsTabIfPresent(page: Page) {
  const addButton = page.getByRole('button', { name: /^Add$/ })
  if (await addButton.isVisible({ timeout: 1_000 }).catch(() => false)) {
    return
  }

  const tabByRole = page.getByRole('tab', { name: /^Communications$/ }).first()
  const tab = (await tabByRole.isVisible().catch(() => false))
    ? tabByRole
    : page.locator('button').filter({ hasText: /^Communications$/ }).first()

  if (await tab.isVisible({ timeout: 1_000 }).catch(() => false)) {
    await tab.click({ timeout: 10_000 })
    if (await tab.getAttribute('role').catch(() => null) === 'tab') {
      await expect(tab).toHaveAttribute('aria-selected', 'true')
    }
  }

  await expect(addButton).toBeVisible({ timeout: 10_000 })
}

async function createCommunication(api: APIRequestContext, fixture: EntityFixture, summary: string, body = 'E2E seeded communication body'): Promise<CommunicationResponse> {
  const response = await api.post('/communications', {
    data: {
      type: 'Note',
      direction: 'Internal',
      summary,
      body,
      occurredAt: new Date().toISOString(),
      emailReference: null,
      participants: [
        {
          displayName: `${fixture.entityType} Participant`,
          email: `${fixture.entityType.toLowerCase()}-participant@example.local`,
          participantType: 'Other',
          role: null,
          linkedEntityType: null,
          linkedEntityId: null,
        },
      ],
      links: [
        {
          entityType: fixture.entityType,
          entityId: fixture.id,
          isPrimary: true,
          label: fixture.label,
        },
      ],
      followUp: null,
    },
  })
  expect(response.ok()).toBeTruthy()
  return response.json()
}

async function getHistory(api: APIRequestContext, fixture: EntityFixture) {
  const response = await api.get(`/communications?entityType=${fixture.entityType}&entityId=${fixture.id}&page=1&pageSize=20`)
  expect(response.ok()).toBeTruthy()
  return response.json()
}

function devToken(roles: string[]) {
  const header = base64url({ alg: 'HS256', typ: 'JWT' })
  const payload = base64url({
    iss: 'http://localhost:9000/application/o/nebula/',
    sub: 'dev-user-001',
    name: 'Sarah Chen',
    nebula_roles: roles,
    regions: ['West', 'Central', 'East', 'South'],
    exp: Math.floor(Date.now() / 1000) + 86400,
  })
  return `${header}.${payload}.dev`
}

function base64url(value: object) {
  return Buffer.from(JSON.stringify(value)).toString('base64url')
}
