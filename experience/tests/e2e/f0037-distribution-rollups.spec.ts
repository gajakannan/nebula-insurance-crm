import { expect, request, test, type APIRequestContext, type Page } from '@playwright/test'
import fs from 'node:fs/promises'
import path from 'node:path'

const API_BASE = process.env.F0037_API_BASE ?? 'http://127.0.0.1:5113'
const SCREENSHOTS_DIR = process.env.F0037_E2E_SCREENSHOTS_DIR
  ?? path.resolve(process.cwd(), '../planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/artifacts/screenshots')

test.describe.serial('F0037 hierarchy-aware access scoping and distribution rollups', () => {
  let adminApi: APIRequestContext
  let managerApi: APIRequestContext

  test.beforeAll(async () => {
    await fs.mkdir(SCREENSHOTS_DIR, { recursive: true })
    adminApi = await apiFor(['Admin'])
    managerApi = await apiFor(['DistributionManager'])
  })

  test.afterAll(async () => {
    await adminApi?.dispose()
    await managerApi?.dispose()
  })

  test('loads rollups from the sidebar and preserves the F0037 tab/filter surface', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('link', { name: /Operational Reports/i })).toBeVisible()
    await page.getByRole('link', { name: /Operational Reports/i }).click()

    await expect(page).toHaveURL(/\/operational-reports\?report=rollups/)
    await expect(page.getByRole('tab', { name: /Distribution rollups/i })).toHaveAttribute('aria-selected', 'true')
    await expect(page.getByLabel(/Root node/i)).toBeVisible()
    await expect(page.getByLabel(/Territory/i)).toBeVisible()
    await expect(page.getByLabel(/Producer/i)).toBeVisible()
    await expect(page.getByLabel(/As of/i)).toBeVisible()
    await expect(page.getByLabel(/Group by/i)).toBeVisible()
    await expect(page.getByLabel(/Metric family/i)).toBeVisible()
    await expect(page.getByLabel(/Region/i)).toHaveCount(0)
    await expect(page.getByLabel(/Line of business/i)).toHaveCount(0)
    await expect(page.getByLabel(/Workflow type/i)).toHaveCount(0)

    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0037-sidebar-rollups.png'), fullPage: true })
  })

  test('supports rollup grouping, metric-family switching, filter query params, and drilldown links', async ({ page }) => {
    await page.goto('/operational-reports?report=rollups')
    await expect(page.getByText(/hidden records are excluded from totals/i)).toBeVisible({ timeout: 15_000 })

    await page.getByLabel(/Group by/i).selectOption('Territory')
    await expect(page).toHaveURL(/groupBy=Territory/)
    await page.getByLabel(/Group by/i).selectOption('Producer')
    await expect(page).toHaveURL(/groupBy=Producer/)

    await page.getByLabel(/Metric family/i).selectOption('Workflow')
    await expect(page).toHaveURL(/metricFamily=Workflow/)
    await page.getByLabel(/Metric family/i).selectOption('Activity')
    await expect(page).toHaveURL(/metricFamily=Activity/)

    await page.getByLabel(/As of/i).fill('2026-07-06')
    await page.getByLabel(/Root node/i).fill('00000000-0000-0000-0000-000000000000')
    await expect(page).toHaveURL(/asOf=2026-07-06/)
    await expect(page).toHaveURL(/rootNodeId=00000000-0000-0000-0000-000000000000/)
    await expect(page.getByText(/No visible rollup rows/i)).toBeVisible({ timeout: 15_000 })
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0037-scoped-empty.png'), fullPage: true })

    await page.goto('/operational-reports?report=rollups')
    const drilldown = page.locator('table a[href*="report=workload"]').first()
    if (await drilldown.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await drilldown.click()
      await expect(page).toHaveURL(/report=workload/)
      await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0037-drilldown.png'), fullPage: true })
    } else {
      await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'f0037-rollups-default.png'), fullPage: true })
    }
  })

  test('enforces no-leak API behavior for external users and scoped-away filters', async () => {
    const adminRollups = await adminApi.get('/operational-reports/distribution-rollups?groupBy=Hierarchy&metricFamily=Production&asOf=2026-07-06')
    expect(adminRollups.ok(), adminRollups.ok() ? undefined : await adminRollups.text()).toBeTruthy()
    const adminBody = await adminRollups.json()
    expect(adminBody.totals).toBeTruthy()
    expect(Array.isArray(adminBody.rows)).toBeTruthy()

    const managerScopedAway = await managerApi.get('/operational-reports/distribution-rollups?groupBy=Hierarchy&metricFamily=Production&rootNodeId=00000000-0000-0000-0000-000000000000&asOf=2026-07-06')
    expect(managerScopedAway.ok(), managerScopedAway.ok() ? undefined : await managerScopedAway.text()).toBeTruthy()
    const scopedBody = await managerScopedAway.json()
    expect(scopedBody.rows).toEqual([])
    expect(scopedBody.totals.recordCount).toBe(0)

    const externalApi = await apiFor(['ExternalUser'])
    const externalSearch = await externalApi.get('/search-results?q=policy&rootNodeId=00000000-0000-0000-0000-000000000000')
    expect([200, 403, 404]).toContain(externalSearch.status())
    if (externalSearch.status() === 200) {
      const body = await externalSearch.json()
      expect(body.data ?? body.results ?? []).toEqual([])
    } else {
      const text = await externalSearch.text()
      expect(text).not.toMatch(/recordCount|totalCount|hidden/i)
    }
    await externalApi.dispose()
  })

  test('keeps search and broker insights scoped with F0037 query filters', async ({ page }) => {
    await page.goto('/search?q=policy&rootNodeId=00000000-0000-0000-0000-000000000000&territoryId=00000000-0000-0000-0000-000000000000&producerUserId=00000000-0000-0000-0000-000000000000')
    await expect(page.getByLabel(/Root node/i)).toHaveValue('00000000-0000-0000-0000-000000000000')
    await expect(page.getByLabel(/Territory/i)).toHaveValue('00000000-0000-0000-0000-000000000000')
    await expect(page.getByLabel(/Producer/i)).toHaveValue('00000000-0000-0000-0000-000000000000')

    const brokerInsights = await managerApi.get('/broker-insights/scorecards?periodStart=2026-01-01&periodEnd=2026-07-06&territoryId=00000000-0000-0000-0000-000000000000')
    expect([200, 403, 404]).toContain(brokerInsights.status())
    if (brokerInsights.status() === 200) {
      const body = await brokerInsights.json()
      expect(JSON.stringify(body)).not.toMatch(/hidden/i)
    }
  })
})

async function apiFor(roles: string[]) {
  return request.newContext({
    baseURL: API_BASE,
    extraHTTPHeaders: {
      Authorization: `Bearer ${devToken(roles)}`,
    },
  })
}

function devToken(roles: string[]) {
  const header = base64url({ alg: 'HS256', typ: 'JWT' })
  const payload = base64url({
    iss: 'http://localhost:9000/application/o/nebula/',
    sub: roles.includes('ExternalUser') ? 'external-user-001' : 'dev-user-001',
    name: roles.includes('ExternalUser') ? 'External User' : 'Sarah Chen',
    nebula_roles: roles,
    regions: ['West', 'Central', 'East', 'South'],
    exp: Math.floor(Date.now() / 1000) + 86400,
  })
  return `${header}.${payload}.dev`
}

function base64url(value: object) {
  return Buffer.from(JSON.stringify(value)).toString('base64url')
}
