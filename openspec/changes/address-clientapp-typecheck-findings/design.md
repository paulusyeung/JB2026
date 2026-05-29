## Context

The ClientApp frontend in `JB2026.WebApp/ClientApp` is configured with strict TypeScript and Vue compiler checks, but the current baseline is not green. A focused validation run for a sidebar menu move first exposed a broken composable file, then surfaced a wider set of pre-existing failures across CKEditor integration, shared component generics, nullability checks, event contracts, deprecated configuration, and unused-symbol strictness.

The findings are not all equivalent. Some failures are primarily validation noise, such as unused locals under `noUnusedLocals`, while others indicate real runtime risk, such as unsafe array element access, missing properties on store contracts, incompatible event signatures, and component prop mismatches. The design needs to preserve strict compiler settings, reduce rediscovery of baseline failures, and sequence remediation so higher-risk defects are addressed before cosmetic cleanup.

## Goals / Non-Goals

**Goals:**
- Restore a trustworthy ClientApp typecheck baseline without weakening strict TypeScript enforcement.
- Partition the current backlog into remediation tracks that map to real code ownership boundaries.
- Prioritize fixes that can produce runtime failures before build-only cleanup.
- Standardize how shared UI component contracts are typed so downstream views stop accumulating ad hoc casts and mismatches.
- Ensure future frontend changes can run a repeatable validation command and distinguish new regressions from known baseline work.

**Non-Goals:**
- Large UX rewrites unrelated to the type failures.
- Replacing CKEditor, Vuetify, or other framework dependencies as part of this change.
- Relaxing compiler settings such as `strict`, `noUnusedLocals`, or component prop checks to make the build pass artificially.
- Solving every style or architectural concern in the frontend while addressing the typecheck backlog.

## Decisions

### 1) Split remediation into risk-based tracks instead of fixing errors in compiler order
- Decision: organize the work into tracks for validation baseline, CKEditor contracts, shared UI typing, null-safety/runtime correctness, and strict-mode cleanup.
- Rationale: compiler order is not the same as user risk. Grouping by failure class allows targeted fixes, easier review, and clearer ownership.
- Alternative considered: fix files in the exact order reported by `vue-tsc -b`. Rejected because it mixes high-risk and low-risk changes arbitrarily and creates noisy review slices.

### 2) Keep strict compiler settings and remove root causes instead of suppressing errors
- Decision: do not lower strict compiler settings or add broad suppression to silence the backlog. Fix contracts, nullability, and configuration at the source.
- Rationale: the current failures were valuable precisely because they exposed unsafe or inconsistent code paths. Broad suppression would hide both present and future regressions.
- Alternative considered: temporarily disable `noUnusedLocals`, loosen component prop types, or add global type assertions. Rejected because it would reduce signal quality and postpone the same defects.

### 3) Treat runtime-risk findings as blocking even when the app currently renders in dev
- Decision: prioritize nullability faults, unsafe indexing, invalid event signatures, store contract mismatches, and missing component exports ahead of unused locals and generic cleanup.
- Rationale: these errors correspond to flows that can throw or misbehave when exercised with edge-case data or alternate paths.
- Alternative considered: clear the easiest errors first to reduce count quickly. Rejected because it optimizes the metric, not the operational risk.

### 4) Fix shared component contracts centrally before patching every caller
- Decision: for patterns such as `ListMobileCard` generic constraints and action/menu prop shapes, update the owning abstraction first and then narrow caller changes to what the stronger contract requires.
- Rationale: many current view-level errors are symptoms of a shared typing boundary that is too weak or too narrow.
- Alternative considered: cast each caller independently. Rejected because it multiplies local work and bakes incorrect assumptions into feature views.

### 5) Align CKEditor usage with the installed package contract rather than forcing local casts
- Decision: normalize the CKEditor wrapper/editor type contract to the version actually installed in the workspace and use adapter types only where the third-party package boundary requires them.
- Rationale: the current errors indicate a versioned mismatch between what `@ckeditor/ckeditor5-vue` expects and what local components pass as the editor constructor.
- Alternative considered: cast `ClassicEditor` usages to `unknown as Editor` at each call site. Rejected because it hides the real integration mismatch and makes upgrades harder.

### 6) Maintain a short, repeatable validation path during remediation
- Decision: use the client app typecheck command as the baseline gate and rerun focused diagnostics on touched files after each repair slice.
- Rationale: this change exists to make validation trustworthy. The validation loop must stay short enough that engineers actually use it during feature work.
- Alternative considered: rely on ad hoc editor diagnostics only. Rejected because the CLI remains the shared gate for consistent validation.

## Risks / Trade-offs

- [Shared typing fixes may fan out across many views] -> Mitigation: fix the owning abstraction first, then make small caller updates with targeted validation after each slice.
- [Runtime-risk and build-only issues are interleaved in the compiler output] -> Mitigation: maintain an explicit triage order and document which findings are blocking for execution risk versus validation hygiene.
- [Third-party package versions may have incompatible upstream typings] -> Mitigation: constrain fixes to local adapter boundaries and avoid mass casting at call sites.
- [Strict cleanup work can become endless churn] -> Mitigation: defer low-risk unused-symbol cleanup until runtime-risk and shared-contract failures are under control.
- [Developers may reintroduce invalid patterns while backlog work is in progress] -> Mitigation: document directives in specs and keep the typecheck command as a required pre-merge signal for touched frontend slices.

## Migration Plan

1. Stabilize the validation baseline by resolving confirmed configuration and obviously broken source files that prevent meaningful typecheck output.
2. Repair CKEditor integration typings so editor-backed forms no longer fail on package contract mismatches.
3. Correct shared UI component contracts, especially generic list/card, action menu, and event signature boundaries.
4. Fix nullability and unsafe access findings in admin dialogs, schedule flows, and other stateful views.
5. Triage and clear remaining strict-mode hygiene failures such as unused locals once runtime-risk and shared-contract issues are resolved.
6. Re-run `npm --prefix JB2026.WebApp/ClientApp run typecheck` as the final verification gate for the change.

Rollback is code-level rather than migration-based: each remediation slice should be independently reversible, and no runtime behavior change should depend on partially completed cleanup in unrelated areas.

## Open Questions

- Which shared UI typing boundary should be repaired first after CKEditor: `ListMobileCard`, scheduler/action-menu contracts, or store/view-model contract mismatches?
- Should the strict-mode cleanup track remove all unused locals now, or only in files that are already touched by higher-priority fixes?
- Are there existing CI jobs or branch policies that should be updated to enforce the client app typecheck once the baseline is green?