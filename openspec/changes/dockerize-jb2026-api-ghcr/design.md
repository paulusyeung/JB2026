## Context

JB2026.Api is a .NET API project that is currently run using host-level dependencies and environment-specific configuration. This creates drift between developer machines, CI runners, and deployment targets. The repository already uses GitHub and can natively publish container packages to GHCR, making GHCR the lowest-friction registry target.

The repository already has a containerized ClientApp (`clientapp-docker.yml`) that publishes to GHCR with Buildx, registry-based layer caching, and metadata-action tagging. This change follows the same patterns for consistency.

JB2026.Api references three sibling projects (`JB2026.Infrastructure`, `JB2026.EfCore`, `JB2026.DataAccess`), which requires using the repository root as the Docker build context. The API also requires a `ConnectionStrings__Primary` database connection string at runtime.

Constraints:
- Containerization must not change API behavior or route contracts (one new `/healthz` endpoint is added for container probes).
- Build should be deterministic and cache-friendly for CI.
- Published images must support traceability back to source commit and release tag.
- Registry publishing must rely on GitHub-native auth and least-privilege permissions.
- The existing `ci.yml` build-test-lint pipeline must not be affected.

Stakeholders:
- API developers who need a reliable local runtime.
- DevOps/release owners who need repeatable CI image publishing.
- Consumers/deployers of JB2026.Api images.

## Goals / Non-Goals

**Goals:**
- Provide a production-ready Docker image for JB2026.Api via a multi-stage build.
- Add a `/healthz` health-check endpoint and a Dockerfile `HEALTHCHECK` instruction for orchestrator compatibility.
- Define consistent runtime defaults (port, ASP.NET Core environment, config mapping).
- Document required environment variables, especially `ConnectionStrings__Primary`.
- Publish images to GHCR with stable tagging and OCI labels.
- Automate build and publish through GitHub Actions with clear guardrails, following the established `clientapp-docker.yml` patterns.
- Document local developer and CI usage.

**Non-Goals:**
- Re-architecting JB2026.Api internals or changing API contracts.
- Introducing Kubernetes manifests, Helm charts, or deployment orchestration.
- Replacing existing non-container deployment paths in this change.
- Image signing or attestation (deferred to a future hardening change).

## Decisions

1. Multi-stage Dockerfile for JB2026.Api
   - Decision: Use SDK image for restore/build/publish and ASP.NET runtime image for final stage.
   - Rationale: Smaller runtime image, reduced attack surface, and standard .NET production pattern.
   - Alternative considered: Single-stage Dockerfile.
   - Why not: Larger final image, slower pull times, and unnecessary build tooling in runtime container.

2. Runtime conventions
   - Decision: Expose a single HTTP port (default 8080), set `ASPNETCORE_URLS=http://+:8080`, and keep environment configurable via env vars.
   - Rationale: Works consistently across local Docker and common platforms where internal port mapping is explicit.
   - Alternative considered: Preserve default Kestrel 80/5000 conventions.
   - Why not: Less explicit for container users and can vary between templates/runtime expectations.

3. Registry and tagging strategy
   - Decision: Publish to GHCR under `ghcr.io/<owner>/jb2026-api` with tags for commit SHA (short, no prefix), branch name, rolling `latest` on main, and release semver tags triggered by `v*` tag patterns (e.g., `v1.0.0`, `v2.1.3`).
   - Rationale: Enables immutable traceability (SHA), human-friendly discovery (latest on main), feature-branch testing (branch name), and release pinning (semver).
   - Alternative considered: SHA-only tags.
   - Why not: Harder for operators to consume canonical rolling tags.

4. GitHub Actions authentication and permissions
   - Decision: Use `GITHUB_TOKEN` with `packages: write` permission and GitHub Actions Docker metadata/build-push actions.
   - Rationale: Avoids long-lived static credentials and uses platform-native least-privilege permissions.
   - Alternative considered: Personal access token secret.
   - Why not: Higher secret-management burden and broader misuse risk.

