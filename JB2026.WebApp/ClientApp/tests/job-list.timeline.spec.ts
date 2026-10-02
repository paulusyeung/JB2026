import { expect, test, type Page } from '@playwright/test'

const ORDER_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const ORDER_NUMBER = '170446'
const JOB_NUMBER = '1'

// Mirrors the API contract: DeliveredOn ascending, then MessageBody ascending.
// A job's timeline is its own pushes plus the job 0 push. Job 0 was migrated into job 1 and has
// no JobOrder row, so it can never be opened by itself. A neighbouring job's pushes are excluded
// because one stage fans out across jobs (order 170195 "JB5 已排單" wrote 14 rows, one per job).
const TIMELINE_ENTRIES = [
  {
    fcmHistoryId: 'cccccccc-cccc-cccc-cccc-ccccccccccc1',
    deliveredOn: '2026-04-03T09:05:00',
    messageTitle: 'JB5 新增訂單',
    messageBody: `${ORDER_NUMBER}-0: Acme Ltd`,
    topic: 'Device',
  },
  {
    fcmHistoryId: 'cccccccc-cccc-cccc-cccc-ccccccccccc2',
    deliveredOn: '2026-04-05T14:22:31',
    messageTitle: 'JB5 有紙',
    messageBody: `${ORDER_NUMBER}-${JOB_NUMBER}: Acme Ltd`,
    topic: 'Device',
  },
  {
    fcmHistoryId: 'cccccccc-cccc-cccc-cccc-ccccccccccc3',
    deliveredOn: '2026-04-05T14:30:00',
    messageTitle: 'JB5 有紙',
    messageBody: `${ORDER_NUMBER}-${JOB_NUMBER}: Acme Ltd`,
    topic: 'Device',
  },
]

// A neighbouring job of the same order. Only job 0 and the selected job belong in the timeline.
const NEIGHBOUR_JOB_ENTRY = {
  fcmHistoryId: 'cccccccc-cccc-cccc-cccc-cccccccccccd',
  deliveredOn: '2026-04-04T11:11:11',
  messageTitle: 'JB5 已排單',
  messageBody: `${ORDER_NUMBER}-2: Acme Ltd`,
  topic: 'Device',
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'job-timeline-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Timeline Test', role: 'Admin', email: 'timeline@test.local' }),
    )
  })
}

type MockApiState = {
  timelineRequestCount: number
}

