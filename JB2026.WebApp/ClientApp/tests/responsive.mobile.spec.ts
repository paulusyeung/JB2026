import { expect, test, type Page } from '@playwright/test'

type ThemeMode = 'light' | 'dark'

async function injectFakeSession(page: Page, mode: ThemeMode = 'light') {
  await page.addInitScript((themeMode: ThemeMode) => {
    localStorage.setItem('jb2026.accessToken', 'mobile-smoke-fake-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Mobile Smoke', role: 'Admin', email: 'mobile@test.local' }),
    )
    localStorage.setItem(
      'jb2026.theme.v2',
      JSON.stringify({ mode: themeMode, scheme: themeMode === 'dark' ? 'forest' : 'nature' }),
    )
  })
}

async function mockMobileApiRoutes(page: Page) {
  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/user-profiles/me', (route) =>
    route.fulfill({
      json: {
        userId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        username: 'admin',
        displayName: 'Administrator',
        role: 'Admin',
      },
    }),
  )

  await page.route('**/api/v2/stock/products**', (route) =>
    route.fulfill({
      json: [
        {
          productId: '11111111-1111-1111-1111-111111111111',
          stockNumber: 'STK-001',
          productCode: 'PAPER-A4',
          productName: 'A4 Art Paper 128gsm',
          balance: 320,
          sellingPrice: 12.5,
          cogs: 8.1,
          attachmentCount: 1,
          createdOn: '2026-03-31T00:00:00Z',
          createdBy: 'smoke',
          modifiedOn: '2026-04-01T00:00:00Z',
          modifiedBy: 'smoke',
        },
      ],
    }),
  )

  await page.route('**/api/v2/job-orders**', (route) =>
    route.fulfill({
      json: [
        {
          orderId: '11111111-1111-1111-1111-111111111111',
          orderNumber: 'JB260331',
          jobNumber: '01',
          customerName: 'Acme Corp',
          customerRef: 'REF-100',
          orderTitle: 'Modern Job Order',
          orderedBy: 'smoke',
          orderedOn: '2026-03-31T00:00:00Z',
          requiredOn: '2026-04-05T00:00:00Z',
          completedOn: null,
          qty: 250,
          paymentTerms: 'Net 30',
          remarks: 'Test order',
          status: 0,
          createdBy: 'smoke',
          createdOn: '2026-03-31T00:00:00Z',
          modifiedBy: 'smoke',
          modifiedOn: '2026-03-31T00:00:00Z',
          invoiceAmount: 123.45,
          invoiceRef: 'INV-100',
          productStyle: 'Brochure',
          attachmentProductCount: 1,
          attachmentCustomerCount: 0,
        },
      ],
    }),
  )

  await page.route('**/api/v2/quotations**', (route) =>
    route.fulfill({ json: { rows: [], rowCount: 0, keyword: '' } }),
  )

  await page.route('**/api/v2/reports/run', (route) =>
    route.fulfill({
      json: {
        reportName: 'Exceptional_Report',
        generatedAtUtc: '2026-03-30T00:00:00Z',
        totalRows: 1,
        totalCostA: 123.45,
        rows: [
          {
            headerId: '11111111-1111-1111-1111-111111111111',
            machineType: '1',
            quoteNumber: 1001,
            quoteNumberIndex: 1,
            quoteNumberIndexPair: '1001-1',
            quotedOn: '2026-03-30T00:00:00Z',
            quotedBy: 'tester',
            approvedOn: null,
            approvedBy: null,
            printTitle: 'Exceptional report sample',
            customerName: 'Acme',
            printsSize: 'A4',
            printsColor: '4C',
            printsQty: 100,
            materialName: 'Art Paper',
            materialCost: 20,
            totalCostA: 123.45,
            unitCostA: 1.23,
            status: 1,
          },
        ],
      },
    }),
  )

  await page.route('**/api/v2/settings', async (route) => {
    const request = route.request()
    if (request.method() === 'PUT') {
      await route.fulfill({
        json: {
          companyName: 'JB2026 Printing',
          timeZone: 'Asia/Kuala_Lumpur',
          currencyCode: 'MYR',
          enableLegacyFallback: true,
        },
      })
      return
    }

    await route.fulfill({
      json: {
        companyName: 'JB2026 Printing',
        timeZone: 'Asia/Kuala_Lumpur',
        currencyCode: 'MYR',
        enableLegacyFallback: true,
      },
    })
  })

  await page.route('**/api/v2/public/content', (route) =>
    route.fulfill({
      json: [
        {
          slug: 'company-profile',
          title: 'Company Profile',
          summary: 'Overview of JB2026 printing capabilities and service scope.',
          urlPath: '/public/company-profile',
        },
      ],
    }),
  )

  await page.route('**/api/v2/help/articles', (route) =>
    route.fulfill({
      json: [
        {
          articleId: 'getting-started',
          title: 'Getting Started',
          category: 'Onboarding',
          content: 'Learn how to navigate the JB2026 workspace and key modules.',
        },
      ],
    }),
  )

  await page.route('**/api/v2/sml/invoice-stats**', (route) =>
    route.fulfill({
      json: {
        generatedAtUtc: '2026-04-06T00:00:00Z',
        rowCount: 2,
        rows: [
          {
            customerName: 'SML DH',
            invoiceNumber: '66200',
            invoiceDate: '2015-01-15',
            invoiceAmount: 17227.52,
            createdOn: '2015-01-15T10:00:00Z',
            createdBy: 'alice',
            purchaseOrder: '5910444941',
            productCode: '8MMACPY01T#002',
            qty: 4944,
            unit: 'pcs',
            price: 0.16,
            amount: 791.04,
            year: 2015,
            month: 1,
          },
          {
            customerName: 'SML DH',
            invoiceNumber: 'DH1',
            invoiceDate: '2016-02-02',
            invoiceAmount: 3406.48,
            createdOn: '2016-02-02T10:00:00Z',
            createdBy: 'bob',
            purchaseOrder: '8110522367',
            productCode: 'THEUAHY002#001',
            qty: 4400,
            unit: 'pcs',
            price: 0.7742,
            amount: 3406.48,
            year: 2016,
            month: 2,
          },
        ],
      },
    }),
  )
}

