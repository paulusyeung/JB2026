# Benchmarking Baseline and Comparison Plan

## Baseline Metrics
- Error rate (overall and critical journeys)
- Latency (P50/P95)
- Throughput (requests per second / jobs per hour)
- Top user journeys response profile

## Datasets and Workloads
- Production-like anonymized dataset for representative read/write workflows.
- Workload set includes API critical paths, heavy DB queries, and job execution paths.

## Checkpoints
- Baseline capture complete before Phase 2 spikes begin.
- Re-capture after each major phase (Phase 2, 4, 5, 7).
- Compare against baseline and flag regressions beyond agreed threshold.

## Comparison Thresholds
- P95 regression threshold: <= agreed budget per endpoint/job category.
- Error-rate regression threshold: no sustained increase beyond defined SLO budget.
- Throughput regression threshold: no sustained drop below agreed capacity target.

## Owners
- Accountable: QA/Performance Lead
- Consulted: API/Data/Platform leads
- Informed: Product Owner, DevOps lead