async function mockApi(page: Page, timeline: unknown[] = TIMELINE_ENTRIES): Promise<MockApiState> {
  const state: MockApiState = {
    timelineRequestCount: 0,
  }

  const jobOrder = {
    orderId: ORDER_ID,
    orderType: 1,
    orderNumber: ORDER_NUMBER,
    jobNumber: JOB_NUMBER,
    customerName: 'Acme Ltd',
    customerRef: 'TC-001',
    orderTitle: 'Timeline Test Order',
    productCode: 'PROD-001',
    productStyle: '',
    outputRef: '',
    invoiceRef: '',
    invoiceAmount: 0,
    attachmentProductCount: 0,
    attachmentCustomerCount: 0,
    orderedBy: 'tester',
    orderedOn: '2026-04-01T00:00:00',
    requiredOn: '2026-06-01T00:00:00',
    qty: 100,
    paymentTerms: 'Net 30',
    remarks: '',
    status: 1,
    createdBy: 'test',
    createdOn: '2026-04-01T00:00:00',
    modifiedBy: null,
    modifiedOn: null,
  }

  // JobDetail has no job number, so the form falls back to the order number for the subtitle.
  const jobDetail = {
    ...jobOrder,
    paymentTerms: 'Net 30',
    remarks: '',
    productDetails: '',
    styleTitles: [],
    attachments: [],
    step1Status: 0,
    step2Status: 0,
    step3Status: 0,
  }

  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/user-preferences/**', (route) =>
    route.request().method() === 'PUT'
      ? route.fulfill({ json: { metadata: null } })
      : route.fulfill({ status: 404, json: null }),
  )

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname

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

    // Detail endpoint used by the job list editor when a row is opened.
    if (path === `/api/v2/jobs/${ORDER_ID}` && request.method() === 'GET') {
      await route.fulfill({ json: jobDetail })
      return
    }

    if (path === `/api/v2/job-orders/${ORDER_ID}/timeline` && request.method() === 'GET') {
      state.timelineRequestCount += 1
      await route.fulfill({ json: timeline })
      return
    }

    await route.fulfill({ status: 404, body: 'Not found' })
  })

  return state
}

async function selectOnlyRow(page: Page) {
  await page.getByRole('button', { name: 'Checkbox' }).click()
  await page.locator('.v-data-table__tr').first().getByRole('checkbox').check()
}

function timelineDialog(page: Page) {
  return page.locator('.job-timeline-dialog')
}

function jobFormDialog(page: Page) {
  return page.getByRole('dialog').filter({ hasText: 'Job Order' })
}

test('Timeline button is disabled until exactly one row is selected', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Timeline Test Order')).toBeVisible()

  const timelineButton = page.getByRole('button', { name: 'Timeline' })
  await expect(timelineButton).toBeDisabled()

  await selectOnlyRow(page)
  await expect(timelineButton).toBeEnabled()
})

test('timeline dialog lists FCMHistory rows oldest first with delivery time and message body', async ({ page }) => {
  await injectFakeSession(page)
  const apiState = await mockApi(page)

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Timeline Test Order')).toBeVisible()

  await selectOnlyRow(page)
  await page.getByRole('button', { name: 'Timeline' }).click()

  const dialog = timelineDialog(page)
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText(`${ORDER_NUMBER}-${JOB_NUMBER}: Acme Ltd`).first()).toBeVisible()
  await expect.poll(() => apiState.timelineRequestCount).toBe(1)

  // DeliveredOn is rendered with the global date format plus minutes and seconds.
  const timestamps = await dialog.locator('.job-timeline__time').allTextContents()
  expect(timestamps).toHaveLength(3)
  for (const timestamp of timestamps) {
    expect(timestamp).toMatch(/04\/0[35]\/2026/)
    expect(timestamp).toMatch(/\d{1,2}:\d{2}:\d{2}/)
  }

  // Rows arrive oldest first, so 09:05:00 precedes 14:22:31 regardless of the AM/PM suffix.
  expect(timestamps[0]).toMatch(/(09:05:00 AM|09:05:00)/)
  expect(timestamps[1]).toMatch(/(02:22:31 PM|14:22:31)/)
  expect(timestamps[2]).toMatch(/(02:30:00 PM|14:30:00)/)

  const bodies = await dialog.locator('.job-timeline__message').allTextContents()
  expect(bodies).toEqual([
    `${ORDER_NUMBER}-0: Acme Ltd`,
    `${ORDER_NUMBER}-${JOB_NUMBER}: Acme Ltd`,
    `${ORDER_NUMBER}-${JOB_NUMBER}: Acme Ltd`,
  ])
})

test('timeline shows job 0 and the selected job, but not a neighbouring job', async ({ page }) => {
  await injectFakeSession(page)
  // The server keeps job 0 plus the selected job, so hand the client that post-filter result for
  // job 1 and assert the neighbouring job is absent.
  await mockApi(
    page,
    [NEIGHBOUR_JOB_ENTRY, ...TIMELINE_ENTRIES].filter((entry) => {
      const jobNumber = Number(entry.messageBody.slice(`${ORDER_NUMBER}-`.length).split(':')[0])
      return jobNumber === 0 || jobNumber === Number(JOB_NUMBER)
    }),
  )

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Timeline Test Order')).toBeVisible()

  await selectOnlyRow(page)
  await page.getByRole('button', { name: 'Timeline' }).click()

  const dialog = timelineDialog(page)
  await expect(dialog).toBeVisible()
  await expect(dialog.locator('.job-timeline__message')).toHaveCount(3)
  // Job 0 was migrated into job 1 and has no JobOrder row, so this push is only reachable here.
  await expect(dialog.getByText(`${ORDER_NUMBER}-0: Acme Ltd`)).toHaveCount(1)
  await expect(dialog.getByText(`${ORDER_NUMBER}-2: Acme Ltd`)).toHaveCount(0)
})

test('timeline dialog is movable and closes from the [X] button', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page)

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Timeline Test Order')).toBeVisible()

  await selectOnlyRow(page)
  await page.getByRole('button', { name: 'Timeline' }).click()

  const dialog = timelineDialog(page)
  await expect(dialog).toBeVisible()

  const card = page.locator('.job-timeline-dialog')
  // Wait out the dialog enter transition so the title bar reports a stable hit box.
  await expect(page.locator('.v-overlay__content').filter({ has: card })).not.toHaveClass(/dialog-transition/)

  const handle = card.locator('.v-card-title')
  await expect(handle).toHaveCSS('cursor', 'move')

  const box = (await handle.boundingBox())!
  const startX = box.x + box.width / 2
  const startY = box.y + box.height / 2

  await page.mouse.move(startX, startY)
  await page.mouse.down()
  await page.mouse.move(startX + 8, startY, { steps: 4 })
  await page.mouse.move(startX + 120, startY + 60, { steps: 12 })
  await page.mouse.up()

  await expect.poll(() => card.evaluate((element) => (element as HTMLElement).style.transform || '')).toBe('translate(120px, 60px)')

  await handle.getByRole('button', { name: 'Close' }).click()
  await expect(dialog).not.toBeVisible()
})

test('timeline dialog shows the empty state when no FCMHistory row matches', async ({ page }) => {
  await injectFakeSession(page)
  await mockApi(page, [])

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Timeline Test Order')).toBeVisible()

  await selectOnlyRow(page)
  await page.getByRole('button', { name: 'Timeline' }).click()

  const dialog = timelineDialog(page)
  await expect(dialog.getByText('No timeline entries recorded for this job number.')).toBeVisible()
})

test('job form Timeline button opens the timeline for the edited job', async ({ page }) => {
  await injectFakeSession(page)
  const apiState = await mockApi(page)

  await page.goto('/app/job-order/job-list')
  await expect(page.getByText('Timeline Test Order')).toBeVisible()

  await page.getByText('Timeline Test Order').click()
  const form = jobFormDialog(page)
  await expect(form.getByText('Edit Job Order')).toBeVisible()

  const timelineButton = form.getByRole('button', { name: 'Timeline' })
  await expect(timelineButton).toBeEnabled()
  await timelineButton.click()

  const dialog = timelineDialog(page)
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText(`${ORDER_NUMBER}-${JOB_NUMBER}: Acme Ltd`).first()).toBeVisible()
  await expect(dialog.locator('.job-timeline__message')).toHaveCount(3)

  // The form passes the edited job's own order id, so the endpoint is hit exactly once.
  await expect.poll(() => apiState.timelineRequestCount).toBe(1)
})

test('job form Timeline button is disabled for an unsaved job', async ({ page }) => {
  await injectFakeSession(page)
  const apiState = await mockApi(page)

  await page.goto('/app/jobs')
  await page.getByRole('button', { name: 'New' }).click()

  const form = jobFormDialog(page)
  await expect(form.getByText('New Job Order')).toBeVisible()

  await expect(form.getByRole('button', { name: 'Timeline' })).toBeDisabled()
  expect(apiState.timelineRequestCount).toBe(0)
})