#!/usr/bin/env bash
# =============================================================================
# JB2026 — Create a sudo user with SSH key auth, disable password SSH
# =============================================================================
# Run ONCE on the fresh server as root, BEFORE provision-server.sh:
#   sudo ./setup-user.sh <username> <public-key-file>
#
# Example (from your build machine):
#   scp ops/deploy/setup-user.sh ~/.ssh/id_ed25519.pub root@<VM-IP>:~
#   ssh root@<VM-IP>
#   sudo ./setup-user.sh deploy ~/id_ed25519.pub
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

# Passwordless sudo for this automation user. Authentication is the SSH key
# (deploy has no password), and the deploy/rollback scripts run `sudo` with no
# TTY password prompt. Scoped to this user only.
echo "$USER ALL=(ALL) NOPASSWD: ALL" > "/etc/sudoers.d/$USER"
chmod 440 "/etc/sudoers.d/$USER"

echo "==> Installing SSH public key"
mkdir -p "/home/$USER/.ssh"
chmod 700 "/home/$USER/.ssh"
# The script runs as root (sudo), so the .ssh dir would be root-owned and sshd
# (running as $USER) could not read authorized_keys. Chown it explicitly.
chown "$USER:$USER" "/home/$USER/.ssh"
install -m 600 -o "$USER" -g "$USER" "$KEYFILE" "/home/$USER/.ssh/authorized_keys"

echo "==> Enabling public-key authentication (drop-in wins over cloud-init)"
mkdir -p /etc/ssh/sshd_config.d
cat > /etc/ssh/sshd_config.d/00-jb2026-ssh.conf <<CONF
# Managed by JB2026 setup-user.sh.
# Password auth is deliberately LEFT ENABLED until key login is verified
# (Step 4), then disabled by harden-ssh.sh. The 00- prefix makes this file
# take precedence over Ubuntu's /etc/ssh/sshd_config.d/50-cloud-init.conf.
PubkeyAuthentication yes
CONF
systemctl restart ssh

echo ""
echo "User '$USER' is ready. Log in with:  ssh $USER@<VM-IP>"
echo "Password SSH is still enabled as a safety net until you verify key login,"
echo "then run harden-ssh.sh to disable password auth and lock root."
