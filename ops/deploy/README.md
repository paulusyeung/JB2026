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
rm -f ~/id_ed25519.pub ~/setup-user.sh   # staged bootstrap files; safe to delete
exit
```

That script:

- creates user `deploy` with sudo
- installs your public key into `/home/deploy/.ssh/authorized_keys`
- fixes ownership of `/home/deploy/.ssh` to `deploy` (required — sshd runs as
  `deploy` and cannot read a root-owned `.ssh`)
- enables public-key SSH via a drop-in (`/etc/ssh/sshd_config.d/00-jb2026-ssh.conf`,
  which overrides Ubuntu's cloud-init drop-in)
- leaves **password SSH enabled** for now, as a safety net
- restarts `sshd`

#### Step 4 — Verify key login works (build machine)

```bash
ssh deploy@<VM-IP>
```

- **No password prompt** (or only your key passphrase) → success. Type `exit`.
- **Still asks for a password / permission denied** → the `.pub` did not land in
  `authorized_keys` **or** `/home/deploy/.ssh` is owned by `root` (a known
  `setup-user.sh` pitfall). Fix ownership with
  `sudo chown deploy:deploy /home/deploy/.ssh` and retry. Do not continue to 1.2
  until key login works.

Leave password SSH enabled for now — harden at the end of Section 1 (Step 1.6),
once provisioning, storage, and secrets are verified.

#### Note

`deploy` is given **passwordless sudo** by `setup-user.sh` (via
`/etc/sudoers.d/deploy`). Authentication is the SSH key — `deploy` has no
password — and the deploy/rollback scripts rely on non-interactive `sudo`. If you
later want to tighten this, replace that sudoers file with a scoped one, but the
deploy scripts currently run `sudo bash -s` and need broad rights.

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

> **Expected warning — benign nginx error during install.** If the host has IPv6
> disabled, `apt-get install nginx` prints
> `nginx: [emerg] socket() [::]:80 failed (Address family not supported by protocol)`
> and the nginx package's post-install fails. This is **expected and harmless**:
> the stock nginx *default* vhost listens on `[::]:80`, which the kernel rejects.
> The script removes that default vhost, runs `dpkg --configure -a` to finish the
> package, and at the end does `systemctl enable --now nginx` explicitly — so
> provision keeps going and finishes with `Provisioning complete.`
>
> Verify the outcome (don't be alarmed by the error above):
> ```bash
> systemctl status nginx --no-pager   # expect: active (running)
> curl -I http://127.0.0.1/           # expect: 302 -> /app/
> ```

### 1.3 Mount external shared storage

The backend reads paths from the `LegacyFiles` config section and **fails to
store attachments if `FileAgentRoot` is empty**. A helper script mounts a
CIFS/NFS share and creates the expected subfolders:

```bash
# Run from ~/deploy as the deploy user (passwordless sudo). These are console
# commands — mount-storage.sh already exists; you are NOT creating a new script.
# Pass SHARE_TYPE/SHARE_SRC on the sudo line, because sudo clears the environment.
cd ~/deploy

# For CIFS, create the credentials file first (do NOT commit it):
sudo install -m 640 -o root -g jb2026 /dev/stdin /etc/jb2026/storage.creds <<'CRED'
username=jb2026
password=CHANGE_ME
domain=WORKGROUP
CRED

