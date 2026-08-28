#!/usr/bin/env bash
# =============================================================================
# JB2026 — Harden sshd: disable password auth + lock root login
# =============================================================================
# Run ONCE on the server as root, ONLY AFTER you have verified key login works
# (README Step 4: `ssh deploy@<VM-IP>` drops you in with no password prompt).
#
#   sudo ./harden-ssh.sh
#
# Until this runs, password SSH is left enabled as a recovery path so a broken
# key setup cannot lock you out. Uses a drop-in (00- prefix) so it overrides
# Ubuntu's /etc/ssh/sshd_config.d/50-cloud-init.conf.
# =============================================================================
set -euo pipefail

[ "$(id -u)" -eq 0 ] || { echo "ERROR: run as root (sudo ./harden-ssh.sh)"; exit 1; }

echo "==> Verifying a non-root sudo user exists before locking root"
for u in $(awk -F: '($3>=1000)&&($3!=65534){print $1}' /etc/passwd); do
  if id -nG "$u" | tr ' ' '\n' | grep -qx sudo; then
    echo "    sudo user present: $u"
    HAVE_SUDO=1
    break
  fi
done
[ "${HAVE_SUDO:-}" = "1" ] || {
  echo "ERROR: no sudo user found. Create one / confirm key login before hardening."; exit 1;
}

echo "==> Writing sshd hardening drop-in"
mkdir -p /etc/ssh/sshd_config.d
cat > /etc/ssh/sshd_config.d/00-jb2026-ssh.conf <<CONF
# Managed by JB2026 harden-ssh.sh — applied AFTER key login was verified.
PubkeyAuthentication yes
PasswordAuthentication no
PermitRootLogin no
CONF

systemctl restart ssh
echo "SSH hardened: key login only; root password login disabled."
echo "Confirm you can still log in from another terminal before closing this session."
