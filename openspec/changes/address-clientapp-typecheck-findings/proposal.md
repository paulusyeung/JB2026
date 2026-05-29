## Why

The ClientApp frontend currently fails strict TypeScript validation, and the first focused validation run for a small sidebar change exposed a broader backlog of unchecked typing, null-safety, and component-contract issues. Several findings are build-only quality failures, but others represent credible runtime risks in dialogs, admin views, scheduler screens, and shared UI components.

This change is needed now because the current validation baseline is not trustworthy. Small feature work keeps discovering unrelated failures, which increases delivery risk, obscures regressions, and makes it harder to distinguish safe local changes from pre-existing defects.

## What Changes

- Establish a documented remediation plan for the current ClientApp typecheck backlog instead of treating each compiler error as isolated cleanup.
- Group the validated findings into implementation tracks: editor integration typing, shared component generic contracts, null-safety and event handler correctness, and strict-mode cleanup.
- Define directives for prioritizing errors that can become runtime failures ahead of build-only noise.
- Require a stable frontend validation path so future UI changes can be verified without rediscovering the same baseline issues.
- Capture guardrails for resolving typing problems without weakening strict compiler settings or masking real defects.

## Capabilities

### New Capabilities
- `frontend-typecheck-baseline`: Establish and maintain a known-good ClientApp typecheck baseline with repeatable validation.
- `ckeditor-editor-contract-hardening`: Align CKEditor component usage and editor typings with the installed CKEditor Vue integration contract.
- `shared-ui-type-contract-hardening`: Correct generic, event, and prop contracts in shared UI components and their callers.
- `clientapp-null-safety-remediation`: Remove validated nullability and unsafe access patterns in forms, list views, and scheduling flows.
- `strict-mode-cleanup-governance`: Define how unused symbols, deprecated config patterns, and low-risk strict-mode failures are triaged and cleaned up.

### Modified Capabilities

## Impact

- Frontend validation: `JB2026.WebApp/ClientApp` typecheck becomes an explicit delivery gate instead of an unreliable optional signal.
- Shared UI components: `src/components/grids/ListMobileCard.vue`, CKEditor wrappers/usages, action menus, and scheduler-related controls require contract fixes.
- Feature views: admin maintenance screens, billing dialogs, schedule screens, quotation screens, and reporting views are affected by the validated findings.
- Tooling and configuration: TypeScript/Vue compiler configuration must remain strict while deprecations and compatibility mismatches are resolved correctly.
- Delivery process: future frontend changes can be validated locally without first triaging unrelated compiler failures.