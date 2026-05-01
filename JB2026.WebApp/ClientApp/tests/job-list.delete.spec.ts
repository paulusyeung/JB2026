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
  productStyle: '',
  productDetails: '',
  outputRef: '',
  invoiceRef: '',
  invoiceAmount: 1200,
  attachmentProductCount: 0,
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

const JOB_B: JobRow = {
  ...JOB_A,
  orderId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  orderNumber: 'JB260102',
  jobNumber: '02',
  orderTitle: 'Label Set',
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'job-list-delete-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Test User', role: 'Admin', email: 'test@test.local' }),
    )
  })
}

async function mockApi(page: Page, jobs: JobRow[] = [JOB_A, JOB_B]) {
  const state = { rows: [...jobs], deleteRequests: [] as string[] }

  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({
        json: {
          userId: 'aaaaaaaa-0000-0000-0000-000000000000',
          username: 'admin',
          displayName: 'Administrator',
          role: 'Admin',
        },
      })
      return
    }

    if (path === '/api/v2/job-orders' && request.method() === 'GET') {
      await route.fulfill({ json: state.rows })
      return
    }

    // DELETE /api/v2/job-orders/:id
    const deleteMatch = path.match(/^\/api\/v2\/job-orders\/([0-9a-f-]+)$/i)
    if (deleteMatch && request.method() === 'DELETE') {
      const id = deleteMatch[1]
      state.deleteRequests.push(id)
      const target = state.rows.find((r) => r.orderId === id)
      if (!target) {
        await route.fulfill({ status: 404 })
        return
      }
      state.rows = state.rows.filter((r) => r.orderId !== id)
      await route.fulfill({ json: target })
      return
    }

    await route.fulfill({ json: [] })
  })

  return state
}

// ─── tests ───────────────────────────────────────────────────────────────────

test.describe('Job List delete action', () => {
  // 5.1: button visibility and enabled/disabled state

  test('delete button is visible and disabled when no rows are selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Banner Print')).toBeVisible()

    const deleteBtn = page.getByRole('button', { name: /delete selected/i })
    await expect(deleteBtn).toBeVisible()
    await expect(deleteBtn).toBeDisabled()
  })

  test('delete button becomes enabled when a job is selected via checkbox mode', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Banner Print')).toBeVisible()

    // Enable checkbox mode
    await page.getByRole('button', { name: /checkbox/i }).click()

    const deleteBtn = page.getByRole('button', { name: /delete selected/i })
    await expect(deleteBtn).toBeDisabled()

    // Select the first job by clicking the row checkbox
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.first().click()

    await expect(deleteBtn).toBeEnabled()
  })

  test('delete button remains visible outside checkbox mode', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Banner Print')).toBeVisible()

    // Checkbox mode is off by default — delete should still be visible but disabled
    const deleteBtn = page.getByRole('button', { name: /delete selected/i })
    await expect(deleteBtn).toBeVisible()
    await expect(deleteBtn).toBeDisabled()
  })

  // 5.2: confirmation-cancel and confirmation-accept flows

  test('canceling the confirmation dialog does not delete any jobs', async ({ page }) => {
    await injectFakeSession(page)
    const state = await mockApi(page)
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Banner Print')).toBeVisible()

    await page.getByRole('button', { name: /checkbox/i }).click()
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.first().click()

    // Dismiss the confirmation
    page.on('dialog', (dialog) => dialog.dismiss())
    await page.getByRole('button', { name: /delete selected/i }).click()

    // No delete requests should have fired
    expect(state.deleteRequests).toHaveLength(0)
    // Job is still visible
    await expect(page.getByText('Banner Print')).toBeVisible()
  })

  test('accepting confirmation deletes the selected job and refreshes the list', async ({ page }) => {
    await injectFakeSession(page)
    const state = await mockApi(page)
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Banner Print')).toBeVisible()

    await page.getByRole('button', { name: /checkbox/i }).click()
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.first().click()

    page.on('dialog', (dialog) => dialog.accept())
    await page.getByRole('button', { name: /delete selected/i }).click()

    // List should refresh and banner print should be gone
    await expect(page.getByText('Banner Print')).not.toBeVisible({ timeout: 5000 })
    expect(state.deleteRequests).toHaveLength(1)
  })

  // 5.3: batch delete mixed outcomes and post-delete refresh/selection clearing

  test('batch delete shows aggregate result when some items fail', async ({ page }) => {
    await injectFakeSession(page)
    const state = await mockApi(page, [JOB_A, JOB_B])

    // Override: make JOB_B delete fail
    await page.route('**/api/v2/job-orders/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', async (route) => {
      if (route.request().method() === 'DELETE') {
        state.deleteRequests.push('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
        await route.fulfill({ status: 500 })
        return
      }
      await route.continue()
    })

    await page.goto('/app/job-order/job-list')
    await expect(page.getByText('Banner Print')).toBeVisible()
    await expect(page.getByText('Label Set')).toBeVisible()

    await page.getByRole('button', { name: /checkbox/i }).click()
    // Select all via header checkbox
    const headerCheckbox = page.getByRole('checkbox').first()
    await headerCheckbox.click()

    page.on('dialog', (dialog) => dialog.accept())
    await page.getByRole('button', { name: /delete selected/i }).click()

    // Wait for the error/result feedback to appear (aggregate message)
    await expect(page.getByText(/could not be deleted/i)).toBeVisible({ timeout: 5000 })
  })

  test('after successful delete, selection is cleared', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/job-order/job-list')

    await expect(page.getByText('Banner Print')).toBeVisible()

    await page.getByRole('button', { name: /checkbox/i }).click()
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.first().click()

    const deleteBtn = page.getByRole('button', { name: /delete selected/i })
    await expect(deleteBtn).toBeEnabled()

    page.on('dialog', (dialog) => dialog.accept())
    await deleteBtn.click()

    // After delete completes, delete button should be disabled again (no selection)
    await expect(deleteBtn).toBeDisabled({ timeout: 5000 })
  })
})
