import { expect, test, type Page } from '@playwright/test'

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'mobile-smoke-fake-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Mobile Smoke', role: 'Admin', email: 'mobile@test.local' }),
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
})