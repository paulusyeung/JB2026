import { expect, test, type Page } from '@playwright/test'

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'smoke-test-fake-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Smoke Test', email: 'smoke@test.local', role: 'Admin' }),
    )
    localStorage.setItem(
      'view-settings-billing-statement',
      JSON.stringify({
        visibleColumns: ['icon', 'ln', 'clientName', 'clientCode', 'outstandingBalance'],
        sortKey: 'clientName',
        sortDirection: 'asc',
        checkboxMode: true,
        viewMode: 'detail',
      }),
    )
  })
}

async function mockStatementRoutes(page: Page, state: { launchBodies: any[]; documentRequests: string[] }) {
  const context = page.context()

  await context.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await context.route('**/api/v2/user-profiles/me', (route) =>
    route.fulfill({
      json: {
        userId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        username: 'admin',
        displayName: 'Administrator',
        role: 'Admin',
      },
    }),
  )

  await context.route('**/api/v2/settings', (route) =>
    route.fulfill({
      json: {
        companyName: 'JB2026 Printing',
        timeZone: 'Asia/Kuala_Lumpur',
        currencyCode: 'MYR',
        enableLegacyFallback: true,
      },
    }),
  )

  await context.route('**/api/v2/user-preferences/**', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { metadata: null } })
      return
    }

    await route.fulfill({ json: { metadata: route.request().postDataJSON()?.metadata ?? null } })
  })

  await context.route('**/api/v2/billing/clients**', (route) =>
    route.fulfill({
      json: {
        clients: [
          {
            externalClientId: 'client-1',
            name: 'Acme Print',
            displayName: 'Acme Print',
            idNumber: 'C-001',
            outstandingBalance: 1250.5,
          },
          {
            externalClientId: 'client-2',
            name: 'Beta Labels',
            displayName: 'Beta Labels',
            idNumber: 'C-002',
            outstandingBalance: 400,
          },
        ],
      },
    }),
  )

  await context.route('**/api/v2/billing/statements/client**', async (route) => {
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON()
      state.launchBodies.push(body)

      await route.fulfill({
        json: {
          launchUrl: `/api/v2/billing/statements/client?externalClientId=${body.externalClientId}&dateRangePreset=${encodeURIComponent(body.dateRangePreset)}&status=${body.status}&includeCredits=${body.includeCredits}&includePayments=${body.includePayments}&includeAging=${body.includeAging}`,
        },
      })
      return
    }

    state.documentRequests.push(route.request().url())

    await route.fulfill({
      contentType: 'application/pdf',
      body: '%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF',
    })
  })
}

async function selectFirstClient(page: Page) {
  await expect(page.locator('.billing-statement-table tbody tr')).toHaveCount(2)
  await page.locator('.billing-statement-table tbody .v-selection-control').first().click()
}

test.describe('Billing statement dialog', () => {
  test('gates the Statement button and opens with the requested defaults', async ({ page }) => {
    const state = { launchBodies: [] as any[], documentRequests: [] as string[] }
    await injectFakeSession(page)
    await mockStatementRoutes(page, state)

    await page.goto('/app/billing/statement')

    const statementButton = page.getByRole('button', { name: 'Statement' })
    await expect(statementButton).toBeDisabled()

    await selectFirstClient(page)
    await expect(statementButton).toBeEnabled()

    await statementButton.click()

    const dialog = page.getByRole('dialog')
    await expect(dialog.getByText('Client Statement', { exact: true })).toBeVisible()
    await expect(dialog.getByRole('button', { name: 'Close' })).toBeVisible()
    await expect(dialog.getByLabel('Date Range')).toHaveValue('All Outstanding')
    await expect(dialog.getByLabel('Status')).toHaveValue('All')
    await expect(dialog.getByLabel('Credits')).not.toBeChecked()
    await expect(dialog.getByLabel('Payments')).not.toBeChecked()
    await expect(dialog.getByLabel('Aging')).toBeChecked()
  })

  test('close icon closes the dialog without launching a statement request', async ({ page }) => {
    const state = { launchBodies: [] as any[], documentRequests: [] as string[] }
    await injectFakeSession(page)
    await mockStatementRoutes(page, state)

    await page.goto('/app/billing/statement')
    await selectFirstClient(page)
    await page.getByRole('button', { name: 'Statement' }).click()

    await page.getByRole('button', { name: 'Close' }).click()

    await expect(page.getByRole('dialog')).not.toBeVisible()
    await expect.poll(() => state.launchBodies.length).toBe(0)
  })

  test('cancel closes the dialog without launching a statement request', async ({ page }) => {
    const state = { launchBodies: [] as any[], documentRequests: [] as string[] }
    await injectFakeSession(page)
    await mockStatementRoutes(page, state)

    await page.goto('/app/billing/statement')
    await selectFirstClient(page)
    await page.getByRole('button', { name: 'Statement' }).click()

    await page.getByRole('button', { name: 'Cancel' }).click()

    await expect(page.getByRole('dialog')).not.toBeVisible()
    await expect.poll(() => state.launchBodies.length).toBe(0)
  })

  test('submits the selected options and opens the returned statement in a new tab', async ({ page }) => {
    const state = { launchBodies: [] as any[], documentRequests: [] as string[] }
    await injectFakeSession(page)
    await mockStatementRoutes(page, state)

    await page.goto('/app/billing/statement')
    await selectFirstClient(page)
    await page.getByRole('button', { name: 'Statement' }).click()

    await page.getByLabel('Date Range').focus()
    await page.getByLabel('Date Range').press('ArrowDown')
    await page.getByRole('option', { name: 'This Month' }).click()
    await page.getByLabel('Credits').check()
    await page.getByLabel('Aging').check()

    const popupPromise = page.waitForEvent('popup')
    await page.getByRole('button', { name: 'Proceed' }).click()
    const popup = await popupPromise

    await expect.poll(() => state.launchBodies.length).toBe(1)
    expect(state.launchBodies[0]).toEqual({
      externalClientId: 'client-1',
      dateRangePreset: 'This Month',
      status: 'All',
      includeCredits: true,
      includePayments: false,
      includeAging: true,
    })

    await expect(page.getByRole('dialog')).toBeVisible()
    await popup.waitForLoadState('domcontentloaded')
    await expect.poll(() => state.documentRequests.length).toBe(1)
    await expect(popup.locator('iframe')).toHaveAttribute('src', /^blob:/)
  })
})