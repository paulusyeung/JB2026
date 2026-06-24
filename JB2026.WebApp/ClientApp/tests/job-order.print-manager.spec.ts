import { expect, test, type Page } from '@playwright/test'

const ORDER_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const ORDER_NUMBER = 'ORD-001'
const JOB_NUMBER = '42'

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'job-print-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Print Test', role: 'Admin', email: 'print@test.local' }),
    )
  })
}

type MockApiState = {
  printRequestCount: number
  lastPrintBody: unknown
  forcePrintFailure: boolean
}

async function mockApi(page: Page): Promise<MockApiState> {
  const state: MockApiState = {
    printRequestCount: 0,
    lastPrintBody: null,
    forcePrintFailure: false,
  }

  const jobOrder = {
    orderId: ORDER_ID,
    orderType: 1,
    orderNumber: ORDER_NUMBER,
    jobNumber: JOB_NUMBER,
    customerName: 'Test Customer',
    customerRef: 'TC-001',
    orderTitle: 'Print Test Order',
    productCode: 'PROD-001',
    productStyle: '',
    outputRef: '',
    invoiceRef: '',
    invoiceAmount: 0,
    attachmentProductCount: 0,
    attachmentCustomerCount: 0,
    orderedBy: 'tester',
    orderedOn: '2026-01-01T00:00:00Z',
    requiredOn: '2026-06-01T00:00:00Z',
    qty: 100,
    paymentTerms: 'Net 30',
    remarks: '',
    status: 1,
    createdBy: 'test',
    createdOn: '2026-01-01T00:00:00Z',
    modifiedBy: null,
    modifiedOn: null,
  }

  const jobDetail = {
    orderId: ORDER_ID,
    orderNumber: `${ORDER_NUMBER}-${JOB_NUMBER}`,
    customerName: 'Test Customer',
    customerRef: 'TC-001',
    orderTitle: 'Print Test Order',
    orderedBy: 'tester',
    orderedOn: '2026-01-01T00:00:00Z',
    requiredOn: '2026-06-01T00:00:00Z',
    qty: 100,
    status: 1,
    paymentTerms: 'Net 30',
    remarks: '',
    productDetails: '',
    productStyle: '',
    styleTitles: ['Cover Design', 'Inner Pages'],
    attachments: [],
  }

  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({
        json: {
          userId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          username: 'admin',
          displayName: 'Administrator',
          role: 'Admin',
        },
      })
      return
    }

    if (path === '/api/v2/job-orders' && request.method() === 'GET') {
      await route.fulfill({ json: [jobOrder] })
      return
    }

    if (path === `/api/v2/jobs/${ORDER_ID}` && request.method() === 'GET') {
      await route.fulfill({ json: jobDetail })
      return
    }

    if (path === `/api/v2/jobs/${ORDER_ID}/print` && request.method() === 'POST') {
      state.printRequestCount += 1
      state.lastPrintBody = await request.postDataJSON()

      if (state.forcePrintFailure) {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ title: 'Unable to generate job-order print PDF' }),
        })
        return
      }

      await route.fulfill({
        status: 200,
        headers: { 'content-type': 'application/pdf' },
        body: '%PDF-1.4\n% job-order print test\n',
      })
      return
    }

    await route.fulfill({ status: 404, body: 'Not found' })
  })

  return state
}

async function openPrintManagerFromJobList(page: Page) {
  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Print Test Order')).toBeVisible()
  // Open the job form by clicking on the row
  await page.getByText('Print Test Order').click()
  // Wait for the form dialog to open
  await expect(page.getByRole('dialog').getByText('Edit Job Order')).toBeVisible()
  // Click Print Order button
  await page.getByRole('dialog').getByRole('button', { name: 'Print Order' }).click()
  // Wait for the print manager dialog to open
  await expect(page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()).toBeVisible()
}

test('print manager dialog opens when Print Order is clicked', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Print Test Order')).toBeVisible()

  await page.getByText('Print Test Order').click()
  await expect(page.getByRole('dialog').getByText('Edit Job Order')).toBeVisible()

  await page.getByRole('dialog').getByRole('button', { name: 'Print Order' }).click()

  await expect(page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()).toBeVisible()
  await expect(page.getByLabel('Order Number')).toBeVisible()
})

test('print manager dialog pre-populates order number and all workflows selected by default', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)

  await openPrintManagerFromJobList(page)

  const printDialog = page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()
  await expect(printDialog.getByLabel('Order Number')).toBeVisible()
  // Both workflows should be shown
  await expect(printDialog.getByText('Cover Design')).toBeVisible()
  await expect(printDialog.getByText('Inner Pages')).toBeVisible()
})

test('print manager submits request with selected options', async ({ page }) => {
  await injectFakeSession(page)
  const apiState = await mockApi(page)

  await page.evaluate(() => {
    window.open = () => null
  })

  await openPrintManagerFromJobList(page)

  const printDialog = page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()

  // Enable no-picture toggle
  await printDialog.getByLabel('No Picture').check()

  // Click Print
  await printDialog.getByRole('button', { name: 'Print' }).click()

  await expect.poll(() => apiState.printRequestCount).toBe(1)
  const body = apiState.lastPrintBody as { noPicture: boolean; noProductDetails: boolean; layout: string }
  expect(body.noPicture).toBe(true)
  expect(body.noProductDetails).toBe(false)
  expect(body.layout).toBe('default')
})

test('print manager shows error when print request fails', async ({ page }) => {
  await injectFakeSession(page)
  const apiState = await mockApi(page)
  apiState.forcePrintFailure = true

  await openPrintManagerFromJobList(page)

  const printDialog = page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()
  await printDialog.getByRole('button', { name: 'Print' }).click()

  await expect(printDialog.getByText('Unable to generate the order PDF right now.')).toBeVisible()
  await expect.poll(() => apiState.printRequestCount).toBe(1)
  // Dialog remains open (recoverable)
  await expect(printDialog).toBeVisible()
})

test('print manager cancel button closes the dialog', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)

  await openPrintManagerFromJobList(page)

  const printDialog = page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()
  await printDialog.getByRole('button', { name: 'Cancel' }).click()

  await expect(printDialog).not.toBeVisible()
})

test('workflow select-all toggle selects and deselects all workflows', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)

  await openPrintManagerFromJobList(page)

  const printDialog = page.getByRole('dialog').filter({ hasText: 'Print Order' }).last()

  // Deselect all using select-all toggle
  const selectAll = printDialog.getByLabel('Select All')
  await selectAll.uncheck()

  // Reselect all
  await selectAll.check()

  // Both workflows should be checked
  await expect(printDialog.getByLabel('Cover Design')).toBeChecked()
  await expect(printDialog.getByLabel('Inner Pages')).toBeChecked()
})
