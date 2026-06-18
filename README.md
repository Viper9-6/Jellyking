# Jellyking ♔

A zero-config media stack dashboard that replaces the Organizr + Caddy/nginx setup with a single container.

Jellyking proxies your native service WebUIs (Jellyfin, qBittorrent, Sonarr, Radarr, …) through one origin using YARP — no iframes — and presents them in a clean tabbed UI. Services are added at runtime from the admin UI (any host + port + base path), and optional per-service auto-login injects stored credentials so each WebUI opens already signed in.

## Quick Start (Docker)

The image isn't published, so build it from this repo:

```bash
git clone <your-repo-url> Jellyking && cd Jellyking
docker compose up -d --build
```

Open **http://localhost:5656/** — on first launch you're prompted to **create the admin account**. Then use **Add Service** to point Jellyking at each running service.

`docker-compose.yml` uses host networking (services discovered on `localhost`). If your *arr stack is itself a Docker Compose stack, use `docker-compose.bridge.yml` instead (build first with `docker build -t jellyking .`, then reach services by their compose service name).

### What gets persisted

`docker-compose.yml` mounts `./jellyking-data:/data`. That directory holds your accounts, configured services, settings, **encrypted** service credentials, the DataProtection keys, and the self-signed TLS cert — back it up; don't delete it.

## First-run setup

1. Open `http://<host>:5656/` → create the admin username + password.
2. **Add Service** (admin): pick a template or Blank, set Host, Port, Base Path (the subpath Jellyking serves it on, e.g. `/sonarr`), Health Path, and (optional) auto-login credentials.
3. Each service must be told it's served from that subpath (one-time), then restarted:

| Service     | Setting location                                | Value          |
|-------------|-------------------------------------------------|----------------|
| Jellyfin    | Dashboard → Networking → Base URL               | `/jellyfin`    |
| Sonarr      | Settings → General → URL Base                   | `/sonarr`      |
| Radarr      | Settings → General → URL Base                   | `/radarr`      |
| Prowlarr    | Settings → General → URL Base                   | `/prowlarr`    |
| Lidarr      | Settings → General → URL Base                   | `/lidarr`      |
| Readarr     | Settings → General → URL Base                   | `/readarr`     |
| Jellyseerr  | Settings → General → URL Base                   | `/jellyseerr`  |
| Bazarr      | Settings → General → Base URL                   | `/bazarr`      |
| qBittorrent | Tools → Options → Web UI → `WEBUI_ROOTPATH`     | `/qbit`        |
| SABnzbd     | Config → General → URL Base                     | `/sabnzbd`     |

You can run **multiple instances** (e.g. two Radarrs) by adding each with a unique slug, port, and base path.

## Auto-login (credential manager)

When adding or editing a service, choose an authentication type so the WebUI loads already signed in (secrets are encrypted at rest with ASP.NET Core Data Protection):

- **API Key** (`X-Api-Key`) — Sonarr, Radarr, Prowlarr, Lidarr, Readarr, Bazarr, Jellyseerr, SABnzbd.
- **Jellyfin Token** (`X-Emby-Token`) — authenticates Jellyfin API calls.
- **qBittorrent (username + password)** — Jellyking performs a server-side login, caches the `SID` cookie, and injects it on every proxied request.

Notes:
- qBittorrent behind a reverse proxy also needs **WebUI → Host header validation** disabled (or the proxy origin allowed) in qBittorrent's settings, otherwise POSTs return 403.
- Full Jellyfin WebUI single sign-on needs the Jellyfin SSO plugin (trusted header); the stored token alone authenticates API calls but the WebUI login screen is client-gated.

## Accounts & security

- First-run admin setup, login, logout, self-service password change, and admin user management (create / change role / reset password / delete) are all in **Settings → Users**.
- **Local access (no login)** toggle in Settings → General: when on, loopback requests are treated as an admin without a password. Great for a single-user machine; keep it off if the port is reachable beyond the host.

## TLS / HTTPS (opt-in)

Plain HTTP by default. To enable HTTPS (auto-generates a self-signed cert into `data/`):

```bash
JELLYKING__Security__UseHttps=true   # env var, or Jellyking:Security:UseHttps in appsettings.json
```

HTTP on `5656` then 307-redirects to HTTPS on `5657`, with HSTS and a `Secure` auth cookie. For a real domain, put Jellyking behind Caddy/Traefik/nginx with a real certificate instead.

## Configuration

Via `appsettings.json` or `JELLYKING__Section__Key` env vars (double-underscore separates sections):

```bash
JELLYKING__Server__Port=5656
JELLYKING__Detection__IntervalSeconds=30
JELLYKING__Security__UseHttps=true
DataDirectory=/data          # top-level key (not under Jellyking:)
```

## Development

Requires .NET 10 SDK and Node.js 20+.

```bash
# Backend
cd src/Jellyking.Host && dotnet run

# Frontend (dev server on :3000 with HMR, proxies API + service paths to :5656)
cd frontend && npm install && npm run dev

# Production build: frontend into wwwroot, then publish
cd frontend && npm run build && cd ..
dotnet publish src/Jellyking.Host -c Release -o publish
./publish/Jellyking
```

## Stack

- **Backend:** ASP.NET Core 10 (`src/Jellyking.Host` + `src/Jellyking.Core`)
- **Reverse proxy:** YARP 2.3
- **Auth:** cookie auth with Admin/User policies + optional localhost bypass
- **Frontend:** React + Vite + TypeScript (`frontend/`)
- **Storage:** JSON files under `data/` (users, services, settings, encrypted credentials)
- **Logging:** Serilog to console and `logs/jellyking-*.log`
