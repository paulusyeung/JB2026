## ADDED Requirements

### Requirement: OWIN and Thinktecture Middleware Must Be Fully Replaced
All OWIN/Katana middleware and Thinktecture IdentityModel packages MUST be removed from the migrated solution and replaced with native ASP.NET Core middleware.

#### Scenario: No OWIN or Thinktecture references in solution
- **WHEN** the migrated solution's package references are scanned
- **THEN** zero references to OWIN, Katana, or Thinktecture IdentityModel packages SHALL exist

### Requirement: CORS Policy Must Be Defined Using Native ASP.NET Core
CORS SHALL be configured using `AddCors` and `UseCors` in the ASP.NET Core pipeline with named policies.

#### Scenario: CORS headers are present on allowed cross-origin response
- **WHEN** a cross-origin request is made to an endpoint with an active CORS policy
- **THEN** the response SHALL include the correct `Access-Control-Allow-Origin` header
