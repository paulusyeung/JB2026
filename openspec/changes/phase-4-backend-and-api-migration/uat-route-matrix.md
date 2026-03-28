# UAT Route Matrix

## Test Identity

- Username: `admin`
- Password: `password123`

## Representative Sample IDs

- Job / Job Order sample ID: `1e84b2e5-3f73-4d60-9d0d-08dc50c00001`
- Quotation sample ID: `2a84b2e5-3f73-4d60-9d0d-08dc50c00001`

## Route Mapping

| Capability | Legacy Reference | Migrated v1 | Migrated v2 | UAT Notes |
|---|---|---|---|---|
| Auth token | `/api/Token` or `/api/Token/{username}/{password}` | `POST /api/v1/auth/token` | `POST /api/v2/auth/token` | Body uses JSON username/password instead of legacy GET/header pattern |
| Current user profile | `/api/UserInfo` or `/api/User` | `GET /api/v1/user-profiles/me` | `GET /api/v2/user-profiles/me` | Requires bearer token |
| User profile by username | `/api/UserInfo/{username}` | `GET /api/v1/user-profiles/{username}` | `GET /api/v2/user-profiles/{username}` | Requires bearer token |
| Jobs range | `/api/Job/{starton}/{days}` | `GET /api/v1/jobs/range?startOn=2026-03-27&days=10` | `GET /api/v2/jobs/range?startOn=2026-03-27&days=10` | Range/list parity route |
| Job detail | `/api/Job/{id}` | `GET /api/v1/jobs/{id}` | `GET /api/v2/jobs/{id}` | Use sample job ID |
| Job style titles | `/api/Job/details/{id}` | `GET /api/v1/jobs/{id}/details` | `GET /api/v2/jobs/{id}/details` | Use sample job ID |
| Job orders list | `/api/JobOrders` | `GET /api/v1/job-orders` | `GET /api/v2/job-orders` | List parity route |
| Job order detail | `/api/JobOrders/{id}` | `GET /api/v1/job-orders/{id}` | `GET /api/v2/job-orders/{id}` | Use sample job order ID |
| Quotation search | `/api/Qt/Keyword/{keyword}` | `GET /api/v1/quotations/search/ABC` | `GET /api/v2/quotations/search/ABC` | Search parity route |
| Quotation range | `/api/Qt/{starton}/{days}` | `GET /api/v1/quotations?startOn=2026-03-27&days=10` | `GET /api/v2/quotations?startOn=2026-03-27&days=10` | Range parity route |
| Quotation PDF | `/api/Qt/pdf/{id}` | `GET /api/v1/quotations/{id}/pdf` | `GET /api/v2/quotations/{id}/pdf` | Use sample quotation ID |

## Suggested UAT Order

1. Authenticate and capture bearer token on v1 and v2 routes.
2. Validate user profile lookup.
3. Validate jobs range and one job detail.
4. Validate job orders list and one order detail.
5. Validate quotation search, range, and one PDF response.
6. Record any business-significant differences before sign-off.
