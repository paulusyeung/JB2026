## Why

The ClientApp (Vue 3 SPA) currently has no containerized build or deployment path. Dockerizing it enables reproducible builds, streamlined CI/CD, and consistent deployment across environments. Targeting GitHub Container Registry (GHCR) as the image storage keeps everything within the existing GitHub ecosystem, leveraging built-in authentication and access controls.

## What Changes

- Add a **multi-stage Dockerfile** for the ClientApp that builds the Vite production bundle and serves it via nginx (alpine).
- Add a **`.dockerignore`** file to exclude `node_modules`, build artifacts, and source maps from the image context.
- Add a **GitHub Actions workflow** that builds the image, tags it by commit/branch, and pushes to GHCR (`ghcr.io`).
- The resulting image is a standalone, production-ready static asset server — no Node.js runtime required at runtime.

## Capabilities

### New Capabilities

- `client-app-docker-image`: A multi-stage Docker image that builds the Vue 3 ClientApp and serves the production bundle via nginx. Includes proper caching layers, non-root user, and minimal attack surface.
- `client-app-ghcr-publish`: A GitHub Actions workflow that builds, tags, and pushes the ClientApp Docker image to GitHub Container Registry on pushes and pull requests to main.

### Modified Capabilities

<!-- None — this is a new capability with no existing specs to modify. -->

## Impact

- **New files**: `JB2026.WebApp/ClientApp/Dockerfile`, `JB2026.WebApp/ClientApp/.dockerignore`, `.github/workflows/clientapp-docker.yml`
- **No code changes** to the ClientApp source — purely infrastructure additions.
- **CI/CD**: New workflow requires `packages: write` and `contents: read` permissions on GHCR.
- **Dependencies**: None added to the project. The Dockerfile uses public base images (`node:22-alpine`, `nginx:alpine`).

</contents>