5. Build context and caching
   - Decision: Use repository-root build context with scoped copy steps, Docker Buildx, and registry-based BuildKit layer caching (matching the existing `clientapp-docker.yml` pattern with `cache-from`/`cache-to` stored in GHCR).
   - Rationale: Reliable project file resolution in multi-project solutions and faster incremental CI builds. The Dockerfile must copy `.sln` and all referenced `.csproj` files before `dotnet restore` to maximize layer cache hits.
   - Alternative considered: API-project-only context.
   - Why not: Can break restore when project references or solution-wide files are needed.

6. Health-check endpoint
   - Decision: Add a minimal `/healthz` GET endpoint to JB2026.Api and a `HEALTHCHECK` instruction in the Dockerfile that probes it.
   - Rationale: Container orchestrators (Docker Compose, ECS, Kubernetes) require a health endpoint to distinguish healthy containers from crashed ones. JB2026.Api currently has no health-check endpoint.
   - Alternative considered: Use a TCP check or probe an existing route (e.g., Swagger).
   - Why not: TCP checks don't verify application readiness, and Swagger may be disabled in production.

7. Database connection as a required runtime variable
   - Decision: Treat `ConnectionStrings__Primary` as a required runtime environment variable. The container should fail fast with a descriptive error if it is not provided.
   - Rationale: The API registers `JB5LegacyReadContext` via EF Core and will crash on first database access without a connection string. Fail-fast behavior surfaces misconfiguration immediately rather than producing confusing 500 errors at request time.
   - Alternative considered: Start the container "healthy" without a database and degrade gracefully.
   - Why not: The API is non-functional without database access, so pretending to be healthy would mislead orchestrators and operators.

## Risks / Trade-offs

- [Configuration mismatch between local and container runtime] -> Mitigation: Define required env vars in docs and keep defaults explicit in Dockerfile/workflow examples.
- [Image bloat from incorrect copy patterns] -> Mitigation: Add `.dockerignore` at repo root and separate restore/build layers in Dockerfile.
- [Accidental publication from non-main branches] -> Mitigation: Restrict push conditions (`push: ${{ github.event_name != 'pull_request' }}`) and enforce branch/tag filters in workflow triggers.
- [Token permission failures in org-restricted repos] -> Mitigation: Document required repository settings for GitHub Actions package publishing.
- [Using `latest` can obscure rollback intent] -> Mitigation: Treat SHA and semver tags as primary deployment references; use `latest` only for non-production environments.
- [Container starts without required database connection string] -> Mitigation: Fail fast at startup with a descriptive error and document `ConnectionStrings__Primary` as mandatory.
- [New workflow conflicts with existing `ci.yml`] -> Mitigation: Use separate workflow file with independent triggers; validate that `ci.yml` is unaffected.

## Migration Plan

1. Add `/healthz` endpoint to JB2026.Api.
2. Add Dockerfile (at repo root or `JB2026.Api/`) and `.dockerignore` (at repo root) for JB2026.Api and validate local build/run.
3. Add GitHub Actions workflow for build and conditional push to GHCR.
4. Validate package publication and tag set in GHCR on a test branch/release tag.
5. Update README/docs with local and CI usage guidance, including required environment variables.
6. Rollout: keep existing deployment method available until image-based consumers are validated.

Rollback strategy:
- Disable or revert workflow file to stop new pushes.
- Consumers continue using prior deployment path or previously published known-good image tags.

## Resolved Questions

- **Semver vs. latest for production:** Use pinned semver tags (`v*`) for production deployments. Allow `latest` for non-production/staging environments only. Document this as policy.
- **Image signing/attestation (Cosign):** Deferred to a future hardening change. Adds complexity with limited immediate value for a private/internal image.
- **Package visibility:** Start as repository-scoped private. Promote to org-visible when other repositories need to consume the image. Easier to open up than to lock down.
