# Phase 6 UI Modernization — Deployment & UAT Runbook

**Last Updated:** March 29, 2026  
**Phase:** 6 — UI Modernization  
**Target:** Staging → Production slice-by-slice feature flag migration

---

## Overview

This runbook guides the team through UAT validation and production deployment of each UI slice (A → D). Each slice follows the same workflow:

1. **Build & Smoke** — Frontend + backend build succeeds; Playwright smoke suite passes
2. **Stage Validation** — Slice deployed to staging; feature flag enabled; UAT sign-off obtained
3. **Production Flip** — Feature flag enabled in production; monitoring active; fallback ready

---

## Slice Readiness Checklist

### ✅ **Slice A: Read-only lists and dashboards**

**Implementation Status:**
- [x] Dashboard view with KPI cards, feature flag list, Chart.js trend chart
- [x] Jobs grid with v-data-table-server (sort, filter, pagination)
- [x] Quotations grid with full-text search
- [x] Virtual-scroll fallback for 500+ row grids
- [x] API layer: GET /api/v2/jobs/range, GET /api/v2/quotations
- [x] Playwright smoke tests (3 tests: dashboard, jobs, quotations)
- [x] TypeScript strict mode compliant
- [x] .NET build succeeds (0 errors)

**Ready for Staging:** ✅ **YES** — All build and smoke criteria met

**UAT Focus Areas:**
- KPI values match legacy dashboard
- Pagination/sort behavior matches legacy grids  
- Virtual scroll activates smoothly at 500+ rows
- Filter and search text matching works correctly
- No 404 errors on API calls

**Deployment Window:** ~2 hours (includes UAT + monitoring ramp-up)

---

### ✅ **Slice B: Create/edit form views**

**Implementation Status:**
- [x] Job order create/edit form with Vuetify controls
- [x] Form dialog with validation (required fields, date ordering, positive numbers)
- [x] Status and payment terms select dropdowns
- [x] API layer: POST /api/v2/jobs (create), PATCH /api/v2/jobs/{id} (update)
- [x] Playwright smoke tests (4 tests: login form, detail panel, new dialog, cancel)
- [x] Save success snackbar UX
- [x] Error handling with retry capability

**Ready for Staging:** ✅ **YES** — Form interactivity and API contract complete

**UAT Focus Areas:**
- New job order creation saves correctly
- Field validation errors display as expected
- Edit existing job updates without data loss
- Payment terms and status enums convert correctly
- API 400/404 errors are handled gracefully

**Deployment Window:** ~2 hours (includes UAT + form data integrity audit)

---

### ✅ **Slice C: Scheduler/calendar views**

**Implementation Status:**
- [x] FullCalendar (Apache 2.0) time grid and day grid views
- [x] Drag-and-drop event rescheduling with API persistence
- [x] Event data model: ScheduleId, OrderId, StartOn, EndOn, Status, Priority
- [x] API layer: GET /api/v2/job-schedules/range, PATCH /api/v2/job-schedules/{id}/time
- [x] API unit tests (13 passing tests for controller)
- [x] Playwright smoke tests (2 tests: calendar render, prev/next nav)
- [x] Error banner when API calls fail; visual revert on error

**Ready for Staging:** ✅ **YES** — Calendar UI and drag-drop persistence verified

**UAT Focus Areas:**
- Calendar loads events in correct date range
- Drag-and-drop updates persisted correctly (check RescheduledCount)
- Prev/next buttons navigate weeks properly
- Event cancellation or completion dates display correctly
- No orphaned events after page refresh

**Deployment Window:** ~3 hours (includes UAT + database transaction audit)

---

### ✅ **Slice D: Rich-text editor views**

**Implementation Status:**
- [x] CKEditor 5 classic build (GPL v2 open-source)
- [x] Toolbar: bold, italic, lists, tables, links
- [x] HTML preview pane for rendered output
- [x] CKEditor 4 legacy HTML content parity test (7 constructs: h2, strong, em, ul/li, table, links)
- [x] Playwright smoke tests (3 tests: toolbar render, preview section, HTML parity)
- [x] No proprietary CKEditor packages in bundle

**Ready for Staging:** ✅ **YES** — OSS migration complete with HTML parity validation

**UAT Focus Areas:**
- Legacy CKEditor 4 content renders without data loss
- Toolbar buttons (bold, italic, lists) function correctly
- Saved content persists through page reload
- No 3rd-party editor scripts blocked by CSP
- GPL v2 licence terms reviewed by legal

**Deployment Window:** ~2 hours (includes UAT + content audit)

---

## Pre-Deployment Checklist (Staging)

Before each slice is enabled in staging:

