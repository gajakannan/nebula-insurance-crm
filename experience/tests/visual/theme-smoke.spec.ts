import { expect, test, type Page, type TestInfo } from '@playwright/test'

type Theme = 'dark' | 'light'

const THEMES: Theme[] = ['dark', 'light']

const PAGES = [
  {
    slug: 'dashboard',
    open: async (page: Page, theme: Theme) => {
      await openAppPage(page, '/', theme)
    },
    ready: (page: Page) => page.getByText('Your opportunities at a glance'),
  },
  {
    slug: 'brokers',
    open: async (page: Page, theme: Theme) => {
      await openAppPage(page, '/', theme)
      await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Brokers' }).click()
      await page.waitForLoadState('networkidle')
    },
    ready: (page: Page) => page.getByRole('heading', { name: 'Broker Directory' }),
  },
  {
    slug: 'brokers-new',
    open: async (page: Page, theme: Theme) => {
      await openAppPage(page, '/', theme)
      await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Brokers' }).click()
      await page.getByRole('link', { name: 'New Broker' }).click()
      await page.waitForLoadState('networkidle')
    },
    ready: (page: Page) => page.getByLabel('Legal Name'),
  },
] as const

test.beforeEach(async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await mockNebulaApis(page)
})

for (const theme of THEMES) {
  test.describe(`${theme} theme`, () => {
    for (const pageCase of PAGES) {
      test(`${pageCase.slug} renders and captures screenshot`, async ({ page }, testInfo) => {
        await pageCase.open(page, theme)
        await expect(pageCase.ready(page)).toBeVisible()
        await attachPageScreenshot(page, testInfo, pageCase.slug, theme)
      })
    }

    test('dashboard KPI card text remains readable', async ({ page }) => {
      await openAppPage(page, '/', theme)
      await expect(page.getByText('Your opportunities at a glance')).toBeVisible()

      const kpiBand = page.getByRole('region', { name: 'KPI band' })
      await expect(kpiBand.getByText('Active Brokers')).toBeVisible()

      const metrics = await page.evaluate(() => {
        const label = Array.from(document.querySelectorAll('p')).find(
          (el) => el.textContent?.trim() === 'Active Brokers',
        ) as HTMLElement | undefined

        if (!label) {
          throw new Error('KPI label "Active Brokers" not found')
        }

        const value = label.parentElement?.querySelector('p.mt-1') as HTMLElement | null
        const canvasBand = label.closest('.canvas-section') as HTMLElement | null

        if (!value) {
          throw new Error('KPI value not found')
        }

        return {
          labelColor: getComputedStyle(label).color,
          valueColor: getComputedStyle(value).color,
          bandBg: canvasBand ? getComputedStyle(canvasBand).backgroundColor : 'rgba(0, 0, 0, 0)',
          bodyBg: getComputedStyle(document.body).backgroundColor,
        }
      })

      const effectiveSurface = compositeOver(parseCssColor(metrics.bandBg), parseCssColor(metrics.bodyBg))
      const labelContrast = contrastRatio(parseCssColor(metrics.labelColor), effectiveSurface)
      const valueContrast = contrastRatio(parseCssColor(metrics.valueColor), effectiveSurface)

      // Label text is intentionally de-emphasized; keep it above a minimum contrast floor.
      expect(labelContrast).toBeGreaterThan(3)
      expect(valueContrast).toBeGreaterThan(4.5)
    })
  })
}

