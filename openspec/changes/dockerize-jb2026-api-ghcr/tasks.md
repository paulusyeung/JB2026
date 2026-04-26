## 1. Container Build Foundation

- [x] 1.1 Add a multi-stage Dockerfile for JB2026.Api using .NET SDK build/publish and ASP.NET runtime stages; copy `.sln` and all referenced `.csproj` files (`JB2026.Api`, `JB2026.Infrastructure`, `JB2026.EfCore`, `JB2026.DataAccess`) before restore to maximize layer cache hits
- [x] 1.2 Add `.dockerignore` at the repository root to minimize build context and avoid copying unnecessary files (e.g., `.git/`, `node_modules/`, test projects, IDE files); pattern it after the existing `ClientApp/.dockerignore`
- [ ] 1.3 Validate local Docker build success and confirm output image starts correctly

## 2. Runtime Configuration and Health

- [x] 2.1 Add a minimal `/healthz` GET endpoint to JB2026.Api that returns HTTP 200 when the API is operational
- [x] 2.2 Add a `HEALTHCHECK` instruction in the Dockerfile that probes the `/healthz` endpoint
- [x] 2.3 Configure container runtime defaults (port 8080, `ASPNETCORE_URLS`, environment) and document override behavior
- [x] 2.4 Document `ConnectionStrings__Primary` as a required environment variable; ensure the container fails fast at startup with a descriptive error if it is not provided
- [ ] 2.5 Verify the API responds successfully when running in a container with default settings and a valid connection string
- [ ] 2.6 Verify documented environment overrides are applied at container startup

## 3. GHCR Publishing Workflow

- [x] 3.1 Add GitHub Actions workflow to build JB2026.Api container image on `push` to `main`, `push` of `v*` tags, and `pull_request` events
- [x] 3.2 Set up Docker Buildx and configure registry-based BuildKit layer caching (`cache-from`/`cache-to` stored in GHCR), matching the existing `clientapp-docker.yml` pattern
- [x] 3.3 Configure login and push to GHCR using `GITHUB_TOKEN` and minimum required workflow permissions; skip login and push on pull request events (`push: ${{ github.event_name != 'pull_request' }}`)
- [x] 3.4 Implement deterministic tagging and metadata labels: short commit SHA (no prefix), rolling `latest` on main, sanitized branch name on non-main branches, and semver tag on `v*` tag events; normalize image name to lowercase

## 4. Documentation and Operability

- [x] 4.1 Document local Docker build/run commands and prerequisites for JB2026.Api, including example `docker run` with required environment variables
- [x] 4.2 Document GHCR prerequisites (repo/org permissions, package settings, expected image path)
- [x] 4.3 Add troubleshooting guidance for common CI publishing failures (permission denied, tag mismatch, missing metadata)

## 5. Validation and Rollout Safety

- [ ] 5.1 Run a dry-run validation in CI to confirm workflow path filters and trigger conditions behave as intended
- [ ] 5.2 Confirm pushed image tags and OCI labels in GHCR match documented conventions
- [x] 5.3 Verify that the existing `ci.yml` pipeline is unaffected by the new Docker workflow (no conflicting triggers, no resource contention)
- [x] 5.4 Define rollback steps by disabling/reverting the workflow and using previously known-good tags
