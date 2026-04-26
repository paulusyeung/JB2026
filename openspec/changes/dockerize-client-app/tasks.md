## 1. Dockerfile

- [x] 1.1 Create `JB2026.WebApp/ClientApp/Dockerfile` with multi-stage build (node:22-alpine build stage → nginx:alpine runtime stage)
- [x] 1.2 Configure build stage: copy `package.json` and `pnpm-lock.yaml`, install dependencies, copy source, run `vite build`
- [x] 1.3 Configure runtime stage: copy built assets from `wwwroot/app`, use default nginx config, ensure nginx runs as non-root user
- [ ] 1.4 Verify the Dockerfile builds locally with `docker build` and the container serves the SPA correctly on port 80

## 2. Dockerignore

- [x] 2.1 Create `JB2026.WebApp/ClientApp/.dockerignore` excluding `node_modules/`, `tests/`, `test-results/`, `.git/`, `dist/`, `*.log`, and local config files

## 3. GitHub Actions Workflow

- [x] 3.1 Create `.github/workflows/clientapp-docker.yml` with triggers for `push` (main + other branches) and `pull_request`
- [x] 3.2 Add checkout step and set up Docker Buildx
- [x] 3.3 Add login step to GHCR using `docker/login-action` with GitHub token (only on push, not on PR)
- [x] 3.4 Add build-and-push step using `docker/build-push-action` with GHCR layer caching, tagging with commit SHA and `latest` (main only), and branch name tags
- [x] 3.5 Configure workflow permissions (`contents: read`, `packages: write`)

## 4. Verification

- [ ] 4.1 Test the workflow on a feature branch (build only, no push)
- [ ] 4.2 Merge to main and verify image is published to GHCR with correct tags
- [ ] 4.3 Pull the published image locally and verify the SPA loads correctly

