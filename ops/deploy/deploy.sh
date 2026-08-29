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
#   <user@host>  SSH destination (the user must have passwordless sudo rights;
#                the SSH key is the only auth factor, so no password is prompted).
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
REMOTE_STAGING="jb2026-${VERSION}.tar.gz"

[ -d "$OUT/api" ] && [ -d "$OUT/web/app" ] || {
  echo "ERROR: artifacts missing. Run ./build.sh first." >&2
  exit 1
}

echo "==> Packaging version $VERSION"
tar czf "$TARBALL" -C "$OUT" api web

# Stage under the SSH user's home — deploy cannot write to $APP_HOME/tmp
# (owned by jb2026). The remote sudo step moves it into place.
echo "==> Shipping to $HOST:~/$REMOTE_STAGING"
scp "$TARBALL" "${HOST}:~/${REMOTE_STAGING}"

echo "==> Installing on remote (sudo password may be requested)"
ssh -t "$HOST" sudo bash -s -- "$VERSION" "$APP_HOME" "$REMOTE_STAGING" <<'REMOTE'
set -euo pipefail
VERSION="$1"
APP_HOME="$2"
STAGING_NAME="$3"
# Resolve the invoking user's home (sudo -u keeps SUDO_USER)
INVOKER="${SUDO_USER:-$USER}"
INVOKER_HOME="$(getent passwd "$INVOKER" | cut -d: -f6)"
STAGING="${INVOKER_HOME}/${STAGING_NAME}"
TARBALL="${APP_HOME}/tmp/jb2026-${VERSION}.tar.gz"
DEST="${APP_HOME}/releases/${VERSION}"

[ -f "$STAGING" ] || { echo "ERROR: staged tarball not found: $STAGING"; exit 1; }
mkdir -p "${APP_HOME}/tmp"
mv -f "$STAGING" "$TARBALL"

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
rm -f "$TARBALL"
echo "Deployed $VERSION"
REMOTE

HOST_ONLY="${HOST#*@}"
echo ""
echo "Deploy $VERSION complete."
echo "Verify:"
echo "  curl -fsS http://${HOST_ONLY}/healthz     # backend health (200 JSON)"
echo "  curl -fsSI http://${HOST_ONLY}/app/       # SPA (200)"
echo "  curl -fsSI http://${HOST_ONLY}/           # root redirect (302 -> /app/)"
echo "Rollback: ./rollback.sh $HOST <previous-version>"
echo "List releases: ssh $HOST 'ls /opt/jb2026/releases'"
