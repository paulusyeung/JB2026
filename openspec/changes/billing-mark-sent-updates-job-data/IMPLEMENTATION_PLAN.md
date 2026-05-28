# Implementation Plan: Mark Sent Action Updates Job Order Data

## Overview

When a draft invoice is marked as sent via the "Mark Sent" button in `BillingInvoicesView`, the system should also update the related Job Order records with invoice metadata. Specifically, for each job number linked to that invoice, update the first (or primary) job with:

- **Invoice Number** — The Invoice Ninja invoice number
- **Invoice Amount** — The total amount from the sent invoice
- **Modified On** — Current date/time
- **Modified By** — Currently logged-in user

---

## Problem Statement

Currently, when an invoice is marked as sent in Invoice Ninja through the Mark Sent button:
1. ✅ The invoice status changes from `Draft` → `Sent` in Invoice Ninja
2. ✅ The billing invoices list updates with the new status
3. ❌ **The associated job order(s) are NOT updated with invoice reference data**

This creates a disconnect: an invoice exists and is sent, but the job order record doesn't reflect the invoice state, forcing manual data entry or leaving the job order out of sync.

---

## Business Requirements

### Primary Requirement

> When a draft invoice is marked as sent, if that invoice contains one or more Job Numbers in its line items or custom fields, update the **first** job order's:
> - `invoiceRef` = external Invoice Ninja invoice ID (or number)
> - `invoiceAmount` = total invoice amount
> - `modifiedOn` = current timestamp
> - `modifiedBy` = current authenticated user

### Key Constraints

1. **Single Job Update** — Update only the **first** job number found on the invoice (not all referenced jobs)
2. **Data Consistency** — The job update must complete successfully or fail gracefully without breaking the invoice send operation
3. **User Context** — The `modifiedBy` field must reflect the authenticated user performing the "Mark Sent" action
4. **Timestamp** — Use server-side timestamp for consistency
5. **No Invoice Ninja Exposure** — Keep Job Order updates within the JB2026 backend; no direct Job Ninja calls from Job services

---

## Data Flow Diagram

```
BillingInvoicesView
    ↓
[User clicks Mark Sent on Draft invoice]
    ↓
sendInvoice(externalInvoiceId)
    ↓
POST /api/v2/billing/invoices/{id}/send
    ↓
BillingController.SendInvoice()
    ↓
BillingService.SendInvoiceAsync(externalInvoiceId)
    ├─ ✅ Validates Draft status
    ├─ ✅ Calls Invoice Ninja bulk send
    ├─ ✅ Fetches updated invoice
    ├─ ✅ Returns InvoiceBillingSummary
    └─ NEW: Extract job numbers & call UpdateJobOrdersFromInvoice()
        ↓
    UpdateJobOrdersFromInvoice(invoiceId, jobNumbers, summary, currentUser)
        ├─ Finds first job number in jobNumbers array
        ├─ Loads JobOrder by jobNumber/orderId relationship
        ├─ Updates: invoiceRef, invoiceAmount, modifiedOn, modifiedBy
        ├─ Calls PUT /api/v2/job-orders/{orderId}
        └─ Returns success/failure (logged, non-blocking)
    ↓
Response: SendInvoiceResponse { billingSummary, sentAt, jobUpdateStatus? }
    ↓
Frontend updates invoice list and optionally job list
```

---

## Implementation Phases

### Phase 1: Backend Data Layer & Repository

**Files to Modify:**
- `JB2026.Api/Services/EfJobManagementRepository.cs`
- `JB2026.Api/Models/UpdateJobOrderRequest.cs` (if fields need addition)

**Changes:**

1. **Add Job Lookup Method** — Create or extend a method to find job orders by job number:
   ```csharp
   /// <summary>
   /// Retrieves a JobOrder by job number (supports multiple jobs with same number across orders).
   /// Returns the first match or null.
   /// </summary>
   public async Task<JobOrder> GetJobOrderByJobNumberAsync(string jobNumber)
   {
       return await _dbContext.JobOrders
           .FirstOrDefaultAsync(j => j.JobNumber == jobNumber);
   }
   ```

