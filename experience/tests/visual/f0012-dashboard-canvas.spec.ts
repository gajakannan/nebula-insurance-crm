import { expect, test, type Page, type TestInfo } from '@playwright/test'

type Theme = 'dark' | 'light'

interface DashboardRouteCalls {
  kpiPeriods: number[]
  flowPeriods: number[]
  outcomePeriods: number[]
  agingCalls: number
  mixCalls: number
}

interface DashboardMockOptions {
  failMix?: boolean
}

const PERIOD_KPIS: Record<number, {
  activeBrokers: number
  openSubmissions: number
  renewalRate: number
  avgTurnaroundDays: number
}> = {
  30: { activeBrokers: 128, openSubmissions: 19, renewalRate: 0.61, avgTurnaroundDays: 3.2 },
  90: { activeBrokers: 128, openSubmissions: 33, renewalRate: 0.69, avgTurnaroundDays: 4.1 },
  180: { activeBrokers: 128, openSubmissions: 44, renewalRate: 0.74, avgTurnaroundDays: 4.8 },
  365: { activeBrokers: 128, openSubmissions: 57, renewalRate: 0.79, avgTurnaroundDays: 5.4 },
}

const PERIOD_FLOW_COUNTS: Record<number, { received: number; triaging: number; review: number; quoted: number }> = {
  30: { received: 6, triaging: 5, review: 3, quoted: 2 },
  90: { received: 11, triaging: 8, review: 6, quoted: 4 },
  180: { received: 18, triaging: 14, review: 9, quoted: 7 },
  365: { received: 24, triaging: 18, review: 12, quoted: 9 },
}

const PERIOD_OUTCOME_COUNTS: Record<number, { bound: number; declined: number; expired: number }> = {
  30: { bound: 4, declined: 2, expired: 1 },
  90: { bound: 8, declined: 4, expired: 2 },
  180: { bound: 14, declined: 6, expired: 3 },
  365: { bound: 21, declined: 9, expired: 5 },
}

test.beforeEach(async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
})

test('flat canvas renders nudge zone, KPI band, connected flow, and terminal outcomes without panel wrappers', async ({ page }) => {
  await mockDashboardApis(page)
  await openDashboard(page, 'dark')

  await expect(page.locator('[aria-label="Nudge zone"]')).toBeVisible()
  await expect(page.locator('[aria-label="Story controls"]')).toBeVisible()
  await expect(page.getByRole('button', { name: /Received stage, 18 opportunities/i })).toBeVisible()
  await expect(page.getByRole('button', { name: /Bound outcome, 14 exits/i })).toBeVisible()
  await expect(page.locator('[aria-label="Activity section"]')).toBeVisible()
  await expect(page.locator('[aria-label="My tasks section"]')).toBeVisible()

  await expectFlatCanvasSurface(page)
})

test('period switching keeps KPI, flow, and outcomes synchronized', async ({ page }) => {
  const calls = await mockDashboardApis(page)
  await openDashboard(page, 'dark')

  await expect.poll(async () => readKpiValue(page, 'Open Submissions')).toBe('44')
  await expect(page.getByRole('button', { name: /Received stage, 18 opportunities/i })).toBeVisible()
  await expect(page.getByRole('button', { name: /Bound outcome, 14 exits/i })).toBeVisible()

  await page.getByRole('tab', { name: '30d' }).click()

  await expect.poll(async () => readKpiValue(page, 'Open Submissions')).toBe('19')
  await expect(page.getByRole('button', { name: /Received stage, 6 opportunities/i })).toBeVisible()
  await expect(page.getByRole('button', { name: /Bound outcome, 4 exits/i })).toBeVisible()

  expect(calls.kpiPeriods).toEqual(expect.arrayContaining([180, 30]))
  expect(calls.flowPeriods).toEqual(expect.arrayContaining([180, 30]))
  expect(calls.outcomePeriods).toEqual(expect.arrayContaining([180, 30]))
})

