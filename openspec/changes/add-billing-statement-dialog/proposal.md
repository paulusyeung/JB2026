## Why

The Billing statement list currently stops at selection gating: the `Statement` toolbar action becomes enabled for a single selected client, but clicking it only shows a follow-up placeholder message. Users need a complete statement request flow from that screen so they can choose the statement scope, send the request through the backend-owned Invoice Ninja integration, and immediately view the generated statement without leaving JB2026 to assemble parameters manually.

## What Changes

- Replace the placeholder `Statement` button behavior in `BillingStatementView` with a modal dialog that opens only when exactly one client is selected.
- Add statement request inputs in that dialog: `Date Range` preset (`All Outstanding` default, `This Month`, `Last Month`, `This Quarter`, `This Year`), `Status` (`All` default, `Paid`, `Unpaid`), and selector toggles for `Credits`, `Payments`, and `Aging`.
- Add `Cancel` and `Proceed` actions, with `Proceed` submitting the selected client and request options to a backend billing endpoint that maps the request to Invoice Ninja `POST /api/v1/client_statement`.
- Return the generated statement in a way that opens in a new browser tab while keeping Invoice Ninja credentials and request construction on the server.
- Add localized labels, loading/error feedback, and focused tests for dialog defaults, gating, request payload construction, and successful statement launch behavior.

## Capabilities

### New Capabilities
- `billing-statement-request-dialog`: Collect statement filter options, submit a statement request for the selected billing client, and open the resulting statement in a new tab.

### Modified Capabilities
- `billing-statement-view`: Change the existing `Statement` toolbar action from a placeholder message to a dialog-launching workflow while preserving single-selection gating.

## Impact

- Frontend UI: `BillingStatementView` and likely a new shared billing dialog component/composable for the statement request flow.
- Frontend services: `ClientApp/src/services/billing.ts` types and API helpers for statement request/launch behavior.
- Backend/API: new billing controller/service surface in `JB2026.Api` to construct and proxy the Invoice Ninja `POST /api/v1/client_statement` request for a specific client.
- Invoice Ninja integration: expand the backend-owned billing integration beyond client listing so it can request client statement output using `client_id`, `start_date`, `end_date`, and the three `show_*_table` flags without exposing credentials to the browser.
- Localization and tests: new `billing.statement.*` dialog strings plus focused API and web-app coverage for request mapping, defaults, validation, and new-tab launch behavior.