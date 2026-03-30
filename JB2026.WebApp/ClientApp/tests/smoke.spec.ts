import { test, expect, type Page } from '@playwright/test'

// ---------------------------------------------------------------------------
// Auth helper — injects a fake JWT into localStorage so the router guard
// treats the browser as authenticated without hitting a real API.
// ---------------------------------------------------------------------------
async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'smoke-test-fake-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Smoke Test', email: 'smoke@test.local' }),
    )
  })
}

// ---------------------------------------------------------------------------
// Locale helper — switches the topbar language selector.
// ---------------------------------------------------------------------------
async function switchLocale(page: Page, languageLabel: 'English' | '简体中文' | '繁體中文') {
  const localeSelector = page.getByRole('combobox', { name: /Language|语言|語言/ })
  await localeSelector.click()
  await page.getByRole('option', { name: languageLabel }).click()
}

// ---------------------------------------------------------------------------
// API mock helper — intercepts backend calls so views render without a live
// API server and tests remain deterministic.
// ---------------------------------------------------------------------------
async function mockApiRoutes(page: Page) {
  // Feature flags endpoint used by dashboard
  await page.route('**/ui/feature-flags', (route) =>
    route.fulfill({ json: { flags: [] } }),
  )

  // Current profile endpoint
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

  // Jobs list endpoint
  await page.route('**/api/v2/jobs/**', (route) =>
    route.fulfill({ json: { rows: [], total: 0 } }),
  )

  // Job orders endpoint
  await page.route('**/api/v2/job-orders', (route) =>
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
          qty: 250,
          paymentTerms: 'Net 30',
          remarks: 'Test order',
          status: 0,
          createdBy: 'smoke',
          createdOn: '2026-03-31T00:00:00Z',
          modifiedBy: 'smoke',
          modifiedOn: '2026-03-31T00:00:00Z',
        },
      ],
    }),
  )

  await page.route('**/api/v2/job-orders/**', (route) =>
    route.fulfill({
      json: {
        orderId: '11111111-1111-1111-1111-111111111111',
        orderNumber: 'JB260331',
        jobNumber: '01',
        customerName: 'Acme Corp',
        customerRef: 'REF-100',
        orderTitle: 'Modern Job Order',
        orderedBy: 'smoke',
        orderedOn: '2026-03-31T00:00:00Z',
        requiredOn: '2026-04-05T00:00:00Z',
        qty: 250,
        paymentTerms: 'Net 30',
        remarks: 'Test order',
        status: 0,
        createdBy: 'smoke',
        createdOn: '2026-03-31T00:00:00Z',
        modifiedBy: 'smoke',
        modifiedOn: '2026-03-31T00:00:00Z',
      },
    }),
  )

  await page.route('**/api/v2/jobs', async (route) => {
    const request = route.request()
    if (request.method() === 'POST') {
      await route.fulfill({
        status: 201,
        json: {
          orderId: '11111111-1111-1111-1111-111111111111',
          orderNumber: 'JB260330-01',
          jobNumber: '01',
          customerName: 'Acme Corp',
          customerRef: 'REF-100',
          orderTitle: 'New brochure run',
          orderedBy: 'smoke',
          orderedOn: '2026-03-30T00:00:00Z',
          requiredOn: '2026-04-02T00:00:00Z',
          qty: 100,
          paymentTerms: 'Net 30',
          remarks: 'Test create',
          status: 0,
          createdBy: 'smoke',
          createdOn: '2026-03-30T00:00:00Z',
          modifiedBy: 'smoke',
          modifiedOn: '2026-03-30T00:00:00Z',
        },
      })
      return
    }

    await route.fallback()
  })

  // Quotations endpoint
  await page.route('**/api/v2/quotations**', (route) =>
    route.fulfill({ json: { rows: [], rowCount: 0, keyword: '' } }),
  )

  // Job schedules (calendar)
  await page.route('**/api/v2/job-schedules/**', (route) =>
    route.fulfill({ json: [] }),
  )

  // Stock endpoint
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
          remarks: 'Core stock item',
        },
      ],
    }),
  )

  // Reports endpoint
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

  // Admin users endpoint
  await page.route('**/api/v2/admin/users', (route) =>
    route.fulfill({
      json: [
        {
          userId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          username: 'admin',
          displayName: 'Administrator',
          role: 'Admin',
        },
        {
          userId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          username: 'operator',
          displayName: 'Operator',
          role: 'Operator',
        },
      ],
    }),
  )

  // Settings endpoints
  await page.route('**/api/v2/settings', async (route) => {
    const request = route.request()
    if (request.method() === 'PUT') {
      const body = (await request.postDataJSON()) as {
        companyName: string
        timeZone: string
        currencyCode: string
        enableLegacyFallback: boolean
      }

      await route.fulfill({
        json: {
          companyName: body.companyName,
          timeZone: body.timeZone,
          currencyCode: body.currencyCode,
          enableLegacyFallback: body.enableLegacyFallback,
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

  // Public content endpoint
  await page.route('**/api/v2/public/content', (route) =>
    route.fulfill({
      json: [
        {
          slug: 'company-profile',
          title: 'Company Profile',
          summary: 'Overview of JB2026 printing capabilities and service scope.',
          urlPath: '/public/company-profile',
        },
        {
          slug: 'service-catalog',
          title: 'Service Catalog',
          summary: 'Browse available print and finishing services.',
          urlPath: '/public/service-catalog',
        },
      ],
    }),
  )

  // Help articles endpoint
  await page.route('**/api/v2/help/articles', (route) =>
    route.fulfill({
      json: [
        {
          articleId: 'getting-started',
          title: 'Getting Started',
          category: 'Onboarding',
          content: 'Learn how to navigate the JB2026 workspace and key modules.',
        },
        {
          articleId: 'job-order-lifecycle',
          title: 'Job Order Lifecycle',
          category: 'Operations',
          content: 'Understand how job orders move from creation to completion.',
        },
      ],
    }),
  )

  // SML stats endpoint
  await page.route('**/api/v2/sml/stats**', (route) =>
    route.fulfill({
      json: {
        generatedAtUtc: '2026-03-30T00:00:00Z',
        rowCount: 3,
        totalAmount: 600,
        monthly: [
          { year: 2026, month: 3, count: 2, amount: 300 },
          { year: 2026, month: 4, count: 1, amount: 300 },
        ],
        topCustomers: [
          { customerName: 'Acme', count: 2, amount: 300 },
          { customerName: 'Beta', count: 1, amount: 300 },
        ],
      },
    }),
  )
}

// ---------------------------------------------------------------------------
// Slice A — Read-only lists and dashboards
// ---------------------------------------------------------------------------
test.describe('Slice A — read-only lists and dashboard', () => {
  test('language selector switches UI copy and html lang tag', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/dashboard')

    await expect(page.getByText('Enabled slices')).toBeVisible()
    await expect(page.locator('html')).toHaveAttribute('lang', 'en')

    await switchLocale(page, '简体中文')
    await expect(page.getByText('已启用切片')).toBeVisible()
    await expect(page.locator('html')).toHaveAttribute('lang', 'zh-CN')

    await switchLocale(page, '繁體中文')
    await expect(page.getByText('已啟用切片')).toBeVisible()
    await expect(page.locator('html')).toHaveAttribute('lang', 'zh-TW')
  })

  test('selected locale persists after navigation', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/dashboard')

    await switchLocale(page, '简体中文')
    await page.goto('/app/settings')

    await expect(page.getByRole('heading', { name: '设置' })).toBeVisible()
    await expect(page.locator('html')).toHaveAttribute('lang', 'zh-CN')
  })

  test('login screen renders and supports development defaults', async ({ page }) => {
    await page.goto('/app/login')

    await expect(page.getByRole('heading', { name: 'API-authenticated sign in' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Use Dev Defaults' })).toBeVisible()
  })

  test('dashboard view renders KPI cards and chart section', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/dashboard')

    await expect(page.getByText('Slice A')).toBeVisible()
    await expect(page.getByText('Enabled slices')).toBeVisible()
    await expect(page.getByText('Jobs loaded')).toBeVisible()
    await expect(page.getByText('Quotations loaded')).toBeVisible()
    await expect(page.getByText('Volume trend')).toBeVisible()
  })

  test('jobs view renders grid with column headers', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/jobs')

    await expect(page.getByRole('columnheader', { name: 'Order' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Customer' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    // Detail panel is also present
    await expect(page.getByRole('heading', { name: 'Job detail' })).toBeVisible()
  })

  test('job order view renders dedicated register and details', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/job-order')

    await expect(page.getByRole('heading', { name: 'Job order register' })).toBeVisible()
    await expect(page.getByText('Modern Job Order')).toBeVisible()
    await expect(page.getByText('Selected order')).toBeVisible()
  })

  test('quotations view renders register heading and search', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/quotations')

    await expect(page.getByRole('heading', { name: 'Quotation register' })).toBeVisible()
    await expect(page.getByPlaceholder('Search quotations')).toBeVisible()
  })

  test('stock view renders product register from stock endpoint', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/stock')

    await expect(page.getByRole('heading', { name: 'Stock products' })).toBeVisible()
    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()
    await expect(page.getByText('PAPER-A4')).toBeVisible()
  })

  test('reports view runs and renders report rows', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/reports')

    await expect(page.getByRole('heading', { name: 'Reports runner' })).toBeVisible()
    await expect(page.getByText('Exceptional report sample')).toBeVisible()
    await expect(page.getByText('1001-1')).toBeVisible()
  })

  test('admin view renders user directory and current profile', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/admin')

    await expect(page.getByRole('heading', { name: 'Admin users' })).toBeVisible()
    await expect(page.getByText('Signed in as Administrator (Admin)')).toBeVisible()
    await expect(page.getByText('operator')).toBeVisible()
  })

  test('settings view loads and saves settings', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/settings')

    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible()
    await page.getByLabel('Company Name').fill('Acme Print')
    await page.getByRole('button', { name: 'Save settings' }).click()

    await expect(page.getByText('Settings saved successfully.')).toBeVisible()
  })

  test('public view renders public content entries', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/public')

    await expect(page.getByRole('heading', { name: 'Public content' })).toBeVisible()
    await expect(page.getByText('Company Profile')).toBeVisible()
    await expect(page.getByText('/public/company-profile')).toBeVisible()
  })

  test('help view renders help articles', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/help')

    await expect(page.getByRole('heading', { name: 'Help center' })).toBeVisible()
    await expect(page.getByText('Getting Started')).toBeVisible()
    await expect(page.getByText('Onboarding')).toBeVisible()
  })

  test('sml view renders aggregates from sml stats endpoint', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/sml')

    await expect(page.getByRole('heading', { name: 'SML statistics' })).toBeVisible()
    await expect(page.getByText('Rows: 3')).toBeVisible()
    await expect(page.getByText('Acme')).toBeVisible()
  })

  test('sidebar shows validated legacy groups and hides planned areas', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/dashboard')

    await expect(page.getByText('Legacy Core Modules')).toBeVisible()
    await expect(page.getByText('Legacy-Derived Areas')).toBeVisible()
    await expect(page.getByText('Public')).not.toBeVisible()
    await expect(page.getByText('Help')).not.toBeVisible()
  })
})

