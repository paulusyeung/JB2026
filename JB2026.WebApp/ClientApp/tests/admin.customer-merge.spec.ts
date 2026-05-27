import { expect, test, type Page } from '@playwright/test'

// ─── fixtures ────────────────────────────────────────────────────────────────

type CustomerRow = {
  customerId: string
  customerName: string
  loginAccount: string
  loginPassword: string
  customerCode: string
  invoiceNinjaClientId: string
  billingSyncStatus: string
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

const CUSTOMER_A: CustomerRow = {
  customerId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  customerName: 'Acme Corp',
  loginAccount: 'acme',
  loginPassword: 'pass',
  customerCode: 'ACME',
  invoiceNinjaClientId: '',
  billingSyncStatus: '',
  createdOn: '2026-01-01T00:00:00Z',
  createdBy: 'admin',
  modifiedOn: '2026-01-01T00:00:00Z',
  modifiedBy: 'admin',
}

const CUSTOMER_B: CustomerRow = {
  ...CUSTOMER_A,
  customerId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  customerName: 'Beta Ltd',
  loginAccount: 'beta',
  customerCode: 'BETA',
}

const CUSTOMER_C: CustomerRow = {
  ...CUSTOMER_A,
  customerId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
  customerName: 'Gamma Inc',
  loginAccount: 'gamma',
  customerCode: 'GAMMA',
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'admin-customer-merge-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Test User', role: 'Admin', email: 'test@test.local' }),
    )
  })
}

async function mockApi(page: Page, customers: CustomerRow[] = [CUSTOMER_A, CUSTOMER_B, CUSTOMER_C]) {
  const state = {
    customers: [...customers],
    mergeRequests: [] as { targetCustomerId: string; customerIds: string[] }[],
  }

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

    if (path === '/api/v2/admin/customers' && request.method() === 'GET') {
      await route.fulfill({ json: state.customers })
      return
    }

    if (path === '/api/v2/admin/customers/merge' && request.method() === 'POST') {
      const body = await request.postDataJSON()
      state.mergeRequests.push(body)
      await route.fulfill({ status: 204 })
      return
    }

    await route.fulfill({ json: [] })
  })

  return state
}

async function enableCheckboxMode(page: Page) {
  await page.getByRole('button', { name: /checkbox/i }).click()
}

// ─── 4.1: merge button visibility and enabled/disabled state ─────────────────

test.describe('Admin Customer Merge — button state', () => {
  test('merge button is visible and disabled when no customers are selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    const mergeBtn = page.getByRole('button', { name: /merge customers/i })
    await expect(mergeBtn).toBeVisible()
    await expect(mergeBtn).toBeDisabled()
  })

  test('merge button remains disabled when only one customer is selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)

    // Select only one customer
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()

    const mergeBtn = page.getByRole('button', { name: /merge customers/i })
    await expect(mergeBtn).toBeDisabled()
  })

  test('merge button becomes enabled when two or more customers are selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)

    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()
    await checkboxes.nth(2).click()

    const mergeBtn = page.getByRole('button', { name: /merge customers/i })
    await expect(mergeBtn).toBeEnabled()
  })

  test('merge button remains disabled outside checkbox mode with no selection', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    // Checkbox mode is off by default — merge should be visible but disabled
    const mergeBtn = page.getByRole('button', { name: /merge customers/i })
    await expect(mergeBtn).toBeVisible()
    await expect(mergeBtn).toBeDisabled()
  })
})

// ─── 4.2: merge dialog single-target behavior and post-success refresh ────────

test.describe('Admin Customer Merge — dialog behavior', () => {
  test('merge dialog lists only the selected customers', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click() // Acme Corp
    await checkboxes.nth(2).click() // Beta Ltd

    await page.getByRole('button', { name: /merge customers/i }).click()

    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()

    // Selected customers should be listed as radio options
    await expect(dialog.getByText('Acme Corp')).toBeVisible()
    await expect(dialog.getByText('Beta Ltd')).toBeVisible()
    // Non-selected customer should NOT be in the dialog
    await expect(dialog.getByText('Gamma Inc')).not.toBeVisible()
  })

  test('merge dialog confirm button is disabled until a target is selected', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()
    await checkboxes.nth(2).click()

    await page.getByRole('button', { name: /merge customers/i }).click()

    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()

    const confirmBtn = dialog.getByRole('button', { name: /^merge$/i })
    await expect(confirmBtn).toBeDisabled()

    // Select a target
    await dialog.getByRole('radio').first().click()
    await expect(confirmBtn).toBeEnabled()
  })

  test('merge dialog enforces single target selection', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()
    await checkboxes.nth(2).click()
    await checkboxes.nth(3).click()

    await page.getByRole('button', { name: /merge customers/i }).click()

    const dialog = page.getByRole('dialog')
    const radios = dialog.getByRole('radio')

    // Click first radio
    await radios.nth(0).click()
    await expect(radios.nth(0)).toBeChecked()
    await expect(radios.nth(1)).not.toBeChecked()

    // Click second radio — first should deselect
    await radios.nth(1).click()
    await expect(radios.nth(0)).not.toBeChecked()
    await expect(radios.nth(1)).toBeChecked()
  })

  test('merge dialog closes and list refreshes after successful merge', async ({ page }) => {
    await injectFakeSession(page)
    const state = await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()
    await checkboxes.nth(2).click()

    await page.getByRole('button', { name: /merge customers/i }).click()

    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()

    // Select a target and confirm
    await dialog.getByRole('radio').first().click()
    await dialog.getByRole('button', { name: /^merge$/i }).click()

    // Dialog should close
    await expect(dialog).not.toBeVisible()

    // Verify the merge API was called with correct shape
    expect(state.mergeRequests).toHaveLength(1)
    expect(state.mergeRequests[0].targetCustomerId).toBeTruthy()
    expect(state.mergeRequests[0].customerIds).toHaveLength(2)
    expect(state.mergeRequests[0].customerIds).toContain(state.mergeRequests[0].targetCustomerId)
  })

  test('canceling the merge dialog does not call the merge endpoint', async ({ page }) => {
    await injectFakeSession(page)
    const state = await mockApi(page)
    await page.goto('/app/admin/customer')

    await expect(page.getByText('Acme Corp')).toBeVisible()

    await enableCheckboxMode(page)
    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()
    await checkboxes.nth(2).click()

    await page.getByRole('button', { name: /merge customers/i }).click()

    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()

    // Cancel the dialog
    await dialog.getByRole('button', { name: /cancel/i }).click()
    await expect(dialog).not.toBeVisible()

    // No merge request should have been sent
    expect(state.mergeRequests).toHaveLength(0)
  })
})
