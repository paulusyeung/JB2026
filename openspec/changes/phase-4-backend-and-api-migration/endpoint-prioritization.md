# Endpoint Domain Prioritization Matrix - Phase 4 Migration

## Executive Summary

Endpoints prioritized by **business criticality** (revenue impact, operational dependency) and **technical dependency depth** (which endpoints must migrate before others). This ordering minimizes risk and enables parallel sprints.

---

## Prioritization Criteria

### Business Criticality Levels
- **CRITICAL:** Directly impacts revenue, customer-facing operations, or legal/compliance. Migration blocks delivery.
- **HIGH:** Impacts operations or enables other critical flows. Migration needed before non-critical features.
- **MEDIUM:** Supports operations but has workarounds. Migration can be scheduled flexibly.
- **LOW:** Legacy/deprecated or minimal downstream impact. Migrate last or post-MVP.

### Dependency Depth
- **Leaf Node (0):** No other migrated endpoints depend on this endpoint
- **1-Depth:** Other endpoints depend on this one to function
- **2+ Depth:** Multiple downstream endpoints or cross-domain dependencies

---

## Domain Priority Ranking

### **PRIORITY 1 - Critical Path (Weeks 1-2)**
**Total: 2 domains | Est. Effort: 2-3 sprints**

#### 1.1 Authentication & Identity (CIRITCal)
- **Domain:** TokenController, UserInfoController / UserController
- **Business Criticality:** CRITICAL - all other endpoints depend on valid auth
- **Dependency Depth:** 0 (foundational, no upstream dependencies)
- **Endpoints:**
  - TokenController: `GET /api/Token`, `GET /api/Token/{username}/{password}`
  - UserInfoController (JB5.API): `GET /api/UserInfo`, `GET /api/UserInfo/{username}`
  - UserController (JB5.REST): `GET /api/User`, `GET /api/User?userkey={key}`
- **Key Considerations:**
  - All other endpoints have `[JwtAuthentication]` filter - auth replacement must be in place first
  - Blocking dependency for middleware replacement (Phase 2, Task 2.4)
  - Legacy JWT implementation uses header/querystring credentials - must support until all clients migrated
- **Recommended Approach:** Implement auth first, establish new token flow, verify all downstream migration points

#### 1.2 Core Job Management (CRITICAL)
- **Domain:** JobOrdersController (JB5.API), JobController (JB5.REST)
- **Business Criticality:** CRITICAL - revenue-generating feature, primary business domain
- **Dependency Depth:** 1 (Print scheduling and other operations depend on job data)
- **Endpoints:**
  - JobOrdersController (JB5.API): CRUD operations `/api/JobOrders[/{id}]`
  - JobController (JB5.REST): Search/read endpoints `/api/Job/*`
- **Key Considerations:**
  - JB5.API version is transactional (PUT, POST, DELETE) - orders originate here
  - JB5.REST version is read-heavy and search-focused - supports reporting and dashboards
  - Both use EF6 DbContext - requires EF Core mapping completion first
  - Print scheduling (Priority 2.1) depends on job data availability
- **Recommended Approach:** Migrate JB5.API JobOrders first (CRUD), then JB5.REST JobController (read queries)

---

### **PRIORITY 2 - Operational Support (Weeks 2-4)**
**Total: 4 domains | Est. Effort: 4-5 sprints**

#### 2.1 Print Scheduling & Operations (HIGH)
- **Domain:** PrintsController (JB5.API), ScheduleController (JB5.REST)
- **Business Criticality:** HIGH - operational capability, machines depend on job schedules
- **Dependency Depth:** 1 (depends on Core Job Management migrated first)
- **Endpoints:**
  - PrintsController: `GET /api/Prints/Pending`, `GET /api/Prints/Scheduled{/id}`, `GET /api/Prints/Completed{/id}`
  - ScheduleController: *[full scan needed]* - schedule CRUD and queries
- **Key Considerations:**
  - Heavy use of user machine-type metadata XML parsing - complex business logic
  - Real-time operational endpoint - may have higher SLA requirements
  - Uses vwJobSchedule_* views - verify EF Core mapping supports these
