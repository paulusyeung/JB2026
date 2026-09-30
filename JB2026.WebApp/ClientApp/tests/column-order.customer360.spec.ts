import { expect, test, type Locator, type Page } from '@playwright/test'

// ─── fixtures ────────────────────────────────────────────────────────────────

const CUSTOMER_360_PATH = '/app/crm/customer-360'
const STORAGE_PREFIX = 'view-settings-'
const COMPANY_NAME = 'Acme Industrial'

type TableCase = {
  tab: string
  tableClass: string
  viewId: string
  label: string
  firstKeys: string[]
  /** Column index the first column is dragged onto; it has to be a reorderable, on-screen cell. */
  dropTarget: number
}

const TABLES: TableCase[] = [
  {
    tab: 'Job Orders',
    tableClass: 'job-orders-table',
    viewId: 'crm-customer360-job-orders',
    label: 'Job orders',
    firstKeys: ['orderType', 'ln', 'orderNumber'],
    // Index 1 is the pinned row-number column, so the drag has to reach past it.
    dropTarget: 2,
  },
  {
    tab: 'Invoices',
    tableClass: 'invoices-table',
    viewId: 'crm-customer360-invoices',
    label: 'Invoices',
    firstKeys: ['invoiceNumber', 'clientName', 'invoiceDate'],
    dropTarget: 1,
  },
  {
    tab: 'Opportunities',
    tableClass: 'opportunities-table',
    viewId: 'crm-customer360-opportunities',
    label: 'Opportunities',
    firstKeys: ['name', 'stage', 'closeDate'],
    dropTarget: 1,
  },
  {
    tab: 'Tasks',
    tableClass: 'tasks-table',
    viewId: 'crm-customer360-tasks',
    label: 'Tasks',
    firstKeys: ['title', 'status', 'body'],
    dropTarget: 1,
  },
  {
    tab: 'Files',
    tableClass: 'files-table',
    viewId: 'crm-customer360-files',
    label: 'Files',
    firstKeys: ['archiveSerialNumber', 'correspondentName', 'title'],
    dropTarget: 1,
  },
  {
    tab: 'Emails',
    tableClass: 'emails-table',
    viewId: 'crm-customer360-emails',
    label: 'Emails',
    firstKeys: ['sender', 'subject', 'date'],
    dropTarget: 1,
  },
]


// Every table renders a placeholder instead of its grid while it holds no rows,
// so each one gets a single row.
const JOB_ORDER = {
  orderId: 'jo-1',
  orderType: 0,
  orderNumber: 'JB260101',
  jobNumber: '01',
  customerName: COMPANY_NAME,
  customerRef: 'REF-001',
  orderTitle: 'Banner Print',
  productStyle: 'Woven',
  invoiceRef: '',
  invoiceAmount: 1200,
  attachmentProductCount: 1,
  attachmentCustomerCount: 0,
  orderedBy: 'admin',
  orderedOn: '2026-01-15T00:00:00Z',
  requiredOn: '2026-02-01T00:00:00Z',
  completedOn: null,
  status: 1,
  createdBy: 'admin',
  createdOn: '2026-01-15T00:00:00Z',
  modifiedBy: 'admin',
  modifiedOn: '2026-01-15T00:00:00Z',
}

const INVOICE = {
  invoiceNumber: 'INV-001',
  clientName: COMPANY_NAME,
  invoiceDate: '2026-01-20',
  status: 'SENT',
  amount: 1200,
  dueDate: '2026-02-19',
}

const OPPORTUNITY = {
  id: 'opp-1',
  companyId: 'c-1',
  name: 'Banner Rollout',
  stage: 'QUALIFICATION',
  closeDate: '2026-03-01',
  amount: 5000,
  company: COMPANY_NAME,
  pointOfContact: 'Jane Doe',
  owner: 'admin',
  createdOn: '2026-01-10T00:00:00Z',
  createdBy: 'admin',
  updatedOn: '2026-01-11T00:00:00Z',
  updatedBy: 'admin',
}