2. **Verify UpdateJobOrder Supports invoiceRef & invoiceAmount** — Ensure the existing update method handles these fields:
   ```csharp
   // Existing method — verify these fields are included:
   jobOrder.InvoiceRef = updateRequest.InvoiceRef;
   jobOrder.InvoiceAmount = updateRequest.InvoiceAmount;
   jobOrder.ModifiedOn = updateRequest.ModifiedOn;
   jobOrder.ModifiedBy = updateRequest.ModifiedBy;
   ```

---

### Phase 2: Backend Business Logic

**Files to Modify:**
- `JB2026.Api/Services/Billing/IBillingService.cs`
- `JB2026.Api/Services/Billing/BillingService.cs`
- `JB2026.Api/Services/Billing/JobOrderInvoiceMappingHelper.cs` (new or extend)

**Changes:**

1. **Extract Job Numbers from Invoice** — Create a helper method in `JobOrderInvoiceMappingHelper`:
   ```csharp
   /// <summary>
   /// Extracts job numbers from an Invoice Ninja invoice.
   /// Looks in custom field IN_CF_INVOICE_JOB_NO (configured in Invoice Ninja setup).
   /// </summary>
   public static List<string> ExtractJobNumbersFromInvoice(
       InvoiceNinjaInvoiceResponse invoice)
   {
       var jobNumbers = new List<string>();
       
       // Look in custom_value1 (mapped to job number field)
       if (!string.IsNullOrWhiteSpace(invoice.CustomValue1))
       {
           jobNumbers.Add(invoice.CustomValue1.Trim());
       }
       
       // Optional: scan line items for additional job references
       // if multiple jobs can be on one invoice
       
       return jobNumbers;
   }
   ```

2. **Extend BillingService.SendInvoiceAsync()** — After successful send, extract job numbers and queue the job update:
   ```csharp
   public async Task<InvoiceBillingSummary> SendInvoiceAsync(
       string externalInvoiceId, 
       string? currentUsername = null,  // NEW: for ModifiedBy
       IJobManagementRepository? jobRepo = null)  // NEW: for updating jobs
   {
       // ... existing Draft validation & send logic ...
       
       var updatedInvoice = await _invoiceNinjaClient.GetAsync<InvoiceNinjaInvoiceResponse>(
           $"/invoices/{externalInvoiceId}");
       
       var summary = MapToSummary(updatedInvoice);
       
       // NEW: Update related job orders
       if (jobRepo != null && currentUsername != null)
       {
           var jobNumbers = JobOrderInvoiceMappingHelper.ExtractJobNumbersFromInvoice(updatedInvoice);
           
           if (jobNumbers.Count > 0)
           {
               try
               {
                   await UpdateJobOrderFromInvoiceAsync(
                       jobNumbers[0],  // First job only
                       externalInvoiceId,
                       updatedInvoice.Number,
                       updatedInvoice.Amount,
                       currentUsername,
                       jobRepo);
               }
               catch (Exception ex)
               {
                   _logger.LogWarning(
                       "Failed to update job order from invoice {ExternalInvoiceId}: {Error}",
                       externalInvoiceId,
                       ex.Message);
                   // Non-blocking: continue and return success
               }
           }
       }
       
       return summary;
   }
   ```

3. **Add UpdateJobOrderFromInvoiceAsync()** method:
   ```csharp
   private async Task UpdateJobOrderFromInvoiceAsync(
       string jobNumber,
       string externalInvoiceId,
       string invoiceNumber,
       decimal invoiceAmount,
       string modifiedBy,
       IJobManagementRepository jobRepo)
   {
       var jobOrder = await jobRepo.GetJobOrderByJobNumberAsync(jobNumber);
       
       if (jobOrder == null)
       {
           _logger.LogWarning(
               "Job order with job number {JobNumber} not found for invoice {InvoiceId}",
               jobNumber,
               externalInvoiceId);
           return;
       }
       
       jobOrder.InvoiceRef = externalInvoiceId;
       jobOrder.InvoiceAmount = invoiceAmount;
       jobOrder.ModifiedOn = DateTime.UtcNow;
       jobOrder.ModifiedBy = modifiedBy;
       
       await jobRepo.UpdateJobOrderAsync(jobOrder);
       
       _logger.LogInformation(
           "Updated job order {OrderId} with invoice {InvoiceId}",
           jobOrder.OrderId,
           externalInvoiceId);
   }
   ```

