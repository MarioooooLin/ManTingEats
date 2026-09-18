#!/bin/sh
# 備份 ManTingEats 正式環境資料庫，建議透過 VPS 上的 crontab 排程執行（例如每日凌晨）
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKUP_DIR="$PROJECT_DIR/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# shellcheck source=/dev/null
set -a
. "$PROJECT_DIR/.env"
set +a

mkdir -p "$BACKUP_DIR"

docker compose -f "$PROJECT_DIR/docker-compose.prod.yml" exec -T db \
  mysqldump -u root -p"$DB_ROOT_PASSWORD" ManTingEatsDb > "$BACKUP_DIR/mantingeats_${TIMESTAMP}.sql"

# 僅保留最近 7 天的備份
find "$BACKUP_DIR" -name "*.sql" -mtime +7 -delete