const TASK = {
  id: 'task-1',
  title: 'Follow up quote',
  status: 'IN_PROGRESS',
  body: '<p>Call the customer</p>',
  dueDate: '2026-02-01',
  assignee: 'admin',
  relations: [{ id: 'c-1', type: 'COMPANY' }],
  createdOn: '2026-01-10T00:00:00Z',
  createdBy: 'admin',
  updatedOn: '2026-01-11T00:00:00Z',
  updatedBy: 'admin',
}

// The company record carries the opportunity and task lists the tab placeholders check.
const COMPANY = {
  id: 'c-1',
  name: COMPANY_NAME,
  people: [],
  opportunities: [OPPORTUNITY],
  tasks: [TASK],
}

const FILE = {
  id: 11,
  title: 'Quotation.pdf',
  archiveSerialNumber: 'ARC-11',
  correspondentName: COMPANY_NAME,
  ownerName: 'admin',
  noteCount: 2,
  documentTypeName: 'Quotation',
  created: '2026-01-12T00:00:00Z',
  pageCount: 3,
  isSharedByRequester: false,
  mimeType: 'application/pdf',
  tags: [],
}

const EMAIL = {
  id: 'mail-1',
  folder: 'Inbox',
  sender: 'Jane Doe <jane@acme.test>',
  subject: 'Re: banner artwork',
  date: '2026-01-14T09:30:00Z',
  size: 20480,
  hasAttachment: true,
}

const TABLE_ROWS: Record<string, unknown> = {
  '/api/v2/job-orders': [JOB_ORDER],
  '/api/v2/billing/invoices': { invoices: [INVOICE] },
  '/api/v2/crm/opportunities': [OPPORTUNITY],
  '/api/v2/crm/tasks': [TASK],
  '/api/v2/email/search': [EMAIL],
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'customer360-column-order-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Test User', role: 'Admin', email: 'test@test.local' }),
    )
  })
}

async function mockApi(page: Page) {
  const preferenceWrites: string[] = []
  const pageErrors: string[] = []

  page.on('pageerror', (error) => pageErrors.push(error.message))

  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname
    const method = request.method()

    if (path === '/api/v2/crm/companies') {
      await route.fulfill({ json: [COMPANY] })
      return
    }

    if (path.startsWith('/api/v2/crm/companies/')) {
      if (path.endsWith('/files')) {
        await route.fulfill({ json: { documents: [FILE] } })
        return
      }
      await route.fulfill({ json: COMPANY })
      return
    }

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({ json: { userId: 'test', username: 'admin', displayName: 'Administrator', role: 'Admin' } })
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

    // Every remaining list request (job orders, invoices, opportunities, tasks, files, emails).
    if (method === 'GET') {
      await route.fulfill({ json: TABLE_ROWS[path] ?? [] })
      return
    }

    await route.fulfill({ json: {} })
  })

  return { preferenceWrites, pageErrors }
}

async function selectCompany(page: Page) {
  await page.locator('.v-autocomplete input').first().click()
  await page.getByRole('option', { name: COMPANY_NAME }).click()
  await page.locator('.company-info').waitFor()
}

function headersOf(page: Page, table: TableCase) {
  return page.locator(`.${table.tableClass} .reorderable-th`)
}

async function openTable(page: Page, table: TableCase) {
  await page.goto(CUSTOMER_360_PATH)
  await selectCompany(page)
  await page.getByRole('tab', { name: table.tab }).click()
  const headers = headersOf(page, table)
  await headers.first().waitFor()
  await waitForTableInPlace(page, table)
  return headers
}

/**
 * The tab window slides horizontally and the tables are wider than the pane, so a header that has
 * just appeared can still be measured mid-transition. Wait until the first header sits inside the
 * table's own scroll viewport before anything is read or dragged.
 */
async function waitForTableInPlace(page: Page, table: TableCase) {
  await expect
    .poll(
      async () => {
        const cell = await headersOf(page, table).first().boundingBox()
        const wrapper = await page.locator(`.${table.tableClass} .v-table__wrapper`).boundingBox()
        if (!cell || !wrapper) return Number.MAX_SAFE_INTEGER
        return Math.abs(cell.x - wrapper.x)
      },
      { timeout: 5000 },
    )
    .toBeLessThan(2)
}

