# JB2026 Docker Deployment Guide

Deploy the **backend** (.NET 8 API) and **frontend** (Vue 3 SPA + Nginx) as
two Docker images, orchestrated with Docker Compose. SQL Server stays
**external** (recommended for production). Optional in-compose SQL is
documented at the end for local/dev only.

| Piece | Image | Default port |
| --- | --- | --- |
| Backend | `<user>/jb2026-backend:<tag>` | container `8080` |
| Frontend | `<user>/jb2026-frontend:<tag>` | container `80` (host often `80` or `443` via TLS proxy) |

Source of truth in the repo:

| File | Role |
| --- | --- |
| `Dockerfile` (repo root) | Backend multi-stage build |
| `JB2026.WebApp/ClientApp/Dockerfile` | Frontend multi-stage build |
| `docker-compose.yml` (repo root) | Runtime stack (pulls published images) |
| `.env.example` (repo root) | Env template → copy to `.env` next to compose |
| `build_push_cmd.sh` | Build + push both images to Docker Hub |

This guide is the Docker path. The bare-metal systemd/Nginx path lives in
[`ops/deploy/`](../deploy/README.md) — pick **one** model per host.

---

## Architecture (what talks to what)

```
Browser
  │  HTTPS (recommended) or HTTP
  ▼
┌─────────────────────────────┐
│  frontend (Nginx :80)       │
│  /app/  → Vue SPA           │
│  /api/  → proxy_pass        │──► backend :8080  ──► SQL Server (external)
└─────────────────────────────┘         │
                                        ├── optional: Billing / Twenty / Ollama / Paperless
```

- The SPA is served under `/app/`.
- Browser calls same-origin `/api/...`; Nginx proxies to the backend
  (`BACKEND_URL`, default `http://backend:8080` on the Compose network).
- You normally **do not** need to expose the backend port publicly if the
  frontend proxy is the only entry point.

---

## Prerequisites

### Build machine (CI or laptop)

- Docker Engine 24+ with BuildKit (`DOCKER_BUILDKIT=1` is default on modern installs)
- Docker Hub account (or another registry — adjust image names)
- Git checkout of this repo
- Network access to pull base images (`mcr.microsoft.com`, `docker.io`)

### Target server

- Linux x86_64 (guide assumes **Ubuntu 24.04 LTS**; other distros work with equivalent packages)
- Docker Engine + Compose plugin (`docker compose` v2)
- Outbound access to Docker Hub (to pull images) and to your SQL Server
- Open firewall for **80/443** (and SSH). Prefer **not** opening `8080` publicly.
- A reachable SQL Server and a database/login for the app

### Secrets you must have before go-live

| Secret / config | Notes |
| --- | --- |
| `DB_CONNECTION_STRING` | SQL Server; app **refuses to start** without it |
| `JWT_KEY` | ≥ 32 characters; random; never commit |
| Public URL / origin | For `CORS_ALLOWED_ORIGINS` if anything hits the API off same-origin |

Generate a JWT key:

```bash
openssl rand -base64 48
```

---

## Section 1 — Build and publish images

Run from the **repository root** on the build machine.

### 1.1 Log in to the registry

```bash
docker login
# username / password or access token
```

### 1.2 Build and push (helper script)

```bash
chmod +x ./build_push_cmd.sh
./build_push_cmd.sh <docker-hub-username> v1.0.0
```

That builds and pushes:

- `<user>/jb2026-backend:v1.0.0`
- `<user>/jb2026-frontend:v1.0.0`

Omit the tag to publish `:latest` (fine for smoke tests; **pin a semver/SHA tag in production**).

### 1.3 Equivalent manual commands

```bash
USER=youruser
TAG=v1.0.0

docker build -t "${USER}/jb2026-backend:${TAG}" -f Dockerfile .
docker build -t "${USER}/jb2026-frontend:${TAG}" \
  -f JB2026.WebApp/ClientApp/Dockerfile ./JB2026.WebApp/ClientApp

docker push "${USER}/jb2026-backend:${TAG}"
docker push "${USER}/jb2026-frontend:${TAG}"
```

### 1.4 Verify locally before pushing (optional)

```bash
# Backend only — needs a real connection string
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Primary='Server=...;Database=JB2026;User Id=...;Password=...;TrustServerCertificate=True' \
  -e Jwt__Key='your-secure-random-key-at-least-32-chars' \
  youruser/jb2026-backend:v1.0.0

curl -sS http://127.0.0.1:8080/healthz
# expect: {"status":"Healthy"} or equivalent JSON
```

---

## Section 2 — Prepare the server (once)

