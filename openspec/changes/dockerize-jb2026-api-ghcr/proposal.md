## Why

JB2026.Api currently depends on environment-specific setup, which slows onboarding and creates inconsistent runtime behavior between local development and production. We need a reproducible containerized runtime and a standard publishing path to GitHub Container Registry (GHCR) so builds are portable, traceable, and deployment-ready.

## What Changes

- Add a production-ready Docker build definition for JB2026.Api using a multi-stage .NET build pipeline.
- Add a lightweight `/healthz` endpoint for container health probes and a `HEALTHCHECK` instruction in the Dockerfile.
- Define container runtime configuration for ASP.NET Core environment variables, ports, and required connection strings.
- Add documentation for local build/run workflows using Docker, including required environment variables (e.g., `ConnectionStrings__Primary`).
- Add a **new** CI workflow (separate from the existing `ci.yml` pipeline) to build, tag, and push JB2026.Api images to GHCR on main branch and release tags (`v*`).
- Standardize image tagging strategy (commit SHA, semantic tag, branch name, and latest for main) and minimum metadata labels.
- Add registry authentication and required repository/organization secret guidance for GitHub Actions.

## Capabilities

### New Capabilities
- `api-containerization`: Build and run JB2026.Api as a deterministic Docker image suitable for local and deployment use.
- `ghcr-image-publishing`: Publish versioned JB2026.Api container images from GitHub Actions to GitHub Container Registry.

### Modified Capabilities
- None.

## Impact

- Affected code and assets: JB2026.Api source (new `/healthz` endpoint), container build assets (Dockerfile and `.dockerignore` at repo root), a new CI workflow under `.github/workflows`, and repository documentation (README and/or docs). The existing `ci.yml` pipeline is not modified.
- APIs: One new endpoint (`/healthz`) for container health probes. No changes to existing API contracts; runtime packaging and delivery are changed.
- Dependencies and systems: Docker tooling for local/CI builds, GHCR package permissions, GitHub Actions credentials/permissions, and a database connection string (`ConnectionStrings__Primary`) as a required runtime environment variable.
