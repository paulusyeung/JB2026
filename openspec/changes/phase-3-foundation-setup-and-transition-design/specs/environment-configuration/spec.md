## ADDED Requirements

### Requirement: Application Must Support Environment-Specific Configuration Without Code Changes
The application SHALL load configuration appropriate to the runtime environment (dev, test, pre-prod, prod) entirely through environment variables and injected secrets, with no environment-specific values hard-coded in source.

#### Scenario: Application starts with environment-scoped configuration
- **WHEN** the application is started with a specific environment variable set (e.g., ASPNETCORE_ENVIRONMENT=Production)
- **THEN** the correct environment-scoped configuration values SHALL be loaded

#### Scenario: No secrets appear in source control
- **WHEN** the repository is scanned for secrets
- **THEN** no API keys, passwords, or connection strings SHALL appear in committed files
