# JB2026 Server Deployment

Target: a fresh **Ubuntu Server 24.04 LTS** (headless) VM that hosts **both**
the ASP.NET Core API and the Vue SPA. SQL Server lives on a separate host
(the app only needs a connection string).

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

### 1.1 Create the deploy user & harden SSH (recommended: SSH key)

Do this once. All steps marked **(build machine)** run on your laptop/dev box.
Steps marked **(VM)** run over SSH as root on the new Ubuntu server.

**What you're creating**

| File | Where it lives | Role |
| --- | --- | --- |
| `~/.ssh/id_ed25519` | build machine only | private key — never copy this to the VM |
| `~/.ssh/id_ed25519.pub` | build machine; also installed on VM | public key — unlocks SSH login as `deploy` |

#### Step 1 — Generate a keypair (build machine)

```bash
ssh-keygen -t ed25519 -C "jb2026-deploy"
```

When prompted:

- **File location:** press Enter → saves as `~/.ssh/id_ed25519` (and `.pub`)
- **Passphrase:** optional. Empty = no passphrase (simpler deploys). A passphrase
  means you'll type it (or use `ssh-agent`) each time you SSH.

Confirm the files exist:

```bash
ls -l ~/.ssh/id_ed25519 ~/.ssh/id_ed25519.pub
```

#### Step 2 — Install the public key on the VM (build machine → VM)

You still need root password login for this one-time bootstrap. From the repo
root on the build machine (replace `<VM-IP>`):

```bash
scp ops/deploy/setup-user.sh ~/.ssh/id_ed25519.pub root@<VM-IP>:~
```

You'll be asked for the **root password** of the VM.

#### Step 3 — Create user `deploy` and lock SSH to keys (VM)

```bash
ssh root@<VM-IP>          # root password again
sudo ./setup-user.sh deploy ~/id_ed25519.pub
rm -f ~/id_ed25519.pub    # staged copy; safe to delete on the VM
exit
```

That script:

- creates user `deploy` with sudo
- installs your public key into `/home/deploy/.ssh/authorized_keys`
- disables password SSH (`PasswordAuthentication no`)
- keeps pubkey SSH on
- restarts `sshd`

#### Step 4 — Verify key login works (build machine)

```bash
ssh deploy@<VM-IP>
```

- **No password prompt** (or only your key passphrase) → success. Type `exit`.
- **Still asks for a password / permission denied** → the `.pub` did not land in
  `authorized_keys`. Re-check Step 2–3; do not continue to 1.2 until this works.

After Step 4, password SSH is off. Future logins and `./ops/deploy/deploy.sh`
use this key automatically (`scp`/`ssh` read `~/.ssh/id_ed25519`).

#### Optional later

- Set `PermitRootLogin no` in `/etc/ssh/sshd_config` once `deploy` works.
- Grant `deploy` passwordless sudo only for the commands deploy/restart need.

<details>
<summary>Alternatives (manual paste / ssh-copy-id)</summary>

Manual (on the VM as root), paste the **contents** of your build machine's
`~/.ssh/id_ed25519.pub` into `authorized_keys`:

```bash
adduser --disabled-password --gecos "" deploy
usermod -aG sudo deploy
mkdir -p /home/deploy/.ssh && chmod 700 /home/deploy/.ssh
echo "ssh-ed25519 AAAA...paste-from-id_ed25519.pub..." > /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
chown -R deploy:deploy /home/deploy/.ssh
sed -i 's/^#\?PasswordAuthentication .*/PasswordAuthentication no/'  /etc/ssh/sshd_config
sed -i 's/^#\?PubkeyAuthentication .*/PubkeyAuthentication yes/'     /etc/ssh/sshd_config
sed -i 's/^#\?PermitRootLogin .*/PermitRootLogin prohibit-password/' /etc/ssh/sshd_config
systemctl restart ssh
```

Or, if `deploy` already exists and password SSH is still allowed, from the
build machine:

```bash
ssh-copy-id -i ~/.ssh/id_ed25519.pub deploy@<VM-IP>
```

</details>

### 1.2 Copy this folder and provision

```bash
# build machine — only works after 1.1 Step 4 succeeds
scp -r ops/deploy deploy@<VM-IP>:~
ssh deploy@<VM-IP>
sudo ./deploy/provision-server.sh
```

What the script installs / creates:

| Item | Detail |
| --- | --- |
| Packages | `curl wget git unzip ca-certificates gnupg lsb-release apt-transport-https ufw nginx cifs-utils nfs-common` |
| Runtime | `aspnetcore-runtime-8.0` (via Microsoft repo) |
| Firewall (ufw) | allow `OpenSSH`, `80`, `443` |
| Account | system user `jb2026` (`/usr/sbin/nologin`) |
| Folders | `/opt/jb2026/{releases,tmp}`, `/srv/jb2026`, `/etc/jb2026/env` |
| systemd | `/etc/systemd/system/jb2026-api.service` (enabled, not started; binds `127.0.0.1:8080`) |
| Nginx | `/etc/nginx/sites-available/jb2026` → `sites-enabled`; default site removed |

