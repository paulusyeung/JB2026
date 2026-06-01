import { expect, test, type Page } from '@playwright/test'

type BillingClientRow = {
  externalClientId: string
  name: string
  displayName: string
  idNumber: string
  outstandingBalance: number
}

const CLIENT_A: BillingClientRow = {
  externalClientId: 'client-1',
  name: 'Acme Printing',
  displayName: 'Acme Printing Ltd',
  idNumber: 'ACME',
  outstandingBalance: 1234.5,
}

const CLIENT_B: BillingClientRow = {
  externalClientId: 'client-2',
  name: 'Beta Studio',
  displayName: 'Beta Studio',
  idNumber: 'BETA',
  outstandingBalance: 25,
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'billing-statement-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Test User', role: 'Admin', email: 'test@test.local' }),
    )
  })
}

async function mockApi(page: Page, clients: BillingClientRow[] = [CLIENT_A, CLIENT_B]) {
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

    if (path === '/api/v2/billing/clients' && request.method() === 'GET') {
      await route.fulfill({ json: { clients } })
      return
    }

    await route.fulfill({ json: [] })
  })
}

async function enableCheckboxMode(page: Page) {
  await page.getByRole('button', { name: /check box/i }).click()
}

test.describe('Billing Statement view', () => {
  test('billing menu and route expose the statement view', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/billing/statement')

    await expect(page.getByText('Billing Statement')).toBeVisible()
    await expect(page.getByText('Statement').first()).toBeVisible()
  })

  test('statement list excludes login and password columns and formats outstanding balance', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/billing/statement')

    await expect(page.getByText('Acme Printing Ltd')).toBeVisible()
    await expect(page.getByText('$1,234.50')).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Client ID' })).toHaveCount(0)
    await expect(page.getByText(/login account/i)).toHaveCount(0)
    await expect(page.getByText(/^password$/i)).toHaveCount(0)
  })

  test('statement list places the line number column before client', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/billing/statement')

    await page.getByRole('button', { name: /columns/i }).click()
    const menuItems = await page.locator('.toolbar-menu-list .v-list-item-title').allInnerTexts()
    const visibleItems = menuItems.map((value) => value.trim()).filter(Boolean)

    expect(visibleItems.indexOf('#')).toBeGreaterThanOrEqual(0)
    expect(visibleItems.indexOf('Client')).toBeGreaterThanOrEqual(0)
    expect(visibleItems.indexOf('#')).toBeLessThan(visibleItems.indexOf('Client'))
  })

  test('statement button is enabled only when exactly one client is checked', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/billing/statement')

    await expect(page.getByText('Acme Printing Ltd')).toBeVisible()

    const statementButton = page.getByRole('button', { name: /^statement$/i }).last()
    await expect(statementButton).toBeDisabled()

    await enableCheckboxMode(page)

    const checkboxes = page.getByRole('checkbox')
    await checkboxes.nth(1).click()
    await expect(statementButton).toBeEnabled()

    await checkboxes.nth(2).click()
    await expect(statementButton).toBeDisabled()
  })
})