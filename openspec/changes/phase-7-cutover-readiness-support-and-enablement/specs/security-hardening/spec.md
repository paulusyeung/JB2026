## ADDED Requirements

### Requirement: Zero Critical or High OWASP Top 10 Findings Before Cutover
The OWASP Dependency-Check scan and Semgrep SAST scan MUST both produce zero critical (CVSS ≥ 9.0) or high (CVSS ≥ 7.0) findings. Any finding at these levels MUST be remediated before the go/no-go checklist is signed.

#### Scenario: Dependency scan is clean before go/no-go meeting
- **WHEN** OWASP Dependency-Check is run against all NuGet and NPM dependencies
- **THEN** zero findings with CVSS ≥ 7.0 SHALL be present in the report

#### Scenario: SAST scan is clean before go/no-go meeting
- **WHEN** Semgrep is run against the full codebase
- **THEN** zero critical or high severity findings SHALL be present in the report

### Requirement: No Secrets May Exist in the Repository or Configuration Files
A secrets scan MUST be run across the entire git history and all configuration files. Any detected secrets MUST be rotated and removed before cutover.

#### Scenario: Secrets scan finds no committed secrets
- **WHEN** a secrets scanning tool (e.g., truffleHog or gitleaks) is run against the repository
- **THEN** zero detected secrets SHALL be present in the repository history or configuration files

### Requirement: All HTTP Endpoints Must Enforce HTTPS and HSTS
All production HTTP endpoints MUST redirect to HTTPS. HSTS headers MUST be present on all responses from the production host.

#### Scenario: HTTP request is redirected to HTTPS
- **WHEN** an HTTP request is made to any production endpoint
- **THEN** the response SHALL be a 301/308 redirect to the HTTPS equivalent and the HSTS header SHALL be present on the HTTPS response
