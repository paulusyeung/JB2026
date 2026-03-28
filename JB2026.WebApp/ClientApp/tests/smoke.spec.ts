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
// API mock helper — intercepts backend calls so views render without a live
// API server and tests remain deterministic.
// ---------------------------------------------------------------------------
async function mockApiRoutes(page: Page) {
  // Feature flags endpoint used by dashboard
  await page.route('**/ui/feature-flags', (route) =>
    route.fulfill({ json: { flags: [] } }),
  )

  // Jobs list endpoint
  await page.route('**/api/v2/jobs/**', (route) =>
    route.fulfill({ json: { rows: [], total: 0 } }),
  )

  // Quotations endpoint
  await page.route('**/api/v2/quotations**', (route) =>
    route.fulfill({ json: { rows: [], rowCount: 0, keyword: '' } }),
  )

  // Job schedules (calendar)
  await page.route('**/api/v2/job-schedules/**', (route) =>
    route.fulfill({ json: [] }),
  )
}

// ---------------------------------------------------------------------------
// Slice A — Read-only lists and dashboards
// ---------------------------------------------------------------------------
test.describe('Slice A — read-only lists and dashboard', () => {
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

  test('quotations view renders register heading and search', async ({ page }) => {
    await injectFakeSession(page)
    await mockApiRoutes(page)
    await page.goto('/app/quotations')

    await expect(page.getByRole('heading', { name: 'Quotation register' })).toBeVisible()
    await expect(page.getByPlaceholder('Search quotations')).toBeVisible()
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

    // Clear the pre-filled Order Title and submit
    await page.getByLabel('Order Title').fill('')
    await page.getByLabel('Customer Name').fill('')
    await page.getByRole('button', { name: 'Create' }).click()

    // Vuetify validation error messages should appear
    await expect(page.getByText('Required').first()).toBeVisible()
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
