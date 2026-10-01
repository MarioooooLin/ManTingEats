#!/bin/sh
# 將開發用測試菜單（menu-seed.sql：MenuItems、AddOns）匯入本機 Docker 資料庫。
# 僅含菜單／加料測試資料，不含訂單、訂位、員工等個人資料（repo 為公開，勿將真實資料加入此檔）。
#
# 使用方式（Windows 請用 Git Bash 執行，PowerShell 的 < 重新導向會破壞中文編碼）：
#   1. docker compose up -d --build   （web 啟動時會自動 Migrate 建立資料表）
#   2. sh scripts/dev-data/import-menu-seed.sh
# 使用 INSERT IGNORE，重複執行不會產生重複資料。
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

docker compose -f "$PROJECT_DIR/docker-compose.yml" exec -T db \
  mysql -uroot -prootpassword --default-character-set=utf8mb4 ManTingEatsDb < "$SCRIPT_DIR/menu-seed.sql"

echo "已匯入測試菜單與加料。"
