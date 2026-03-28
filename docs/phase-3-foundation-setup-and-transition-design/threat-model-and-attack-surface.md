# Threat Model and Attack Surface Analysis

## Scope
Target architecture composed of JB2026.Api, JB2026.Rest, JB2026.WebApp, shared infrastructure, and data access dependencies.

## Primary Attack Surface
- Public HTTP endpoints exposed by API and REST hosts.
- Authentication/session token handling paths.
- Configuration and secret injection channels.
- Inter-service and data-store connectivity.
- CI/CD supply chain and dependency ingestion.

## High-Priority Threats
| ID | Threat | Priority | Mitigation Action | Decision |
|---|---|---|---|---|
| TM-01 | Secret leakage through committed config or logs | High | Enforce env-var/user-secrets model, run secret scanning, redact sensitive log fields | Mitigate |
| TM-02 | Dependency supply-chain compromise | High | Block on vulnerability scan and license gate, pin reviewed package versions | Mitigate |
| TM-03 | Broken access control on new endpoints | High | Apply authentication/authorization middleware and contract tests before route activation | Mitigate |
| TM-04 | Excessive trust in internal network boundaries | High | Require TLS, validate service identity, and enforce least-privilege service credentials | Mitigate |
| TM-05 | Telemetry sink outage causes blind operations | High | Use console fallback exporter and alert on missing telemetry heartbeat | Accepted risk with fallback |

## Mitigation Tracking
- TM-01 linked to environment and observability runbooks.
- TM-02 linked to CI security and license stages.
- TM-03 tracked as mandatory control for Phase 4 API slices.
- TM-04 tracked as deployment baseline control for all environments.
- TM-05 accepted with documented fallback and operations alerting.