test('chapter overlays compose onto base flow and lazy-load aging/mix data with fallback behavior', async ({ page }) => {
  const calls = await mockDashboardApis(page, { failMix: true })
  await openDashboard(page, 'dark')

  expect(calls.agingCalls).toBe(0)
  expect(calls.mixCalls).toBe(0)

  await page.getByRole('tab', { name: 'Friction' }).click()
  await expect(page.getByLabel('Friction overlay')).toBeVisible()
  await expect(page.getByText('bottleneck')).toBeVisible()

  await page.getByRole('tab', { name: 'Outcomes' }).click()
  await expect(page.getByLabel('Outcomes overlay')).toBeVisible()
  await expect(page.getByText(/% of exits/).first()).toBeVisible()

  await page.getByRole('tab', { name: 'Aging' }).click()
  await expect.poll(() => calls.agingCalls).toBe(1)
  await expect(page.getByLabel('Aging overlay')).toBeVisible()
  await expect(page.getByText('Aging intensity')).toBeVisible()

  await page.getByRole('tab', { name: 'Mix' }).click()
  await expect.poll(() => calls.mixCalls).toBe(1)
  await expect(page.getByText('Unable to load mix overlay data')).toBeVisible()

  // Base flow remains visible while overlay fails.
  await expect(page.getByRole('button', { name: /Received stage, 18 opportunities/i })).toBeVisible()
})

test('canvas width adapts correctly across all left/right rail collapse combinations', async ({ page }) => {
  await mockDashboardApis(page)
  await openDashboard(page, 'dark')

  const shell = page.locator('.lg-sidebar-offset').first()
  const canvas = page.locator('main > div.mx-auto.w-full').first()

  const readVars = async () => shell.evaluate((el) => {
    const style = (el as HTMLElement).style
    return {
      sidebarWidth: style.getPropertyValue('--sidebar-width').trim(),
      chatWidth: style.getPropertyValue('--chat-panel-width').trim(),
    }
  })

  const readCanvasWidth = async () => {
    const box = await canvas.boundingBox()
    return box?.width ?? 0
  }

  const initialVars = await readVars()
  const bothExpandedWidth = await readCanvasWidth()
  expect(initialVars.sidebarWidth).toBe('16rem')
  expect(initialVars.chatWidth).toBe('22rem')

  await page.getByRole('button', { name: 'Collapse sidebar' }).click()
  await expect(page.getByRole('button', { name: 'Expand sidebar' })).toBeVisible()
  const leftOnlyVars = await readVars()
  const leftOnlyWidth = await readCanvasWidth()
  expect(leftOnlyVars.sidebarWidth).toBe('4rem')
  expect(leftOnlyVars.chatWidth).toBe('22rem')
  expect(leftOnlyWidth).toBeGreaterThan(bothExpandedWidth + 100)

  await page.getByRole('button', { name: 'Expand sidebar' }).click()
  await page.getByRole('button', { name: 'Collapse chat panel' }).click()
  await expect(page.getByRole('button', { name: 'Expand chat panel', exact: true })).toBeVisible()
  const rightOnlyVars = await readVars()
  const rightOnlyWidth = await readCanvasWidth()
  expect(rightOnlyVars.sidebarWidth).toBe('16rem')
  expect(rightOnlyVars.chatWidth).toBe('4rem')
  expect(rightOnlyWidth).toBeGreaterThan(bothExpandedWidth + 100)

  await page.getByRole('button', { name: 'Collapse sidebar' }).click()
  const bothCollapsedVars = await readVars()
  const bothCollapsedWidth = await readCanvasWidth()
  expect(bothCollapsedVars.sidebarWidth).toBe('4rem')
  expect(bothCollapsedVars.chatWidth).toBe('4rem')
  expect(bothCollapsedWidth).toBeGreaterThan(rightOnlyWidth + 100)
})

