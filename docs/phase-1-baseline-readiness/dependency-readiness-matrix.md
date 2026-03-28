# Dependency and License Readiness Matrix

This matrix expands the Phase 0 dependency baseline with Phase 1 ownership, disposition, and Gate B readiness context.

| Dependency | Usage Area | Current License | Redistribution Compatible | Replacement or Target Strategy | License Disposition | Owner | Related Phase 2 Domain | Gate B Readiness | Notes |
|---|---|---|---|---|---|---|---|---|---|
| DevExpress v15.2 | UI controls, reporting, charting | Proprietary commercial | No | Replace with OSS or approved free community alternative | Replace required before broad UI migration | UI Lead | DevExpress replacement spike | Open | Final candidate selection belongs to Phase 2 spike work |
| CKEditor legacy integration | Rich-text editing | Legacy or version-specific licensing to confirm | TBD | Evaluate CKEditor OSS build or alternative editor | License validation required before carry-forward | UI Lead | UI modernization planning | Open | Distribution obligations remain unresolved |
| Enterprise Library | Legacy infrastructure utilities | MS-PL (legacy) | Evaluate | Replace with `Microsoft.Extensions.*` and modern OSS equivalents | Replace planned | Platform Lead | Foundation setup | Ready for planning | Existing direction is documented in repo planning artifacts |
| Thinktecture IdentityModel | Legacy auth and CORS support | Legacy package terms | No | Replace with native ASP.NET Core auth and CORS middleware | Replace planned | API Lead | Auth/session spike | Ready for planning | Target stack is already defined at direction level |
| OWIN/Katana packages | Legacy middleware pipeline | OSS or legacy package mix | Not target-compatible | Replace with native ASP.NET Core middleware pipeline | Replace planned | API Lead | Auth/session spike and API pilot | Ready for planning | Required for .NET 8 migration |
| Hangfire | Scheduled jobs | OSS, exact version evidence pending | Yes, pending version verification | Keep or upgrade based on .NET 8 support | Keep or upgrade pending evidence | Data Lead | Runtime operations review | Open | Need exact version and operating pattern from legacy source |
| log4net | Logging | Apache-2.0 | Yes | Keep or replace during observability foundation work | Evaluate in target architecture | Platform Lead | Observability baseline | Ready for planning | No redistribution blocker identified |
| Magick.NET | Image processing | Apache-2.0 | Yes | Keep or upgrade after runtime validation | Evaluate runtime compatibility | API Lead | API pilot slice | Ready for planning | Compatibility validation deferred to implementation phases |
| Google GData feature | Deprecated external feature | Deprecated legacy | N/A | Do not migrate | Out of scope | Product Owner | None | Closed | Reconfirmed as excluded in Phase 1 |

## Gate B Readiness Notes
- Every critical dependency row now has an owner and a disposition.
- Remaining open items are selection or evidence tasks, not ownership gaps.
- `Keep CE` decisions have not been made in this phase; any future `Keep CE` path still requires evidence per Phase 0 governance rules.