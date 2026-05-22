## Context

The BillingInvoicesView currently provides users with invoice list management (Mark Sent action). Users need quick access to PDF documents (invoices and delivery notes) without leaving the application. Invoice Ninja is the billing system of record and stores these documents.

**Current State:**
- BillingInvoicesView has a toolbar with multiple action buttons (Columns, Sorting, Check Box, Views, New Invoice, Mark Sent)
- Mark Sent button demonstrates the pattern: disabled by default, enabled when exactly one checkbox-selected invoice exists
- v-menu component used for other dropdowns (Views, Columns, Sorting)
- All billing API calls go through authenticated backend proxy endpoints
- Files must be downloaded through the backend (CORS policy prevents direct Invoice Ninja calls)

**Constraints:**
- Must maintain consistent UI with existing toolbar buttons
- Downloads must use backend proxy for authentication and CORS handling
- Invoice Ninja API documentation defines available document types and download endpoints
- Delivery notes may not be available for all invoice statuses in Invoice Ninja

## Goals / Non-Goals

**Goals:**
- Enable users to download invoice PDFs from the listing with one click
- Enable users to download delivery notes (when available) from the listing
- Follow existing UI patterns and styling conventions
- Maintain responsive design (toolbar adapts to mobile view)
- Provide clear error feedback if download fails

**Non-Goals:**
- Batch downloads of multiple invoices
- Document preview or inline viewing
- Custom file naming beyond Invoice Ninja's defaults
- Automatic document generation or transformation
- Integration with local file system beyond browser's standard download directory

## Decisions

### 1. Button Placement and State
**Decision:** New "Download" button placed immediately after "Mark Sent" button, using same enablement logic (disabled until exactly one invoice selected in checkbox mode).

**Rationale:** Consistent with existing "Mark Sent" button pattern. Both are single-invoice actions that require checkbox mode and exact selection count.

**Alternatives Considered:**
- Enable button for any selection (rejected: confusing which invoice document to download)
- Separate Download button from checkbox mode (rejected: inconsistent with existing patterns)

### 2. Menu Pattern
**Decision:** Use v-menu component with two menu items ("Invoice PDF" and "Delivery Note") consistent with existing "Views" menu pattern.

**Rationale:** Vuetify v-menu is already used in the application; provides familiar UX for toolbar dropdowns. Two static options don't require scrolling.

**Alternatives Considered:**
- Single "Download" button with two separate buttons below (rejected: more complex, less standard)
- Toggle between document types (rejected: adds extra step vs. direct menu selection)

### 3. Backend Endpoints
**Decision:** Create two separate GET endpoints:
- `GET /api/v2/billing/invoices/{id}/download/pdf`
- `GET /api/v2/billing/invoices/{id}/download/delivery-note`

**Rationale:** Separate endpoints provide clear intent, simplified error handling per document type, and better aligns with REST conventions. Both are read-only, non-state-changing operations.

**Alternatives Considered:**
- Single parameterized endpoint with `?type=pdf|delivery-note` (rejected: less RESTful)
- Include in existing invoice summary endpoint (rejected: conflates data retrieval with file download)
**Invoice Ninja API Requirements**:
- **PDF Download**: Invoice Ninja uses `GET /api/v1/invoice/{invitation_key}/download` (requires invitation_key, NOT invoice id)
- **Delivery Note**: Invoice Ninja uses `GET /api/v1/invoices/{id}/delivery_note` (uses invoice id) ✓
- Both require `X-API-TOKEN` and `X-Requested-With: XMLHttpRequest` headers

**Implementation Strategy for PDF**:
Since externalInvoiceId is the invoice ID (not invitation_key), we must:
1. Fetch the invoice details with `?include=invitations` parameter
2. Extract the invitation_key from the invitations array
3. Call Invoice Ninja's PDF endpoint with the extracted invitation_key
4. Alternative: Store invitation_key during invoice generation for faster access (future optimization)
### 4. File Download Mechanism
**Decision:** Backend returns `application/pdf` with `Content-Disposition: attachment` header. Frontend uses a hidden anchor element to trigger browser download.