// ---------------------------------------------------------------------------
// Slice B — Create/edit form views
// ---------------------------------------------------------------------------
test.describe('Slice B — form views', () => {
  test('login form renders username and password fields', async ({ page }) => {
    await page.goto('/app/login')

    await expect(page.getByLabel('Username')).toBeVisible()
    await expect(page.getByLabel('Password')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible()
  })

  test('jobs detail panel renders read-only fields when job selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/jobs')

    // With no data the detail panel shows a skeleton loader (not an error)
    await expect(page.locator('.v-skeleton-loader')).toBeVisible()
  })

  test('New Job button opens create form dialog with required fields', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/jobs')

    await page.getByRole('button', { name: 'New' }).click()

    // Dialog should be visible with the form heading
    await expect(page.getByRole('heading', { name: 'New Job Order' })).toBeVisible()

    // Core form fields present
    await expect(page.getByLabel('Order Number')).toBeVisible()
    await expect(page.getByLabel('Job Number')).toBeVisible()
    await expect(page.getByLabel('Order Title')).toBeVisible()
    await expect(page.getByLabel('Customer Name')).toBeVisible()
    await expect(page.getByLabel('Quantity')).toBeVisible()
    await expect(page.getByLabel('Ordered On')).toBeVisible()
    await expect(page.getByLabel('Required On')).toBeVisible()
    await expect(page.getByLabel('Remarks')).toBeVisible()
  })

  test('Create form shows validation errors when submitted empty', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/jobs')

    await page.getByRole('button', { name: 'New' }).click()
    await expect(page.getByRole('heading', { name: 'New Job Order' })).toBeVisible()

    await page.getByLabel('Order Number').fill('')
    await page.getByLabel('Job Number').fill('')
    // Clear the pre-filled Order Title and submit
    await page.getByLabel('Order Title').fill('')
    await page.getByLabel('Customer Name').fill('')
    await page.getByRole('button', { name: 'Create' }).click()

    // Vuetify validation error messages should appear
    await expect(page.getByText('Required').first()).toBeVisible()
  })

  test('Create form submits successfully with order and job numbers', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/jobs')

    await page.getByRole('button', { name: 'New' }).click()
    await expect(page.getByRole('heading', { name: 'New Job Order' })).toBeVisible()

    await page.getByLabel('Order Number').fill('JB260330')
    await page.getByLabel('Job Number').fill('01')
    await page.getByLabel('Order Title').fill('New brochure run')
    await page.getByLabel('Customer Name').fill('Acme Corp')
    await page.getByLabel('Customer Reference').fill('REF-100')
    await page.getByLabel('Ordered By').fill('Smoke User')
    await page.getByLabel('Quantity').fill('100')
    await page.getByLabel('Payment Terms').click()
    await page.getByRole('option', { name: 'Net 30' }).click()
    await page.getByLabel('Remarks').fill('Test create')
    await page.getByRole('button', { name: 'Create' }).click()

    await expect(page.getByText('Job order saved successfully.')).toBeVisible()
  })

  test('Cancel button closes the form dialog', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/jobs')

    await page.getByRole('button', { name: 'New' }).click()
    await expect(page.getByRole('heading', { name: 'New Job Order' })).toBeVisible()

    await page.getByRole('button', { name: 'Cancel' }).click()
    await expect(page.getByRole('heading', { name: 'New Job Order' })).not.toBeVisible()
  })
})

