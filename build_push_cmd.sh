#!/usr/bin/env bash
# =============================================================================
# Build and push the JB2026 backend and frontend images to Docker Hub.
#
# Usage:
#   ./build_push_cmd.sh <docker-hub-username> [image-tag]
#
# Examples:
#   ./build_push_cmd.sh johndoe            # tags as :latest
#   ./build_push_cmd.sh johndoe v1.0.0     # tags as :v1.0.0
#
# The script logs you in, builds both images, and pushes them. The frontend
# image is built from ./JB2026.WebApp/ClientApp (its own Dockerfile).
# =============================================================================
set -euo pipefail

USERNAME="${1:?Provide your Docker Hub username as the first argument}"
TAG="${2:-latest}"

BACKEND_IMAGE="${USERNAME}/jb2026-backend:${TAG}"
FRONTEND_IMAGE="${USERNAME}/jb2026-frontend:${TAG}"

echo "==> Logging in to Docker Hub"
docker login

echo "==> Building backend: ${BACKEND_IMAGE}"
docker build -t "${BACKEND_IMAGE}" -f Dockerfile .

echo "==> Building frontend: ${FRONTEND_IMAGE}"
docker build -t "${FRONTEND_IMAGE}" -f JB2026.WebApp/ClientApp/Dockerfile ./JB2026.WebApp/ClientApp

echo "==> Pushing backend"
docker push "${BACKEND_IMAGE}"

echo "==> Pushing frontend"
docker push "${FRONTEND_IMAGE}"

echo "Done. Images published:"
echo "  ${BACKEND_IMAGE}"
echo "  ${FRONTEND_IMAGE}"
