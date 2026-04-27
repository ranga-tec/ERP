#!/usr/bin/env bash
set -euo pipefail

project_root="/opt/iss"
compose_file="$project_root/deploy/docker-compose.vps.yml"
env_file="$project_root/deploy/.env"
backup_root="/opt/iss-backups"
timestamp="$(date +%Y%m%d-%H%M%S)"

if [[ ! -f "$env_file" ]]; then
  echo "Missing env file: $env_file" >&2
  exit 1
fi

set -a
source "$env_file"
set +a

mkdir -p "$backup_root"

db_backup="$backup_root/iss-db-$timestamp.dump"
app_data_backup="$backup_root/iss-app-data-$timestamp.tar.gz"

docker compose --env-file "$env_file" -f "$compose_file" exec -T db \
  pg_dump -U "$POSTGRES_USER" -d "${POSTGRES_DB:-iss}" -Fc > "$db_backup"

docker run --rm \
  -v iss_api_app_data:/source:ro \
  -v "$backup_root:/backup" \
  alpine:3.22 \
  sh -c "tar -czf /backup/$(basename "$app_data_backup") -C /source ."

find "$backup_root" -type f -name 'iss-db-*.dump' -mtime +7 -delete
find "$backup_root" -type f -name 'iss-app-data-*.tar.gz' -mtime +7 -delete

echo "Backups written to $backup_root"