- **Prerequisite:** Job Management (Priority 1.2) must be operational
- **Recommended Approach:** Migrate after jobs are stable; reuse job query layer

#### 2.2 Quotations & Pricing (HIGH)
- **Domain:** QuotationController
- **Business Criticality:** HIGH - quotation generation affects sales pipeline
- **Dependency Depth:** 0 (standalone, though may feed into jobs downstream)
- **Endpoints:**
  - QuotationController: `GET /api/Qt/{starton}/{days}`, *[additional methods - full scan needed]*
- **Key Considerations:**
  - Quote generation involves complex cost calculations
  - May have custom report-generation dependencies
  - Read-heavy; good candidate for parallel migration
- **Recommended Approach:** Can start in parallel with job migration; focus on read paths first

#### 2.3 Supplier & Stock Management (MEDIUM-HIGH)
- **Domain:** SupplierController, StockController
- **Business Criticality:** MEDIUM-HIGH - procurement and inventory critical but may have fallback processes
- **Dependency Depth:** 1 (may feed cost calculations in quotations, impact on job planning)
- **Endpoints:**
  - SupplierController: *[full scan needed]*
  - StockController: *[full scan needed]*
- **Key Considerations:**
  - Inventory is operational but may have manual workarounds
  - Can be migrated in parallel with Priority 2 items
- **Prerequisite:** None, independent of other domains
- **Recommended Approach:** Group together; can run parallel sprint

#### 2.4 File Management & Cloud Storage (MEDIUM)
- **Domain:** FileAgentController, CloudDiskController
- **Business Criticality:** MEDIUM - file storage for job assets, not directly revenue-generating
- **Dependency Depth:** 1 (jobs may reference attachments, but may not be required for initial operation)
- **Endpoints:**
  - FileAgentController: *[full scan needed]*
  - CloudDiskController: *[full scan needed]*
- **Key Considerations:**
  - May have external cloud provider dependencies (AWS/Azure/GCP)
  - Fallback: local file storage may be available
- **Recommended Approach:** Post-MVP, can be lifted after core domains stable

---

### **PRIORITY 3 - Reporting & Integration (Weeks 4-6)**
**Total: 5 domains | Est. Effort: 3-4 sprints**

#### 3.1 Dashboard & Analytics (MEDIUM)
- **Domain:** DashboardController
- **Business Criticality:** MEDIUM - operational visibility, not critical path
- **Dependency Depth:** 2 (uses data from Jobs, Quotations, Schedules)
- **Endpoints:**
  - DashboardController: *[full scan needed]* - likely read-heavy aggregation queries
- **Prerequisite:** Priority 1.2 (Jobs), Priority 2.2 (Quotations), Priority 2.1 (Schedules) must be operational
- **Recommended Approach:** Migrate after core domains; may cache results to reduce load

#### 3.2 Notifications (Firebase Cloud Messaging) (MEDIUM)
- **Domain:** FCMController, FCMHistoryController
- **Business Criticality:** MEDIUM - push notifications enhance UX but not required for core functionality
- **Dependency Depth:** 1 (depends on user/job context)
- **Endpoints:**
  - FCMController: *[full scan needed]*
  - FCMHistoryController: *[full scan needed]*
- **Key Considerations:**
  - External dependency on Firebase
  - History endpoint is append-only, low-risk migration
- **Recommended Approach:** Can run parallel; firewall external dependencies, plan mocking for CI

#### 3.3 Webhooks & Integrations (MEDIUM)
- **Domain:** WebhookSubscriptionController
- **Business Criticality:** MEDIUM - third-party integrations, not core revenue path
- **Dependency Depth:** 1 (triggered by core domain events)
- **Endpoints:**
  - WebhookSubscriptionController: *[full scan needed]* - likely CRUD + trigger endpoints
- **Key Considerations:**
  - Event-driven architecture; must preserve order/delivery guarantees
  - External consumers may depend on specific payloads
- **Prerequisite:** None technical; Priority 1.2 (Jobs) operational for event testing
- **Recommended Approach:** Migrate subscriptions separately from event triggers; publish migration timeline to consumers