- [ ] Feature flag config created in `UiModernizationOptions` (appsettings.Staging.json)
- [ ] Slice flag set to **disabled** initially
- [ ] Smoke test suite runs green in CI
- [ ] Load balancer health check targets SPA entry point
- [ ] Monitoring dashboards configured for SPA error rate
- [ ] Fallback route (legacy WebForms) tested with flag disabled
- [ ] Database backups current (slice data snapshot)
- [ ] Product owner & QA notified of staging date

---

## Staging UAT Workflow

### Step 1: Feature Flag Enable

**Location:** `JB2026.WebApp/appsettings.Staging.json`

Example for Slice A (jobs slice):

```json
{
  "UiModernization": {
    "Slices": {
      "jobs": {
        "DisplayName": "Jobs",
        "Enabled": true,
        "Prefixes": [ "/jobs" ],
        "LegacyBaseUrl": "https://staging-legacy.jb2026.local/"
      }
    },
    "LegacyBaseUrl": "https://staging-legacy.jb2026.local/"
  }
}
```

### Step 2: Smoke Test Execution

Run the Playwright suite in CI or locally:

```bash
# From JB2026.WebApp/ClientApp/
pnpm install
pnpm run test:smoke
```

**Expected result:** All tests pass (0 failures)

### Step 3: Manual UAT (Product Owner or QA)

Assign a **single day** for hands-on testing:

1. **Functional validation** — Verify use cases from original WebForms work identically
2. **Data correctness** — Spot-check 5–10 records; verify calculations and formatting
3. **Performance** — Note page load times; compare to legacy app if feasible
4. **Error handling** — Test with invalid inputs; verify error messages match expected behavior
5. **Accessibility** — Tab navigation, ARIA labels, keyboard-only workflows

**Sign-off:** UAT acceptance captured in a ticket or signed PDF checklist

<details>
<summary>UAT Acceptance Template</summary>

```
Feature Slice: [Slice Name]
Tester: [Name]
Date: [YYYY-MM-DD]
Environment: Staging

Functional Tests:
- [ ] Primary workflow A passes
- [ ] Primary workflow B passes
- [ ] Error conditions handled

Data Validation:
- [ ] Sample records match source data
- [ ] Calculations correct (if applicable)
- [ ] Timestamps in user timezone

Performance:
- [ ] Page load < 3 seconds
- [ ] No console errors
- [ ] Memory usage stable

Sign-off:
- [ ] Approved for production deployment
- [ ] Defects logged as separate issues

Signed: ________________  Date: __________
```

</details>

### Step 4: Rollback Test

Before production, verify the fallback path:

1. Disable the feature flag in staging
2. Refresh the browser
3. Verify the request routes to legacy WebForms
4. Verify legacy app renders correctly

**Expected result:** Zero routing errors; legacy app fully functional

---

## Production Deployment

### Pre-Flight Checklist

- [ ] UAT sign-off ticket exists and is linked
- [ ] All Playwright smoke tests green in CI
- [ ] Rollback plan documented (flag toggle back to disabled)
- [ ] Monitoring alerts configured for error rates > 0.1%
- [ ] Support team briefed on symptoms and rollback procedure
- [ ] Change advisory board (CAB) approval obtained (if required)

### Deployment Steps

1. **Merge to main branch**
   ```bash
   # PR must include:
   # - Feature flag config update (slice Enabled: true)
   # - UAT sign-off ticket reference
   # - Slice readiness checklist link
   git merge --no-ff feature/slice-[name]-production
   ```

2. **Deploy to production**
   ```bash
   # Via your CI/CD pipeline (e.g., Azure DevOps, GitHub Actions)
   # Triggers: dotnet build, npm build, deploy ASP.NET app + SPA bundle
   ```

3. **Monitor for 15 minutes**
   - Error rate dashboard (target: < 0.1%)
   - Load time percentiles (p95 < 3s)
   - API response times (p95 < 500ms)
   - User session count and lock-outs

4. **Report go-live status**
   - @ T+15 min: Declare go-live successful or initiate rollback

### Rollback Procedure

If error rate exceeds 0.1% or critical issues are reported:

1. **Revert feature flag** in production `appsettings.json`:
   ```json
   { "Slices": { "jobs": { "Enabled": false } } }
   ```

2. **Redeploy** (takes ~2–3 minutes with cache TTL)

3. **Verify** legacy route is serving requests (check logs)

4. **Notify** support and product team of rollback status

---

## Post-Deployment (Production)

### Weekly Review (for 4 weeks)