---

### Phase 3: Backend API Controller

**Files to Modify:**
- `JB2026.Api/Controllers/BillingController.cs`

**Changes:**

1. **Update Endpoint Signature** — Modify the `SendInvoice` action to inject `IJobManagementRepository` and get the current user:
   ```csharp
   [HttpPost("invoices/{externalInvoiceId}/send")]
   public async Task<ActionResult<SendInvoiceResponse>> SendInvoice(
       string externalInvoiceId,
       [FromServices] IJobManagementRepository jobManagementRepository)
   {
       var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
           ?? "System";
       
       try
       {
           var summary = await _billingService.SendInvoiceAsync(
               externalInvoiceId,
               currentUser,        // NEW: pass current user
               jobManagementRepository);  // NEW: pass job repo
           
           return Ok(new SendInvoiceResponse
           {
               BillingSummary = summary,
               SentAt = DateTime.UtcNow
           });
       }
       catch (BillingException ex)
       {
           return HandleBillingException(ex);
       }
   }
   ```

---

### Phase 4: Frontend Service Layer

**Files to Modify:**
- `JB2026.WebApp/ClientApp/src/services/billing.ts`

**Changes:**

1. **Update SendInvoice Response Type** (optional, for future expansion):
   ```typescript
   export interface SendInvoiceResponse {
     billingSummary: InvoiceBillingSummary
     sentAt: string
     jobUpdateStatus?: {
       jobNumber: string
       orderId: string
       updateStatus: 'success' | 'notFound' | 'failed'
     }
   }
   ```

2. **Update sendInvoice Function** (minimal change, backend handles the work):
   ```typescript
   export async function sendInvoice(externalInvoiceId: string): Promise<InvoiceBillingSummary> {
     const response = await apiClient.post<SendInvoiceResponse>(
       `/api/v2/billing/invoices/${externalInvoiceId}/send`
     )
     
     // Optional: Log job update status if returned
     if (response.data.jobUpdateStatus?.updateStatus !== 'success') {
       console.warn(
         `Job update status for ${response.data.jobUpdateStatus?.jobNumber}:`,
         response.data.jobUpdateStatus?.updateStatus
       )
     }
     
     return response.data.billingSummary
   }
   ```

---

### Phase 5: Frontend UI/UX

**Files to Modify:**
- `JB2026.WebApp/ClientApp/src/views/BillingInvoicesView.vue` (potentially)
- `JB2026.WebApp/ClientApp/src/views/JobListView.vue` (potentially)

**Changes:**

1. **Optional: Show Success Message with Job Update Status** — Enhance `performMarkSent()`:
   ```typescript
   async function performMarkSent() {
       const selectedId = selectedInvoiceIds.value[0]
       if (!selectedId) return
       
       isSendingInvoice.value = true
       errorMessage.value = ''
       
       try {
           const result = await sendInvoice(selectedId)  // Already does job update
           
           // Find and update the invoice in the list
           const invoiceIndex = invoices.value.findIndex(
               (inv) => inv.externalInvoiceId === selectedId
           )
           if (invoiceIndex >= 0) {
               invoices.value[invoiceIndex] = result
           }
           
           // Show success notice
           showMarkSentConfirmation.value = false
           successMessage.value = t('billing.invoices.messages.markSentSuccess')
           
           // Optional: Refresh job list if job update succeeded
           // This is handled server-side logging; UI can show success
       } catch (error) {
           errorMessage.value = t('billing.invoices.messages.markSentFailed')
       } finally {
           isSendingInvoice.value = false
           showMarkSentConfirmation.value = false
       }
   }
   ```

2. **Optional: Refresh Job List After Mark Sent** — If needed, emit an event to parent or call refresh:
   ```typescript
   // After successful send, optionally trigger a job list refresh
   // This is NOT required since the job update happens server-side
   // But can improve UX if the user has both views open
   ```

---

## Implementation Checklist

### Backend