Result: the VM exposes only `:80` publicly. The API listens privately on
`127.0.0.1:8080` and is reached through Nginx (`/api/`, `/healthz`).

### 1.3 Mount external shared storage

The backend reads paths from the `LegacyFiles` config section and **fails to
store attachments if `FileAgentRoot` is empty**. A helper script mounts a
CIFS/NFS share and creates the expected subfolders:

```bash
# on the server, as root (from ~/deploy):
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

### 1.4 Fill secrets + storage env

```bash
sudo nano /etc/jb2026/env      # fill ConnectionStrings__Primary, Jwt__Key,
                              # and confirm LegacyFiles__* paths
```

Important: the repo's `appsettings.json` contains real-looking secrets
(Twenty CRM key, Paperless token, Invoice Ninja key, Mailcow password). Do
**not** copy it to the server — set only the values you need in `/etc/jb2026/env`.

Also ensure the VM can reach SQL Server (typically TCP `1433`) from this host
before the first deploy.

### 1.5 Post-provision checklist

Before Section 2:

- [ ] `ssh deploy@<VM-IP>` works (key auth; password SSH disabled)
- [ ] `/etc/jb2026/env` has a real connection string + JWT key (≥ 32 chars)
- [ ] `mountpoint /srv/jb2026` is true; subfolders exist
- [ ] SQL is reachable from the VM (`nc -vz <sql-host> 1433` or equivalent)
- [ ] UFW shows 22/80/443 allowed (`sudo ufw status`)

Then run Section 2 from the build machine.

---

## Section 2 — Repeatable build & deploy

Run on the **build machine** (the dev box — already has .NET 8 SDK + Node 22+
with pnpm).

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
- `scp`s to the SSH user's home (not `/opt/...` — that tree is owned by
  `jb2026`), then the remote sudo step moves it into `/opt/jb2026/tmp`.
- Extracts to `/opt/jb2026/releases/<version>`, fixes ownership/permissions,
  atomically swaps the `current` symlink, then `systemctl restart jb2026-api`
  and `nginx -s reload`.
- You will be prompted for the sudo password (TTY is allocated).

Version default: `git describe --tags --always`, else a timestamp.

### 2.3 Verify

```bash
curl -fsS  http://<VM-IP>/healthz     # backend health → 200 JSON
curl -fsSI http://<VM-IP>/app/        # SPA → 200
curl -fsSI http://<VM-IP>/            # root → 302 Location: /app/
```

On the server if something fails:

```bash
sudo systemctl status jb2026-api
sudo journalctl -u jb2026-api -e --no-pager
sudo nginx -t
```

### 2.4 Rollback

```bash
./ops/deploy/rollback.sh <user@host> <previous-version>
```

Re-points `current` to a previously installed release and restarts. List
installed versions:

```bash
ssh deploy@<VM-IP> 'ls -1 /opt/jb2026/releases'
```

Old releases remain under `/opt/jb2026/releases/` until you delete them.
Prune manually when disk gets tight, e.g. keep the last 5 + whatever
`current` points at:

```bash
ssh deploy@<VM-IP>
cd /opt/jb2026/releases
# inspect, then: sudo rm -rf <old-version>
```

---

## Layout on the server

```
/opt/jb2026/
  current -> releases/<version>     # symlink swapped on each deploy
  releases/<version>/{api,web}
  tmp/                              # incoming tarballs (cleared after install)
/etc/jb2026/env                     # SECRETS (640 root:jb2026), never overwritten
/srv/jb2026/                        # external share (attachments, cloud, …)
```

## Notes

- **HTTP only until TLS:** the provision script opens port 443, but no
  certificate is installed. Add `certbot` + a 443 server block when you have a
  DNS name. Until then, traffic is plaintext on `:80`.
- **Backend build type:** framework-dependent (the runtime is installed in
  Section 1). For a fully self-contained VM, add `--self-contained -r linux-x64`
  to the `dotnet publish` line in `build.sh` and skip the runtime install.
- **Backend exposure:** localhost-only behind Nginx (recommended). To expose
  `8080` publicly instead, change `ASPNETCORE_URLS` in the unit and the
  firewall — that is a Section 1 change, not a deploy change.
- **Database schema:** this deploy path does not run migrations. Own schema
  changes separately (SSMS / your existing SQL process) before pointing
  `ConnectionStrings__Primary` at a new database.
- **systemd notify:** the unit uses `Type=simple` because `Program.cs` does not
  call `UseSystemd()`. Switch to `Type=notify` if you add it.
