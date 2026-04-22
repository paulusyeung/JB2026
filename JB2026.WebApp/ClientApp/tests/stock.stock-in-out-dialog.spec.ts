import { expect, test, type Page } from '@playwright/test'

type ProductRow = {
  productId: string
  stockNumber: string
  productCode: string
  productName: string
  balance: number
  sellingPrice: number
  cogs: number
  remarks: string
  attachmentCount: number
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

const baseProduct: ProductRow = {
  productId: '11111111-1111-1111-1111-111111111111',
  stockNumber: 'CUS-CAT-0001',
  productCode: 'PAPER-A4',
  productName: 'A4 Art Paper 128gsm',
  balance: 320,
  sellingPrice: 12.5,
  cogs: 8.1,
  remarks: 'Core stock item',
  attachmentCount: 0,
  createdOn: '2026-03-31T00:00:00Z',
  createdBy: 'smoke',
  modifiedOn: '2026-04-01T00:00:00Z',
  modifiedBy: 'smoke',
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'stock-in-out-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Stock Test', role: 'Admin', email: 'stock@test.local' }),
    )
  })
}

async function mockApi(page: Page, products: ProductRow[]) {
  let currentBalance = products[0]?.balance ?? 320

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
      await route.fulfill({ json: products.map((p) => ({ ...p, balance: currentBalance })) })
      return
    }

    if (path.endsWith('/transactions') && request.method() === 'POST') {
      const payload = (await request.postDataJSON()) as { qty: number }
      currentBalance += payload.qty
      await route.fulfill({
        status: 201,
        json: {
          inOutId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          productId: products[0]?.productId,
          newBalance: currentBalance,
        },
      })
      return
    }

    if (path.endsWith('/transactions') && request.method() === 'POST') {
      await route.fulfill({ status: 404, body: '' })
      return
    }

    if (path.endsWith('/movements') && request.method() === 'GET') {
      await route.fulfill({ json: [] })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && request.method() === 'GET') {
      const id = path.split('/').pop() ?? ''
      const row = products.find((item) => item.productId === id)
      if (!row) {
        await route.fulfill({ status: 404, body: '' })
        return
      }
      await route.fulfill({
        json: {
          productId: row.productId,
          customerCode: 'CUS',
          categoryCode: 'CAT',
          sequenceNumber: '0001',
          stockNumber: row.stockNumber,
          productCode: row.productCode,
          productName: row.productName,
          productionInfo: '',
          remarks: row.remarks,
          sellingPrice: row.sellingPrice,
          cogs: row.cogs,
          balance: currentBalance,
          createdOn: row.createdOn,
          createdBy: row.createdBy,
          modifiedOn: row.modifiedOn,
          modifiedBy: row.modifiedBy,
        },
      })
      return
    }

    if (path === '/api/v2/stock/products/next-number') {
      await route.fulfill({ json: { customerCode: 'CUS', categoryCode: 'CAT', sequenceNumber: '0002', stockNumber: 'CUS-CAT-0002' } })
      return
    }

    if (path === '/api/v2/stock/products/validate-code') {
      await route.fulfill({ json: { isUnique: true } })
      return
    }

    await route.fulfill({ json: [] })
  })
}

// Task 6.1: Component tests for both launch paths and dialog visibility lifecycle

test.describe('stock in/out dialog - launch paths and lifecycle', () => {
  test('Stock In/Out button is disabled when no row is selected in StockView', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    const btn = page.getByRole('button', { name: 'Stock In/Out' })
    await expect(btn).toBeVisible()
    await expect(btn).toBeDisabled()
  })

  test('Stock In/Out button is enabled when exactly one row is selected in StockView', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    // Enable checkbox mode and select one row
    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()

    const btn = page.getByRole('button', { name: 'Stock In/Out' })
    await expect(btn).toBeEnabled()
  })

  test('Stock In/Out dialog opens from StockView when one row is selected', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()

    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await expect(page.getByText('Stock In/Out Transaction')).toBeVisible()
    await expect(page.getByText('CUS-CAT-0001')).toBeVisible()
  })

  test('Stock In/Out dialog opens from ProductRecordDialog action button', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    // Click row to open product record dialog
    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    // Click Stock In/Out action inside product record dialog
    await page.getByRole('dialog').getByRole('button', { name: 'Stock In/Out' }).click()

    await expect(page.getByText('Stock In/Out Transaction')).toBeVisible()
  })

  test('Stock In/Out dialog closes when X button is clicked', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await expect(page.getByText('Stock In/Out Transaction')).toBeVisible()

    // Close via X button (last close button in the dialog)
    await page.locator('.stock-in-out-dialog').getByRole('button', { name: 'Close' }).click()

    await expect(page.getByText('Stock In/Out Transaction')).toHaveCount(0)
  })
})

// Task 6.2: Validation tests

test.describe('stock in/out dialog - validation', () => {
  test('shows error when quantity is empty and save is attempted', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await expect(page.getByText('Stock In/Out Transaction')).toBeVisible()
    await page.getByRole('button', { name: 'Save and Close' }).click()

    await expect(page.getByText('Quantity is required.')).toBeVisible()
  })

  test('shows error when quantity is non-integer (decimal)', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('3.14')
    await page.getByRole('button', { name: 'Save and Close' }).click()

    await expect(page.getByText('Quantity must be a non-zero integer.')).toBeVisible()
  })

  test('shows error when quantity is zero', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('0')
    await page.getByRole('button', { name: 'Save and Close' }).click()

    await expect(page.getByText('Quantity must be a non-zero integer.')).toBeVisible()
  })

  test('accepts positive integer quantity for stock-in', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('50')
    await page.getByRole('button', { name: 'Save and Close' }).click()

    // Dialog should close without validation errors
    await expect(page.getByText('Stock In/Out Transaction')).toHaveCount(0)
  })

  test('accepts negative integer quantity for stock-out', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('-20')
    await page.getByRole('button', { name: 'Save and Close' }).click()

    await expect(page.getByText('Stock In/Out Transaction')).toHaveCount(0)
  })
})

// Task 6.4: Save & Close success behavior and caller refresh signaling

test.describe('stock in/out dialog - save and close behavior', () => {
  test('Save & Close persists transaction and closes dialog', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('10')
    await page.getByLabel('Reference').fill('REF-001')

    const [transactionRequest] = await Promise.all([
      page.waitForRequest((req) => req.url().includes('/transactions') && req.method() === 'POST'),
      page.getByRole('button', { name: 'Save and Close' }).click(),
    ])

    expect(transactionRequest.postDataJSON()).toMatchObject({ qty: 10, reference: 'REF-001' })
    await expect(page.getByText('Stock In/Out Transaction')).toHaveCount(0)
  })

  test('Save without close keeps dialog open and resets quantity', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('5')
    await page.getByRole('button', { name: 'Save', exact: true }).click()

    // Dialog should remain open, quantity should be cleared
    await expect(page.getByText('Stock In/Out Transaction')).toBeVisible()
    await expect(page.getByLabel('Quantity (+/-)')).toHaveValue('')
  })

  test('Cancelled confirmation does not close dialog or reset form', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page, [baseProduct])
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Stock In/Out' }).click()

    await page.getByLabel('Quantity (+/-)').fill('99')
    await page.getByRole('button', { name: 'Save and Close' }).click()

    // Dialog should remain open with qty intact
    await expect(page.getByText('Stock In/Out Transaction')).toBeVisible()
    await expect(page.getByLabel('Quantity (+/-)')).toHaveValue('99')
  })
})
