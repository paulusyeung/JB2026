import { chromium } from '@playwright/test'

const browser = await chromium.launch({ headless: true })
const page = await browser.newPage()

page.on('console', (msg) => console.log('[console]', msg.type(), msg.text()))
page.on('pageerror', (err) => console.log('[pageerror]', err.message))

await page.addInitScript(() => {
  localStorage.setItem('jb2026.accessToken', 'probe-token')
  localStorage.setItem('jb2026.sessionProfile', JSON.stringify({ userId: 'probe', displayName: 'Probe User', email: 'probe@test.local' }))
})

await page.route('**/api/v2/sml/invoice-stats**', (route) =>
  route.fulfill({
    json: {
      generatedAtUtc: '2026-04-07T00:00:00Z',
      rowCount: 2,
      rows: [
        { customerName: 'SML DH', invoiceNumber: '66200', purchaseOrder: '5910444941', productCode: '8MM', qty: 4944, unit: 'pcs', price: 0.16, amount: 791.04, year: 2015, month: 1 },
        { customerName: 'SML DH', invoiceNumber: 'DH1', purchaseOrder: '8110522367', productCode: 'THEU', qty: 4400, unit: 'pcs', price: 0.7742, amount: 3406.48, year: 2016, month: 2 },
      ],
    },
  }),
)

await page.goto('http://127.0.0.1:5174/app/job-order/sml/invoice-stats', { waitUntil: 'domcontentloaded', timeout: 120000 })
await page.waitForTimeout(8000)

const data = await page.evaluate(() => {
  const heading = document.querySelector('h3')?.textContent || null
  const rowsText = [...document.querySelectorAll('div')].map((d) => d.textContent || '').find((t) => t.includes('Rows:')) || null
  const pivot = document.querySelector('web-pivot-table')
  if (!pivot) return { heading, rowsText, pivotFound: false }
  const anyPivot = pivot
  return {
    heading,
    rowsText,
    pivotFound: true,
    dataElementId: pivot.getAttribute('data-element-id'),
    text: (pivot.textContent || '').trim().slice(0, 200),
    shadowText: (anyPivot.shadowRoot?.textContent || '').trim().slice(0, 500),
    hasSetData: typeof anyPivot.setData === 'function',
    hasSetOptions: typeof anyPivot.setOptions === 'function',
    optionsAttr: pivot.getAttribute('options'),
  }
})

console.log('[probe-mocked]', JSON.stringify(data, null, 2))
await page.screenshot({ path: 'pivot_probe_mocked.png', fullPage: true })
await browser.close()