| Metric | Target | Check |
|--------|--------|-------|
| Error rate | < 0.1% | Dashboard alert if exceeded |
| Page load (p95) | < 3 seconds | Synthetic monitoring |
| API latency (p95) | < 500 ms | Backend timing logs |
| User complaints | 0 | Support ticket triage |
| Feature flag cache hits | > 99% | Telemetry / perf instrumentation |

### Monthly Cleanup (After 4 weeks stable)

Once a slice is stable for 4 weeks:

- [ ] Remove legacy WebForms route and controller (if not shared with other slices)
- [ ] Archive legacy view files to backup branch
- [ ] Update deployment documentation
- [ ] Celebrate with the team! 🎉

---

## Slice Deployment Order (Recommended)

**Week 1 (Staging):**
1. Slice A (read-only dashboards) — lowest risk, validates infrastructure
2. Slice D (editor) — isolated feature, fewer dependencies

**Week 2 (Production):**
1. Slice A → Production (low risk, proven in staging)
2. Slice D → Production (if staging A successful)

**Week 3 (Staging):**
3. Slice B (forms) — write operations, higher risk
4. Slice C (scheduler) — complex state, calendar library risk

**Week 4 (Production):**
3. Slice B → Production (if staging B successful)
4. Slice C → Production (final slice completes Phase 6)

---

## Escalation Contacts

| Role | Name | Phone | Slack |
|------|------|-------|-------|
| Product Owner | [Name] | +1 (XXX) XXX-XXXX | @po-jb2026 |
| Tech Lead | [Name] | +1 (XXX) XXX-XXXX | @tech-lead |
| DevOps | [Name] | +1 (XXX) XXX-XXXX | @devops-oncall |
| Support Lead | [Name] | +1 (XXX) XXX-XXXX | @support-lead |

**Incidents out of hours:** Page on-call via [incident routing procedure]

---

## Known Issues & Workarounds

### Issue 1: Virtual scroll causes flicker on rapid resize
- **Trigger:** Resizing browser window while 500+ rows visible
- **Workaround:** Debounce grid resize handler (200 ms)
- **Status:** Candidate for v1.1 improvement

### Issue 2: CKEditor toolbar overflow on mobile viewports < 480px
- **Trigger:** Opening editor on phone (landscape orientation)
- **Workaround:** Hide toolbar on mobile; show simplified mode
- **Status:** Candidate for accessibility sprint

### Issue 3: Feature flag cache stale for 60 seconds after update
- **Trigger:** Admin toggling flag; new users see old value
- **Workaround:** Document 60-second TTL; clear app cache if urgent
- **Status:** Accepted technical debt for Phase 6

---

## Success Criteria

Phase 6 is **complete** when ALL of the following are true:

1. ✅ All 4 slices (A, B, C, D) deployed to production with flags enabled
2. ✅ Zero unresolved Severity 1 (P1) incidents post-deployment for each slice
3. ✅ Error rate remains < 0.1% for 4 consecutive weeks after final slice deployment
4. ✅ UAT sign-off tickets exist and are linked from each deployment PR
5. ✅ Legacy WebForms routes decommissioned (or documented for Phase 8)
6. ✅ Playwright smoke suite runs green in CI/CD pipeline
7. ✅ Legal sign-off obtained for GPL v2 CKEditor 5 licence

---

## Appendix: Configuration Reference

### Feature Flag Configuration Schema

```json
{
  "UiModernization": {
    "LegacyBaseUrl": "https://legacy.example.com/",
    "CacheTtlSeconds": 60,
    "Slices": {
      "[slice-key]": {
        "DisplayName": "Human-readable name",
        "Enabled": true,
        "Prefixes": ["/route-prefix-1", "/route-prefix-2"],
        "Description": "Optional slice description for runbooks"
      }
    }
  }
}
```

### Slice Keys (Canonical)

- `jobs` → Slice A (read-only jobs/dashboard)
- `quotations` → Slice A (read-only quotations)
- `job-form` → Slice B (job create/edit forms)
- `scheduler` → Slice C (calendar/scheduler)
- `editor` → Slice D (rich-text editor)

### Environment-Specific Overrides

- **Development** (local): All slices enabled, no legacy URL needed
- **Staging**: Slices toggled per UAT phase; legacy URL points to staging WebForms
- **Production**: Slices enabled post-UAT; legacy URL optional (fallback only)

---

## References

- [Design Document](../design.md) — Architecture and decisions
- [Requirement Specs](../specs/) — Feature requirements per group
- [API Endpoints](../../../docs/phase-4-backend-and-api-migration/) — Backend contract (Phase 4)
- [Playwright Tests](../../ClientApp/tests/smoke.spec.ts) — E2E test suite
