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

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'stock-record-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Stock Test', role: 'Admin', email: 'stock@test.local' }),
    )
  })
}

async function mockApi(page: Page) {
  const state = {
    printRequestCount: 0,
    forcePrintFailure: false,
  }

  const products: ProductRow[] = [
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
  ]

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
      await route.fulfill({ json: products })
      return
    }

    if (path === '/api/v2/stock/products' && request.method() === 'POST') {
      const payload = (await request.postDataJSON()) as {
        customerCode: string
        categoryCode: string
        sequenceNumber: string
        productCode: string
        productName: string
        productionInfo: string
        remarks: string
        sellingPrice: number
        cogs: number
      }

      const created: ProductRow = {
        productId: '22222222-2222-2222-2222-222222222222',
        stockNumber: `${payload.customerCode}-${payload.categoryCode}-${payload.sequenceNumber.padStart(4, '0')}`,
        productCode: payload.productCode,
        productName: payload.productName,
        balance: 0,
        sellingPrice: payload.sellingPrice,
        cogs: payload.cogs,
        remarks: payload.remarks,
        attachmentCount: 0,
        createdOn: '2026-04-02T00:00:00Z',
        createdBy: 'smoke',
        modifiedOn: '2026-04-02T00:00:00Z',
        modifiedBy: 'smoke',
      }

      products.push(created)
      await route.fulfill({
        status: 201,
        json: {
          productId: created.productId,
          customerCode: payload.customerCode,
          categoryCode: payload.categoryCode,
          sequenceNumber: payload.sequenceNumber,
          stockNumber: created.stockNumber,
          productCode: created.productCode,
          productName: created.productName,
          productionInfo: payload.productionInfo,
          remarks: created.remarks,
          sellingPrice: created.sellingPrice,
          cogs: created.cogs,
          balance: 0,
          createdOn: created.createdOn,
          createdBy: created.createdBy,
          modifiedOn: created.modifiedOn,
          modifiedBy: created.modifiedBy,
        },
      })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && path.endsWith('/movements')) {
      await route.fulfill({
        json: [
          {
            inOutId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            inOutDate: '2026-04-01T00:00:00Z',
            reference: 'IN-1',
            qty: 50,
            runningBalance: 50,
            modifiedOn: '2026-04-01T00:00:00Z',
            modifiedBy: 'smoke',
          },
        ],
      })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && path.endsWith('/print') && request.method() === 'GET') {
      state.printRequestCount += 1

      if (state.forcePrintFailure) {
        await route.fulfill({
          status: 500,
          json: {
            title: 'Unable to generate stock print PDF',
          },
        })
        return
      }

      await route.fulfill({
        status: 200,
        headers: {
          'content-type': 'application/pdf',
        },
        body: '%PDF-1.4\n% stock print test\n',
      })
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

    if (path === '/api/v2/stock/products/validate-code') {
      const code = url.searchParams.get('productCode')
      await route.fulfill({ json: { isUnique: code !== 'PAPER-A4-TAKEN' } })
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
          sequenceNumber: row.stockNumber.split('-')[2],
          stockNumber: row.stockNumber,
          productCode: row.productCode,
          productName: row.productName,
          productionInfo: 'Production info',
          remarks: row.remarks,
          sellingPrice: row.sellingPrice,
          cogs: row.cogs,
          balance: row.balance,
          createdOn: row.createdOn,
          createdBy: row.createdBy,
          modifiedOn: row.modifiedOn,
          modifiedBy: row.modifiedBy,
        },
      })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && request.method() === 'PUT') {
      const id = path.split('/').pop() ?? ''
      const payload = (await request.postDataJSON()) as {
        customerCode: string
        categoryCode: string
        sequenceNumber: string
        productCode: string
        productName: string
        productionInfo: string
        remarks: string
        sellingPrice: number
        cogs: number
      }

      const idx = products.findIndex((item) => item.productId === id)
      if (idx === -1) {
        await route.fulfill({ status: 404, body: '' })
        return
      }

      products[idx] = {
        ...products[idx],
        stockNumber: `${payload.customerCode}-${payload.categoryCode}-${payload.sequenceNumber.padStart(4, '0')}`,
        productCode: payload.productCode,
        productName: payload.productName,
        remarks: payload.remarks,
        sellingPrice: payload.sellingPrice,
        cogs: payload.cogs,
      }

      await route.fulfill({
        json: {
          productId: products[idx].productId,
          customerCode: payload.customerCode,
          categoryCode: payload.categoryCode,
          sequenceNumber: payload.sequenceNumber,
          stockNumber: products[idx].stockNumber,
          productCode: products[idx].productCode,
          productName: products[idx].productName,
          productionInfo: payload.productionInfo,
          remarks: products[idx].remarks,
          sellingPrice: products[idx].sellingPrice,
          cogs: products[idx].cogs,
          balance: products[idx].balance,
          createdOn: products[idx].createdOn,
          createdBy: products[idx].createdBy,
          modifiedOn: products[idx].modifiedOn,
          modifiedBy: products[idx].modifiedBy,
        },
      })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && request.method() === 'DELETE') {
      const id = path.split('/').pop() ?? ''
      const idx = products.findIndex((item) => item.productId === id)
      if (idx !== -1) {
        products.splice(idx, 1)
      }
      await route.fulfill({ status: 200, json: { productId: id, outcome: 'retired' } })
      return
    }

    await route.fulfill({ json: [] })
  })

  return state
}

