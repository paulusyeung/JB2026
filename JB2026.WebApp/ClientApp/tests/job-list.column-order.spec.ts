import { expect, test, type Page } from '@playwright/test'

// ─── fixtures ────────────────────────────────────────────────────────────────

type JobRow = {
  orderId: string
  orderType: number
  orderNumber: string
  jobNumber: string
  customerName: string
  customerRef: string
  orderTitle: string
  productCode: string
  productStyle: string
  productDetails: string
  outputRef: string
  invoiceRef: string
  invoiceAmount: number
  attachmentProductCount: number
  attachmentCustomerCount: number
  orderedBy: string
  orderedOn: string
  requiredOn: string
  completedOn: null
  qty: number
  paymentTerms: string
  remarks: string
  status: number
  createdBy: string
  createdOn: string
  modifiedBy: string
  modifiedOn: string
}

const JOB_A: JobRow = {
  orderId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  orderType: 0,
  orderNumber: 'JB260101',
  jobNumber: '01',
  customerName: 'Acme Corp',
  customerRef: 'REF-001',
  orderTitle: 'Banner Print',
  productCode: 'PC-001',
  productStyle: 'Woven',
  productDetails: '',
  outputRef: '',
  invoiceRef: '',
  invoiceAmount: 1200,
  attachmentProductCount: 1,
  attachmentCustomerCount: 0,
  orderedBy: 'admin',
  orderedOn: '2026-01-15T00:00:00Z',
  requiredOn: '2026-02-01T00:00:00Z',
  completedOn: null,
  qty: 500,
  paymentTerms: 'Net 30',
  remarks: '',
  status: 1,
  createdBy: 'admin',
  createdOn: '2026-01-15T00:00:00Z',
  modifiedBy: 'admin',
  modifiedOn: '2026-01-15T00:00:00Z',
}

const STORAGE_KEY = 'view-settings-joblist'

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'job-list-column-order-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Test User', role: 'Admin', email: 'test@test.local' }),
    )
  })
}

async function mockApi(page: Page) {
  const preferenceWrites: string[] = []

  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({
        json: { userId: 'test', username: 'admin', displayName: 'Administrator', role: 'Admin' },
      })
      return
    }

    if (path === '/api/v2/job-orders' && request.method() === 'GET') {
      await route.fulfill({ json: [JOB_A] })
      return
    }

    if (path.startsWith('/api/v2/user-preferences') && request.method() === 'PUT') {
      preferenceWrites.push(request.postData() ?? '')
      await route.fulfill({ json: { metadata: null } })
      return
    }

    await route.fulfill({ status: 404, json: null })
  })

  return { preferenceWrites }
}

async function headerKeys(page: Page) {
  return page.locator('.job-list-th').evaluateAll((cells) => cells.map((cell) => cell.getAttribute('data-column-key') ?? ''))
}

async function dragHeader(page: Page, sourceIndex: number, targetIndex: number) {
  const headers = page.locator('.job-list-th')
  const source = (await headers.nth(sourceIndex).boundingBox())!
  const target = (await headers.nth(targetIndex).boundingBox())!

  await page.mouse.move(source.x + source.width / 2, source.y + source.height / 2)
  await page.mouse.down()
  await page.mouse.move(source.x + source.width / 2 + 8, source.y + source.height / 2, { steps: 4 })
  await page.mouse.move(target.x + target.width / 2, target.y + target.height / 2, { steps: 15 })
  await page.waitForTimeout(150)
  await page.mouse.up()
  await page.waitForTimeout(250)
}

// ─── tests ───────────────────────────────────────────────────────────────────

test.describe('Job list column order', () => {
  test('a header can be dragged to another position and the order is persisted', async ({ page }) => {
    await injectFakeSession(page)
    const { preferenceWrites } = await mockApi(page)
    await page.goto('/app/job-order/job-list')
    await expect(page.getByText('Banner Print')).toBeVisible()

    const initial = await headerKeys(page)
    expect(initial[0]).toBe('orderType')

    await dragHeader(page, 0, 5)
    const reordered = await headerKeys(page)
    expect(reordered).not.toEqual(initial)
    expect(reordered[5]).toBe('orderType')

    const stored = await page.evaluate((key) => localStorage.getItem(key), STORAGE_KEY)
    expect(stored).toContain('columnOrder')
    expect(JSON.parse(stored ?? '{}').columnOrder.indexOf('orderType')).toBe(5)

    await page.waitForTimeout(800)
    expect(preferenceWrites.some((body) => body.includes('columnOrder'))).toBe(true)

    await page.reload()
    await expect(page.getByText('Banner Print')).toBeVisible()
    expect(await headerKeys(page)).toEqual(reordered)
  })

  test('the columns menu follows the custom order and can reset it', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/job-order/job-list')
    await expect(page.getByText('Banner Print')).toBeVisible()

    const initial = await headerKeys(page)
    await dragHeader(page, 0, 5)
    const reordered = await headerKeys(page)

    await page.getByRole('button', { name: /columns/i }).click()
    const menuItems = page.locator('.v-overlay__content .v-list-item-title')
    await expect(menuItems).toHaveCount(reordered.length + 1)
    expect((await menuItems.allTextContents())[5]).toBe('Type')

    await page.getByText('Reset Column Order').click()
    await page.waitForTimeout(300)
    expect(await headerKeys(page)).toEqual(initial)
  })

  test('header sorting and select-all keep working with reorderable headers', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/job-order/job-list')
    await expect(page.getByText('Banner Print')).toBeVisible()

    await page.locator('.job-list-th').filter({ hasText: 'Order Number' }).click()
    await expect(page.locator('.v-data-table__th--sorted').first()).toBeVisible()

    await page.getByRole('button', { name: /checkbox/i }).click()
    const selectAllHeader = page.locator('.job-list-th').first()
    await selectAllHeader.locator('.v-selection-control').click()
    await expect(page.locator('tbody .v-selection-control').first()).toHaveClass(/v-selection-control--dirty/)
  })
})
