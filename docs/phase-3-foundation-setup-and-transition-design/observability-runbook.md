# Observability Runbook

## Baseline Components
- Structured request logging via Serilog.
- Distributed tracing via OpenTelemetry.
- Health check endpoints for liveness and readiness.

## Health Endpoints
- /health/live
- /health/ready

## Logging Configuration
- Sink: console (default for baseline).
- Format: structured JSON-compatible events.
- Required request fields: timestamp, method, path, status code, elapsed time.

## Trace Export Configuration
- Service name source: JB2026:Observability:ServiceName.
- Exporter selection:
  - OTLP when JB2026__Observability__OtlpEndpoint is set.
  - Console exporter fallback when OTLP endpoint is not set.

## Operational Checks
1. Confirm service startup logs are emitted.
2. Confirm each HTTP request produces one request log event.
3. Confirm trace spans are exported to configured sink.
4. Confirm liveness endpoint returns HTTP 200 when process is alive.
5. Confirm readiness endpoint reflects dependency state.

## Incident Triage Hooks
- Correlate incidents using trace identifier from log context.
- Escalate if readiness endpoint is unhealthy for more than 5 minutes.