test('activity and tasks are stacked below the canvas and keep action handoff links', async ({ page }) => {
  await mockDashboardApis(page)
  await openDashboard(page, 'dark')

  const activitySection = page.locator('[aria-label="Activity section"]')
  const tasksSection = page.locator('[aria-label="My tasks section"]')

  const activityBox = await activitySection.boundingBox()
  const tasksBox = await tasksSection.boundingBox()

  expect(activityBox).not.toBeNull()
  expect(tasksBox).not.toBeNull()
  expect(tasksBox!.y).toBeGreaterThan(activityBox!.y + activityBox!.height - 8)

  const taskLink = page.getByRole('link', { name: 'Blue Horizon Brokerage' })
  await expect(taskLink).toBeVisible()
  await expect(taskLink).toHaveAttribute('href', /\/brokers\/broker-1$/)
})

test('keyboard and screen-reader navigation works with reduced-motion mode', async ({ page }) => {
  await mockDashboardApis(page)
  await openDashboard(page, 'dark')

  await expect(page.getByRole('tablist', { name: 'Story chapters' })).toBeVisible()
  await expect(page.getByRole('tablist', { name: 'Opportunity period window' })).toBeVisible()

  const frictionTab = page.getByRole('tab', { name: 'Friction' })
  await frictionTab.focus()
  await page.keyboard.press('Enter')
  await expect(frictionTab).toHaveAttribute('aria-selected', 'true')

  await expect(page.getByLabel('Friction overlay')).toBeVisible()
  await expect(page.getByRole('button', { name: /Received stage, 18 opportunities/i })).toBeVisible()

  const sidebarTransitionDuration = await page.locator('.sidebar').first().evaluate((el) => {
    return getComputedStyle(el).transitionDuration
  })
  expect(sidebarTransitionDuration).toBe('0s')
})

test('responsive breakpoints render without horizontal overflow and capture no-panel-border snapshots', async ({ page }, testInfo) => {
  await mockDashboardApis(page)

  const breakpoints = [
    { name: 'macbook', width: 1440, height: 900 },
    { name: 'tablet-landscape', width: 1024, height: 768 },
    { name: 'tablet-portrait', width: 768, height: 1024 },
    { name: 'phone', width: 375, height: 812 },
  ] as const

  for (const viewport of breakpoints) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height })
    await openDashboard(page, 'light')

    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
    await expectFlatCanvasSurface(page)

    const overflowDelta = await page.evaluate(() => {
      return document.documentElement.scrollWidth - document.documentElement.clientWidth
    })
    // Allow small fixed-rail gutter variance observed in containerized Chromium snapshots.
    expect(overflowDelta).toBeLessThanOrEqual(24)

    await attachPageScreenshot(page, testInfo, `dashboard-canvas-${viewport.name}`)
  }
})

async function openDashboard(page: Page, theme: Theme) {
  await page.addInitScript((selectedTheme) => {
    localStorage.setItem('nebula-theme', selectedTheme)
  }, theme)

  await page.goto('/')
  await page.waitForLoadState('networkidle')

  await page.addStyleTag({
    content: `
      *, *::before, *::after {
        animation: none !important;
        transition: none !important;
        caret-color: transparent !important;
      }
    `,
  })
}

async function readKpiValue(page: Page, label: string): Promise<string | null> {
  return page.evaluate((targetLabel) => {
    const metricLabel = Array.from(document.querySelectorAll('p')).find(
      (el) => el.textContent?.trim() === targetLabel,
    ) as HTMLElement | undefined

    if (!metricLabel) return null

    const valueNode = metricLabel.parentElement?.querySelector('p.mt-1') as HTMLElement | null
    return valueNode?.textContent?.trim() ?? null
  }, label)
}