**Rationale:** Standard browser download mechanism; no external libraries needed. Respects user's download folder preferences. Works across all browsers.

**Code Pattern:**
```typescript
const link = document.createElement('a')
link.href = URL.createObjectURL(blob)
link.download = filename
link.click()
URL.revokeObjectURL(link.href)
```

**Alternatives Considered:**
- window.open() with target="_blank" (rejected: opens new tab instead of downloading)
- Fetch with response blob handling (works but requires more setup; chosen pattern is cleaner)

### 5. Error Handling
**Decision:** Errors display in existing `errorMessage` alert banner. Disabled state feedback via v-btn's `:disabled` prop; no tooltip initially.

**Rationale:** Consistent with existing error handling (Mark Sent errors use same banner). Alert banner visible without blocking interaction. Single error space prevents visual clutter.

**Alternatives Considered:**
- Toast/snackbar notifications (rejected: inconsistent with existing error pattern)
- Inline button error states (rejected: v-btn doesn't support per-state error messages well)
- Tooltip on disabled button (could add later based on user feedback)

### 6. Service Layer Functions
**Decision:** Add two new functions to `src/services/billing.ts`:
- `async downloadInvoicePdf(externalInvoiceId: string): Promise<Blob>`
- `async downloadDeliveryNote(externalInvoiceId: string): Promise<Blob>`

**Rationale:** Isolates API client concerns in service layer. Returns Blob for easy DOM anchor element usage. Consistent with existing service function patterns.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| Invoice Ninja API unavailability | Backend returns standard billing error response; frontend displays error message |
| Delivery note not available for invoice status | Invoice Ninja API returns 404 or empty response; handle gracefully with user-facing message || PDF download requires invitation_key fetch | Backend first fetches invoice with `?include=invitations` to extract key; adds one extra API call per PDF download |
| invitation_key not available in API response | If Invoice Ninja API doesn't include invitations in response, must use alternative endpoint or defer PDF download to future release || Large PDF file download blocking UI | Files return quickly; no progress bar needed for typical invoice PDFs (<5MB) |
| Multiple concurrent downloads | Browser queues downloads naturally; no rate limiting needed for this use case |
| CORS issues with direct calls (if attempted) | Backend proxy pattern already established; direct calls not attempted |
| Menu placement on mobile with long button text | Toolbar uses flex-wrap; menu adapts responsively (existing implementation handles this) |

## Migration Plan

**Deployment:**
1. Deploy backend changes (new endpoints) in API service
2. Deploy frontend changes (button, menu, service functions) in WebApp
3. No database migrations required
4. No data migrations required
5. Endpoint availability verified by existing billing connectivity check

**Rollback:**
- Remove new endpoints from API
- Remove Download button and service functions from frontend
- Users revert to external Invoice Ninja access
- No state corruption possible; download operations are read-only

## Open Questions

1. **Does InvoiceNinjaInvoiceResponse include invitations array?**
   - CRITICAL: The invitation_key required for PDF download must come from the invitations array in the invoice response
   - Current model does NOT include invitation fields
   - Must verify with actual Invoice Ninja API call before implementation starts
   - If invitations are NOT returned, we must decide: defer PDF feature or use alternative approach

2. Should we add a loading state to the Download button while file is downloading?
   - Current approach: No loading indicator (invitation fetch is transparent)
   - Alternative: Set `:loading="isDownloading"` to show spinner during multi-step fetch
   
3. Should downloaded files be named after the invoice number or Invoice Ninja's defaults?
   - Current approach: Use descriptive naming from backend `Content-Disposition` header
   - Alternative: Override filename in frontend (requires more coordination)

4. Should we batch download multiple invoices in future?
   - Current: Out of scope (single-invoice only)
   - Future: Can extend with separate UI control after this feature stabilizes

5. Should we store invitation_key during invoice generation for optimization?
   - Current approach: Fetch on each download (simpler, but adds API call)
   - Alternative: Store invitation_key in metadata during invoice generation (faster, but changes data model)