### 2.1 Install Docker (Ubuntu 24.04)

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker "$USER"
```

Log out/in (or `newgrp docker`) so `docker` works without sudo.

```bash
docker version
docker compose version
```

### 2.2 Create a deploy directory

Keep compose + secrets **outside** a full git clone if you prefer a thin host:

```bash
sudo mkdir -p /opt/jb2026
sudo chown "$USER":"$USER" /opt/jb2026
cd /opt/jb2026
```

Copy onto the server (scp, rsync, or git sparse checkout):

- `docker-compose.yml`
- `.env` (created next — **never** commit real secrets)

Example from the build machine:

```bash
scp docker-compose.yml deploy@<VM-IP>:/opt/jb2026/
# .env: create on the server; do not scp secrets over plaintext if you can avoid it
```

### 2.3 Firewall (UFW sketch)

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
# do NOT open 8080 / 1433 publicly unless you have a strong reason
sudo ufw enable
sudo ufw status
```

### 2.4 (Optional) Private Hub images

If the Hub repo is private:

```bash
docker login
# credentials stored for the deploy user under ~/.docker/config.json
```

Prefer a **read-only** Hub access token scoped to pull only.

---

## Section 3 — Configure `.env`

On the server:

```bash
cd /opt/jb2026
# If you have the repo:
#   cp /path/to/repo/.env.example .env
# Otherwise create .env from the template below.
nano .env
chmod 600 .env
```

Minimum production example:

```bash
DOCKER_HUB_USERNAME=youruser
IMAGE_TAG=v1.0.0

FRONTEND_PORT=80
# Leave backend unpublished in production by not mapping it, or bind to localhost only
# (see Hardening). Default compose publishes BACKEND_PORT — override if needed:
BACKEND_PORT=8080

DB_CONNECTION_STRING=Server=sql.example.com,1433;Database=JB2026;User Id=jb2026;Password=REDACTED;TrustServerCertificate=True;Encrypt=True

JWT_KEY=paste-openssl-rand-output-here
JWT_ISSUER=jb2026-api
JWT_AUDIENCE=jb2026-clients

# Public origins that may call the API directly (mobile, other hosts).
# Same-origin via Nginx /api/ often means browsers do not need CORS for the SPA,
# but set your real site origin anyway:
CORS_ALLOWED_ORIGINS=https://app.example.com

BILLING_BASE_URL=
TWENTY_CRM_BASE_URL=
OLLAMA_BASE_URL=
PAPERLESS_NGX_BASE_URL=
```

Notes:

- Compose interpolates `${VAR:?message}` — missing `DB_CONNECTION_STRING` or
  `JWT_KEY` **aborts** `docker compose up` with a clear error.
- Connection string host must be reachable **from inside the backend container**
  (not `localhost` unless SQL runs in the same network namespace).
- Linux + Ollama on the host: use
  `OLLAMA_BASE_URL=http://host.docker.internal:11434` and add to the backend
  service (Compose file change):

  ```yaml
  extra_hosts:
    - "host.docker.internal:host-gateway"
  ```

---

## Section 4 — First deploy

```bash
cd /opt/jb2026
docker compose pull
docker compose up -d
docker compose ps
docker compose logs -f --tail=100
```

Expected:

- `jb2026-backend` healthy (Compose healthcheck hits `/healthz`)
- `jb2026-frontend` up after backend is healthy (`depends_on` + `service_healthy`)

### 4.1 Smoke checks

```bash
# Frontend → SPA
curl -sSI http://127.0.0.1/app/ | head -n 20

# Frontend → API proxy
curl -sS http://127.0.0.1/api/v2/...   # or hit /healthz via proxy if you add a location;
                                       # by default /healthz is only on the backend port

# Direct backend (only if BACKEND_PORT is published)
curl -sS http://127.0.0.1:8080/healthz
curl -sS http://127.0.0.1:8080/health/live
curl -sS http://127.0.0.1:8080/health/ready
```

Open a browser: `http://<server>/app/`

### 4.2 Useful Compose commands

```bash
docker compose logs -f backend
docker compose logs -f frontend
docker compose restart backend
docker compose down          # stop & remove containers (volumes kept if any)
docker compose down -v       # also remove named volumes (destroys mssql-data if used)
```

---

## Section 5 — TLS (recommended for production)

Compose as shipped serves **HTTP**. Put TLS in front.

### Option A — Caddy on the host (simple)

Install Caddy, proxy to the published frontend port:

```caddy
app.example.com {
    reverse_proxy 127.0.0.1:80
}
```

Set `FRONTEND_PORT=80` but bind only localhost in compose if Caddy is the public listener
(change ports to `"127.0.0.1:80:80"`), and open **443** on the firewall.