async function expectFlatCanvasSurface(page: Page) {
  const forbiddenElements = page.locator('.glass-card, .surface-card, .content-inset, hr')
  await expect(forbiddenElements).toHaveCount(0)

  const hasCanvasBorders = await page.$$eval('.canvas-section', (sections) => {
    return sections.some((section) => {
      const style = getComputedStyle(section)
      return (
        style.borderTopWidth !== '0px'
        || style.borderRightWidth !== '0px'
        || style.borderBottomWidth !== '0px'
        || style.borderLeftWidth !== '0px'
      )
    })
  })

  expect(hasCanvasBorders).toBe(false)
}

async function attachPageScreenshot(page: Page, testInfo: TestInfo, name: string) {
  const screenshotPath = testInfo.outputPath(`${name}.png`)

  await page.screenshot({
    path: screenshotPath,
    fullPage: true,
    animations: 'disabled',
    caret: 'hide',
  })

  await testInfo.attach(name, {
    path: screenshotPath,
    contentType: 'image/png',
  })
}

async function mockDashboardApis(page: Page, options: DashboardMockOptions = {}): Promise<DashboardRouteCalls> {
  const calls: DashboardRouteCalls = {
    kpiPeriods: [],
    flowPeriods: [],
    outcomePeriods: [],
    agingCalls: 0,
    mixCalls: 0,
  }

  await page.route('**/dashboard/kpis**', async (route) => {
    const periodDays = readPeriod(route.request().url(), 90)
    const normalized = normalizePeriod(periodDays, 90)
    calls.kpiPeriods.push(normalized)

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(PERIOD_KPIS[normalized]),
    })
  })

  await page.route('**/dashboard/opportunities/flow**', async (route) => {
    const periodDays = readPeriod(route.request().url(), 180)
    const normalized = normalizePeriod(periodDays, 180)
    calls.flowPeriods.push(normalized)

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(buildFlow(normalized)),
    })
  })

  await page.route('**/dashboard/opportunities/outcomes**', async (route) => {
    const periodDays = readPeriod(route.request().url(), 180)
    const normalized = normalizePeriod(periodDays, 180)
    calls.outcomePeriods.push(normalized)

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(buildOutcomes(normalized)),
    })
  })

  await page.route('**/dashboard/opportunities/aging**', async (route) => {
    calls.agingCalls += 1
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(buildAging()),
    })
  })

  await page.route('**/dashboard/opportunities/hierarchy**', async (route) => {
    calls.mixCalls += 1

    if (options.failMix) {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'internal_error', title: 'Server Error' }),
      })
      return
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(buildMix()),
    })
  })

  await page.route('**/dashboard/nudges', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        nudges: [
          {
            nudgeType: 'OverdueTask',
            title: 'Missing broker documentation',
            description: 'Submission S-2048 is blocked pending broker response.',
            linkedEntityType: 'Task',
            linkedEntityId: 'task-2048',
            linkedEntityName: 'Task 2048',
            urgencyValue: 91,
            ctaLabel: 'Review Task',
          },
          {
            nudgeType: 'StaleSubmission',
            title: 'Submission idle for 8 days',
            description: 'Submission S-1017 has not moved stages.',
            linkedEntityType: 'Submission',
            linkedEntityId: 'sub-1017',
            linkedEntityName: 'Submission S-1017',
            urgencyValue: 72,
            ctaLabel: 'Open Submission',
          },
        ],
      }),
    })
  })

  await page.route('**/my/tasks**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        tasks: [
          {
            id: 'task-1',
            title: 'Request updated application',
            status: 'InProgress',
            dueDate: '2026-03-22T12:00:00Z',
            linkedEntityType: 'Submission',
            linkedEntityId: 'sub-2048',
            linkedEntityName: 'Submission S-2048',
            isOverdue: false,
          },
          {
            id: 'task-2',
            title: 'Call broker for quote clarification',
            status: 'Open',
            dueDate: '2026-03-08T12:00:00Z',
            linkedEntityType: 'Broker',
            linkedEntityId: 'broker-1',
            linkedEntityName: 'Blue Horizon Brokerage',
            isOverdue: true,
          },
        ],
        totalCount: 2,
      }),
    })
  })

  await page.route('**/timeline/events**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        data: [
          {
            id: 'evt-1',
            eventType: 'SubmissionMoved',
            eventDescription: 'Submission moved to In Review.',
            entityType: 'Submission',
            entityId: 'sub-2048',
            entityName: 'Submission S-2048',
            actorDisplayName: 'Riley Chen',
            occurredAt: '2026-03-12T15:00:00Z',
          },
        ],
        page: 1,
        pageSize: 12,
        totalCount: 1,
        totalPages: 1,
      }),
    })
  })

  return calls
}

