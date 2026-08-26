#!/usr/bin/env bash
# =============================================================================
# JB2026 — Rollback to a previous release (Section 2, helper)
# =============================================================================
# Repoints /opt/jb2026/current to an already-installed release and restarts.
# Releases are kept under /opt/jb2026/releases/<version> by deploy.sh.
#
# Usage:
#   ./rollback.sh <user@host> <version>
# =============================================================================
set -euo pipefail

HOST="${1:?usage: rollback.sh <user@host> <version>}"
VERSION="${2:?provide the release version to roll back to}"
APP_HOME=/opt/jb2026

echo "==> Rolling back $HOST to $VERSION"
ssh -t "$HOST" sudo bash -s -- "$APP_HOME" "$VERSION" <<'REMOTE'
set -euo pipefail
APP_HOME="$1"
VERSION="$2"
DEST="$APP_HOME/releases/$VERSION"
[ -d "$DEST" ] || { echo "ERROR: release $VERSION not found in $APP_HOME/releases"; exit 1; }
ln -sfn "$DEST" "${APP_HOME}/current.new"
mv -Tf "${APP_HOME}/current.new" "${APP_HOME}/current"
systemctl restart jb2026-api
nginx -s reload
echo "Rolled back to $VERSION"
REMOTE