- [ ] **Repository Layer**
  - [ ] Add `GetJobOrderByJobNumberAsync()` to `IJobManagementRepository`
  - [ ] Verify `UpdateJobOrder()` handles `invoiceRef`, `invoiceAmount`, `modifiedOn`, `modifiedBy`
  - [ ] Add unit tests for job lookup and update

- [ ] **Business Logic**
  - [ ] Create/extend `JobOrderInvoiceMappingHelper.ExtractJobNumbersFromInvoice()`
  - [ ] Extend `BillingService.SendInvoiceAsync()` with job parameters
  - [ ] Add `BillingService.UpdateJobOrderFromInvoiceAsync()` private method
  - [ ] Add logging for job update operations (success and failure)

- [ ] **API Controller**
  - [ ] Update `BillingController.SendInvoice()` to inject `IJobManagementRepository`
  - [ ] Extract current user from claims
  - [ ] Pass user and repository to `SendInvoiceAsync()`
  - [ ] Test with multiple endpoints (verify routing)

- [ ] **Error Handling & Logging**
  - [ ] Non-blocking job updates (failures logged, not thrown)
  - [ ] Graceful degradation if job not found
  - [ ] Clear logging messages for troubleshooting

### Frontend

- [ ] **Services**
  - [ ] Update `sendInvoice()` response type (optional)
  - [ ] Add console logging for job update status (optional)

- [ ] **Views**
  - [ ] Verify `BillingInvoicesView.performMarkSent()` works with updated response
  - [ ] Optional: Show success feedback mentioning job update
  - [ ] Optional: Trigger job list refresh after send (if both views open)

- [ ] **UI/UX**
  - [ ] Test Mark Sent button behavior with invoices linked to jobs
  - [ ] Verify success message displays correctly
  - [ ] Verify error messages are clear and non-intrusive

### Testing

- [ ] **Integration Tests**
  - [ ] Mark invoice as sent with linked job → verify job fields updated
  - [ ] Mark invoice as sent without linked job → verify no errors
  - [ ] Mark invoice with invalid job number → verify graceful handling
  - [ ] Verify `modifiedBy` is set to current user
  - [ ] Verify `modifiedOn` is set to current timestamp

- [ ] **End-to-End Tests**
  - [ ] BillingInvoicesView Mark Sent → Job appears updated in JobListView
  - [ ] Verify invoice and job data consistency after operation

---

## Error Handling Strategy

