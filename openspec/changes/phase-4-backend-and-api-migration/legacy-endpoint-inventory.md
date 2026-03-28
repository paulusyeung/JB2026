# Legacy Endpoint Inventory - JB5.API and JB5.REST

## Overview

This document catalogs all Web API 2 endpoints from the legacy JB5.API and JB5.REST projects that must be migrated to ASP.NET Core in Phase 4. Endpoints are organized by domain and business capability.

**Source Projects:**
- JB5.API (C:\Projects\JB2015\JB5.API)
- JB5.REST (C:\Projects\JB2015\JB5.REST)

---

## JB5.API Endpoints

### Domain: Orders & Job Management

#### Controller: JobOrdersController
Manages legacy job order data

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/JobOrders` | Yes (JWT) | IQueryable<JobOrder> | Get list of all job orders (top 100) |
| GET | `/api/JobOrders/{id}` | No | JobOrder | Get specific job order by ID |
| POST | `/api/JobOrders` | No | JobOrder | Create new job order |
| PUT | `/api/JobOrders/{id}` | No | void (204) | Update existing job order |
| DELETE | `/api/JobOrders/{id}` | No | JobOrder | Delete job order |

### Domain: Prints & Scheduling

#### Controller: PrintsController
Manages print jobs and scheduling

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/Prints/Pending` | Yes (JWT) | IQueryable<vwJobSchedule_AvailableList> | Get pending print jobs for user's machine |
| GET | `/api/Prints/Scheduled` | Yes (JWT) | JSON (vwJobScheduleList_OnAir) | Get scheduled jobs for user's machine |
| GET | `/api/Prints/Scheduled/{id}` | Yes (JWT) | JSON (vwJobScheduleList_OnAir) | Get scheduled jobs for specific machine (0=all, 1-5=machine ID) |
| GET | `/api/Prints/Completed` | Yes (JWT) | IQueryable<vwJobScheduleList> | Get completed jobs for user's machine (today) |
| GET | `/api/Prints/Completed/{id}` | Yes (JWT) | IQueryable<vwJobScheduleList> | Get completed jobs for specific machine (today) |

### Domain: Authentication & Tokens

#### Controller: TokenController
Manages JWT token generation and validation

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/Token` | No (Anonymous) | string (JWT) | Generate token with credentials in request headers (username, password) |
| GET | `/api/Token/{username}/{password}` | No (Anonymous) | string (JWT) | Generate token with credentials in URL parameters |

### Domain: User Information

#### Controller: UserInfoController
Manages user profile information

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/UserInfo` | Yes (JWT) | UserInfoEx | Get current authenticated user's profile |
| GET | `/api/UserInfo/{username}` | Yes (JWT) | UserInfoEx | Get specific user's profile by username |

---

## JB5.REST Endpoints

### Domain: Orders & Job Management

#### Controller: JobController
Manages job orders and scheduling

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/Job/{id}` | Yes (JWT) | vwJobList (JSON) | Get job details by Order ID |
| GET | `/api/Job/details/{id}` | Yes (JWT) | Array (JSON) | Get job PD style titles/details |
| GET | `/api/Job/{starton}/{days}` | Yes (JWT) | Array (JSON) | Get jobs within date range (start date + number of days) |
| GET | `/api/Job/ByMonth/{id}/{date}` | Yes (JWT) | Array (JSON) | Get jobs for specific month, filtered by client ID (0=all) |
| *Additional methods exist - full scan needed* | | | | |

#### Controller: ScheduleController
Manages job scheduling

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: Quotations

#### Controller: QuotationController
Manages quotation/quote data

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/Qt/{starton}/{days}` | Yes (JWT) | Array (JSON) | Get quotations within date range |
| *Additional methods exist - full scan needed* | | | | |

### Domain: Users & Authentication

#### Controller: UserController
Manages user profiles and authentication

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| GET | `/api/User` | Yes (JWT) | UserEx (JSON) | Get current authenticated user's profile |
| GET | `/api/User?userkey={key}` | Yes (JWT) | UserEx (JSON) | Get user by userkey parameter |
| *Additional methods exist - full scan needed* | | | | |

#### Controller: TokenController
User authentication tokens (REST variant)

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: File Management

#### Controller: FileAgentController
Manages file uploads and storage

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

#### Controller: CloudDiskController
Cloud storage integration

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: Stock & Inventory

#### Controller: StockController
Stock/inventory management

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: Suppliers

#### Controller: SupplierController
Supplier management

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: Dashboard & Reporting

#### Controller: DashboardController
Dashboard data and KPIs

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: Integrations

#### Controller: FCMController
Firebase Cloud Messaging integration

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

#### Controller: FCMHistoryController
FCM history and logs

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

#### Controller: WebhookSubscriptionController
Webhook management and subscriptions

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

### Domain: SML (Specific Module/Service)

#### Controller: SMLController
SML-related endpoints

| HTTP Method | Route | Auth Required | Return Type | Description |
|-------------|-------|---------------|-------------|-------------|
| *To be determined - requires full file scan* | | | | |

---

## Migration Considerations

### Authentication Strategy
- **Current:** JWT-based authentication via `[JwtAuthentication]` filter attributes
- **Common patterns:** Some endpoints accept credentials in headers (username/password), others use JWT tokens
- **Target:** Migrate to native ASP.NET Core authentication middleware (details from Phase 1 auth architecture spike)

### Common Characteristics
1. **Web API 2 Framework:** All endpoints inherit from `ApiController`
2. **Route Attributes:** Both conventional (`/api/ControllerName`) and explicit `[Route]` attributes used
3. **Async Support:** Several endpoints use `async/await` Task-based returns
4. **Data Access:** Direct EF6 DbContext usage; must be replaced with EF Core
5. **HttpActionResult Returns:** Mix of typed `IHttpActionResult` and direct return types (IQueryable, string, etc.)
6. **Static HttpContext:** JwtAuthentication filter uses Thread.CurrentPrincipal and ClaimsPrincipal
7. **Error Handling:** Minimal error handling; returns NotFound(), BadRequest(), etc.

### Priority Ranking (Recommended Migration Order)

**Phase 1 - Critical Path (Auth & Foundation)**
1. TokenController (auth foundation)
2. UserInfoController / UserController (user identity)
3. JobOrdersController (core domain)

**Phase 2 - Business Core**
4. JobController
5. QuotationController
6. PrintsController / ScheduleController

**Phase 3 - Supporting Systems**
7. Supplier, Stock, File/Cloud, SML controllers
8. FCM and Webhook integrations
9. Dashboard/reporting

---

## Next Steps

1. **Complete Endpoint Scan:** Read all remaining controller files to fully document endpoints marked as "requires full file scan"
2. **Business Owner Interview:** Confirm endpoint priority with product owner based on business criticality
3. **Dependency Analysis:** Map endpoint interdependencies to identify safe migration sequencing
4. **Baseline Snapshot Collection:** Capture live response from each endpoint for parity testing (Task 1.3)
5. **Coexistence Routing Design:** Define routing prefix convention and load-balancing strategy (Task 1.4)

**Status:** Task 1.1 in progress - initial mapping complete, detailed scan pending for partially documented controllers.

---

**Last Updated:** 2026-03-27
**Owner:** API Lead / Platform Team