### Option B — Nginx on the host

Terminate TLS and `proxy_pass` to `http://127.0.0.1:80`. Forward
`Host`, `X-Forwarded-For`, `X-Forwarded-Proto`.

### Option C — Traefik / Caddy as a Compose service

Add a reverse-proxy service on the `jb2026` network; stop publishing host
port 80 on `frontend` and let the proxy attach via labels. (Not bundled —
add when you need automated certs inside Compose.)

After TLS is live, set:

```bash
CORS_ALLOWED_ORIGINS=https://app.example.com
```

and redeploy (`docker compose up -d`).

---

## Section 6 — Updates and rollback

### 6.1 Rolling forward

On the build machine:

```bash
./build_push_cmd.sh youruser v1.0.1
```

On the server:

```bash
cd /opt/jb2026
# edit .env → IMAGE_TAG=v1.0.1
docker compose pull
docker compose up -d
docker compose ps
```

Compose recreates containers when the image digest/tag changes.

### 6.2 Rollback

Point `IMAGE_TAG` back to the last known-good tag and re-pull/up:

```bash
# .env
IMAGE_TAG=v1.0.0
docker compose pull
docker compose up -d
```

Keep **immutable tags** (`v1.0.0`, git SHA). Do not rely on `:latest` for rollback.

### 6.3 Zero-downtime note

Single-replica Compose restarts briefly drop connections. For true zero-downtime
you need a load balancer + multiple replicas (or blue/green). This stack is
intentionally simple.

---

## Section 7 — Backend / frontend wiring details

### `BACKEND_URL` (frontend)

Frontend entrypoint substitutes `BACKEND_URL` into Nginx at container start
(default `http://backend:8080`).

- **Same Compose stack:** leave unset; service name `backend` resolves on network `jb2026`.
- **Split hosts:** set explicitly, e.g. `BACKEND_URL=http://10.0.0.5:8080` or an internal DNS name.
  The URL must be reachable **from the frontend container**, not from the browser.

To set it in Compose, add under `frontend.environment`:

```yaml
- BACKEND_URL=http://backend:8080
```

### CORS

Nginx same-origin `/api/` means the browser origin is the frontend host.
Set `CORS_ALLOWED_ORIGINS` to that origin (and any native/mobile origins).

### Health endpoints (backend)

| Path | Purpose |
| --- | --- |
| `/healthz` | Lightweight liveness used by image + Compose healthchecks |
| `/health/live` | ASP.NET health map (liveness-style) |
| `/health/ready` | Readiness (registered checks) |

---

## Section 7.5 — Storing uploaded attachments on a Windows Server share (SMB/CIFS)

The backend stores uploaded files (CloudDisk compatibility uploads, job/product
attachments) under the path set by `LegacyFiles:CloudDiskRoot` (and legacy SML
files under `LegacyFiles:SmlFileRoot`). The Docker image does **not** mount that
share by itself — you wire it up at runtime. Both config keys are mapped to
`/data/attachments` inside the backend container by the stock compose file.

### Option A — mount on the host, bind into the container (recommended)

On the target server, mount the Windows share once (persists across reboots via
`/etc/fstab`):

```bash
sudo apt-get install -y cifs-utils
sudo mkdir -p /mnt/jb2026-attachments
# test mount:
sudo mount -t cifs //winserver/share /mnt/jb2026-attachments \
  -o username=svc_jb,password=SECRET,vers=3.0,uid=1000,gid=1000
```

Add to `/etc/fstab` so it survives reboot:

```fstab
//winserver/share  /mnt/jb2026-attachments  cifs  username=svc_jb,password=SECRET,vers=3.0,uid=1000,gid=1000,_netdev  0  0
```

Then point compose at it in `.env`:

```bash
ATTACHMENTS_HOST_PATH=/mnt/jb2026-attachments
```

The `backend` service already bind-mounts that path to `/data/attachments` and
sets `LegacyFiles__CloudDiskRoot` / `LegacyFiles__SmlFileRoot` to it.

### Option B — let Compose mount the SMB share directly

Uncomment the `jb2026-attachments` volume definition and the
`- jb2026-attachments:/data/attachments` line under `backend.volumes` in
`docker-compose.yml`, then set in `.env`:

```bash
SMB_SHARE=//winserver/share
SMB_USER=svc_jb
SMB_PASSWORD=SECRET
```

> **Permissions:** the share must be writable by the container's user. The
> `uid=1000,gid=1000` in the mount options above usually suffices; adjust to
> match the image's runtime user if uploads fail with permission errors.

### Verify

