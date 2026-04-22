import { expect, test, type Page } from '@playwright/test'

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'stock-mobile-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Stock Mobile', role: 'Admin', email: 'stock.mobile@test.local' }),
    )
  })
}

async function mockApi(page: Page) {
  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({
        json: {
          userId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          username: 'admin',
          displayName: 'Administrator',
          role: 'Admin',
        },
      })
      return
    }

    if (path === '/api/v2/stock/products' && request.method() === 'GET') {
      await route.fulfill({
        json: [
          {
            productId: '11111111-1111-1111-1111-111111111111',
            stockNumber: 'CUS-CAT-0001',
            productCode: 'PAPER-A4',
            productName: 'A4 Art Paper 128gsm',
            balance: 320,
            sellingPrice: 12.5,
            cogs: 8.1,
            remarks: 'Core stock item',
            attachmentCount: 1,
            createdOn: '2026-03-31T00:00:00Z',
            createdBy: 'smoke',
            modifiedOn: '2026-04-01T00:00:00Z',
            modifiedBy: 'smoke',
          },
        ],
      })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && request.method() === 'GET') {
      await route.fulfill({
        json: {
          productId: '11111111-1111-1111-1111-111111111111',
          customerCode: 'CUS',
          categoryCode: 'CAT',
          sequenceNumber: '0001',
          stockNumber: 'CUS-CAT-0001',
          productCode: 'PAPER-A4',
          productName: 'A4 Art Paper 128gsm',
          productionInfo: 'Production info',
          remarks: 'Core stock item',
          sellingPrice: 12.5,
          cogs: 8.1,
          balance: 320,
          createdOn: '2026-03-31T00:00:00Z',
          createdBy: 'smoke',
          modifiedOn: '2026-04-01T00:00:00Z',
          modifiedBy: 'smoke',
        },
      })
      return
    }

    if (path.endsWith('/movements')) {
      await route.fulfill({ json: [] })
      return
    }

    if (path === '/api/v2/stock/products/validate-code') {
      await route.fulfill({ json: { isUnique: true } })
      return
    }

    if (path === '/api/v2/stock/products/next-number') {
      await route.fulfill({
        json: {
          customerCode: 'CUS',
          categoryCode: 'CAT',
          sequenceNumber: '0002',
          stockNumber: 'CUS-CAT-0002',
        },
      })
      return
    }

    await route.fulfill({ json: [] })
  })
}

test('stock product dialog is usable on mobile viewport', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)
  await page.goto('/app/stock')

  await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()
  await page.getByText('A4 Art Paper 128gsm').click()

  await expect(page.getByText('Edit Product Record')).toBeVisible()
  await expect(page.getByLabel('Customer Code')).toBeVisible()
  await expect(page.getByLabel('Category Code')).toBeVisible()
  await expect(page.getByLabel('Product Code')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Save and Close' })).toBeVisible()
})