# Then mount (replace with your real server/share). For NFS use
# SHARE_TYPE=nfs SHARE_SRC="host:/export/path" and skip the credentials file.
sudo SHARE_TYPE=cifs SHARE_SRC="//fileserver/jb2026" ./mount-storage.sh
```

The script adds the share to `/etc/fstab` (`_netdev,nofail`) and creates the
expected subfolders under `/srv/jb2026`, owned by `jb2026`. This **persists
across reboots**: systemd reads `fstab` at boot, `_netdev` waits for the
network, and `nofail` avoids hanging the boot if the share is temporarily
unreachable. The `jb2026-api.service` unit also has `RequiresMountsFor=/srv/jb2026`,
so the API is held until the mount is present.

Because the mount is owned `jb2026:jb2026` (mode `770`) and the API runs as
`jb2026`, the backend has full read/write access to the storage. (CIFS is
case-insensitive, so the lowercase `LegacyFiles__*` paths in `/etc/jb2026/env`
resolve to the share's mixed-case folders such as `DropBox`/`InBox`.)

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
``` bash
timeout 3 bash -c '</dev/tcp/<server_ip>/1433' && echo "Port is Open" || echo "Port is Closed"
```

### 1.5 Post-provision checklist

Before Section 2:

- [ ] `ssh deploy@<VM-IP>` works (key auth; password SSH disabled)
- [ ] `/etc/jb2026/env` has a real connection string + JWT key (≥ 32 chars)
- [ ] `mountpoint /srv/jb2026` is true; subfolders exist
- [ ] SQL is reachable from the VM (`nc -vz <sql-host> 1433` or equivalent)
- [ ] `ssh deploy@<VM-IP>` works (key auth)
- [ ] `/etc/jb2026/env` has a real connection string + JWT key (≥ 32 chars)
- [ ] `mountpoint /srv/jb2026` is true; subfolders exist
- [ ] SQL is reachable from the VM (`nc -vz <sql-host> 1433` or equivalent)
- [ ] UFW shows 22/80/443 allowed (`sudo ufw status`)

Then run Section 2 from the build machine.

### 1.6 Harden SSH (disable password auth + lock root)

Only now — after key login is verified (Step 4), the server is provisioned
(1.2), storage is mounted (1.3), secrets are set (1.4), and the checklist above
passes — should you disable password SSH. Keeping it on through all the setup
steps above leaves a recovery path if anything goes wrong.

```bash
# on the VM, as root (from ~/deploy):
sudo ./harden-ssh.sh
```

`harden-ssh.sh` refuses to run unless a sudo user exists, writes the drop-in
(`PasswordAuthentication no`, `PermitRootLogin no`), restarts `sshd`, and leaves
you with key-only access. Keep another terminal logged in while you run it so you
can recover if anything goes wrong.

Future logins and `./ops/deploy/deploy.sh` use this key automatically
(`scp`/`ssh` read `~/.ssh/id_ed25519`).

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

---

## Troubleshooting

- **`apt-get install nginx` shows `socket() [::]:80 failed (Address family not
  supported by protocol)`** — expected and harmless on hosts with IPv6 disabled
  (see the note in Section 1.2). The script removes the stock default vhost and
  finishes nginx via `systemctl enable --now nginx`. Provision still ends with
  `Provisioning complete.` Verify with `systemctl status nginx` (active) and
  `curl -I http://127.0.0.1/` (302 → `/app/`).

- **`ssh deploy@<VM-IP>` asks for a password or says permission denied** — the
  key is being rejected. The usual cause is `/home/deploy/.ssh` owned by `root`
  (the setup script runs as root, so `mkdir .ssh` creates it root-owned; it must
  be `deploy`-owned for sshd to read `authorized_keys`). Fix:
  ```bash
  sudo chown -R deploy:deploy /home/deploy/.ssh
  sudo chmod 700 /home/deploy/.ssh
  ```

- **`sudo` prompts for a password or fails** — `deploy` is created with no
  password, so password-based sudo cannot work. `setup-user.sh` grants
  passwordless sudo via `/etc/sudoers.d/deploy`. If that file is missing (e.g. an
  older script was used), recreate it:
  ```bash
  echo 'deploy ALL=(ALL) NOPASSWD: ALL' | sudo tee /etc/sudoers.d/deploy
  sudo chmod 440 /etc/sudoers.d/deploy
  ```

- **Provision aborts early / skips steps** — if you ever see the nginx error and
  provision does *not* print `Provisioning complete.`, you are running a script
  version before this fix. Re-run the current `provision-server.sh`; it tolerates
  the nginx post-install error and continues.
