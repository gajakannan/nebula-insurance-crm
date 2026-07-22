import { expect, test, type Page } from '@playwright/test'

type Theme = 'dark' | 'light'

const screenshotDirectory = process.env.F0026_SCREENSHOT_DIR

test.beforeEach(async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' })
  await mockBillingApis(page)
})

for (const theme of ['dark', 'light'] as const) {
  for (const viewport of [
    { name: 'desktop', width: 1440, height: 900 },
    { name: 'mobile', width: 390, height: 844 },
  ] as const) {
    test(`billing workspace renders in ${theme} at ${viewport.name} width`, async ({ page }) => {
      await page.setViewportSize(viewport)
      await openPage(page, '/billing', theme)
      await expect(page.getByText('Agency-Bill Workspace')).toBeVisible()
      await expect(page.getByText('INV-2026-1042')).toBeVisible()
      await capture(page, `billing-${theme}-${viewport.name}.png`)
    })
  }

  test(`reconciliation workspace renders in ${theme}`, async ({ page }) => {
    await openPage(page, '/billing/reconciliation', theme)
    await expect(page.getByText('Source-Filtered Backlog')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Invoice Reference Conflict' })).toBeVisible()
    await capture(page, `billing-reconciliation-${theme}-desktop.png`)
  })

  test(`invoice evidence detail renders in ${theme}`, async ({ page }) => {
    await openPage(page, '/billing/invoices/10000000-0000-0000-0000-000000000026', theme)
    await expect(page.getByText('Applications and Receipt Provenance')).toBeVisible()
    await expect(page.getByText('Permitted Audit History')).toBeVisible()
    await expect(page.getByText('Expected Commission Context')).toBeVisible()
    await capture(page, `billing-invoice-detail-${theme}-desktop.png`)
  })
}

async function openPage(page: Page, path: string, theme: Theme) {
  await page.addInitScript((selectedTheme) => {
    localStorage.setItem('nebula-theme', selectedTheme)
  }, theme)
  await page.goto(path, { waitUntil: 'domcontentloaded' })
  await page.addStyleTag({
    content: '*, *::before, *::after { animation: none !important; transition: none !important; caret-color: transparent !important; }',
  })
}

async function capture(page: Page, fileName: string) {
  if (!screenshotDirectory) throw new Error('F0026_SCREENSHOT_DIR is required')
  await page.screenshot({
    path: `${screenshotDirectory}/${fileName}`,
    fullPage: true,
    animations: 'disabled',
    caret: 'hide',
  })
}

async function mockBillingApis(page: Page) {
  await page.route('**/billing-invoices**', async (route) => {
    const invoice = {
      id: '10000000-0000-0000-0000-000000000026',
      invoiceNumber: 'INV-2026-1042',
      policyId: '20000000-0000-0000-0000-000000000026',
      policyVersionId: '30000000-0000-0000-0000-000000000026',
      accountId: '40000000-0000-0000-0000-000000000026',
      currency: 'USD',
      originalAmount: 12450,
      outstandingAmount: 12450,
      invoiceDate: '2026-07-19',
      dueDate: '2026-08-18',
      status: 'Outstanding',
      createdAt: '2026-07-19T14:00:00Z',
      createdByUserId: '70000000-0000-0000-0000-000000000026',
      rowVersion: '7',
    }
    const detailRequest = new URL(route.request().url()).pathname.endsWith(invoice.id)
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(detailRequest ? {
        invoice: { ...invoice, outstandingAmount: 0, status: 'Reconciled', rowVersion: '8' },
        applications: [{
          id: '80000000-0000-0000-0000-000000000026', billingInvoiceId: invoice.id,
          paymentReceiptId: '60000000-0000-0000-0000-000000000026', currency: 'USD', appliedAmount: 12450,
          invoiceOutstandingBefore: 12450, invoiceOutstandingAfter: 0, appliedAt: '2026-07-19T15:00:00Z',
          appliedByUserId: '70000000-0000-0000-0000-000000000026',
        }],
        receipts: [{
          id: '60000000-0000-0000-0000-000000000026', source: 'MockVendorCsv', externalReference: 'PAY-2026-1042',
          receivedDate: '2026-07-19', currency: 'USD', amount: 12450, invoiceReference: 'INV-2026-1042', memo: null,
          importBatchId: '90000000-0000-0000-0000-000000000026', importRowNumber: 1, applicationStatus: 'Applied', rowVersion: '4',
        }],
        exceptions: [],
        auditEvents: [{
          id: 'a0000000-0000-0000-0000-000000000026', entityType: 'BillingInvoice', entityId: invoice.id,
          eventType: 'ExactPaymentApplied', eventDescription: 'Exact payment receipt applied', entityName: null,
          actorDisplayName: 'Finance Reviewer', occurredAt: '2026-07-19T15:00:00Z',
        }],
      } : { data: [invoice], page: 1, pageSize: 30, totalCount: 1, totalPages: 1 }),
    })
  })

  await page.route('**/policies/*/billing-summary', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
      policyId: '20000000-0000-0000-0000-000000000026', currency: 'USD', invoiceCount: 1,
      outstandingInvoiceCount: 1, outstandingAmount: 12450, nextDueDate: '2026-08-18', asOf: '2026-07-19T15:00:00Z',
    }) })
  })

  await page.route('**/expected-commissions**', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
      data: [{ id: 'b0000000-0000-0000-0000-000000000026', producerDisplayName: 'Alex Producer', status: 'Calculated', exceptionState: 'None', adjustedExpectedCommission: 1245 }],
      page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
    }) })
  })

  await page.route('**/payment-receipts**', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 }) })
  })

  await page.route('**/reconciliation-backlog', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        openCount: 3,
        exactApplicationCount: 12,
        pendingCorrectionCount: 1,
        rejectedImportRowCount: 2,
        duplicateImportRowCount: 4,
        oldestOpenDays: 4,
        byType: [{ type: 'InvoiceReferenceConflict', count: 2 }, { type: 'AmountMismatch', count: 1 }],
      }),
    })
  })

  await page.route('**/reconciliation-exceptions**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        data: [{
          id: '50000000-0000-0000-0000-000000000026',
          type: 'InvoiceReferenceConflict',
          billingInvoiceId: '10000000-0000-0000-0000-000000000026',
          paymentReceiptId: '60000000-0000-0000-0000-000000000026',
          importBatchId: null,
          importRowOutcomeId: null,
          status: 'Open',
          openedAt: '2026-07-18T14:00:00Z',
          openedByUserId: 'finance-reviewer',
          resolvedAt: null,
          resolvedByUserId: null,
          resolutionCode: null,
          resolutionNote: null,
          pendingCorrection: {
            id: 'c0000000-0000-0000-0000-000000000026',
            reconciliationExceptionId: '50000000-0000-0000-0000-000000000026',
            billingInvoiceId: '10000000-0000-0000-0000-000000000026',
            beforeOutstandingAmount: 12450,
            correctionAmount: -450,
            proposedOutstandingAmount: 12000,
            reason: 'Verified source adjustment',
            evidenceNote: 'Manager evidence packet received.',
            status: 'Pending',
            requestedByUserId: '70000000-0000-0000-0000-000000000026',
            requestedAt: '2026-07-18T15:00:00Z',
            decisionByUserId: null,
            decisionAt: null,
            decisionNote: null,
            rowVersion: '9',
          },
          rowVersion: '3',
        }],
        page: 1,
        pageSize: 40,
        totalCount: 1,
        totalPages: 1,
      }),
    })
  })
}
