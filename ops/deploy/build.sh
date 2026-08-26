#!/usr/bin/env bash
# =============================================================================
# JB2026 — Build backend + frontend (Section 2, step 1)
# =============================================================================
# Run on the BUILD MACHINE (this dev box). Requires:
#   - .NET 8 SDK   (dotnet)
#   - Node 22+     (node) and pnpm (corepack enable pnpm)
#
# Produces artifacts/ with:
#   artifacts/api/        -> dotnet publish output (JB2026.Api.dll + deps)
#   artifacts/web/app/    -> built Vue SPA (vite base '/app/')
#
# Then run ./deploy.sh <user@host> to ship them.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUT="$REPO_ROOT/artifacts"

rm -rf "$OUT/api" "$OUT/web"
mkdir -p "$OUT/api" "$OUT/web/app"

echo "==> Building backend (.NET 8) -> $OUT/api"
dotnet publish "$REPO_ROOT/JB2026.Api/JB2026.Api.csproj" \
  --configuration Release \
  --output "$OUT/api"

echo "==> Building frontend (Vue 3) -> $OUT/web/app"
cd "$REPO_ROOT/JB2026.WebApp/ClientApp"
pnpm install --frozen-lockfile
pnpm run build
# vite.config.ts: outDir '../wwwroot/app', base '/app/'
cp -r "$REPO_ROOT/JB2026.WebApp/wwwroot/app/." "$OUT/web/app/"

echo ""
echo "Build complete. Artifacts in: $OUT"
echo "  api/   (backend)"
echo "  web/   (frontend SPA)"
echo "Next: ./deploy.sh <user@host>"
