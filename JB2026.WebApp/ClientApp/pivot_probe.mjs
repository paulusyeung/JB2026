import { chromium } from '@playwright/test'

const browser = await chromium.launch({ headless: true })
const page = await browser.newPage()

page.on('console', (msg) => {
  console.log('[console]', msg.type(), msg.text())
})
page.on('pageerror', (err) => {
  console.log('[pageerror]', err.message)
})

await page.addInitScript(() => {
  localStorage.setItem('jb2026.accessToken', 'probe-token')
  localStorage.setItem('jb2026.sessionProfile', JSON.stringify({ userId: 'probe', displayName: 'Probe User', email: 'probe@test.local' }))
})

await page.goto('http://127.0.0.1:5174/app/job-order/sml/invoice-stats', { waitUntil: 'domcontentloaded', timeout: 120000 })
await page.waitForTimeout(8000)

const data = await page.evaluate(() => {
  const heading = document.querySelector('h3')?.textContent || null
  const pivot = document.querySelector('web-pivot-table')
  if (!pivot) return { heading, pivotFound: false }
  const anyPivot = pivot
  return {
    heading,
    pivotFound: true,
    dataElementId: pivot.getAttribute('data-element-id'),
    text: (pivot.textContent || '').trim(),
    shadowText: (anyPivot.shadowRoot?.textContent || '').trim().slice(0, 500),
    hasSetData: typeof anyPivot.setData === 'function',
    hasSetOptions: typeof anyPivot.setOptions === 'function',
    optionsAttr: pivot.getAttribute('options'),
  }
})

console.log('[probe]', JSON.stringify(data, null, 2))
await page.screenshot({ path: 'pivot_probe.png', fullPage: true })
await browser.close()
