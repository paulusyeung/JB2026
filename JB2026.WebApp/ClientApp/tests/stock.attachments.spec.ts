import { expect, test, type Page } from '@playwright/test'

type ProductRow = {
  productId: string
  stockNumber: string
  productCode: string
  productName: string
  balance: number
  sellingPrice: number
  cogs: number
  remarks: string
  attachmentCount: number
  createdOn: string
  createdBy: string
  modifiedOn: string
  modifiedBy: string
}

type AttachmentRow = {
  attachmentId: string
  productId: string
  attachmentIndex: number
  fileName: string
  fileExtension: string
  fileSizeBytes: number
  existsOnDisk: boolean
}

const product: ProductRow = {
  productId: '11111111-1111-1111-1111-111111111111',
  stockNumber: 'CUS-CAT-0001',
  productCode: 'PAPER-A4',
  productName: 'A4 Art Paper 128gsm',
  balance: 100,
  sellingPrice: 12.5,
  cogs: 8.1,
  remarks: 'Core stock item',
  attachmentCount: 2,
  createdOn: '2026-03-31T00:00:00Z',
  createdBy: 'smoke',
  modifiedOn: '2026-04-01T00:00:00Z',
  modifiedBy: 'smoke',
}

async function injectFakeSession(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('jb2026.accessToken', 'stock-attachment-test-token')
    localStorage.setItem(
      'jb2026.sessionProfile',
      JSON.stringify({ userId: 'test', displayName: 'Stock Test', role: 'Admin', email: 'stock@test.local' }),
    )
  })
}

async function mockApi(page: Page) {
  const attachments: AttachmentRow[] = [
    {
      attachmentId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      productId: product.productId,
      attachmentIndex: 1,
      fileName: 'sample-image.jpg',
      fileExtension: '.jpg',
      fileSizeBytes: 1337,
      existsOnDisk: true,
    },
    {
      attachmentId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      productId: product.productId,
      attachmentIndex: 2,
      fileName: 'sample-sheet.pdf',
      fileExtension: '.pdf',
      fileSizeBytes: 4096,
      existsOnDisk: true,
    },
  ]

  await page.route('**/ui/feature-flags', (route) => route.fulfill({ json: { flags: [] } }))

  await page.route('**/api/v2/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const path = url.pathname

    if (path === '/api/v2/user-profiles/me') {
      await route.fulfill({
        json: {
          userId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          username: 'admin',
          displayName: 'Administrator',
          role: 'Admin',
        },
      })
      return
    }

    if (path === '/api/v2/stock/products' && request.method() === 'GET') {
      await route.fulfill({ json: [product] })
      return
    }

    if (path === `/api/v2/stock/products/${product.productId}` && request.method() === 'GET') {
      await route.fulfill({
        json: {
          productId: product.productId,
          customerCode: 'CUS',
          categoryCode: 'CAT',
          sequenceNumber: '0001',
          stockNumber: product.stockNumber,
          productCode: product.productCode,
          productName: product.productName,
          productionInfo: 'Production info',
          remarks: product.remarks,
          sellingPrice: product.sellingPrice,
          cogs: product.cogs,
          balance: product.balance,
          createdOn: product.createdOn,
          createdBy: product.createdBy,
          modifiedOn: product.modifiedOn,
          modifiedBy: product.modifiedBy,
        },
      })
      return
    }

    if (path.endsWith('/movements') && request.method() === 'GET') {
      await route.fulfill({ json: [] })
      return
    }

    if (path.endsWith('/validate-code')) {
      await route.fulfill({ json: { isUnique: true } })
      return
    }

    if (path.endsWith('/next-number')) {
      await route.fulfill({
        json: {
          customerCode: 'CUS',
          categoryCode: 'CAT',
          sequenceNumber: '0002',
          stockNumber: 'CUS-CAT-0002',
        },
      })
      return
    }

    if (path === `/api/v2/stock/products/${product.productId}/attachments` && request.method() === 'GET') {
      await route.fulfill({ json: attachments })
      return
    }

    if (path === `/api/v2/stock/products/${product.productId}/attachments` && request.method() === 'POST') {
      attachments.push({
        attachmentId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
        productId: product.productId,
        attachmentIndex: 3,
        fileName: 'uploaded.txt',
        fileExtension: '.txt',
        fileSizeBytes: 256,
        existsOnDisk: true,
      })
      await route.fulfill({ status: 201, json: [attachments[2]] })
      return
    }

    if (path === `/api/v2/stock/products/${product.productId}/attachments` && request.method() === 'DELETE') {
      const payload = (request.postDataJSON() as { attachmentIds: string[] })
      for (const id of payload.attachmentIds) {
        const index = attachments.findIndex((item) => item.attachmentId === id)
        if (index >= 0) {
          attachments.splice(index, 1)
        }
      }

      await route.fulfill({
        status: 200,
        json: {
          productId: product.productId,
          requestedCount: payload.attachmentIds.length,
          deletedCount: payload.attachmentIds.length,
        },
      })
      return
    }

    if (path.startsWith(`/api/v2/stock/products/${product.productId}/attachments/`) && request.method() === 'GET') {
      const fileName = path.includes('bbbbbbbb') ? 'sample-sheet.pdf' : 'sample-image.jpg'
      await route.fulfill({
        status: 200,
        headers: {
          'content-type': fileName.endsWith('.pdf') ? 'application/pdf' : 'image/jpeg',
          'content-disposition': `attachment; filename="${fileName}"`,
        },
        body: 'fake-binary-content',
      })
      return
    }

    if (path.startsWith('/api/v2/stock/products/') && request.method() === 'PUT') {
      await route.fulfill({ status: 200, json: {} })
      return
    }

    await route.fulfill({ json: [] })
  })
}

test.describe('stock attachments dialog', () => {
  test('launches from StockView and supports size mode + selection guards', async ({ page }) => {
    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await page.getByRole('button', { name: 'Checkbox' }).click()
    await page.getByRole('checkbox').nth(1).check()
    await page.getByRole('button', { name: 'Attachment' }).click()

    await expect(page.getByText('Stock Attachments')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Download Selected' })).toBeDisabled()

    await page.getByRole('button', { name: 'X-Large' }).click()
    await expect(page.getByRole('button', { name: 'X-Large' })).toHaveClass(/v-btn--active/)

    await page.getByText('sample-image.jpg').click()
    await expect(page.getByRole('button', { name: 'Download Selected' })).toBeEnabled()
    await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeEnabled()
  })

  test('launches from ProductRecordDialog and allows multi-delete', async ({ page }) => {
    page.on('dialog', (dialog) => dialog.accept())

    await injectFakeSession(page)
    await mockApi(page)
    await page.goto('/app/stock')

    await page.getByText('A4 Art Paper 128gsm').click()
    await expect(page.getByText('Edit Product Record')).toBeVisible()

    await page.getByRole('dialog').getByRole('button', { name: 'Attachment' }).click()
    await expect(page.getByText('Stock Attachments')).toBeVisible()

    await page.getByText('sample-image.jpg').click()
    await page.getByText('sample-sheet.pdf').click()

    await page.getByRole('button', { name: 'Delete Selected' }).click()
    await expect(page.getByText('Selected attachments were deleted.')).toBeVisible()
    await expect(page.getByText('No attachments available for this product.')).toBeVisible()
  })
})
