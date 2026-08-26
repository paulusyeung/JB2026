# JB2026 Server Deployment

Two clearly separated concerns:

- **Section 1 — Provision the server (once):** `provision-server.sh` installs
  packages, firewall, a service account, folders, the systemd unit, and the
  Nginx site. No application code is deployed here.
- **Section 2 — Build & deploy (repeatable):** `build.sh` builds the backend
  and frontend on the dev machine; `deploy.sh` ships the artifacts and
  restarts. `rollback.sh` reverts to a previous release. Secrets and server
  config are never touched by Section 2.

All files live in this folder (`ops/deploy/`).

---

## Section 1 — One-time server provisioning

Run on a fresh **Ubuntu Server 24.04 LTS** (headless) VM. SQL Server is assumed
to live on a separate server (the app only needs a connection string).

1. **Create the deploy user & harden SSH** (run on the server as root, before
   provisioning). A helper script is provided:
   ```bash
   # on your build machine, if you don't already have a key:
   ssh-keygen -t ed25519 -C "jb2026-deploy"

   # copy this folder to the server first (or just the script):
   scp ops/deploy/setup-user.sh root@<VM-IP>:~
   ssh root@<VM-IP>
   sudo ./setup-user.sh deploy ~/.ssh/id_ed25519.pub
   ```
   What it does: creates `deploy` (with sudo), installs your public key into
   `~deploy/.ssh/authorized_keys`, sets `PasswordAuthentication no`,
   `PubkeyAuthentication yes`, `PermitRootLogin prohibit-password`, and restarts
   sshd. After this, log in only with `ssh deploy@<VM-IP>`.

   Equivalent manual commands (if you prefer not to use the script):
   ```bash
   adduser --disabled-password --gecos "" deploy
   usermod -aG sudo deploy
   mkdir -p /home/deploy/.ssh && chmod 700 /home/deploy/.ssh
   echo "ssh-ed25519 AAAA...your-key..." > /home/deploy/.ssh/authorized_keys
   chmod 600 /home/deploy/.ssh/authorized_keys
   chown -R deploy:deploy /home/deploy/.ssh
   sed -i 's/^#\?PasswordAuthentication .*/PasswordAuthentication no/'  /etc/ssh/sshd_config
   sed -i 's/^#\?PubkeyAuthentication .*/PubkeyAuthentication yes/'     /etc/ssh/sshd_config
   sed -i 's/^#\?PermitRootLogin .*/PermitRootLogin prohibit-password/' /etc/ssh/sshd_config
   systemctl restart ssh
   ```
   (Paste your real public key, or use `ssh-copy-id deploy@<VM-IP>` from the
   build machine while password login is still temporarily allowed.)

2. Copy this folder to the server (it contains the unit + nginx config the
   script needs):
   ```bash
   scp -r ops/deploy deploy@<VM-IP>:~
   ```
3. Run the provisioning script as root:
   ```bash
   ssh deploy@<VM-IP>
   sudo ./deploy/provision-server.sh
   ```
4. Mount the external shared folder (backing job attachments, CloudDisk
   uploads, product pictures, SML files). The backend reads these from the
   `LegacyFiles` config section and **fails to store attachments if
   `FileAgentRoot` is empty**. A helper script mounts a CIFS/NFS share and
   creates the expected subfolders:
   ```bash
   # on the server, as root:
   export SHARE_TYPE=cifs                      # or nfs
   export SHARE_SRC="//fileserver/jb2026"      # cifs: //host/share | nfs: host:/path
   # for CIFS, create the credentials file first (do NOT commit it):
   sudo install -m 640 -o root -g jb2026 /dev/stdin /etc/jb2026/storage.creds <<'CRED'
   username=jb2026
   password=CHANGE_ME
   domain=WORKGROUP
   CRED
   sudo ./mount-storage.sh
   ```
   The script adds the share to `/etc/fstab` (`_netdev`) and creates
   `/srv/jb2026/{attachments,cloud,products,sml,...}` owned by `jb2026`.
   The systemd unit already has `RequiresMountsFor=/srv/jb2026` so the API
   waits for the mount at boot.

