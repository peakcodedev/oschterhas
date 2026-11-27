#!/bin/sh
# Runs on the server after the files were copied (see .github/workflows/deploy.yml).
# Needs the shared Caddy from peakcode_infra (network "proxy" and bin/caddy-site with install-global)
# and the image OSCHTERHAS_IMAGE, built by the workflow on this server.
set -eu
cd "$(dirname "$0")"

CADDY_SITE=${CADDY_SITE:-/srv/infra/bin/caddy-site}
: "${OSCHTERHAS_IMAGE:?Set OSCHTERHAS_IMAGE (built by the workflow)}"

[ -f .env ] || {
	echo "deploy: $(pwd)/.env is missing. Create it once from .env.example (docs/betrieb.md)." >&2
	exit 1
}

# Subnet of the shared proxy network: the API trusts forwarded headers and /internal calls from there.
PROXY_SUBNET=$(docker network inspect proxy --format '{{range .IPAM.Config}}{{.Subnet}} {{end}}' | cut -d' ' -f1)
[ -n "$PROXY_SUBNET" ] || { echo "deploy: Docker network 'proxy' not found; deploy peakcode_infra first." >&2; exit 1; }
export OSCHTERHAS_IMAGE PROXY_SUBNET

# Database first, then migrations with the schema owner, then the new version.
docker compose up -d --wait postgres
docker compose run --rm migrate
docker compose up -d --remove-orphans --wait

# Global "ask" before the site block: Caddy refuses on-demand TLS without it.
"$CADDY_SITE" install-global oschterhas caddy/oschterhas.global.caddy
"$CADDY_SITE" install oschterhas caddy/oschterhas.caddy

# Keep the current and the previous image (for a quick rollback), remove older ones.
docker image ls oschterhas-api --format '{{.CreatedAt}}\t{{.Repository}}:{{.Tag}}' | sort -r | tail -n +3 | cut -f2 |
	xargs -r docker image rm || true
docker image prune -f
