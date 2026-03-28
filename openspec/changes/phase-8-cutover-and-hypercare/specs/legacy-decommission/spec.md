## ADDED Requirements

### Requirement: JB2015 Application and Infrastructure Must Not Be Decommissioned Until Hypercare Is Closed
The legacy JB2015 application servers, databases, and coexistence routing infrastructure MUST remain operational until the hypercare exit is formally signed off by the technical lead.

#### Scenario: Decommission is gated on hypercare sign-off
- **WHEN** a decommission task is raised for JB2015 infrastructure
- **THEN** it SHALL be blocked until the hypercare exit sign-off document is referenced in the task

### Requirement: Coexistence Routing and Feature Flag Infrastructure Must Be Removed After Decommission
All feature flag tables, coexistence routing middleware, and legacy proxy configuration MUST be removed from the JB2026 codebase and infrastructure as part of the decommission activity.

#### Scenario: No coexistence code remains after decommission
- **WHEN** the decommission branch is merged
- **THEN** a grep for feature-flag middleware references and coexistence routing handlers SHALL return zero results in the production codebase

### Requirement: The Repository Must Be Prepared for Public Open-Source Publication After Decommission
After JB2015 is decommissioned, the JB2026 repository MUST include: an OSI-approved `LICENSE` file, a `CONTRIBUTING.md`, a `SECURITY.md` with a vulnerability disclosure policy, and passing CI badge.

#### Scenario: Repository is ready for public publication
- **WHEN** the open-source publication checklist is reviewed
- **THEN** `LICENSE`, `CONTRIBUTING.md`, `SECURITY.md`, and a CI status badge SHALL all be present in the repository root and the repository SHALL have no secrets in its git history