async function headerKeys(headers: Locator) {
  return headers.evaluateAll((cells) => cells.map((cell) => cell.getAttribute('data-column-key') ?? ''))
}

async function indexOfKey(headers: Locator, key: string) {
  const index = (await headerKeys(headers)).indexOf(key)
  expect(index, `no "${key}" header was rendered`).toBeGreaterThanOrEqual(0)
  return index
}

/** Mirrors `useColumnOrder`'s move: the source takes the target's position. */
function movedOnto(order: string[], sourceKey: string, targetKey: string) {
  const next = [...order]
  next.splice(next.indexOf(targetKey), 0, ...next.splice(next.indexOf(sourceKey), 1))
  return next
}

/**
 * Drags one header onto another and returns the key the header was actually dropped on, which is
 * `null` when the drop was refused.
 */
async function dragHeader(page: Page, headers: Locator, sourceIndex: number, targetIndex: number) {
  const source = (await headers.nth(sourceIndex).boundingBox())!
  const target = (await headers.nth(targetIndex).boundingBox())!

  await page.mouse.move(source.x + source.width / 2, source.y + source.height / 2)
  await page.mouse.down()
  await page.mouse.move(source.x + source.width / 2 + 8, source.y + source.height / 2, { steps: 4 })
  await page.mouse.move(target.x + target.width / 2, target.y + target.height / 2, { steps: 15 })
  await page.waitForTimeout(150)
  const dropTarget = page.locator('.reorderable-th--drop-target')
  const droppedOnKey = (await dropTarget.count()) ? await dropTarget.first().getAttribute('data-column-key') : null
  await page.mouse.up()
  await page.waitForTimeout(250)
  return droppedOnKey
}

function columnsButton(page: Page, table: TableCase) {
  return page.locator(`.v-tabs-window-item:has(.${table.tableClass}) .toolbar-bar button:has(.mdi-view-column)`)
}

function openOverlay(page: Page) {
  return page.locator('.v-overlay__content:visible')
}

async function readStoredOrder(page: Page, viewId: string): Promise<string[] | null> {
  return page.evaluate((key) => {
    const raw = localStorage.getItem(key)
    if (!raw) return null
    const parsed = JSON.parse(raw) as { columnOrder?: unknown }
    return Array.isArray(parsed.columnOrder) ? (parsed.columnOrder as string[]) : null
  }, `${STORAGE_PREFIX}${viewId}`)
}

// ─── one conversion per table ─────────────────────────────────────────────────

for (const table of TABLES) {
  test(`${table.label} table: headers reorder, persist per user and reset`, async ({ page }) => {
    await injectFakeSession(page)
    const { preferenceWrites, pageErrors } = await mockApi(page)
    const headers = await openTable(page, table)

    const initial = await headerKeys(headers)
    expect(initial.slice(0, 3)).toEqual(table.firstKeys)

    const droppedOn = await dragHeader(page, headers, 0, table.dropTarget)
    expect(droppedOn, `${table.label}: the drop was refused`).toBe(initial[table.dropTarget])

    const reordered = await headerKeys(headers)
    expect(reordered).toEqual(movedOnto(initial, initial[0], droppedOn!))
    expect(reordered[table.dropTarget]).toBe(initial[0])

    // The full order, hidden columns included, is what is persisted.
    const stored = await readStoredOrder(page, table.viewId)
    expect(stored, `${table.label}: columnOrder was not written to local storage`).toEqual(reordered)
    expect(stored!.length).toBe(initial.length)

    await page.waitForTimeout(800)
    expect(preferenceWrites.some((body) => body.includes('columnOrder'))).toBe(true)

    await page.reload()
    await selectCompany(page)
    await page.getByRole('tab', { name: table.tab }).click()
    await waitForTableInPlace(page, table)
    const reloaded = headersOf(page, table)
    expect(await headerKeys(reloaded)).toEqual(reordered)

    // The existing Columns menu keeps working and now ends with a reset entry.
    await columnsButton(page, table).click()
    const overlay = openOverlay(page)
    const menuTitles = (await overlay.locator('.v-list-item-title').allTextContents()).map((title) => title.trim())
    expect(menuTitles[menuTitles.length - 1]).toBe('Reset Column Order')

    // The menu lists the columns in the user's order.
    const visibleTitles = (await reloaded.locator('.v-data-table-header__content').allTextContents())
      .map((title) => title.trim())
      .filter((title) => title.length > 0)
    expect(menuTitles.filter((title) => visibleTitles.includes(title))).toEqual(visibleTitles)

    await overlay.getByText('Reset Column Order').click()
    await page.waitForTimeout(300)
    expect(await headerKeys(headersOf(page, table))).toEqual(initial)

    expect(pageErrors).toEqual([])
  })
}

