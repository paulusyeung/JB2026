## ADDED Requirements

### Requirement: Each Migrated Slice Must Be a Production-Quality ASP.NET Core Implementation
Every migrated API domain slice SHALL be implemented in ASP.NET Core using DI, structured logging, request validation, and error handling middleware from the shared infrastructure library.

#### Scenario: Slice endpoint handles valid request
- **WHEN** a valid request is sent to a migrated slice endpoint
- **THEN** the response SHALL match the legacy baseline in status code, body shape, and key headers

#### Scenario: Slice endpoint handles invalid request consistently
- **WHEN** an invalid request is sent to a migrated slice endpoint
- **THEN** the response SHALL return a structured problem details error in ASP.NET Core standard format

### Requirement: HttpContext.Current Must Not Appear in Migrated Code
No migrated class or method SHALL reference `System.Web.HttpContext.Current` or any OWIN/Katana context accessor.

#### Scenario: Static analysis confirms no HttpContext.Current usage
- **WHEN** static analysis or a grep scan is run on migrated projects
- **THEN** zero occurrences of `HttpContext.Current` SHALL be found
