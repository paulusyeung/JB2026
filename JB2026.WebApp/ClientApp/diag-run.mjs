import pkg from '/home/paulus/Projects/JB2026/JB2026.WebApp/ClientApp/node_modules/.pnpm/playwright@1.58.2/node_modules/playwright/index.js'
const { chromium } = pkg

const base = 'http://localhost:5173/app/'
const browser = await chromium.launch()
const page = await browser.newPage()

const errors = []
page.on('console', (msg) => { if (msg.type() === 'error') errors.push('CONSOLE: ' + msg.text()) })
page.on('pageerror', (err) => errors.push('PAGEERR: ' + (err.stack || err.message)))

await page.addInitScript(() => {
  localStorage.setItem('jb2026.accessToken', 'smoke-test-fake-token')
  localStorage.setItem('jb2026.sessionProfile', JSON.stringify({ userId: 'test', username: 'admin', displayName: 'Smoke', email: 's@t.local', role: 'Admin' }))
})
await page.route('**/api/v2/**', (route) => {
  const url = route.request().url()
  if (url.includes('user-profiles/me')) return route.fulfill({ json: { userId: 'a', username: 'admin', displayName: 'Admin', role: 'Admin' } })
  if (url.includes('crm/tasks/status-options')) return route.fulfill({ json: [{ value: '', label: 'No Status' }, { value: 'TODO', label: 'To do' }, { value: 'IN_PROGRESS', label: 'In progress' }, { value: 'COMPLETED', label: 'Done' }] })
  if (url.includes('crm/')) return route.fulfill({ json: [] })
  if (url.includes('feature-flags')) return route.fulfill({ json: { flags: [] } })
  return route.fulfill({ json: {} })
})

await page.goto(base + 'crm/tasks', { waitUntil: 'networkidle', timeout: 30000 }).catch(e => errors.push('goto: ' + e.message))
await page.waitForTimeout(3000)
console.log('URL:', page.url())
console.log('=== buttons present ===')
const btns = await page.locator('button').allInnerTexts()
console.log(btns.join(' | '))
console.log('=== New Task visible? ===', await page.getByRole('button', { name: /New Task/i }).count())

await page.getByRole('button', { name: /New Task/i }).first().click({ timeout: 8000 }).catch(e => errors.push('newtask: ' + e.message))
await page.waitForTimeout(4000)

console.log('ck-editor count:', await page.locator('.ck-editor').count())
console.log('ck-editable count:', await page.locator('.ck-editor__editable').count())
console.log('ck-toolbar count:', await page.locator('.ck-toolbar').count())
console.log('=== ERRORS ===')
console.log(errors.slice(0, 12).join('\n') || 'NO ERRORS')
await browser.close()
