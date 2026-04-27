# Stock Record PDF Print API

## Endpoint

- Method: `GET`
- Path: `/api/v2/stock/products/{productId}/print`
- Auth: Bearer token (same policy as stock endpoints)
- Success: `200 OK` with `application/pdf`
- Not found: `404 Not Found` when product does not exist or is retired
- Failure: `500 Internal Server Error` with ProblemDetails payload

## Response behavior

- Returns a generated PDF binary that includes:
  - Product identity (stock number, product code, product name)
  - Production info and remarks
  - MOQ and current balance
  - Movement rows with row number, date, reference, quantity, running balance, modified timestamp, modified by
- Movement rows are sorted deterministically by:
  1. `InOutDate` ascending
  2. `ModifiedOn` ascending
- Row numbers are assigned after sorting, starting at `1`

## Frontend integration

- Dialog action: Product Record dialog Print button (edit mode)
- Client path: `printProductRecord(productId)` in stock service
- UX behavior:
  - Attempt to open PDF in a new tab
  - If popup is blocked, download fallback is triggered
  - Localized feedback shown for opened/downloaded/error states

## Configuration

- Renderer engine: QuestPDF Community (`LicenseType.Community`)
- Font loading: embedded resources initialized at application startup through `FontRegistry`
- Embedded fonts:
  - `Lato-Regular.ttf` (Latin)
  - `NotoSansCJKsc-Regular.otf` (CJK)

## Operational notes

- Structured error logging is emitted with product ID context on print failures.
- The renderer no longer uses configurable system font names. Update `FontRegistry` for any font changes and re-run CJK fixture tests.
