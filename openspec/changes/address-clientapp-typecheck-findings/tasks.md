## 1. Validation Baseline

- [x] 1.1 Confirm the canonical ClientApp validation command and document it in the implementation notes for this change.
- [x] 1.2 Fix any remaining configuration or obviously broken source files that prevent the typecheck output from reflecting the real backlog.
- [x] 1.3 Capture and group the current compiler failures into the defined remediation tracks: CKEditor, shared UI contracts, null-safety/runtime correctness, and strict-mode hygiene.

## 2. CKEditor Contract Repair

- [x] 2.1 Identify all ClientApp CKEditor integration points and the shared typing boundary they rely on.
- [x] 2.2 Update the CKEditor integration so the editor constructor and wrapper types conform to the installed `@ckeditor/ckeditor5-vue` contract.
- [x] 2.3 Revalidate the affected CKEditor-backed forms with focused typecheck/editor diagnostics.

## 3. Shared UI Contract Hardening

- [x] 3.1 Repair `ListMobileCard` prop and generic typing so typed item arrays and typed column definitions are accepted without unsafe casts.
- [x] 3.2 Fix shared event/menu/status component signatures so Vuetify-emitted events align with declared handlers.
- [x] 3.3 Update impacted caller views only as needed to satisfy the corrected shared contracts.

## 4. Null-Safety And Runtime-Correctness Repairs

- [x] 4.1 Fix unsafe optional and indexed access in admin customer and supplier dialogs.
- [x] 4.2 Fix scheduler and list-state update paths that assign partially undefined row data or dereference missing rows.
- [x] 4.3 Correct view and store contract mismatches such as undeclared theme properties, missing component exports, and invalid handler bindings.

## 5. Strict-Mode Hygiene Cleanup

- [x] 5.1 Remove unused locals and imports in files already touched by higher-priority remediation.
- [x] 5.2 Triage remaining low-risk hygiene-only failures into bounded cleanup slices rather than one broad sweep.
- [x] 5.3 Ensure deprecated config patterns are resolved at the source without weakening strict compiler settings.

## 6. Verification

- [x] 6.1 Run the full ClientApp typecheck after each remediation track and record the remaining error classes.
- [x] 6.2 Reach a green `npm --prefix JB2026.WebApp/ClientApp run typecheck` result with no compiler errors.
- [x] 6.3 Perform a final review to confirm the remediation preserved strict settings and did not rely on broad suppression or unsafe casts.