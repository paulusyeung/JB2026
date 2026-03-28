## Why

Phase 0 established governance, compliance, and benchmarking rules, but the program still needs a shared current-state baseline before risk spikes begin. Without a documented inventory of applications, interfaces, jobs, dependencies, and operational knowledge, each spike will make local assumptions and produce inconsistent findings.

## What Changes

- Establish a Phase 1 baseline readiness capability covering application inventory, dependency ownership, and documentation baseline.
- Produce a current-state inventory of applications, interfaces, jobs, and external dependencies.
- Expand the dependency/license matrix with owners and replacement disposition.
- Create a documentation baseline for architecture notes, runbooks, and migration decisions.
- Confirm and publish the out-of-scope feature list including Google GData.

## Capabilities

### New Capabilities
- `migration-baseline-inventory`: Produces the current-state inventory for applications, interfaces, jobs, and external dependencies.
- `documentation-baseline`: Establishes the architecture note set, operational runbooks, and migration decision log required for downstream phases.

### Modified Capabilities
- `migration-governance-baseline`: Depends on documented inventory and documentation baselines before Phase 2 risk spikes begin.

## Impact

- Unblocks risk spikes by ensuring every spike starts from an agreed current-state baseline.
- Makes dependency ownership and documentation gaps explicit before implementation work begins.
- Finalises scope exclusion list, removing Google GData work from all planning artefacts.
- Establishes measurable readiness expectations for inventory coverage, required runbooks, and documentation acceptance before Phase 2 begins.
