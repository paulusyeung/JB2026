## Why

The migration plan is defined, but execution cannot start safely without a formal mobilization baseline. We need a phase-specific contract that locks governance gates, scope boundaries, and open-source licensing requirements before implementation work begins.

## What Changes

- Establish a Phase 0 mobilization capability that defines mandatory planning outputs before Phase 1 engineering work.
- Define Gate A/B/C readiness criteria and explicit ownership for architecture, dependency strategy, and cutover readiness.
- Define stakeholder review cadence and decision feedback checkpoints for each phase.
- Establish the legacy-system benchmarking method, datasets, and comparison checkpoints used throughout the migration.
- Require a dependency and license baseline that identifies proprietary components, approved OSS or free community alternatives, and redistribution constraints.
- Require a scoped exclusion list for intentionally non-migrated features, including Google GData.
- Require publishable project governance artifacts (charter, RACI, dependency matrix, license compliance matrix) as phase exit criteria.

## Capabilities

### New Capabilities
- `migration-governance-baseline`: Defines Phase 0 governance, decision gates, dependency/license baseline, and scope boundaries required before migration implementation starts.

### Modified Capabilities
- None.

## Impact

- Affects planning and governance workflows across architecture, API, data, UI, QA, and DevOps.
- Introduces mandatory open-source compliance checkpoints before dependency decisions are approved.
- Introduces a benchmarking baseline used to measure regressions and improvements against JB2015 through later phases.
- Produces traceable artifacts used by later design and implementation phases.
- Reduces migration risk by making non-migrated scope and phase gates explicit at the start.
