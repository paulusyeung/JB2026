#!/usr/bin/env bash
# =============================================================================
# JB2026 — Create a sudo user with SSH key auth, disable password SSH
# =============================================================================
# Run ONCE on the fresh server as root, BEFORE provision-server.sh:
#   sudo ./setup-user.sh <username> <public-key-file>
#
# Example (from your build machine, the key is usually ~/.ssh/id_ed25519.pub):
#   sudo ./setup-user.sh deploy ~/.ssh/id_ed25519.pub
#
# If you don't have a key yet, generate one on the build machine first:
#   ssh-keygen -t ed25519 -C "jb2026-deploy"
# =============================================================================
set -euo pipefail

USER="${1:?usage: setup-user.sh <username> <public-key-file>}"
KEYFILE="${2:?provide path to a public key file (e.g. ~/.ssh/id_ed25519.pub)}"
[ "$(id -u)" -eq 0 ] || { echo "ERROR: run as root (sudo)"; exit 1; }
[ -f "$KEYFILE" ] || { echo "ERROR: public key file not found: $KEYFILE"; exit 1; }

echo "==> Creating user '$USER'"
if id -u "$USER" >/dev/null 2>&1; then
  echo "    user already exists"
else
  adduser --disabled-password --gecos "" "$USER"
fi
usermod -aG sudo "$USER"

echo "==> Installing SSH public key"
mkdir -p "/home/$USER/.ssh"
chmod 700 "/home/$USER/.ssh"
install -m 600 -o "$USER" -g "$USER" "$KEYFILE" "/home/$USER/.ssh/authorized_keys"

echo "==> Hardening sshd (disable password auth)"
sed -i 's/^#\?PasswordAuthentication .*/PasswordAuthentication no/' /etc/ssh/sshd_config
sed -i 's/^#\?PubkeyAuthentication .*/PubkeyAuthentication yes/' /etc/ssh/sshd_config
sed -i 's/^#\?PermitRootLogin .*/PermitRootLogin prohibit-password/' /etc/ssh/sshd_config
systemctl restart ssh

echo ""
echo "User '$USER' is ready. Log in with:  ssh $USER@<VM-IP>"
echo "Password SSH is now disabled; only your key works."