After `docker compose up -d`, confirm the backend sees the share:

```bash
docker compose exec backend ls -la /data/attachments
docker compose exec backend printenv | grep LegacyFiles   # careful: no secrets here
```

If `LegacyFiles:CloudDiskRoot` is empty, the CloudDisk upload endpoints return
a "Set configuration key…" error — so this mount is required for uploads to work.

---

## Section 8 — Optional SQL Server in Compose

Only for **dev/lab**. Production: external SQL + remove/disable the service.

1. Uncomment the `sql-server` service block in `docker-compose.yml`.
2. Set in `.env`:

   ```bash
   SQL_SA_PASSWORD='YourStrong!Passw0rd'
   DB_CONNECTION_STRING=Server=sql-server;Database=JB2026;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
   ```

3. Prefer `depends_on` with `condition: service_healthy` once the SQL healthcheck works.
4. Data persists in the `mssql-data` volume. `docker compose down -v` **wipes it**.

SQL Server images are large and need enough RAM (plan ≥ 2 GB for the SQL container alone).

---

## Section 9 — Troubleshooting

| Symptom | Check |
| --- | --- |
| Compose exits: `DB_CONNECTION_STRING` / `JWT_KEY` | `.env` present next to compose; vars non-empty |
| Backend crash loop | `docker compose logs backend` — bad connection string, SQL unreachable, auth failure |
| Frontend up, API 502 | Backend not healthy; `BACKEND_URL` wrong; backend not on same network |
| CORS errors in browser | `CORS_ALLOWED_ORIGINS` must include exact browser origin (`https://...` not `http://...`) |
| `host.docker.internal` fails on Linux | Add `extra_hosts: ["host.docker.internal:host-gateway"]` |
| Pull denied | `docker login`; image name/tag; private repo permissions |
| Old code after deploy | Confirm `IMAGE_TAG`; `docker compose images`; force `pull` then `up -d --force-recreate` |
| Permission / bind port | Port 80 needs root or cap; or use `FRONTEND_PORT=8088` |

Debug shell:

```bash
docker compose exec backend curl -sS http://127.0.0.1:8080/healthz
docker compose exec frontend wget -qO- http://backend:8080/healthz
docker compose exec backend printenv | grep -E 'ConnectionStrings|Jwt|Cors'   # careful: secrets in output
```

---

## Section 10 — Hardening checklist (production)

Already baked into the stock files (look for `HARDENING:` comments):

- Backend port bound to `127.0.0.1` only; public entry is frontend `/api/` (+ `/healthz` proxy)
- Compose log rotation (`10m` × 3) and `mem_limit` on services
- `extra_hosts: host.docker.internal:host-gateway` on backend (Linux → host Ollama/etc.)
- Frontend Compose + image `HEALTHCHECK` on `/app/`
- Nginx: gzip, security headers, asset cache, HTML `no-cache`, `/healthz` proxy
- Swagger/OpenAPI disabled in Production unless `SWAGGER_ENABLED=true` (`Swagger__Enabled`)

Still your ops responsibility:

1. **Pin image tags** — never deploy `:latest` to prod (`IMAGE_TAG=vX.Y.Z`).
2. **TLS** in front of the frontend (Section 5); optionally bind frontend to `127.0.0.1` when Caddy/Nginx terminates TLS on the host.
3. **`chmod 600 .env`** and restrict who can read `/opt/jb2026`.
4. **Set `CORS_ALLOWED_ORIGINS`** to your real `https://…` origin(s).
5. **Registry** — prefer GHCR/private registry + pull tokens for proprietary builds.
6. **Backups** — SQL backups are your recovery story; containers are disposable.
7. **Updates** — patch host OS + refresh base images periodically; rebuild app images on a cadence.
8. **CSP** — not set in Nginx yet; add when you have a concrete policy.

---

## Section 11 — Relation to `ops/deploy`

| | `ops/deploy` | `ops/docker` (this guide) |
| --- | --- | --- |
| Runtime | systemd + host Nginx | Docker Compose |
| Artifacts | published binaries / SPA files | container images |
| Secrets | `/etc/jb2026/env` | `.env` next to compose |
| Best for | single VM, classic ops | identical runtimes, easy rollback by tag |

Do not run both stacks on the same ports on one host without coordinating
listeners.

---

## Quick reference

```bash
# Build/push
./build_push_cmd.sh youruser v1.0.0

# Server
cp .env.example .env && chmod 600 .env   # fill values
docker compose pull && docker compose up -d
docker compose ps
curl -sS http://127.0.0.1:8080/healthz
curl -sSI http://127.0.0.1/app/
```