function readPeriod(url: string, fallback: number) {
  const parsed = new URL(url)
  const raw = parsed.searchParams.get('periodDays')
  const value = raw ? Number(raw) : fallback

  if (!Number.isFinite(value) || value < 1) {
    return fallback
  }

  return Math.trunc(value)
}

function normalizePeriod(periodDays: number, fallback: number) {
  const allowed = [30, 90, 180, 365]
  if (allowed.includes(periodDays)) return periodDays
  if (periodDays < 30) return 30
  if (periodDays > 365) return 365

  let closest = allowed[0]
  let smallestDistance = Math.abs(periodDays - closest)

  for (const candidate of allowed.slice(1)) {
    const distance = Math.abs(periodDays - candidate)
    if (distance < smallestDistance) {
      smallestDistance = distance
      closest = candidate
    }
  }

  return closest || fallback
}

function buildFlow(periodDays: number) {
  const counts = PERIOD_FLOW_COUNTS[periodDays]

  return {
    entityType: 'submission',
    periodDays,
    windowStartUtc: '2025-09-01T00:00:00Z',
    windowEndUtc: '2026-03-13T00:00:00Z',
    nodes: [
      {
        status: 'Received',
        label: 'Received',
        isTerminal: false,
        displayOrder: 1,
        colorGroup: 'intake',
        currentCount: counts.received,
        inflowCount: 0,
        outflowCount: Math.max(1, counts.received - 1),
        avgDwellDays: 2.1,
        emphasis: 'normal',
      },
      {
        status: 'Triaging',
        label: 'Triaging',
        isTerminal: false,
        displayOrder: 2,
        colorGroup: 'triage',
        currentCount: counts.triaging,
        inflowCount: counts.received,
        outflowCount: Math.max(1, counts.triaging - 1),
        avgDwellDays: 6.3,
        emphasis: 'bottleneck',
      },
      {
        status: 'InReview',
        label: 'In Review',
        isTerminal: false,
        displayOrder: 3,
        colorGroup: 'review',
        currentCount: counts.review,
        inflowCount: counts.triaging,
        outflowCount: Math.max(1, counts.review - 1),
        avgDwellDays: 10.4,
        emphasis: 'blocked',
      },
      {
        status: 'Quoted',
        label: 'Quoted',
        isTerminal: false,
        displayOrder: 4,
        colorGroup: 'decision',
        currentCount: counts.quoted,
        inflowCount: counts.review,
        outflowCount: Math.max(1, counts.quoted - 1),
        avgDwellDays: 1.8,
        emphasis: 'active',
      },
      {
        status: 'Bound',
        label: 'Bound',
        isTerminal: true,
        displayOrder: 5,
        colorGroup: 'decision',
        currentCount: PERIOD_OUTCOME_COUNTS[periodDays].bound,
        inflowCount: counts.quoted,
        outflowCount: 0,
        avgDwellDays: null,
        emphasis: null,
      },
    ],
    links: [
      { sourceStatus: 'Received', targetStatus: 'Triaging', count: counts.received },
      { sourceStatus: 'Triaging', targetStatus: 'InReview', count: counts.triaging },
      { sourceStatus: 'InReview', targetStatus: 'Quoted', count: counts.review },
      { sourceStatus: 'Quoted', targetStatus: 'Bound', count: counts.quoted },
    ],
  }
}

