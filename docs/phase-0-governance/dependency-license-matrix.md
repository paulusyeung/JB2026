# Dependency and License Baseline Matrix

| Dependency | Usage Area | Current License | Redistribution Compatible | Target Strategy | Owner | Status | Evidence/Notes |
|---|---|---|---|---|---|---|---|
| DevExpress v15.2 | UI controls/reporting | Proprietary | No | Replace | UI Lead | Planned | Replace with OSS/free CE options from Phase 2 spike |
| CKEditor legacy integration | Rich-text editor | Legacy/proprietary mix | TBD | Replace | UI Lead | Planned | Migrate to CKEditor OSS build or approved OSS alternative |
| Enterprise Library | Legacy infra | MS-PL (legacy) | Evaluate | Replace | Platform Lead | Planned | Replace with Microsoft.Extensions.* ecosystem |
| Thinktecture IdentityModel | Legacy auth/CORS | Legacy package terms | No (target stack deprecated) | Replace | API Lead | Planned | Use native ASP.NET Core auth and CORS middleware |
| OWIN/Katana packages | Legacy middleware | OSS/legacy | Not target-compatible | Replace | API Lead | Planned | Replace with native ASP.NET Core pipeline |
| Hangfire | Jobs | OSS (license verify) | Yes (pending verification) | Keep/Upgrade | Data Lead | Evaluating | Confirm exact version license and upgrade path |
| log4net | Logging | Apache-2.0 | Yes | Keep/Upgrade | Platform Lead | Evaluating | May replace with Serilog in new foundation |
| Magick.NET | Image processing | Apache-2.0 | Yes | Keep/Upgrade | API Lead | Evaluating | Validate runtime compatibility with .NET 8 |
| Microsoft.Extensions.Configuration.UserSecrets | Shared infrastructure config | MIT | Yes | Keep | Platform Lead | Approved | .NET Foundation package for development-time secret loading |
| OpenTelemetry.Exporter.Console | Shared observability baseline | Apache-2.0 | Yes | Keep | Platform Lead | Approved | Console fallback exporter in non-prod and local environments |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | Shared observability baseline | Apache-2.0 | Yes | Keep | Platform Lead | Approved | OTLP exporter used for vendor-neutral trace shipping |
| OpenTelemetry.Extensions.Hosting | Shared observability baseline | Apache-2.0 | Yes | Keep | Platform Lead | Approved | Host integration for OpenTelemetry in .NET 8 |
| OpenTelemetry.Instrumentation.AspNetCore | Shared observability baseline | Apache-2.0 | Yes | Keep | Platform Lead | Approved | Request tracing instrumentation for ASP.NET Core |
| OpenTelemetry.Instrumentation.Http | Shared observability baseline | Apache-2.0 | Yes | Keep | Platform Lead | Approved | Outbound HTTP dependency tracing instrumentation |
| Serilog.AspNetCore | Shared infrastructure logging | Apache-2.0 | Yes | Keep | Platform Lead | Approved | Structured request logging middleware for host projects |
| Serilog.Settings.Configuration | Shared infrastructure logging | MIT | Yes | Keep | Platform Lead | Approved | Binds Serilog sinks and levels from appsettings |
| Google GData feature | Legacy external feature | Deprecated legacy | N/A | Out of scope | Product Owner | Approved | Explicitly excluded from JB2026 migration |

## Rules
- Every dependency row must include owner, strategy, and compatibility status.
- `Keep CE` requires explicit license evidence proving redistribution is allowed.
- Unresolved compatibility must block Gate B approval.
