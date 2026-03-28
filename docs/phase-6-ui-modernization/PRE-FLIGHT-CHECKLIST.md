# Phase 6 Slice Deployment — Pre-Flight Checklist

**Instructions:** Use this checklist 1 day before each slice deployment to staging/production.

---

## **SLICE A: Read-Only Lists & Dashboards**

**Deployment Date:** _______________  
**Assigned To:** _______________  
**Sign-off:** _______________

### Code & Build

- [ ] Dashboard, Jobs, Quotations views all present in `src/views/`
- [ ] API endpoints verified: `GET /api/v2/jobs/range`, `GET /api/v2/quotations`
- [ ] `pnpm run build` completes with 0 errors
- [ ] `dotnet build JB2026.sln` completes with 0 errors
- [ ] Playwright smoke test count: **3 tests** (dashboard, jobs, quotations)

### Testing

- [ ] `pnpm run test:smoke` passes all 3 tests locally
- [ ] CI pipeline runs smoke tests green (screenshot attached)
- [ ] Virtual scroll behavior tested with 500+ rows (if applicable)
- [ ] No TypeScript errors in IDE

### Staging Prep

- [ ] Feature flag config created in `appsettings.Staging.json`
- [ ] Slice enabled: `"Enabled": true`
- [ ] Load test simulated (5–10 concurrent users, 5 min duration)
- [ ] Error rate baseline captured: _______ %
- [ ] Database backups current

### UAT Sign-Off