function buildOutcomes(periodDays: number) {
  const counts = PERIOD_OUTCOME_COUNTS[periodDays]
  const totalExits = counts.bound + counts.declined + counts.expired

  return {
    periodDays,
    totalExits,
    outcomes: [
      {
        key: 'bound',
        label: 'Bound',
        branchStyle: 'solid',
        count: counts.bound,
        percentOfTotal: Number(((counts.bound / totalExits) * 100).toFixed(1)),
        averageDaysToExit: 6.4,
      },
      {
        key: 'declined',
        label: 'Declined',
        branchStyle: 'red_dashed',
        count: counts.declined,
        percentOfTotal: Number(((counts.declined / totalExits) * 100).toFixed(1)),
        averageDaysToExit: 4.2,
      },
      {
        key: 'expired',
        label: 'Expired',
        branchStyle: 'gray_dotted',
        count: counts.expired,
        percentOfTotal: Number(((counts.expired / totalExits) * 100).toFixed(1)),
        averageDaysToExit: 8.1,
      },
    ],
  }
}

function buildAging() {
  return {
    entityType: 'submission',
    periodDays: 180,
    statuses: [
      {
        status: 'Triaging',
        label: 'Triaging',
        colorGroup: 'triage',
        displayOrder: 1,
        buckets: [
          { key: '0-2', label: '0-2 days', count: 2 },
          { key: '3-5', label: '3-5 days', count: 5 },
          { key: '6-10', label: '6-10 days', count: 4 },
          { key: '11-20', label: '11-20 days', count: 2 },
          { key: '21+', label: '21+ days', count: 1 },
        ],
        total: 14,
      },
      {
        status: 'InReview',
        label: 'In Review',
        colorGroup: 'review',
        displayOrder: 2,
        buckets: [
          { key: '0-2', label: '0-2 days', count: 1 },
          { key: '3-5', label: '3-5 days', count: 2 },
          { key: '6-10', label: '6-10 days', count: 4 },
          { key: '11-20', label: '11-20 days', count: 3 },
          { key: '21+', label: '21+ days', count: 1 },
        ],
        total: 11,
      },
    ],
  }
}

function buildMix() {
  return {
    periodDays: 180,
    root: {
      id: 'root',
      label: 'All Opportunities',
      count: 30,
      children: [
        {
          id: 'submission',
          label: 'Submissions',
          count: 21,
          levelType: 'entityType',
          children: [
            {
              id: 'submission:triage',
              label: 'Triage',
              count: 12,
              levelType: 'colorGroup',
              colorGroup: 'triage',
              children: [
                {
                  id: 'submission:triage:triaging',
                  label: 'Triaging',
                  count: 12,
                  levelType: 'status',
                  colorGroup: 'triage',
                },
              ],
            },
            {
              id: 'submission:review',
              label: 'Review',
              count: 9,
              levelType: 'colorGroup',
              colorGroup: 'review',
              children: [
                {
                  id: 'submission:review:inreview',
                  label: 'In Review',
                  count: 9,
                  levelType: 'status',
                  colorGroup: 'review',
                },
              ],
            },
          ],
        },
        {
          id: 'renewal',
          label: 'Renewals',
          count: 9,
          levelType: 'entityType',
          children: [
            {
              id: 'renewal:waiting',
              label: 'Waiting',
              count: 9,
              levelType: 'colorGroup',
              colorGroup: 'waiting',
              children: [
                {
                  id: 'renewal:waiting:outreach',
                  label: 'Outreach',
                  count: 9,
                  levelType: 'status',
                  colorGroup: 'waiting',
                },
              ],
            },
          ],
        },
      ],
    },
  }
}