#### 3.4 SML (Specialized Module) (MEDIUM)
- **Domain:** SMLController
- **Business Criticality:** MEDIUM - unclear from name; requires stakeholder clarification
- **Dependency Depth:** *[TBD - requires business context]*
- **Endpoints:**
  - SMLController: *[full scan needed]*
- **Key Considerations:**
  - Requires domain expert explanation
- **Recommended Approach:** Schedule clarification meeting before migration planning

---

## Migration Sequencing

### **Sprint 1 (Week 1)**
- Auth/Token endpoints → New ASP.NET Core middleware
- User endpoints → DI-based user context
- *Est. parallelism:* 80% (2 teams can work pre-middleware, then integrate)

### **Sprint 2-3 (Weeks 2-3)**
- **Track A:** JobOrders CRUD (JB5.API) in parallel with JobController reads (JB5.REST)
- **Track B:** QuotationController + SupplierController/StockController
- *Est. parallelism:* 60% (need shared EF Core context layer)

### **Sprint 4 (Week 4)**
- PrintScheduling (depends on Jobs stable)
- FileAgent/CloudDisk (can start once Jobs + Scheduling framework ready)
- *Est. parallelism:* 40% (waiting on dependency completion)

### **Sprint 5-6 (Weeks 5-6)**
- Dashboard (depends on Jobs, Quotes, Schedules)
- FCM, Webhooks, SML
- *Est. parallelism:* 50% (minimal cross-domain dependencies)

---

## Dependency Graph

```
Auth & Identity (P1.1)
├─→ Job Management (P1.2)
│   ├─→ Print Scheduling (P2.1)
│   │   └─→ Dashboard (P3.1)
│   ├─→ Quotations (P2.2)
│   └─→ FCM/Webhooks (P3.2/P3.3)
├─→ Supplier/Stock (P2.3)
│   └─→ Cost Calculations (Quotations P2.2)
├─→ File Management (P2.4)
└─→ SML (P3.4)

[Independent paths:]
- Notifications (P3.2)
- Integrations (P3.3)
```

---

## Risk Mitigation by Priority

### Priority 1 Risks
- **Risk:** Auth migration blocks all downstream work
- **Mitigation:** Parallel middleware setup in Phase 2; test coexistence early
- **Contingency:** Keep legacy auth service running until all endpoints migrated

### Priority 2 Risks
- **Risk:** EF Core mapping incomplete for Job queries
- **Mitigation:** Complete EF Core data layer before Sprint 2 starts; validate views
- **Contingency:** Use ADO.NET for complex queries if EF Core view mapping fails

### Priority 3 Risks
- **Risk:** Dashboard depends on multiple unstable domains
- **Mitigation:** Build aggregation caching layer; make dashboard optional early
- **Contingency:** Display stale data until dependencies stable

---

## Open Questions for Product Owner / Stakeholder Alignment

1. **SML Domain:** What is SML's function? Is it critical or legacy?
2. **File Dependencies:** Are file attachments strictly required for job operations, or optional?
3. **External Integrations:** Are webhook consumers (3rd-party) expecting guaranteed message order and delivery?
4. **Dashboard SLA:** What's the acceptable latency for dashboard data refresh?
5. **Mobile Client:** Is Job.Book.Mobile production and will it consume these APIs during migration?
6. **Quota/Licensing:** Are there per-endpoint rate limits or licensing constraints?

---

## Next Steps

1. **Validate with Product Owner:** Confirm Priority 1-2 ordering; get SML clarification
2. **Complete Endpoint Scans:** Read all remaining controller files for full endpoint list
3. **Capture Baseline Snapshots:** Task 1.3 - record live responses for all endpoints
4. **Design Coexistence Routing:** Task 1.4 - set routing prefix and load-balancing strategy
5. **Kickoff Sprint 1:** Begin Auth migration

---

**Status:** Task 1.2 preliminary prioritization complete. Awaiting stakeholder validation and full endpoint scan completion.

**Last Updated:** 2026-03-27
**Owner:** API Lead / Platform Team
