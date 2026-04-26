## ADDED Requirements

### Requirement: JB2026.Api MUST build as a production Docker image
The system SHALL provide a Docker build definition for JB2026.Api that produces a runnable production image from repository sources using a deterministic multi-stage build process.

#### Scenario: Build image from repository
- **WHEN** a user runs a Docker build for JB2026.Api from the documented repository context
- **THEN** the build MUST complete successfully and produce a tagged container image

#### Scenario: Rebuild with cache after source-only change
- **WHEN** a user rebuilds the image after changing only application source files (no dependency changes)
- **THEN** the NuGet restore layer SHOULD be reused from cache, and only subsequent layers SHOULD rebuild

### Requirement: JB2026.Api container MUST run with explicit runtime defaults
The containerized JB2026.Api runtime SHALL expose a documented HTTP port and SHALL support runtime configuration via environment variables without requiring source code changes.

#### Scenario: Start container with defaults
- **WHEN** a user runs the built image without overriding runtime settings
- **THEN** the API MUST start and listen on the documented default container port

#### Scenario: Override runtime configuration
- **WHEN** a user supplies supported environment variable overrides at container start
- **THEN** the API MUST honor the overrides and remain healthy

#### Scenario: Start container without required database connection string
- **WHEN** a user runs the built image without providing `ConnectionStrings__Primary`
- **THEN** the container MUST fail fast at startup with a descriptive error message indicating the missing configuration

### Requirement: JB2026.Api container MUST expose a health-check endpoint
The containerized JB2026.Api SHALL provide a `/healthz` endpoint and a Dockerfile `HEALTHCHECK` instruction so that container orchestrators can determine container liveness.

#### Scenario: Health check with healthy container
- **WHEN** the container is running and the API is ready to serve requests
- **THEN** a GET request to `/healthz` MUST return HTTP 200

#### Scenario: Docker health check integration
- **WHEN** a container orchestrator runs the Dockerfile HEALTHCHECK instruction
- **THEN** the container MUST report as healthy when the API is operational

### Requirement: Container usage MUST be documented for developers
The repository SHALL document the required commands, prerequisites, and environment variables to build and run JB2026.Api as a Docker container in local development.

#### Scenario: Follow documented local workflow
- **WHEN** a developer follows the documented Docker workflow
- **THEN** they MUST be able to build and run JB2026.Api without undocumented setup steps
