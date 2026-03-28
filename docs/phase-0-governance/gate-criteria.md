# Gate Criteria - A, B, C

## Gate A - Architecture Viability
### Criteria
- Migration charter approved.
- Scope and exclusions published.
- Representative spike plan approved (UI, EF Core, auth/session, API pilot).
- Baseline benchmarking method documented.

### Owners
- Accountable: Platform Lead
- Approvers: API/Data/UI/QA/DevOps Leads, Product Owner

## Gate B - Dependency and License Strategy
### Criteria
- Dependency inventory completed for legacy and target stacks.
- License compatibility status recorded for each dependency.
- Migration strategy assigned (`Replace`, `Keep CE`, `Do not migrate`, `Out of scope`).
- Evidence attached for any `Keep CE` decision.
- Unresolved license ambiguities reviewed and dispositioned.

### Owners
- Accountable: Platform Lead
- Approvers: API/Data/UI/DevOps Leads and compliance reviewer

## Gate C - Cutover Readiness
### Criteria
- Regression, load, DR, security gates passed.
- Transition playbook and rollback runbook approved.
- Support operating model and hypercare ownership approved.
- Go/no-go checklist signed by technical lead and product owner.

### Owners
- Accountable: DevOps Lead
- Approvers: Platform/API/Data/UI/QA Leads and Product Owner