async function openAppPage(page: Page, path: string, theme: Theme) {
  await page.addInitScript((selectedTheme) => {
    localStorage.setItem('nebula-theme', selectedTheme)
  }, theme)

  await page.goto(path)
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

async function attachPageScreenshot(page: Page, testInfo: TestInfo, slug: string, theme: Theme) {
  const screenshotPath = testInfo.outputPath(`${slug}-${theme}.png`)

  await page.screenshot({
    path: screenshotPath,
    fullPage: true,
    animations: 'disabled',
    caret: 'hide',
  })

  await testInfo.attach(`${slug}-${theme}`, {
    path: screenshotPath,
    contentType: 'image/png',
  })
}

async function mockNebulaApis(page: Page) {
  await page.route(
    '**/realms/nebula/protocol/openid-connect/token',
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: 'test-token',
          expires_in: 3600,
          token_type: 'Bearer',
        }),
      })
    },
  )

  await page.route('**/dashboard/kpis**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        activeBrokers: 128,
        openSubmissions: 42,
        renewalRate: 0.83,
        avgTurnaroundDays: 4.6,
      }),
    })
  })

  await page.route('**/dashboard/nudges**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        nudges: [
          {
            nudgeType: 'OverdueTask',
            title: 'Follow up on missing loss runs',
            description: 'Broker A12 has an overdue task blocking submission review.',
            linkedEntityType: 'Task',
            linkedEntityId: 'task-1',
            linkedEntityName: 'Follow-up task',
            urgencyValue: 95,
            ctaLabel: 'Open Task',
          },
          {
            nudgeType: 'StaleSubmission',
            title: 'Submission idle for 9 days',
            description: 'Submission S-2048 has no recent activity.',
            linkedEntityType: 'Submission',
            linkedEntityId: 'sub-2048',
            linkedEntityName: 'Submission S-2048',
            urgencyValue: 72,
            ctaLabel: 'Open Submission',
          },
        ],
      }),
    })
  })

  await page.route('**/dashboard/opportunities/flow**', async (route) => {
    const url = new URL(route.request().url())
    const entityType = url.searchParams.get('entityType')

    const body = entityType === 'renewal'
      ? {
          entityType: 'renewal',
          periodDays: 180,
          windowStartUtc: '2025-09-01T00:00:00Z',
          windowEndUtc: '2026-02-28T00:00:00Z',
          nodes: [
            { status: 'Identified', label: 'Identified', isTerminal: false, displayOrder: 1, colorGroup: 'intake', currentCount: 6, inflowCount: 0, outflowCount: 4 },
            { status: 'Outreach', label: 'Outreach', isTerminal: false, displayOrder: 2, colorGroup: 'waiting', currentCount: 4, inflowCount: 4, outflowCount: 3 },
            { status: 'InReview', label: 'In Review', isTerminal: false, displayOrder: 3, colorGroup: 'review', currentCount: 2, inflowCount: 3, outflowCount: 2 },
            { status: 'Quoted', label: 'Quoted', isTerminal: false, displayOrder: 4, colorGroup: 'decision', currentCount: 3, inflowCount: 2, outflowCount: 2 },
            { status: 'Completed', label: 'Completed', isTerminal: true, displayOrder: 5, colorGroup: 'won', currentCount: 5, inflowCount: 2, outflowCount: 0 },
            { status: 'Lost', label: 'Lost', isTerminal: true, displayOrder: 6, colorGroup: 'lost', currentCount: 2, inflowCount: 1, outflowCount: 0 },
          ],
          links: [
            { sourceStatus: 'Identified', targetStatus: 'Outreach', count: 4 },
            { sourceStatus: 'Outreach', targetStatus: 'InReview', count: 3 },
            { sourceStatus: 'InReview', targetStatus: 'Quoted', count: 2 },
            { sourceStatus: 'Quoted', targetStatus: 'Completed', count: 2 },
            { sourceStatus: 'InReview', targetStatus: 'Lost', count: 1 },
          ],
        }
      : {
          entityType: 'submission',
          periodDays: 180,
          windowStartUtc: '2025-09-01T00:00:00Z',
          windowEndUtc: '2026-02-28T00:00:00Z',
          nodes: [
            { status: 'Received', label: 'Received', isTerminal: false, displayOrder: 1, colorGroup: 'intake', currentCount: 9, inflowCount: 0, outflowCount: 18 },
            { status: 'Triaging', label: 'Triaging', isTerminal: false, displayOrder: 2, colorGroup: 'triage', currentCount: 7, inflowCount: 14, outflowCount: 14 },
            { status: 'WaitingOnBroker', label: 'Waiting on Broker', isTerminal: false, displayOrder: 3, colorGroup: 'waiting', currentCount: 6, inflowCount: 7, outflowCount: 6 },
            { status: 'WaitingOnDocuments', label: 'Waiting on Documents', isTerminal: false, displayOrder: 4, colorGroup: 'waiting', currentCount: 5, inflowCount: 5, outflowCount: 4 },
            { status: 'ReadyForUWReview', label: 'Ready for UW Review', isTerminal: false, displayOrder: 5, colorGroup: 'review', currentCount: 4, inflowCount: 7, outflowCount: 5 },
            { status: 'InReview', label: 'In Review', isTerminal: false, displayOrder: 6, colorGroup: 'review', currentCount: 4, inflowCount: 7, outflowCount: 8 },
            { status: 'QuotePreparation', label: 'Quote Preparation', isTerminal: false, displayOrder: 7, colorGroup: 'decision', currentCount: 3, inflowCount: 5, outflowCount: 5 },
            { status: 'Quoted', label: 'Quoted', isTerminal: false, displayOrder: 8, colorGroup: 'decision', currentCount: 3, inflowCount: 7, outflowCount: 8 },
            { status: 'RequoteRequested', label: 'Requote Requested', isTerminal: false, displayOrder: 9, colorGroup: 'decision', currentCount: 2, inflowCount: 3, outflowCount: 3 },
            { status: 'BindRequested', label: 'Bind Requested', isTerminal: false, displayOrder: 10, colorGroup: 'decision', currentCount: 2, inflowCount: 4, outflowCount: 4 },
            { status: 'Binding', label: 'Binding', isTerminal: false, displayOrder: 11, colorGroup: 'decision', currentCount: 1, inflowCount: 2, outflowCount: 2 },
            { status: 'Bound', label: 'Bound', isTerminal: true, displayOrder: 12, colorGroup: 'decision', currentCount: 12, inflowCount: 6, outflowCount: 0 },
            { status: 'Declined', label: 'Declined', isTerminal: true, displayOrder: 13, colorGroup: 'decision', currentCount: 8, inflowCount: 5, outflowCount: 0 },
            { status: 'Withdrawn', label: 'Withdrawn', isTerminal: true, displayOrder: 14, colorGroup: 'decision', currentCount: 6, inflowCount: 4, outflowCount: 0 },
            { status: 'NotQuoted', label: 'Not Quoted', isTerminal: true, displayOrder: 15, colorGroup: 'decision', currentCount: 4, inflowCount: 3, outflowCount: 0 },
            { status: 'Lost', label: 'Lost', isTerminal: true, displayOrder: 16, colorGroup: 'decision', currentCount: 5, inflowCount: 4, outflowCount: 0 },
          ],
          links: [
            { sourceStatus: 'Received', targetStatus: 'Triaging', count: 14 },
            { sourceStatus: 'Received', targetStatus: 'Withdrawn', count: 2 },
            { sourceStatus: 'Received', targetStatus: 'NotQuoted', count: 2 },
            { sourceStatus: 'Triaging', targetStatus: 'WaitingOnBroker', count: 5 },
            { sourceStatus: 'Triaging', targetStatus: 'WaitingOnDocuments', count: 4 },
            { sourceStatus: 'Triaging', targetStatus: 'ReadyForUWReview', count: 5 },
            { sourceStatus: 'WaitingOnBroker', targetStatus: 'WaitingOnDocuments', count: 3 },
            { sourceStatus: 'WaitingOnBroker', targetStatus: 'ReadyForUWReview', count: 3 },
            { sourceStatus: 'WaitingOnDocuments', targetStatus: 'ReadyForUWReview', count: 4 },
            { sourceStatus: 'ReadyForUWReview', targetStatus: 'InReview', count: 5 },
            { sourceStatus: 'InReview', targetStatus: 'QuotePreparation', count: 4 },
            { sourceStatus: 'InReview', targetStatus: 'Declined', count: 3 },
            { sourceStatus: 'InReview', targetStatus: 'Quoted', count: 1 },
            { sourceStatus: 'QuotePreparation', targetStatus: 'Quoted', count: 5 },
            { sourceStatus: 'Quoted', targetStatus: 'RequoteRequested', count: 3 },
            { sourceStatus: 'Quoted', targetStatus: 'BindRequested', count: 4 },
            { sourceStatus: 'Quoted', targetStatus: 'Lost', count: 1 },
            { sourceStatus: 'RequoteRequested', targetStatus: 'QuotePreparation', count: 2 },
            { sourceStatus: 'RequoteRequested', targetStatus: 'Quoted', count: 1 },
            { sourceStatus: 'BindRequested', targetStatus: 'Binding', count: 2 },
            { sourceStatus: 'BindRequested', targetStatus: 'Bound', count: 2 },
            { sourceStatus: 'Binding', targetStatus: 'Bound', count: 2 },
            { sourceStatus: 'WaitingOnBroker', targetStatus: 'Withdrawn', count: 2 },
            { sourceStatus: 'QuotePreparation', targetStatus: 'NotQuoted', count: 1 },
            { sourceStatus: 'ReadyForUWReview', targetStatus: 'Declined', count: 2 },
          ],
        }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(body),
    })
  })

  await page.route('**/dashboard/opportunities/outcomes**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        periodDays: 180,
        totalExits: 35,
        outcomes: [
          { key: 'bound', label: 'Bound', branchStyle: 'solid', count: 14, percentOfTotal: 40, averageDaysToExit: 12.4 },
          { key: 'declined', label: 'Declined', branchStyle: 'red_dashed', count: 8, percentOfTotal: 22.9, averageDaysToExit: 8.2 },
          { key: 'withdrawn', label: 'Withdrawn', branchStyle: 'gray_dotted', count: 7, percentOfTotal: 20, averageDaysToExit: 10.1 },
          { key: 'notQuoted', label: 'Not Quoted', branchStyle: 'gray_dotted', count: 6, percentOfTotal: 17.1, averageDaysToExit: 6.7 },
        ],
      }),
    })
  })

  await page.route('**/dashboard/opportunities/aging**', async (route) => {
    const url = new URL(route.request().url())
    const entityType = url.searchParams.get('entityType') ?? 'submission'

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        entityType,
        periodDays: 180,
        statuses: [
          {
            status: 'Triaging',
            label: 'Triaging',
            colorGroup: 'triage',
            displayOrder: 2,
            total: 7,
            buckets: [
              { key: 'lt1', label: '<1d', count: 2 },
              { key: '1to3', label: '1-3d', count: 3 },
              { key: '3to7', label: '3-7d', count: 1 },
              { key: 'gt7', label: '7d+', count: 1 },
            ],
            sla: {
              warningDays: 2,
              targetDays: 4,
              totalCount: 7,
              onTimeCount: 4,
              approachingCount: 2,
              overdueCount: 1,
            },
          },
          {
            status: 'WaitingOnBroker',
            label: 'Waiting on Broker',
            colorGroup: 'waiting',
            displayOrder: 3,
            total: 6,
            buckets: [
              { key: 'lt1', label: '<1d', count: 1 },
              { key: '1to3', label: '1-3d', count: 2 },
              { key: '3to7', label: '3-7d', count: 2 },
              { key: 'gt7', label: '7d+', count: 1 },
            ],
          },
        ],
      }),
    })
  })

  await page.route(/\/dashboard\/opportunities\/(submission|renewal)\/[^/]+\/breakdown(?:\?.*)?$/, async (route) => {
    const url = new URL(route.request().url())
    const pathParts = url.pathname.split('/')
    const entityType = pathParts.at(-3) ?? 'submission'
    const status = decodeURIComponent(pathParts.at(-2) ?? 'Unknown')
    const groupBy = url.searchParams.get('groupBy') ?? 'lineOfBusiness'
    const periodDays = Number(url.searchParams.get('periodDays') ?? 180)

    const groupsByDimension = {
      lineOfBusiness: [
        { key: 'property', label: 'Property', count: 4 },
        { key: 'casualty', label: 'Casualty', count: 3 },
        { key: 'marine', label: 'Marine', count: 2 },
      ],
      broker: [
        { key: 'broker-1', label: 'Blue Horizon', count: 3 },
        { key: 'broker-2', label: 'Northline Risk', count: 2 },
        { key: 'broker-3', label: 'Harbor Specialty', count: 1 },
      ],
      brokerState: [
        { key: 'CA', label: 'CA', count: 3 },
        { key: 'TX', label: 'TX', count: 2 },
        { key: 'FL', label: 'FL', count: 1 },
      ],
      assignedUser: [
        { key: 'uw-1', label: 'A. Chen', count: 3 },
        { key: 'uw-2', label: 'R. Singh', count: 2 },
        { key: 'uw-3', label: 'M. Diaz', count: 1 },
      ],
      program: [
        { key: 'prog-1', label: 'Package', count: 3 },
        { key: 'prog-2', label: 'Construction', count: 2 },
        { key: 'prog-3', label: 'Transit', count: 1 },
      ],
    } as const

    const groups = groupsByDimension[groupBy as keyof typeof groupsByDimension] ?? groupsByDimension.lineOfBusiness

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        entityType,
        status,
        groupBy,
        periodDays,
        groups,
        total: groups.reduce((sum, group) => sum + group.count, 0),
      }),
    })
  })

  await page.route(/\/dashboard\/opportunities(?:\?.*)?$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        submissions: [
          { status: 'Received', count: 9, colorGroup: 'intake' },
          { status: 'Triaging', count: 7, colorGroup: 'triage' },
          { status: 'WaitingOnBroker', count: 4, colorGroup: 'waiting' },
          { status: 'InReview', count: 3, colorGroup: 'review' },
          { status: 'Quoted', count: 2, colorGroup: 'decision' },
        ],
        renewals: [
          { status: 'Identified', count: 6, colorGroup: 'intake' },
          { status: 'Outreach', count: 4, colorGroup: 'waiting' },
          { status: 'InReview', count: 2, colorGroup: 'review' },
          { status: 'Quoted', count: 3, colorGroup: 'decision' },
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
            dueDate: '2026-03-05T12:00:00Z',
            linkedEntityType: 'Submission',
            linkedEntityId: 'sub-2048',
            linkedEntityName: 'Submission S-2048',
            isOverdue: false,
          },
          {
            id: 'task-2',
            title: 'Call broker for quote clarification',
            status: 'Open',
            dueDate: '2026-03-01T12:00:00Z',
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
        data: [],
        page: 1,
        pageSize: 12,
        totalCount: 0,
        totalPages: 0,
      }),
    })
  })

  await page.route(/\/brokers(?:\?.*)?$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        data: [
          {
            id: 'broker-1',
            legalName: 'Blue Horizon Brokerage',
            licenseNumber: 'CA-120045',
            state: 'CA',
            status: 'Active',
            email: 'ops@bluehorizon.example',
            phone: '+1-415-555-0101',
            createdAt: '2026-01-01T12:00:00Z',
            updatedAt: '2026-02-20T12:00:00Z',
            rowVersion: 1,
          },
          {
            id: 'broker-2',
            legalName: 'Northline Risk Partners',
            licenseNumber: 'TX-882311',
            state: 'TX',
            status: 'Pending',
            email: 'team@northline.example',
            phone: '+1-512-555-0199',
            createdAt: '2026-01-15T12:00:00Z',
            updatedAt: '2026-02-21T12:00:00Z',
            rowVersion: 3,
          },
        ],
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
      }),
    })
  })
}

