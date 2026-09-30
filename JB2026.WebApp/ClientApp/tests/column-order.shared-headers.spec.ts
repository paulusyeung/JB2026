import { expect, test, type Page } from '@playwright/test'

// ─── fixtures ────────────────────────────────────────────────────────────────

type ViewCase = {
  path: string
  viewId: string
  label: string
}

const VIEWS: ViewCase[] = [
  { path: '/app/stock', viewId: 'stock', label: 'Stock' },
  { path: '/app/crm/people', viewId: 'crm-people', label: 'CRM people' },
  { path: '/app/crm/companies', viewId: 'crm-companies', label: 'CRM companies' },
  { path: '/app/crm/opportunities', viewId: 'crm-opportunities', label: 'CRM opportunities' },
  { path: '/app/crm/staff-members', viewId: 'staff-members', label: 'CRM staff members' },
  { path: '/app/crm/tasks', viewId: 'crm-tasks', label: 'CRM tasks' },
  { path: '/app/admin/user', viewId: 'admin-user', label: 'Admin users' },
  { path: '/app/admin/customer', viewId: 'admin-customer', label: 'Admin customers' },
  { path: '/app/admin/supplier', viewId: 'admin-supplier', label: 'Admin suppliers' },
  { path: '/app/billing/clients', viewId: 'billing-clients', label: 'Billing clients' },
  { path: '/app/billing/invoices', viewId: 'billing-invoices', label: 'Billing invoices' },
  { path: '/app/billing/statement', viewId: 'billing-statement', label: 'Billing statement' },
  { path: '/app/job-order/schedule/pending', viewId: 'pending-schedule', label: 'Schedule pending' },
  { path: '/app/job-order/schedule/completed', viewId: 'completed-schedule', label: 'Schedule completed' },
  { path: '/app/job-order/schedule/packing', viewId: 'packing-schedule', label: 'Schedule packing' },
  { path: '/app/job-order/reports/exceptional', viewId: 'exceptional-report', label: 'Exceptional report' },
]

const STORAGE_PREFIX = 'view-settings-'

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'shared-column-order-token')
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
    const method = request.method()

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({
        json: { userId: 'test', username: 'admin', displayName: 'Administrator', role: 'Admin' },
      })
      return
    }

    if (path === '/api/v2/settings') {
      await route.fulfill({
        json: {
          companyName: '',
          timeZone: '',
          currencyCode: '',
          enableLegacyFallback: false,
          ownerName: '',
          nextOrderNumber: '',
          nextProductNumber: '',
        },
      })
      return
    }

    if (path.startsWith('/api/v2/user-preferences')) {
      if (method === 'PUT') {
        preferenceWrites.push(request.postData() ?? '')
        await route.fulfill({ json: { metadata: null } })
        return
      }
      await route.fulfill({ status: 404, json: null })
      return
    }

    if (path === '/api/v2/billing/invoices') {
      await route.fulfill({ json: { invoices: [] } })
      return
    }

    if (path === '/api/v2/billing/clients') {
      await route.fulfill({ json: { clients: [] } })
      return
    }

    if (path === '/api/v2/billing/groups') {
      await route.fulfill({ json: { groups: [] } })
      return
    }

    if (method === 'GET') {
      await route.fulfill({ json: [] })
      return
    }

    await route.fulfill({ json: {} })
  })

  return { preferenceWrites }
}

async function headerKeys(page: Page) {
  return page.locator('.reorderable-th').evaluateAll((cells) => cells.map((cell) => cell.getAttribute('data-column-key') ?? ''))
}

