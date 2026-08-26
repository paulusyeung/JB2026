#!/usr/bin/env bash
# =============================================================================
# JB2026 — One-time Ubuntu 24.04 server provisioning (Section 1)
# =============================================================================
# Run ONCE on a fresh Ubuntu Server 24.04 (headless), as root (sudo), from
# inside this folder so the systemd unit and nginx site can be copied:
#
#   sudo ./provision-server.sh
#
# What it does:
#   1. Installs base packages + Nginx + .NET 8 ASP.NET Core runtime
#   2. Configures the UFW firewall (SSH, 80, 443)
#   3. Creates a dedicated 'jb2026' service account and /opt/jb2026 layout
#   4. Creates the secrets file /etc/jb2026/env (from env.template)
#   5. Installs the systemd unit and the Nginx site
#
# It does NOT deploy any application code — that is Section 2 (deploy.sh).
# Re-running is safe (idempotent).
# =============================================================================
set -euo pipefail

APP_USER=jb2026
APP_HOME=/opt/jb2026
ETC_HOME=/etc/jb2026
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

[ "$(id -u)" -eq 0 ] || { echo "ERROR: run as root (sudo ./provision-server.sh)"; exit 1; }

echo "==> Installing base packages"
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends \
  curl wget git unzip ca-certificates gnupg lsb-release \
  apt-transport-https ufw nginx cifs-utils nfs-common

echo "==> Installing .NET 8 ASP.NET Core runtime"
if ! dpkg -s aspnetcore-runtime-8.0 >/dev/null 2>&1; then
  wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb \
    -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  rm -f /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y --no-install-recommends aspnetcore-runtime-8.0
else
  echo "    aspnetcore-runtime-8.0 already present"
fi

echo "==> Configuring firewall (SSH, 80, 443)"
ufw allow OpenSSH
ufw allow 80
ufw allow 443
ufw --force enable

echo "==> Creating service account '${APP_USER}'"
id -u "$APP_USER" >/dev/null 2>&1 || \
  useradd --system --home "$APP_HOME" --shell /usr/sbin/nologin "$APP_USER"

echo "==> Creating directory layout"
mkdir -p "$APP_HOME/releases" "$APP_HOME/tmp" "$ETC_HOME"
chown -R "$APP_USER:$APP_USER" "$APP_HOME"
chmod 755 "$APP_HOME"

echo "==> Preparing external storage mountpoint"
mkdir -p /srv/jb2026
chown "$APP_USER:$APP_USER" /srv/jb2026

echo "==> Secrets file /etc/jb2026/env"
if [ ! -f "$ETC_HOME/env" ]; then
  cp "$SCRIPT_DIR/env.template" "$ETC_HOME/env"
  chown root:"$APP_USER" "$ETC_HOME/env"
  chmod 640 "$ETC_HOME/env"
  echo "    !! EDIT $ETC_HOME/env with real values before starting the service."
else
  echo "    $ETC_HOME/env already exists — left untouched."
fi

echo "==> Installing systemd unit"
cp "$SCRIPT_DIR/jb2026-api.service" /etc/systemd/system/jb2026-api.service
systemctl daemon-reload
systemctl enable jb2026-api.service

echo "==> Installing Nginx site"
cp "$SCRIPT_DIR/nginx-jb2026.conf" /etc/nginx/sites-available/jb2026
ln -sf /etc/nginx/sites-available/jb2026 /etc/nginx/sites-enabled/jb2026
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx

echo ""
echo "Provisioning complete."
echo "Next steps:"
echo "  1. Mount storage:  sudo ./mount-storage.sh  (see README)"
echo "  2. Edit $ETC_HOME/env (ConnectionStrings__Primary, Jwt__Key, LegacyFiles)."
echo "  3. Confirm SQL is reachable from this VM (e.g. TCP 1433)."
echo "  4. From the build machine:  ./build.sh && ./deploy.sh <user@host>"
echo "     (deploy starts the API via systemctl restart)."