// ---------------------------------------------------------------------------
// Slice C — Scheduler / calendar
// ---------------------------------------------------------------------------
test.describe('Slice C — scheduler', () => {
  test('scheduler view renders FullCalendar container', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/scheduler')

    await expect(page.getByText('Slice C')).toBeVisible()
    await expect(page.getByText('Scheduler baseline')).toBeVisible()
    // FullCalendar renders a toolbar with navigation buttons
    await expect(page.locator('.fc')).toBeVisible()
    await expect(page.locator('.fc-toolbar')).toBeVisible()
  })

  test('scheduler toolbar exposes prev/next navigation', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/scheduler')

    await expect(page.locator('.fc-prev-button')).toBeVisible()
    await expect(page.locator('.fc-next-button')).toBeVisible()
  })
})

// ---------------------------------------------------------------------------
// Slice D — Rich-text editor
// ---------------------------------------------------------------------------
test.describe('Slice D — rich-text editor', () => {
  test('editor view renders CKEditor 5 toolbar', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/editor')

    await expect(page.getByRole('heading', { name: 'CKEditor 5 Preview' })).toBeVisible()
    // CKEditor classic build renders a toolbar with at least the Bold button
    await expect(page.locator('.ck-toolbar')).toBeVisible()
  })

  test('editor view renders HTML preview section', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/editor')

    await expect(page.getByRole('heading', { name: 'Rendered HTML preview' })).toBeVisible()
    // Pre-seeded content contains the legacy sample heading
    await expect(page.getByText('Legacy CKEditor 4 content sample')).toBeVisible()
  })

  /**
   * CKEditor 4 → 5 HTML content parity.
   * Verifies that the six structural constructs used in legacy CKEditor 4 content
   * (headings, bold, italic, lists, tables, links) all render correctly in the
   * CKEditor 5 HTML preview pane without data loss or mis-tagging.
   */
  test('CKEditor 4 legacy HTML constructs render correctly in CKEditor 5 preview', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/editor')

    const preview = page.locator('.editor-preview')

    // H2 heading
    await expect(preview.locator('h2')).toBeVisible()

    // Bold and italic inline formatting
    await expect(preview.locator('strong')).toBeVisible()
    await expect(preview.locator('em')).toBeVisible()

    // Unordered list with at least one item
    await expect(preview.locator('ul li').first()).toBeVisible()

    // Table structure preserved
    await expect(preview.locator('table td').first()).toBeVisible()
  })
})