| Error Scenario | Current Behavior | Proposed Behavior |
|---|---|---|
| Job number not found in system | N/A (feature doesn't exist) | Log warning, continue, mark invoice as sent anyway |
| Job already has invoice ref | N/A | Overwrite with new invoice data (as requested) |
| User not authenticated | Controller returns 401 | Use "System" as fallback for `modifiedBy` |
| Database update fails | N/A | Log error, but do NOT fail the invoice send operation |
| Invoice Ninja send fails | Existing error handling | Unchanged; job update only happens after successful send |

---

## Configuration & Dependencies

### Required Injections

1. `IJobManagementRepository` — Injected into `BillingController.SendInvoice()`
2. `ILogger<BillingService>` — Already present
3. `IInvoiceNinjaClient` — Already present

### No New Configuration Needed

- Invoice Ninja custom field mapping already exists
- Job number field already mapped in `JobOrderInvoiceMappingHelper`
- User claims extraction uses standard ASP.NET Core patterns

---

## Database Schema Considerations

### Current Fields (Already Exist)

| Table | Field | Type | Nullable | Notes |
|---|---|---|---|---|
| JobOrders | InvoiceRef | VARCHAR(255) | NULL | External Invoice Ninja ID |
| JobOrders | InvoiceAmount | DECIMAL(15,2) | NULL | Invoice total |
| JobOrders | ModifiedOn | DATETIME | NULL | Last modification timestamp |
| JobOrders | ModifiedBy | VARCHAR(50) | NULL | User who last modified |

**No schema changes required** — all fields already exist.

---

## Performance Considerations

1. **Database Query** — `GetJobOrderByJobNumberAsync()` adds one indexed query per mark-sent operation (acceptable)
2. **Job Update** — One additional `UpdateJobOrderAsync()` call (same cost as current job edit)
3. **Logging** — Minimal overhead; no new logs per se, just repurposing existing logger
4. **Non-Blocking** — Job update failures do not delay invoice send response

---

## Rollback Plan

If issues occur:

1. **Backend** — Remove the job repository injection and `UpdateJobOrderFromInvoiceAsync()` calls from `SendInvoiceAsync()`
   - Mark Sent will revert to updating only the invoice, not the job
   - No database schema rollback needed

2. **Frontend** — No changes needed (service accepts existing response format)

---

## Future Enhancements (Out of Scope)

1. **Batch Job Updates** — Update ALL jobs linked to an invoice (not just first)
2. **Selective Update** — Let user choose which job to update via UI
3. **Custom Field Mapping** — Support multiple custom fields for job references
4. **Audit Trail** — Store job update events in an audit log
5. **Webhook Integration** — Trigger external systems when job is marked invoiced
6. **Job Notification** — Send email/notification to order contact when job is invoiced

---

## Testing Scenarios

### Scenario 1: Single Job, Mark Sent
**Setup:** Invoice linked to one job number  
**Action:** Click Mark Sent  
**Expected:**  
- ✅ Invoice status → Sent in Invoice Ninja
- ✅ Job `invoiceRef` → Invoice ID
- ✅ Job `invoiceAmount` → Invoice total
- ✅ Job `modifiedOn` → Current timestamp
- ✅ Job `modifiedBy` → Current user
- ✅ BillingInvoicesView list updates

### Scenario 2: Multiple Jobs Referenced, Mark Sent
**Setup:** Invoice with multiple job numbers in custom fields  
**Action:** Click Mark Sent  
**Expected:**  
- ✅ First job updated with invoice data
- ✅ Other jobs NOT updated (by design)
- ✅ Success message shown

### Scenario 3: No Job Reference, Mark Sent
**Setup:** Invoice with no job number field  
**Action:** Click Mark Sent  
**Expected:**  
- ✅ Invoice sent successfully
- ✅ Job update skipped gracefully
- ✅ No error message (job update is optional)

### Scenario 4: Job Not Found, Mark Sent
**Setup:** Invoice references non-existent job number  
**Action:** Click Mark Sent  
**Expected:**  
- ✅ Invoice sent successfully
- ✅ Warning logged (no visible error to user)
- ✅ Success message shown anyway

### Scenario 5: User Not Authenticated
**Setup:** Request made with invalid or missing auth token  
**Action:** Mark Sent attempted  
**Expected:**  
- ✅ Controller returns 401 Unauthorized
- ✅ No job update attempted

---

## Success Criteria

✅ **Definition of Done:**

1. When "Mark Sent" is clicked on a draft invoice linked to a job:
   - The invoice is marked as sent in Invoice Ninja
   - The first linked job's `invoiceRef`, `invoiceAmount`, `modifiedOn`, `modifiedBy` are updated
   - The current user is recorded in `modifiedBy`
   - The operation completes without errors visible to the user

2. If the job is not found or update fails:
   - The invoice is still marked as sent (job update is non-blocking)
   - The error is logged server-side
   - The UI shows success (invoice marked sent)

3. If no job is linked to the invoice:
   - The invoice is marked as sent
   - No error is shown
   - Job-related logic is silently skipped

4. All changes compile, no warnings, existing tests still pass

---

## Questions to Confirm with Stakeholders

1. **Which field should map to job number on Invoice Ninja?**
   - Currently assuming: `Custom Field 1` (IN_CF_INVOICE_JOB_NO)
   - Confirm with Invoice Ninja config

2. **Should we update ONLY the first job or ALL jobs?**
   - Currently: First job only (per requirements)
   - Is this acceptable?

3. **What should happen if a job already has an invoice reference?**
   - Currently: Overwrite with new invoice data
   - Should we check for conflicts?

4. **Should we show the job update status in the UI?**
   - Currently: Silent (non-blocking operation)
   - Should we log to console or show a notification?

5. **Should job list auto-refresh if both BillingInvoicesView and JobListView are open?**
   - Currently: No auto-refresh (job update happens server-side)
   - Is this acceptable, or should we emit an event?