test.describe('stock product record popup', () => {
  test('opens edit mode from row click and shows movement history', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()
    await page.getByText('A4 Art Paper 128gsm').click()

    await expect(page.getByText('Edit Product Record')).toBeVisible()
    await expect(page.getByText('Stock Movement History')).toBeVisible()
    await expect(page.getByText('IN-1')).toBeVisible()
  })

  test('validates required fields and transitions create to edit after save', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'New Product' }).click()
    await page.getByRole('dialog').getByRole('button', { name: 'Save', exact: true }).click()
    await expect(page.getByText('This field is required.')).toHaveCount(5)

    await page.getByLabel('Customer Code').fill('CUS')
    await page.getByLabel('Category Code').fill('CAT')
    await page.getByRole('button', { name: 'Next Number' }).click()
    await page.getByLabel('Product Code').fill('PAPER-A5')
    await page.getByLabel('Product Name').fill('A5 Art Paper 128gsm')

    await page.getByRole('dialog').getByRole('button', { name: 'Save', exact: true }).click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()
  })

  test('opens create mode from NEW PRODUCT and saves record', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'New Product' }).click()
    await expect(page.getByText('Create Product Record')).toBeVisible()

    await page.getByLabel('Customer Code').fill('CUS')
    await page.getByLabel('Category Code').fill('CAT')
    await page.getByRole('button', { name: 'Next Number' }).click()
    await page.getByLabel('Product Code').fill('PAPER-B5')
    await page.getByLabel('Product Name').fill('B5 Art Paper 128gsm')

    await page.getByRole('button', { name: 'Save and Close' }).click()

    await expect(page.getByText('Create Product Record')).toHaveCount(0)
    await expect(page.getByText('B5 Art Paper 128gsm')).toBeVisible()
  })

  test('delete action removes product and refreshes list', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()
    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click()

    await expect(page.getByText('A4 Art Paper 128gsm')).toHaveCount(0)
  })

  test('delete action from dialog shows retired success message', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click()

    await expect(page.getByText('Product retired. Delete again to permanently remove it.')).toBeVisible()
  })

  test('delete button in toolbar is disabled when no products selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()

    const deleteBtn = page.getByRole('button', { name: 'Delete' })
    await expect(deleteBtn).toBeDisabled()
  })

  test('print action sends request and shows fallback message when popup is blocked', async ({ page }) => {
    await injectFakeSession(page)
    const apiState = await mockApi(page)
    await page.goto('/app/stock')

    await page.evaluate(() => {
      window.open = () => null
    })

    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    await page.getByRole('dialog').getByRole('button', { name: 'Print' }).click()

    await expect(page.getByText('Popup was blocked. PDF downloaded instead.')).toBeVisible()
    await expect.poll(() => apiState.printRequestCount).toBe(1)
  })

  test('print action shows localized error when print API fails', async ({ page }) => {
    await injectFakeSession(page)
    const apiState = await mockApi(page)
    apiState.forcePrintFailure = true
    await page.goto('/app/stock')

    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    await page.getByRole('dialog').getByRole('button', { name: 'Print' }).click()

    await expect(page.getByText('Unable to generate stock print PDF.')).toBeVisible()
    await expect.poll(() => apiState.printRequestCount).toBe(1)
  })

  test('delete from toolbar with checkbox selection removes product', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()

    // Enable checkbox mode and select the row
    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.locator('.v-data-table .v-checkbox-btn').first().click()

    // Delete button should now be enabled
    const deleteBtn = page.getByRole('button', { name: 'Delete' })
    await expect(deleteBtn).toBeEnabled()
    await deleteBtn.click()

    await expect(page.getByText('A4 Art Paper 128gsm')).toHaveCount(0)
    await expect(page.getByText('Product retired. Delete again to permanently remove it.')).toBeVisible()
  })

  test('cancel delete confirmation leaves product unchanged', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.dismiss())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click()

    await expect(page.getByText('Edit Product Record')).toBeVisible()
    await expect(page.getByText('A4 Art Paper 128gsm')).toBeVisible()
  })
})
