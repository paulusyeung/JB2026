## Context

The ClientApp is a Vue 3 + Vite SPA located at `JB2026.WebApp/ClientApp/`. It builds production assets into `../wwwroot/app/` (relative to the ClientApp directory). The build requires Node.js and pnpm/npm. Currently there is no containerized build pipeline for the frontend.

The project uses GitHub Actions for CI and GitHub as the source of truth. GHCR is the natural choice for container registry since it integrates natively with GitHub Actions, supports fine-grained access via GitHub tokens, and requires no external registry setup.

## Goals / Non-Goals

**Goals:**
- Produce a reproducible, minimal Docker image containing the built ClientApp served by nginx.
- Cache Node.js dependencies across builds to keep CI fast.
- Push images to GHCR with semantic tagging (branch name, commit SHA, `latest` on main).
- Keep the Dockerfile self-contained — no external secrets or registry credentials beyond standard GHCR auth.

**Non-Goals:**
- Dockerizing the .NET WebApp or API backends (separate concern).
- Adding health checks or liveness probes beyond nginx defaults.
- Multi-architecture builds (linux/amd64 only for now).
- Development-time Docker Compose setup (can be added later).

## Decisions

### 1. Multi-stage build: Node.js build stage → nginx serve stage

**Rationale:** The build stage needs Node.js, npm, and all dev dependencies. The runtime only needs nginx and the static files. Multi-stage builds keep the final image small (~25 MB nginx:alpine vs ~400 MB node image) and reduce the attack surface.

**Alternatives considered:**
- Single-stage node image serving with `serve` or `http-server` — heavier runtime, unnecessary Node.js dependency.
- Copying built assets to the host and mounting into nginx — not self-contained, harder to version.

### 2. Base images: `node:22-alpine` (build) + `nginx:alpine` (runtime)

**Rationale:** Alpine variants are small and well-maintained. Node 22 is current LTS and matches the project's TypeScript 5.9 + Vite 7 toolchain. Nginx alpine is the standard lightweight static file server.

### 3. Dependency caching via Docker layer caching

**Rationale:** `package.json` and `pnpm-lock.yaml` change far less frequently than source files. Copying them first and installing dependencies before copying source maximizes Docker layer cache hits, speeding up CI builds.

### 4. Non-root user in the runtime stage

**Rationale:** Running nginx as a non-root user follows security best practices. The `nginx:alpine` image already includes the `nginx` user, so we leverage that rather than creating a custom user.

### 5. GHCR tagging strategy

- **Push to `main`**: Tag with `latest` and the git SHA short hash.
- **Push to other branches**: Tag with the branch name (slashes replaced with hyphens) and the git SHA.
- **Pull requests**: Build only, do not push (saves registry quota).

### 6. `.dockerignore` scope

Exclude `node_modules/`, `dist/`, `tests/`, `test-results/`, `.git/`, and any local config files to keep the build context small and avoid leaking sensitive data.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| GHCR storage limits (free tier: 500 MB, 1 GB with verified account) | Images are small (~25-35 MB). Old images should be cleaned up periodically. Consider adding a retention policy workflow later. |
| Vite build output path (`../wwwroot/app`) assumes parent directory structure | The Dockerfile runs `vite build` from within the ClientApp directory, so the relative path resolves correctly. The nginx stage copies from `./wwwroot/app/`. |
| Environment variables for API base URL are baked in at build time | The Vite `VITE_API_BASE_URL` env var is resolved at build time. If different environments need different API targets, the nginx stage can use a sub_filter or the SPA can resolve the API URL dynamically at runtime. For now, the default proxy config in the WebApp handles this. |

## Migration Plan

1. Add `Dockerfile` and `.dockerignore` to `JB2026.WebApp/ClientApp/`.
2. Add GitHub Actions workflow to `.github/workflows/clientapp-docker.yml`.
3. Test locally with `docker build` to verify the image builds and serves correctly.
4. Merge to a feature branch first — the workflow will build but not push (only `main` pushes).
5. After verification, merge to `main` to enable GHCR publishing.

**Rollback:** If the workflow causes issues, it can be disabled by renaming or deleting the YAML file. No code changes are made, so there is nothing to roll back in the application itself.

## Open Questions

- Should the image also include a simple health check endpoint? (nginx can return 200 on `/` by default, which is sufficient for most orchestrators.)
- Do we need platform-specific builds (e.g., `linux/arm64` for Apple Silicon dev)? Deferring for now.

