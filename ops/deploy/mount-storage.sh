#!/usr/bin/env bash
# =============================================================================
# JB2026 — Mount the external shared folder backing file storage
# =============================================================================
# The backend stores job attachments, CloudDisk uploads, product pictures and
# SML files under paths from the "LegacyFiles" config section. Those paths must
# live on the external shared folder (typically an SMB/CIFS share from the
# legacy Windows environment). This script mounts it and prepares subfolders.
#
# Configure by exporting variables (or editing the block below):
#   SHARE_TYPE   cifs (default) | nfs
#   SHARE_SRC    cifs: //host/share      nfs: host:/export/path
#   MOUNT_POINT  /srv/jb2026 (default)
# For CIFS, put credentials in a file (NOT committed to the repo), e.g.
#   /etc/jb2026/storage.creds  ->  username=..., password=..., domain=...
#
# Usage:  sudo ./mount-storage.sh
# Idempotent: safe to re-run.
# =============================================================================
set -euo pipefail

SHARE_TYPE="${SHARE_TYPE:-cifs}"
SHARE_SRC="${SHARE_SRC:?set SHARE_SRC (e.g. //host/share or host:/path)}"
MOUNT_POINT="${MOUNT_POINT:-/srv/jb2026}"
CRED_FILE="${CRED_FILE:-/etc/jb2026/storage.creds}"
APP_USER=jb2026

[ "$(id -u)" -eq 0 ] || { echo "ERROR: run as root (sudo)"; exit 1; }

echo "==> Preparing mount point $MOUNT_POINT"
mkdir -p "$MOUNT_POINT"
chown "$APP_USER:$APP_USER" "$MOUNT_POINT"

FSTAB_LINE=""
if [ "$SHARE_TYPE" = "cifs" ]; then
  echo "==> Installing cifs-utils"
  apt-get install -y --no-install-recommends cifs-utils
  [ -f "$CRED_FILE" ] || { echo "ERROR: CIFS credentials file missing: $CRED_FILE"; exit 1; }
  chown root:"$APP_USER" "$CRED_FILE"
  chmod 640 "$CRED_FILE"
  FSTAB_LINE="${SHARE_SRC} ${MOUNT_POINT} cifs credentials=${CRED_FILE},uid=${APP_USER},gid=${APP_USER},file_mode=0770,dir_mode=0770,iocharset=utf8,nounix,noserverino,vers=3.0,_netdev,nofail 0 0"
else
  echo "==> Installing nfs-common"
  apt-get install -y --no-install-recommends nfs-common
  FSTAB_LINE="${SHARE_SRC} ${MOUNT_POINT} nfs defaults,_netdev,nofail 0 0"
fi

if ! grep -qs "$MOUNT_POINT" /etc/fstab; then
  echo "$FSTAB_LINE" >> /etc/fstab
  echo "    added fstab entry"
fi

if mountpoint -q "$MOUNT_POINT"; then
  echo "    $MOUNT_POINT already mounted"
else
  mount "$MOUNT_POINT"
  echo "    mounted $MOUNT_POINT"
fi

echo "==> Creating storage subfolders (owned by $APP_USER)"
for d in attachments cloud products sml dropbox inbox outbox work; do
  mkdir -p "$MOUNT_POINT/$d"
  chown "$APP_USER:$APP_USER" "$MOUNT_POINT/$d"
  chmod 2770 "$MOUNT_POINT/$d"
done

echo ""
echo "Storage ready. Ensure /etc/jb2026/env contains (mount-storage.sh defaults):"
echo "  LegacyFiles__FileAgentRoot=$MOUNT_POINT/attachments"
echo "  LegacyFiles__InBox=$MOUNT_POINT/attachments"
echo "  LegacyFiles__CloudDiskRoot=$MOUNT_POINT/cloud"
echo "  LegacyFiles__ProductPictureRoot=$MOUNT_POINT/products"
echo "  LegacyFiles__SmlFileRoot=$MOUNT_POINT/sml"
echo "  LegacyFiles__DropBox=$MOUNT_POINT/dropbox"
echo "  LegacyFiles__OutBox=$MOUNT_POINT/outbox"
echo "  LegacyFiles__WorkFolder=$MOUNT_POINT/work"
