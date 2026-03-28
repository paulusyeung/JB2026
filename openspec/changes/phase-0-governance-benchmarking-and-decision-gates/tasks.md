## 1. Governance Baseline Artifacts

- [x] 1.1 Create and approve migration charter with in-scope and out-of-scope boundaries.
- [x] 1.2 Define and publish RACI with named owners for platform, API, data, UI, QA, and DevOps.
- [x] 1.3 Define Gate A, Gate B, and Gate C objective criteria and approval owners.
- [x] 1.4 Define stakeholder review cadence and decision input loop for each phase.
- [x] 1.5 Create a phase transition checklist that blocks Phase 1 start if Phase 0 artifacts are incomplete.

## 2. Legacy Benchmark and Dependency Baseline

- [x] 2.1 Baseline current-state metrics: error rate, latency (P50/P95), throughput, and top user journeys.
- [x] 2.2 Define legacy-versus-modern benchmarking datasets and comparison checkpoints.
- [x] 2.3 Inventory all current third-party dependencies from legacy and planned target stacks.
- [x] 2.4 Record each dependency in the compliance matrix with license type and redistribution compatibility.
- [x] 2.5 Assign migration strategy per dependency (`Replace`, `Keep CE`, `Do not migrate`, `Out of scope`).
- [x] 2.6 Identify proprietary dependencies requiring OSS or free community edition replacements.

## 3. Scope and Compliance Controls

- [x] 3.1 Create and publish the out-of-scope feature registry.
- [x] 3.2 Add explicit entry that Google GData feature migration is out of scope for JB2026.
- [x] 3.3 Define evidence requirements for any `Keep CE` dependency decision (license proof and redistribution terms).
- [x] 3.4 Add review checkpoint for unresolved or ambiguous license terms before Gate B approval.

## 4. Readiness Validation

- [x] 4.1 Run governance review to validate completion of charter, RACI, gate criteria, and compliance matrix.
- [x] 4.2 Run dependency review to verify each tracked component has owner, status, and target decision.
- [x] 4.3 Validate benchmarking plan completeness and approve it as the program comparison baseline.
- [x] 4.4 Record risks and mitigations from Phase 0 outputs into the migration risk register.
- [x] 4.5 Publish Phase 0 sign-off summary and approve transition to Phase 1 baseline and documentation work.
