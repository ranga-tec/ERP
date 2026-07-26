#!/usr/bin/env bash
set -euo pipefail

project_root="/opt/neuedge"
compose_file="$project_root/deploy/docker-compose.vps.yml"
env_file="$project_root/deploy/.env"
backup_root="/opt/neuedge-backups"
timestamp="$(date +%Y%m%d-%H%M%S)"

if [[ ! -f "$env_file" ]]; then
  echo "Missing env file: $env_file" >&2
  exit 1
fi

set -a
source "$env_file"
set +a

mkdir -p "$backup_root"

db_backup="$backup_root/neuedge-db-$timestamp.dump"
app_data_backup="$backup_root/neuedge-app-data-$timestamp.tar.gz"

docker compose --env-file "$env_file" -f "$compose_file" exec -T db \
  pg_dump -U "$POSTGRES_USER" -d "${POSTGRES_DB:-neuedge}" -Fc > "$db_backup"

docker run --rm \
  -v neuedge_api_app_data:/source:ro \
  -v "$backup_root:/backup" \
  alpine:3.22 \
  sh -c "tar -czf /backup/$(basename "$app_data_backup") -C /source ."

find "$backup_root" -type f -name 'neuedge-db-*.dump' -mtime +7 -delete
find "$backup_root" -type f -name 'neuedge-app-data-*.tar.gz' -mtime +7 -delete

echo "Backups written to $backup_root"