type Rgba = { r: number; g: number; b: number; a: number }

function parseCssColor(input: string): Rgba {
  const normalized = input.trim()
  const rgbaMatch = normalized.match(/^rgba?\(([^)]+)\)$/i)
  if (rgbaMatch) {
    const [r, g, b, a = '1'] = rgbaMatch[1].split(',').map((part) => part.trim())
    return {
      r: Number(r),
      g: Number(g),
      b: Number(b),
      a: Number(a),
    }
  }

  const srgbMatch = normalized.match(
    /^color\(srgb\s+([0-9.]+)\s+([0-9.]+)\s+([0-9.]+)(?:\s*\/\s*([0-9.]+))?\)$/i,
  )
  if (srgbMatch) {
    return {
      r: Math.round(Number(srgbMatch[1]) * 255),
      g: Math.round(Number(srgbMatch[2]) * 255),
      b: Math.round(Number(srgbMatch[3]) * 255),
      a: srgbMatch[4] == null ? 1 : Number(srgbMatch[4]),
    }
  }

  throw new Error(`Unsupported CSS color: ${input}`)
}

function compositeOver(fg: Rgba, bg: Rgba): Rgba {
  if (fg.a >= 1) return { ...fg, a: 1 }

  const a = fg.a + bg.a * (1 - fg.a)
  if (a === 0) return { r: 0, g: 0, b: 0, a: 0 }

  return {
    r: Math.round((fg.r * fg.a + bg.r * bg.a * (1 - fg.a)) / a),
    g: Math.round((fg.g * fg.a + bg.g * bg.a * (1 - fg.a)) / a),
    b: Math.round((fg.b * fg.a + bg.b * bg.a * (1 - fg.a)) / a),
    a,
  }
}

function contrastRatio(foreground: Rgba, background: Rgba) {
  const l1 = relativeLuminance(foreground)
  const l2 = relativeLuminance(background)
  const [lighter, darker] = l1 >= l2 ? [l1, l2] : [l2, l1]
  return (lighter + 0.05) / (darker + 0.05)
}

function relativeLuminance(color: Rgba) {
  const channels = [color.r, color.g, color.b].map((value) => {
    const srgb = value / 255
    return srgb <= 0.03928 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4
  })

  return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]
}