async function expectNoHorizontalOverflow(page: Page) {
  await expect
    .poll(async () =>
      page.evaluate(() => ({
        documentFits: document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1,
        bodyFits: document.body.scrollWidth <= document.body.clientWidth + 1,
      })),
    )
    .toEqual({ documentFits: true, bodyFits: true })
}

test.describe('mobile responsive flows', () => {
  test.beforeEach(async ({ page }) => {
    await injectFakeSession(page)
    await mockMobileApiRoutes(page)
  })

  test('shell exposes a mobile navigation drawer toggle', async ({ page }) => {
    await page.goto('/app/stock')

    const navToggle = page.getByRole('button', { name: 'Open navigation' })
    await expect(navToggle).toBeVisible()
    await expect(page.getByRole('button', { name: 'More actions' })).toBeVisible()
    await navToggle.click()

    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible()
    await expect(page.getByText('Core Modules')).toBeVisible()
  })

  test('stock view renders mobile cards on phone viewport', async ({ page }) => {
    await page.goto('/app/stock')

    await expect(page.getByRole('heading', { name: 'Stock Product List' })).toBeVisible()
    await expect(page.locator('.stock-mobile-card')).toHaveCount(1)
    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()
    await expect(page.getByRole('button', { name: 'More' })).toBeVisible()
  })

  test('job list route remains usable on a phone viewport', async ({ page }) => {
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Modern Job Order')).toBeVisible()
    await expect(page.getByText('Acme Corp')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Search' })).toBeVisible()
  })

  test('order list route remains usable on a phone viewport', async ({ page }) => {
    await page.goto('/app/job-order/order-list')

    await expect(page.getByText('Modern Job Order')).toBeVisible()
    await expect(page.getByText('Acme Corp')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Search' })).toBeVisible()
  })

  test('tier 2 views remain readable in dark mode on mobile', async ({ page }) => {
    await injectFakeSession(page, 'dark')
    await mockMobileApiRoutes(page)

    const checks: Array<{ route: string; heading: string; hint: string }> = [
      { route: '/app/quotations', heading: 'Quotation register', hint: 'Search quotations' },
      { route: '/app/reports', heading: 'Reports runner', hint: 'Exceptional report sample' },
      { route: '/app/settings', heading: 'Settings', hint: 'Save settings' },
      { route: '/app/help', heading: 'Help center', hint: 'Getting Started' },
      { route: '/app/public', heading: 'Public content', hint: 'Company Profile' },
      { route: '/app/dashboard', heading: 'Dashboard', hint: 'Enabled slices' },
    ]

    for (const check of checks) {
      await page.goto(check.route)
      await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
      await expect(page.getByRole('heading', { name: check.heading })).toBeVisible()
      await expect(page.getByText(check.hint)).toBeVisible()
      await expectNoHorizontalOverflow(page)
    }
  })

  test('pivot invoice stats remains visible after theme switch on mobile', async ({ page }) => {
    await injectFakeSession(page, 'dark')
    await mockMobileApiRoutes(page)

    await page.goto('/app/job-order/sml/invoice-stats')
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
    await expect(page.getByRole('heading', { name: 'Invoice stats' })).toBeVisible()
    await expect(page.locator('web-pivot-table')).toBeVisible()

    await page.getByRole('button', { name: 'Light' }).click()

    await expect(page.locator('html')).toHaveAttribute('data-theme', 'light')
    await expect(page.locator('web-pivot-table')).toBeVisible()
    await expectNoHorizontalOverflow(page)
  })
})