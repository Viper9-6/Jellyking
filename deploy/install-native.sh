#!/usr/bin/env bash
# install-native.sh — Sonarr-style installer for Jellyking on Linux.
#
# Downloads the latest (or a pinned) prebuilt self-contained release from
# GitHub, extracts it to /opt/jellyking, creates a dedicated system user,
# installs the jellyking.service systemd unit, and starts it.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/Viper9-6/Jellyking/main/deploy/install-native.sh | sudo bash
#   sudo ./install-native.sh             # latest release
#   sudo ./install-native.sh v0.1.0      # a specific tag
#
# The repo is public, so no auth is required. If a plain download fails
# (corporate proxy, GitHub rate limiting, etc.), set GH_TOKEN=<pat> and the
# script will use the authenticated GitHub API instead.
#
set -euo pipefail

REPO="Viper9-6/Jellyking"          # owner/repo (override with JELLYKING_REPO)
REPO="${JELLYKING_REPO:-$REPO}"
INSTALL_DIR="/opt/jellyking"
DATA_DIR="/var/lib/jellyking"
SERVICE_USER="jellyking"
VERSION="${1:-latest}"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run this as root:  sudo $0 $*" >&2
  exit 1
fi

# --- detect architecture → release asset ----------------------------------- #
case "$(uname -m)" in
  x86_64|amd64)        ASSET="jellyking-linux-x64.tar.gz" ;;
  aarch64|arm64)      ASSET="jellyking-linux-arm64.tar.gz" ;;
  *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
esac
echo "Detected arch: $(uname -m)  →  $ASSET"

# --- download the asset ------------------------------------------------------ #
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

download_anonymous() {
  local url
  if [[ "$VERSION" == "latest" ]]; then
    # /releases/latest/download/<asset> redirects to the latest release asset.
    url="https://github.com/$REPO/releases/latest/download/$ASSET"
  else
    url="https://github.com/$REPO/releases/download/$VERSION/$ASSET"
  fi
  echo "Downloading $ASSET ($VERSION) — $url"
  curl -fsSL -o "$TMP/$ASSET" "$url"
}

download_with_token() {
  local token="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
  [[ -z "$token" ]] && return 1
  echo "Downloading $ASSET ($VERSION) via authenticated GitHub API…"
  local api_path
  api_path="$( [[ "$VERSION" == "latest" ]] && echo "latest" || echo "tags/$VERSION" )"
  local api="https://api.github.com/repos/$REPO/releases/$api_path"
  local url
  url="$(curl -fsSL -H "Authorization: Bearer $token" -H "Accept: application/vnd.github+json" "$api" \
         | grep -oE "https://[^\"[:space:]]*${ASSET}" | head -n1)"
  [[ -z "$url" ]] && { echo "Could not find $ASSET in release $VERSION." >&2; return 1; }
  curl -fsSL -H "Authorization: Bearer $token" -o "$TMP/$ASSET" "$url"
}

if ! download_anonymous && ! download_with_token; then
  cat >&2 <<'MSG'
Download failed. The repo is public, so this is usually a proxy/firewall
or transient GitHub issue. Re-run, or set a PAT and retry:
  export GH_TOKEN=ghp_xxxxxxxxxxxxxx
  sudo GH_TOKEN="$GH_TOKEN" bash install-native.sh
MSG
  exit 1
fi

if [[ ! -s "$TMP/$ASSET" ]]; then
  echo "Downloaded file is empty — aborting." >&2
  exit 1
fi
echo "Downloaded: $TMP/$ASSET ($(du -h "$TMP/$ASSET" | cut -f1))"

# --- stop the running service (upgrade) ------------------------------------- #
if systemctl is-active --quiet jellyking 2>/dev/null; then
  echo "Stopping running jellyking service…"
  systemctl stop jellyking
fi

# --- create the system user ------------------------------------------------- #
if ! id "$SERVICE_USER" >/dev/null 2>&1; then
  echo "Creating system user '$SERVICE_USER'…"
  useradd --system --home "$INSTALL_DIR" --shell /usr/sbin/nologin "$SERVICE_USER"
fi

# --- install files ---------------------------------------------------------- #
mkdir -p "$INSTALL_DIR" "$DATA_DIR" "$INSTALL_DIR/logs"
# Back up any previous install before replacing it.
if [[ -n "$(ls -A "$INSTALL_DIR" 2>/dev/null)" ]]; then
  BACKUP="${INSTALL_DIR}.bak.$(date +%Y%m%d%H%M%S)"
  echo "Backing up existing $INSTALL_DIR → $BACKUP"
  mv "$INSTALL_DIR" "$BACKUP"
  mkdir -p "$INSTALL_DIR"
fi

echo "Extracting to $INSTALL_DIR …"
tar -xzf "$TMP/$ASSET" -C "$INSTALL_DIR" --strip-components=1
# The tarball ships jellyking.service inside the app dir; install it.
if [[ -f "$INSTALL_DIR/jellyking.service" ]]; then
  install -m644 "$INSTALL_DIR/jellyking.service" /etc/systemd/system/jellyking.service
fi

chown -R "$SERVICE_USER":"$SERVICE_USER" "$INSTALL_DIR" "$DATA_DIR"

# --- enable + start ---------------------------------------------------------- #
systemctl daemon-reload
systemctl enable --now jellyking

sleep 2
if systemctl is-active --quiet jellyking; then
  echo
  echo "✓ Jellyking installed and running."
  echo "  Service:   systemctl status jellyking"
  echo "  Logs:      journalctl -u jellyking -f   (or $INSTALL_DIR/logs/)"
  echo "  Data:      $DATA_DIR   ← back this up"
  echo "  Open:      http://$(hostname -I 2>/dev/null | awk '{print $1}' || echo localhost):5656/"
else
  echo "✗ Service did not start. Check:  journalctl -u jellyking -n 50" >&2
  exit 1
fi
