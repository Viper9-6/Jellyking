# Jellyking ♔

A single-container media-stack dashboard that proxies your native service WebUIs (Jellyfin, qBittorrent, Sonarr, Radarr, Prowlarr, Lidarr, Readarr, Bazarr, Jellyseerr, SABnzbd) through one origin using [YARP](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/) — **no iframes**, no per-service reverse-proxy config.

- **Add services at runtime** from the admin UI — any host + port + base path, multiple instances allowed (e.g. two Radarrs on different ports).
- **Auto-login / credential manager** — store an API key or qBittorrent login so each WebUI opens already signed in (secrets encrypted at rest).
- **Accounts & users** — first-run admin setup, login, self-service password change, admin user management.
- **Local access bypass** (optional) and **opt-in HTTPS**.
- Expose it through **Cloudflare Tunnels** (or any reverse proxy) — the port never has to be public.

## 1. Install on a Linux server (Native, no Docker)

Two ways: **self-contained** (no .NET runtime needed on the server — recommended) or **build on the server**.

### Option A — Self-contained build (server needs nothing but Linux)

Build on any machine with the .NET 10 SDK + Node 20 (e.g. your dev machine), then copy a single folder to the server.

```bash
# 1. Build the frontend into wwwroot
cd frontend && npm install && npm run build && cd ..

# 2. Publish self-contained for the server's architecture.
#    Use linux-arm64 for Raspberry Pi / ARM servers.
dotnet publish src/Jellyking.Host -c Release -r linux-x64 --self-contained true -o publish-linux

# 3. Copy to the server
rsync -avz publish-linux/  user@server:/opt/jellyking/
scp deploy/jellyking.service user@server:/tmp/jellyking.service
```

On the server:
```bash
sudo useradd --system --home /opt/jellyking --shell /usr/sbin/nologin jellyking
sudo mkdir -p /var/lib/jellyking /opt/jellyking/logs
sudo chown -R jellyking:jellyking /var/lib/jellyking /opt/jellyking
sudo install -m644 /tmp/jellyking.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now jellyking
sudo systemctl status jellyking        # confirm "active (running)"
# open http://<server-ip>:5656/ → create admin → Add Service
```

Back up `/var/lib/jellyking` — that's your accounts, services, settings, encrypted credentials, DataProtection keys, and (if you enable TLS) the self-signed cert.

### Option B — Build directly on the server

```bash
# .NET 10 SDK + Node 20
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0
echo 'export PATH=$HOME/.dotnet:$PATH' >> ~/.bashrc && source ~/.bashrc
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo bash - && sudo apt-get install -y nodejs   # Debian/Ubuntu

git clone https://github.com/Viper9-6/Jellyking.git && cd Jellyking
cd frontend && npm install && npm run build && cd ..
dotnet publish src/Jellyking.Host -c Release -o /opt/jellyking
# then create the user + install the systemd unit as in Option A, with
# ExecStart=/opt/jellyking/Jellyking and the .NET runtime on PATH
```

For Option B the build is **framework-dependent**, so the server needs the ASP.NET Core 10 runtime present (the SDK install above provides it). Option A bundles the runtime, so the server needs nothing.

> Run it behind **Cloudflare Tunnel** (or any reverse proxy) instead of opening port 5656 — see §5. Keep `JELLYKING__Security__UseHttps=false` when behind a tunnel, and keep "Local access (no login)" OFF (see the warning in §5 — the tunnel connects from localhost).

---

## 2. Install on a Linux server (Docker)

```bash
# Docker + compose (Debian/Ubuntu)
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker "$USER"   # log out and back in afterwards

# Clone the private repo (authenticate with a token or SSH key)
git clone https://github.com/Viper9-6/Jellyking.git
cd Jellyking

# Build and run (host networking — services discovered on localhost)
docker compose up -d --build
```

Open **http://<server-ip>:5656/** → you're prompted to **create the admin account**. That's it for the host.

`docker compose up -d --build` builds the image from `Dockerfile` and mounts `./jellyking-data:/data`. **Back up `./jellyking-data/`** — it holds your accounts, configured services, settings, encrypted credentials, DataProtection keys, and the TLS cert.

