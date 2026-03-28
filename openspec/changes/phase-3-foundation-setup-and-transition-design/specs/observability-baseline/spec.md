## ADDED Requirements

### Requirement: Application Must Emit Structured Logs and Traces From Startup
All application components SHALL emit structured log events and distributed trace spans from the first running build, using Serilog and OpenTelemetry.

#### Scenario: Structured log entry produced for each HTTP request
- **WHEN** an HTTP request is processed
- **THEN** a structured log entry SHALL be emitted containing at minimum: timestamp, HTTP method, path, status code, and elapsed time

#### Scenario: Trace span created for each request
- **WHEN** an HTTP request is processed
- **THEN** a corresponding OpenTelemetry trace span SHALL be created and exported to the configured sink

### Requirement: Health Check Endpoints Must Be Exposed
The API SHALL expose at minimum a liveness and a readiness health check endpoint compatible with Kubernetes probes and standard load balancer health polling.

#### Scenario: Liveness endpoint returns healthy status
- **WHEN** GET /health/live is called on a running instance
- **THEN** a 200 response SHALL be returned when the process is alive

#### Scenario: Readiness endpoint reflects dependency state
- **WHEN** GET /health/ready is called
- **THEN** the response SHALL reflect the current state of critical dependencies (e.g., database reachability)