- [ ] UAT date scheduled: _______________
- [ ] Product owner assigned: _______________
- [ ] Fallback path tested (flag disabled → legacy route)
- [ ] UAT acceptance obtained and filed: [Ticket #]
- [ ] No critical defects logged

### Production Readiness

- [ ] PR merged to main with flag config
- [ ] UAT ticket referenced in commit message
- [ ] Monitoring dashboard configured
- [ ] Support team briefed
- [ ] Rollback plan documented and tested

**Status:** ☐ READY  ☐ BLOCKED (see notes)  
**Notes:** ___________________________________________________________________________

---

## **SLICE B: Create/Edit Forms**

**Deployment Date:** _______________  
**Assigned To:** _______________  
**Sign-off:** _______________

### Code & Build

- [ ] JobOrderForm component present in `src/components/forms/`
- [ ] JobsView.vue wired with New/Edit buttons and dialog
- [ ] API endpoints verified: `POST /api/v2/jobs`, `PATCH /api/v2/jobs/{id}`
- [ ] `pnpm run build` completes with 0 errors
- [ ] `dotnet build JB2026.sln` completes with 0 errors
- [ ] Playwright smoke test count: **4 tests** (login, detail, new dialog, cancel)

### Testing

- [ ] `pnpm run test:smoke` passes all 4 tests locally
- [ ] CI pipeline runs smoke tests green (screenshot attached)
- [ ] Form validation tested: required fields, date ordering, positive numbers
- [ ] Error banner displays on API failure
- [ ] No TypeScript errors in IDE

### Staging Prep

- [ ] Feature flag config created in `appsettings.Staging.json`
- [ ] Slice enabled: `"Enabled": true`
- [ ] Test new job creation with 10 test records
- [ ] Test edit existing job update with validation
- [ ] Database transaction audit for ACID compliance
- [ ] Error rate baseline captured: _______ %

### UAT Sign-Off

- [ ] UAT date scheduled: _______________
- [ ] Product owner assigned: _______________
- [ ] Form UX tested: field labels, placeholder text, error messages
- [ ] Data integrity audit completed
- [ ] UAT acceptance obtained and filed: [Ticket #]
- [ ] No critical defects logged

### Production Readiness

- [ ] PR merged to main with flag config
- [ ] UAT ticket referenced in commit message
- [ ] Monitoring dashboard configured
- [ ] Support team briefed on form validation rules
- [ ] Rollback plan documented and tested

**Status:** ☐ READY  ☐ BLOCKED (see notes)  
**Notes:** ___________________________________________________________________________

---

## **SLICE C: Scheduler/Calendar**

**Deployment Date:** _______________  
**Assigned To:** _______________  
**Sign-off:** _______________

### Code & Build

- [ ] SchedulerView.vue with FullCalendar integrated and working
- [ ] API endpoints verified: `GET /api/v2/job-schedules/range`, `PATCH /api/v2/job-schedules/{id}/time`
- [ ] API unit tests pass: **13 tests** (run `dotnet test JB2026.Api.ParityTests --filter JobSchedulesControllerTests`)
- [ ] `pnpm run build` completes with 0 errors
- [ ] `dotnet build JB2026.sln` completes with 0 errors
- [ ] Playwright smoke test count: **2 tests** (calendar render, navigation)

### Testing

- [ ] `pnpm run test:smoke` passes all 2 tests locally
- [ ] CI pipeline runs smoke tests green (screenshot attached)
- [ ] Drag-and-drop event rescheduling tested and persisted correctly
- [ ] Event cancellation / completion dates tested
- [ ] RescheduledCount increments correctly on drag
- [ ] Error banner appears and visual revert works on API failure

### Staging Prep

- [ ] Feature flag config created in `appsettings.Staging.json`
- [ ] Slice enabled: `"Enabled": true`
- [ ] Test calendar with 50+ events across 2-week range
- [ ] Test drag-and-drop on multiple browsers (Chrome, Firefox, Safari)
- [ ] Database transaction audit for rescheduled records
- [ ] Error rate baseline captured: _______ %

### UAT Sign-Off

- [ ] UAT date scheduled: _______________
- [ ] Product owner assigned: _______________
- [ ] Events load in correct date range
- [ ] Schedule updates persist and sync correctly
- [ ] Event status and priority display correctly
- [ ] UAT acceptance obtained and filed: [Ticket #]
- [ ] No critical defects logged

### Production Readiness

- [ ] PR merged to main with flag config
- [ ] UAT ticket referenced in commit message
- [ ] Monitoring includes calendar API latency
- [ ] Support team briefed on event rescheduling workflows
- [ ] Rollback plan tested (includes 1-day event recovery scenario)

**Status:** ☐ READY  ☐ BLOCKED (see notes)  
**Notes:** ___________________________________________________________________________

---

## **SLICE D: Rich-Text Editor**

**Deployment Date:** _______________  
**Assigned To:** _______________  
**Sign-off:** _______________

### Code & Build

- [ ] EditorView.vue with CKEditor 5 classic build
- [ ] RichTextEditor.vue component with toolbar: bold, italic, lists, tables, links
- [ ] No proprietary CKEditor packages in bundle scan
- [ ] `pnpm run build` completes with 0 errors
- [ ] `dotnet build JB2026.sln` completes with 0 errors
- [ ] Playwright smoke test count: **3 tests** (toolbar, preview, HTML parity)

### Testing

- [ ] `pnpm run test:smoke` passes all 3 tests locally
- [ ] CI pipeline runs smoke tests green (screenshot attached)
- [ ] CKEditor 4 legacy HTML parity test passes (h2, strong, em, ul/li, table, links)
- [ ] HTML preview pane renders correctly
- [ ] Editor content persists through page reload

### Staging Prep

- [ ] Feature flag config created in `appsettings.Staging.json`
- [ ] Slice enabled: `"Enabled": true`
- [ ] Test legacy content sample renders without data loss
- [ ] Test new content creation and edit
- [ ] CSP policy checked (no blocked editor scripts)
- [ ] Load test with editor open (memory/CPU impact measured)
- [ ] Error rate baseline captured: _______ %

### UAT Sign-Off

- [ ] UAT date scheduled: _______________
- [ ] Product owner assigned: _______________
- [ ] Legal sign-off obtained for GPL v2 CKEditor 5: ☐ YES ☐ PENDING
- [ ] Legacy content migration tested
- [ ] Toolbar buttons functional (bold, italic, lists, etc.)
- [ ] UAT acceptance obtained and filed: [Ticket #]
- [ ] No critical defects logged

### Production Readiness

- [ ] PR merged to main with flag config
- [ ] UAT ticket referenced in commit message
- [ ] Legal review documented in ticket/PR
- [ ] Monitoring includes editor API timeouts
- [ ] Support team briefed on CKEditor limitations
- [ ] Rollback plan documented (includes content backup scenario)

**Status:** ☐ READY  ☐ BLOCKED (see notes)  
**Notes:** ___________________________________________________________________________

---

## **Global Deployment Checklist**

Before **any** slice goes to production:

### Infrastructure

- [ ] Load balancer health checks updated for SPA endpoint
- [ ] CDN cache headers configured for SPA bundle (_app.js, _app.css, etc.)
- [ ] Database connection pool size adequate (10–20 conns recommended)
- [ ] SSL certificate valid and renewed if within 30 days of expiry

### Monitoring & Alerts

- [ ] Error rate alert configured: > 0.1% triggers page
- [ ] Response time alert configured: p95 > 3s triggers page
- [ ] API latency metric pushed to dashboard
- [ ] Synthetic uptime check configured (5 min interval)
- [ ] Support team has read access to alerts/dashboard

### Documentation & Communication

- [ ] Deployment runbook reviewed by support team
- [ ] Feature flag config screenshot attached to deploy ticket
- [ ] Rollback steps documented and walkthrough completed
- [ ] Change advisory board (CAB) approval obtained (if required)
- [ ] Go-live communication sent to stakeholders (date/time/contact)

### Compliance & Legal

- [ ] Code review completed and approved (2 reviewers minimum)
- [ ] No secrets committed (API keys, connection strings, etc.)
- [ ] GPL v2 CKEditor licence reviewed by legal (Slice D only)
- [ ] DevExpress bundle scan confirms zero proprietary references
- [ ] UAT acceptance artefact linked in PR

### Post-Deployment (Immediate)

- [ ] Monitor error/latency metrics for 15 minutes
- [ ] Sample 5 users; ask if experience is as expected
- [ ] Check browser console for 404 / 403 errors
- [ ] Verify legacy fallback path still works (flag disabled test)
- [ ] Document any issues in support ticket

**Status:** ☐ READY TO DEPLOY  ☐ BLOCKED (see notes)  
**Notes:** ___________________________________________________________________________

---

## **Sign-Off**

**Tech Lead:** _____________________________ Date: _____________

**Product Owner:** _________________________ Date: _____________

**DevOps/Ops:** ____________________________ Date: _____________

**Support Lead:** ___________________________ Date: _____________

---

*For questions or issues, escalate to [Incident Process URL] or page on-call via [Routing Number]*