// ─── pinned utility column ───────────────────────────────────────────────────

test('the job orders row-number column stays put', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)
  const table = TABLES[0]
  const headers = await openTable(page, table)

  const ln = headers.nth(1)
  expect(await ln.getAttribute('data-column-key')).toBe('ln')
  await expect(ln).toHaveAttribute('draggable', 'false')
  await expect(ln).not.toHaveClass(/reorderable-th--draggable/)

  // Dragging it does nothing, and no other column can be dropped onto it.
  const initial = await headerKeys(headers)
  await dragHeader(page, headers, 1, 2)
  await dragHeader(page, headers, 0, 1)
  expect(await headerKeys(headers)).toEqual(initial)
})

// ─── icon header cells ───────────────────────────────────────────────────────

const ICON_CELLS: { key: string; icon: string }[] = [
  { key: 'orderType', icon: 'mdi-tag-outline' },
  { key: 'status', icon: 'mdi-flag' },
  { key: 'attachProduct', icon: 'mdi-paperclip' },
  { key: 'attachCustomer', icon: 'mdi-paperclip' },
]

for (const cell of ICON_CELLS) {
  test(`job orders: the ${cell.key} header keeps its icon and stays reorderable`, async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    const table = TABLES[0]
    await openTable(page, table)

    const headers = headersOf(page, table)
    const header = headers.nth(await indexOfKey(headers, cell.key))
    await expect(header).toBeVisible()
    await expect(header.locator('.v-icon')).toHaveClass(new RegExp(cell.icon))
    expect((await header.locator('.v-icon').getAttribute('title'))?.length ?? 0).toBeGreaterThan(0)
    await expect(header.locator('.sr-only')).toHaveText(/\S/)
    await expect(header).toHaveAttribute('draggable', 'true')
  })
}

test('emails: the attachment header keeps its icon and tooltip text', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)
  const table = TABLES[5]
  await openTable(page, table)

  const headers = headersOf(page, table)
  const header = headers.nth(await indexOfKey(headers, 'hasAttachment'))
  await expect(header.locator('.v-icon')).toHaveClass(/mdi-paperclip/)
  expect((await header.locator('.v-icon').getAttribute('title'))?.length ?? 0).toBeGreaterThan(0)
  await expect(header).toHaveAttribute('draggable', 'true')
})

// ─── column visibility keeps working ─────────────────────────────────────────

test('hiding a column still works and is independent of the order', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)
  const table = TABLES[5]
  await openTable(page, table)
  const initial = await headerKeys(headersOf(page, table))

  await columnsButton(page, table).click()
  await openOverlay(page).locator('.v-list-item-title', { hasText: /^Date$/ }).click()
  await page.keyboard.press('Escape')
  await page.waitForTimeout(300)

  const hidden = await headerKeys(headersOf(page, table))
  expect(hidden).not.toContain('date')
  expect(hidden.length).toBe(initial.length - 1)

  // The stored order keeps the hidden column's place.
  const stored = await readStoredOrder(page, table.viewId)
  expect(stored).toContain('date')
  expect(stored!.indexOf('date')).toBe(initial.indexOf('date'))
})