async function dragHeader(page: Page, sourceIndex: number, targetIndex: number) {
  const headers = page.locator('.reorderable-th')
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

async function readStoredOrder(page: Page, viewId: string): Promise<string[] | null> {
  return page.evaluate((key) => {
    const raw = localStorage.getItem(key)
    if (!raw) {
      return null
    }
    const parsed = JSON.parse(raw) as { columnOrder?: unknown }
    return Array.isArray(parsed.columnOrder) ? (parsed.columnOrder as string[]) : null
  }, `${STORAGE_PREFIX}${viewId}`)
}

// ─── probe: every in-scope view renders reorderable headers ──────────────────

test('every in-scope list view renders reorderable headers', async ({ page }) => {
  test.slow()
  const failures: string[] = []
  const pageErrors: string[] = []

  page.on('pageerror', (error) => pageErrors.push(error.message))

  for (const view of VIEWS) {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto(view.path)
    await page.waitForTimeout(600)

    const count = await page.locator('.reorderable-th').count()
    if (count < 2) {
      failures.push(`${view.label}: ${count} reorderable headers`)
    }

    await page.getByRole('button', { name: /columns/i }).click()
    const resetVisible = await page.getByText('Reset Column Order').count()
    if (resetVisible === 0) {
      failures.push(`${view.label}: reset menu item missing`)
    }
    await page.keyboard.press('Escape')
  }

  expect(failures, failures.join('\n')).toEqual([])
  expect(pageErrors, pageErrors.join('\n')).toEqual([])
})

// ─── behaviour: drag, persistence, reset ─────────────────────────────────────

for (const view of VIEWS) {
  test(`${view.label}: headers reorder, persist and reset`, async ({ page }) => {
    await injectFakeSession(page)
    const { preferenceWrites } = await mockApi(page)
    await page.goto(view.path)

    const headers = page.locator('.reorderable-th')
    await expect(headers.first()).toBeVisible()

    const initial = await headerKeys(page)
    expect(initial.length).toBeGreaterThan(1)
    // Every header is tagged and none loses its key.
    expect(initial.every((key) => key.length > 0)).toBe(true)

    // Drop onto a nearby column: wide tables scroll, far targets land off-screen.
    const targetIndex = Math.min(2, initial.length - 1)
    await dragHeader(page, 0, targetIndex)
    const reordered = await headerKeys(page)
    expect(reordered).not.toEqual(initial)
    expect(reordered[targetIndex]).toBe(initial[0])

    // The full order — including hidden columns — is what gets stored.
    const stored = await readStoredOrder(page, view.viewId)
    expect(stored, `${view.label}: columnOrder was not written to local storage`).not.toBeNull()
    expect(stored!.indexOf(initial[0])).toBe(reordered.indexOf(initial[0]))

    await page.waitForTimeout(800)
    expect(preferenceWrites.some((body) => body.includes('columnOrder'))).toBe(true)

    await page.reload()
    await expect(page.locator('.reorderable-th').first()).toBeVisible()
    expect(await headerKeys(page)).toEqual(reordered)

    await page.getByRole('button', { name: /columns/i }).click()
    const menuItems = page.locator('.v-overlay__content .v-list-item-title')
    await expect(menuItems).toHaveCount(await page.locator('.v-overlay__content .v-list-item').count())
    const menuTitles = (await menuItems.allTextContents()).map((title) => title.trim())

    // The menu lists every column; the visible ones must appear in the custom order.
    const headerTitles = (await page.locator('.reorderable-th .v-data-table-header__content').allTextContents())
      .map((title) => title.trim())
      .filter((title) => title.length > 0)
    expect(menuTitles.filter((title) => headerTitles.includes(title))).toEqual(headerTitles)
    expect(menuTitles[menuTitles.length - 1]).toBe('Reset Column Order')

    await page.getByText('Reset Column Order').click()
    await page.waitForTimeout(300)
    expect(await headerKeys(page)).toEqual(initial)
  })
}

// ─── selection column stays put ──────────────────────────────────────────────

test('the select-all column is never draggable', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)
  // Stock lists keep selection behind the Checkbox menu toggle; turn it on.
  await page.goto('/app/stock')
  await page.getByRole('button', { name: /checkbox/i }).click()

  const first = page.locator('.reorderable-th').first()
  await expect(first).toHaveAttribute('data-column-key', 'data-table-select')
  await expect(first).toHaveAttribute('draggable', 'false')
})

