#!/usr/bin/env bash
# =============================================================================
# JB2026 — Deploy to the server (Section 2, step 2)
# =============================================================================
# Run on the BUILD MACHINE after ./build.sh. Ships the built artifacts to the
# server, installs them under /opt/jb2026/releases/<version>, swaps the
# 'current' symlink atomically, and restarts the API + Nginx.
#
# Usage:
#   ./deploy.sh <user@host> [version]
#
#   <user@host>  SSH destination (the user must have sudo rights; you will be
#                prompted for the sudo password via an allocated TTY).
#   [version]    Release label (default: git describe, else timestamp).
#
# Secrets (/etc/jb2026/env) and server config are NEVER touched by this script.
# =============================================================================
set -euo pipefail

HOST="${1:?usage: deploy.sh <user@host> [version]}"
VERSION="${2:-$(git -C "$(cd "$(dirname "$0")/../.." && pwd)" describe --tags --always 2>/dev/null || date +%Y%m%d-%H%M%S)}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUT="$REPO_ROOT/artifacts"
TARBALL="/tmp/jb2026-${VERSION}.tar.gz"
APP_HOME=/opt/jb2026

[ -d "$OUT/api" ] && [ -d "$OUT/web/app" ] || {
  echo "ERROR: artifacts missing. Run ./build.sh first." >&2
  exit 1
}

echo "==> Packaging version $VERSION"
tar czf "$TARBALL" -C "$OUT" api web

echo "==> Shipping to $HOST:$APP_HOME/tmp"
scp "$TARBALL" "${HOST}:${APP_HOME}/tmp/"

echo "==> Installing on remote (sudo password may be requested)"
ssh -t "$HOST" sudo bash -s -- "$VERSION" "$APP_HOME" <<'REMOTE'
set -euo pipefail
VERSION="$1"
APP_HOME="$2"
TARBALL="${APP_HOME}/tmp/jb2026-${VERSION}.tar.gz"
DEST="${APP_HOME}/releases/${VERSION}"

mkdir -p "$DEST"
tar xzf "$TARBALL" -C "$DEST"
chown -R jb2026:jb2026 "$DEST"

# Fix permissions: api private to service user, web readable by Nginx (www-data)
chmod 750 "$DEST/api"
chmod 755 "$DEST/web"
find "$DEST/web" -type d -exec chmod 755 {} \;
find "$DEST/web" -type f -exec chmod 644 {} \;

# Atomic swap of the 'current' symlink (enables clean rollback)
ln -sfn "$DEST" "${APP_HOME}/current.new"
mv -Tf "${APP_HOME}/current.new" "${APP_HOME}/current"

systemctl restart jb2026-api
nginx -s reload
echo "Deployed $VERSION"
REMOTE

echo ""
echo "Deploy $VERSION complete."
echo "Verify:"
echo "  curl -f http://${HOST#*@}/healthz     # backend health"
echo "  curl -I http://${HOST#*@}/app/         # SPA"
echo "Rollback: ./rollback.sh $HOST $VERSION"
