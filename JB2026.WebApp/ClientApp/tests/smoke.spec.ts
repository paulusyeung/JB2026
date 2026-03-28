import { test, expect } from '@playwright/test'

test('login screen renders and supports development defaults', async ({ page }) => {
  await page.goto('/app/login')

  await expect(page.getByRole('heading', { name: 'API-authenticated sign in' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Use Dev Defaults' })).toBeVisible()
})