// ─── rendering baseline: the component must not change Vuetify's header look ──

test('reorderable headers keep the original Vuetify header rendering', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)
  await page.goto('/app/stock')
  // Selection lives behind the Checkbox toggle, as it does in the shipped views.
  await page.getByRole('button', { name: /checkbox/i }).click()

  const first = page.locator('.reorderable-th').nth(1)
  const classes = (await first.getAttribute('class'))!.split(/\s+/).filter(Boolean)

  expect(classes).toEqual(
    expect.arrayContaining([
      'v-data-table__td',
      'v-data-table__th',
      'v-data-table__th--sticky',
      'reorderable-th',
      'v-data-table-column--align-start',
    ]),
  )

  // The selection header is the padding-free one.
  const select = page.locator('.reorderable-th[data-column-key="data-table-select"]')
  await expect(select).toHaveClass(/v-data-table-column--no-padding/)

  // Sticky positioning, the header content wrapper and width survive.
  const sticky = await first.evaluate((cell) => getComputedStyle(cell).position)
  expect(['sticky', 'relative']).toContain(sticky)
  await expect(first.locator('.v-data-table-header__content')).toBeVisible()

  // A sortable column still sorts and shows Vuetify's sort affordance.
  const sortable = page.locator('.reorderable-th[data-column-key="stockNumber"]')
  await sortable.click()
  await expect(page.locator('.v-data-table__th--sorted').first()).toBeVisible()
  await expect(sortable.locator('.v-data-table-header__sort-icon').first()).toBeVisible()
})

// ─── icon header cells ────────────────────────────────────────────────────────

const ICON_HEADERS: { path: string; key: string; label: string; titled: boolean }[] = [
  // The stock attachment header had no tooltip; the synced indicators did.
  { path: '/app/stock', key: 'attachment', label: 'Stock', titled: true },
  { path: '/app/crm/people', key: 'synced', label: 'CRM people', titled: true },
  { path: '/app/crm/companies', key: 'synced', label: 'CRM companies', titled: true },
  { path: '/app/job-order/schedule/pending', key: 'urgencyLevel', label: 'Schedule pending', titled: false },
]

for (const icon of ICON_HEADERS) {
  test(`${icon.label}: the ${icon.key} header keeps its icon and title`, async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto(icon.path)

    const cell = page.locator(`.reorderable-th[data-column-key="${icon.key}"]`)
    await expect(cell).toBeVisible()

    // The icon carries the accessible name; the cell text stays screen-reader only.
    const iconEl = cell.locator('.v-icon')
    await expect(iconEl).toBeVisible()
    if (icon.titled) {
      expect((await iconEl.getAttribute('title'))?.length ?? 0).toBeGreaterThan(0)
    }
    await expect(cell.locator('.sr-only')).toHaveText(/\S/)

    // Icon cells are still reorderable.
    await expect(cell).toHaveAttribute('draggable', 'true')
  })
}

// ─── views without a registered preference id stay local-only ─────────────────

test('a view without a registered object id makes no preference request', async ({ page }) => {
  const preferenceRequests: string[] = []
  const pageErrors: string[] = []

  page.on('pageerror', (error) => pageErrors.push(error.message))
  page.on('request', (request) => {
    if (request.url().includes('/api/v2/user-preferences')) {
      preferenceRequests.push(`${request.method()} ${request.url()}`)
    }
  })

  await injectFakeSession(page)
  await mockApi(page)
  // QuotationsView has no registered object id, so it stays localStorage-only.
  await page.goto('/app/quotations')
  await page.locator('.v-data-table__th').first().waitFor()

  expect(preferenceRequests).toEqual([])
  expect(pageErrors).toEqual([])
})