5. Edit the secrets + storage file (created by the script from `env.template`):
   ```bash
   sudo nano /etc/jb2026/env      # fill ConnectionStrings__Primary, Jwt__Key,
                                  # and the LegacyFiles__* storage paths
   ```
   Important: the repo's `appsettings.json` contains real-looking secrets
   (Twenty CRM key, Paperless token, Invoice Ninja key, Mailcow password). Do
   **not** copy it to the server — set only the values you need in `/etc/jb2026/env`.

What the script installs / creates:

| Item | Detail |
| --- | --- |
| Packages | `curl wget git unzip ca-certificates gnupg lsb-release apt-transport-https ufw nginx` |
| Runtime | `aspnetcore-runtime-8.0` (via Microsoft repo) |
| Firewall (ufw) | allow `OpenSSH`, `80`, `443` |
| Account | system user `jb2026` (`/usr/sbin/nologin`) |
| Folders | `/opt/jb2026/{releases,tmp}`, `/etc/jb2026/env` |
| systemd | `/etc/systemd/system/jb2026-api.service` (enabled, not started) |
| Nginx | `/etc/nginx/sites-available/jb2026` → `sites-enabled`; default site removed |

Result: the VM exposes only `:80`. The API listens privately on
`127.0.0.1:8080` and is reached only through Nginx's `/api/` proxy.

---

## Section 2 — Repeatable build & deploy

Run on the **build machine** (the dev box — already has .NET 8 SDK + pnpm).

### 2.1 Build
```bash
./ops/deploy/build.sh
```
Produces `artifacts/`:
- `artifacts/api/` — `dotnet publish` output (`JB2026.Api.dll` + deps)
- `artifacts/web/app/` — built Vue SPA (vite `base: '/app/'`)

### 2.2 Deploy
```bash
./ops/deploy/deploy.sh <user@host> [version]
```
- Packages `artifacts/` into `/tmp/jb2026-<version>.tar.gz`.
- `scp`s to `/opt/jb2026/tmp`.
- On the server (via `sudo`): extracts to `/opt/jb2026/releases/<version>`,
  fixes ownership/permissions, atomically swaps the `current` symlink, then
  `systemctl restart jb2026-api` and `nginx -s reload`.
- You will be prompted for the sudo password (TTY is allocated).

### 2.3 Verify
```bash
curl -f http://<VM-IP>/healthz     # backend health (200)
curl -I http://<VM-IP>/app/         # SPA (302 -> /app/)
```

### 2.4 Rollback
```bash
./ops/deploy/rollback.sh <user@host> <previous-version>
```
Re-points `current` to a previously installed release and restarts. Old
releases remain under `/opt/jb2026/releases/` until you delete them.

---

## Layout on the server
```
/opt/jb2026/
  current -> releases/<version>     # symlink swapped on each deploy
  releases/<version>/{api,web}
  tmp/                              # incoming tarballs
/etc/jb2026/env                     # SECRETS (640 root:jb2026), never overwritten
```

## Notes
- **Backend build type:** framework-dependent (the runtime is installed in
  Section 1). For a fully self-contained VM, add `--self-contained -r linux-x64`
  to the `dotnet publish` line in `build.sh` and skip the runtime install.
- **Backend exposure:** localhost-only behind Nginx (recommended). To expose
  `8080` publicly instead, change the API's bind address and the firewall — that
  is a Section 1 change, not a deploy change.
- **TLS:** add `certbot` and a 443 server block later; the provision script
  already opens port 443.
- **systemd notify:** the unit uses `Type=simple` because `Program.cs` does not
  call `UseSystemd()`. Switch to `Type=notify` if you add it.