> The repo is private, so `git clone` needs credentials: a [Personal Access Token](https://github.com/settings/tokens) (`https://<token>@github.com/Viper9-6/Jellyking.git`) or an SSH remote. On the server you can also just copy the folder over.

### Bridge networking (if your *arr stack is itself a compose stack)

```bash
docker build -t jellyking .
cp docker-compose.bridge.yml docker-compose.yml
# edit service hosts to match your compose service names, then:
docker compose up -d
```
In bridge mode you add each service in the Jellyking UI using the **compose service name** (e.g. `sonarr`) as the host, on its container port.

## 3. Add a service

In the UI: **Dashboard → Add Service**. Pick a template (or Blank) and fill in:

| Field        | Example                | What it is                                                        |
|--------------|------------------------|-------------------------------------------------------------------|
| Slug         | `sonarr`               | Unique key; becomes the URL path and the tab id                   |
| Name         | `Sonarr`               | Display name                                                      |
| Host         | `localhost` / `sonarr` | Hostname/IP of the service (localhost on host net, compose name in bridge) |
| Port         | `8989`                 | The service's port                                                 |
| Base Path    | `/sonarr`              | The subpath Jellyking serves it on — **must match the service's own URL Base** |
| Health Path  | `/sonarr/api/v3/system/status` | HTTP GET path that returns 2xx when the service is up     |
| Auto-login   | see §4                 | Optional credential so the WebUI opens signed in                  |

Saved services are probed every ~30s; only **up** services are reachable through the proxy. Cards open the WebUI through Jellyking at `http://<host>:5656<base path>/`.

## 4. One-time per-service setup (base URL + credential)

Each *arr must be told it's served from its subpath, then restarted **once**:

| Service     | Where to set the base URL                       | Value          |
|-------------|--------------------------------------------------|----------------|
| Jellyfin    | Dashboard → Networking → Base URL              | `/jellyfin`    |
| Sonarr      | Settings → General → URL Base                   | `/sonarr`      |
| Radarr      | Settings → General → URL Base                   | `/radarr`      |
| Prowlarr    | Settings → General → URL Base                   | `/prowlarr`    |
| Lidarr      | Settings → General → URL Base                   | `/lidarr`      |
| Readarr     | Settings → General → URL Base                   | `/readarr`     |
| Jellyseerr  | Settings → General → URL Base                   | `/jellyseerr`  |
| Bazarr      | Settings → General → Base URL                   | `/bazarr`      |
| qBittorrent | Tools → Options → Web UI → `WEBUI_ROOTPATH`     | `/qbit`        |
| SABnzbd     | Config → General → URL Base                     | `/sabnzbd`     |

### Auto-login credential (set when adding/editing the service)

| Auth type in Jellyking | Services                                         | Where to find the secret                                   |
|------------------------|--------------------------------------------------|------------------------------------------------------------|
| **API Key**            | Sonarr, Radarr, Prowlarr, Lidarr, Readarr, Bazarr, Jellyseerr, SABnzbd | Settings → General → **API Key** (SABnzbd: Config → General → API Key) |
| **Jellyfin Token**     | Jellyfin                                         | Dashboard → **API Keys** → create a key                    |
| **qBittorrent**        | qBittorrent                                      | qBittorrent **username + password** (WebUI login)         |

- **API Key** is sent as the `X-Api-Key` header on every proxied request, so the *arr WebUI loads already authenticated.
- **qBittorrent** does a server-side login with your username + password, caches the `SID` cookie (~20 min), and injects it on proxied requests.
  - qBittorrent also requires **WebUI → Host header validation** to be **disabled** (or the proxy origin allowed) in its Options → WebUI, otherwise POSTs return `403` even with a valid session.
- **Jellyfin**: the stored token authenticates API calls, but the Jellyfin WebUI's own login screen is gated client-side. Full single sign-on needs the [Jellyfin SSO plugin](https://github.com/9p3/jellyfin-plugin-sso) (trusted header) — the token option is a fallback.

> Secrets are encrypted at rest with ASP.NET Core Data Protection and never returned by the API.

## 5. Expose via Cloudflare Tunnel (recommended)

The port never needs to be public — run `cloudflared` on the server and point it at Jellyking:

```bash
# install cloudflared, then (quick tunnel for testing):
cloudflared tunnel --url http://localhost:5656

# or a named tunnel you route to jellyking.<your-domain>:
cloudflared tunnel create jellyking
cloudflared tunnel route dns jellyking jellyking.example.com
# config.yml → ingress: hostname: jellyking.example.com  service: http://localhost:5656
cloudflared tunnel run jellyking
```

Cloudflare terminates TLS, so **leave `JELLYKING__Security__UseHttps=false`** when behind a tunnel.

> ⚠️ **Keep "Local access (no login)" OFF when using a tunnel.** That bypass treats any loopback connection as admin. `cloudflared` connects to `http://localhost:5656` from the server itself, so with the bypass on, every internet request through the tunnel would be treated as admin — bypassing your login. Leave it off and rely on the admin account (and optionally a Cloudflare Access policy in front).

## 6. Configuration

Via `appsettings.json` or `JELLYKING__Section__Key` env vars (double-underscore separates sections):

```bash
JELLYKING__Server__Port=5656
JELLYKING__Detection__IntervalSeconds=30
JELLYKING__Detection__TimeoutMs=2000
JELLYKING__Security__UseHttps=false        # opt-in HTTPS (self-signed) if not behind a tunnel
DataDirectory=/data                          # top-level key (not under Jellyking:)
```

The `Jellyking` config section: `Server` (Host, Port), `Detection` (TargetHost, IntervalSeconds, TimeoutMs), `Ui` (Theme, Title), `Security` (UseHttps, HttpPort, HttpsPort, CertPath, CertPassword, Hsts).

## 7. Accounts & security

- First run → create admin. **Settings → Users** (admin only): add users, change roles, reset a user's password, delete.
- Header menu → **Change password** for self-service password change.
- **Local access (no login)** toggle in Settings → General — see the Cloudflare warning above.

## 8. Development

Requires .NET 10 SDK and Node.js 20+.

```bash
# Backend (http://localhost:5656)
cd src/Jellyking.Host && dotnet run

# Frontend dev server (http://localhost:3000, HMR, proxies API + service paths to :5656)
cd frontend && npm install && npm run dev

# Production build
cd frontend && npm run build && cd ..
dotnet publish src/Jellyking.Host -c Release -o publish
./publish/Jellyking
```

## 9. Troubleshooting

- **Service shows offline / "port_closed"** — wrong host/port, or the service's base URL doesn't match Jellyking's Base Path. Fix the base URL on the service and restart it.
- **403 on qBittorrent actions** — disable Host header validation in qBittorrent WebUI options.
- **Lost admin / settings after `docker compose down`** — the `./jellyking-data:/data` mount is missing. Re-add it and restart.
- **"Cannot create service: slug exists"** — slugs must be unique; use distinct slugs/ports for multiple instances of the same app.

## Stack

ASP.NET Core 10 + YARP 2.3 · React + Vite + TypeScript · JSON storage under `data/` · Serilog · encrypted credentials via Data Protection.
