# 正式環境維運手冊

> 適用：Vultr 東京 VPS（Ubuntu LTS）＋ Docker Compose（`docker-compose.prod.yml`：web／db／caddy）。
> 程式位於主機的 `/opt/mantingeats`。**本 repo 為公開，主機 IP、網域、密碼一律不寫在這裡**（IP 見 Vultr 後台，密碼見主機上的 `.env`）。
> 營業時間 17:00–02:00；所有維護動作請挑**打烊時段（02:00 之後～17:00 之前）**。

連線到主機（Windows PowerShell 或 Mac 終端機）：

```sh
ssh root@主機IP
```

以下指令都在主機上執行，結束後輸入 `exit` 離開。

---

## 一、自動運作中的項目（不需手動，但要知道）

| 項目 | 運作方式 |
|---|---|
| 資料庫備份 | 每天 **08:00** 由 crontab 執行 `scripts/backup-db.sh`，檔案在 `backups/`，保留 7 天，紀錄寫入 `backups/backup.log` |
| 整台主機備份 | Vultr 自動備份（Auto Backups） |
| HTTPS 憑證 | Caddy 於到期前自動續約 |
| 服務自動重啟 | 三個服務皆為 `restart: unless-stopped`，程式當掉或主機重開機後自動恢復；web 會等 db 健康檢查通過才啟動 |

---

## 二、每週檢查（約 5 分鐘）

```sh
cd /opt/mantingeats && docker compose -f docker-compose.prod.yml ps && ls -lht backups/*.sql | head -3 && df -h / && free -h
```

| # | 看什麼 | ✅ 正常 | ⚠️ 異常時 |
|---|---|---|---|
| 1 | 服務狀態（`STATUS` 欄） | web、db、caddy 都是 `Up`，db 為 `(healthy)` | 缺少某個服務、`Restarting`／`Exited`／`(unhealthy)` → 見「五、出問題時」 |
| 2 | 備份檔 | 最新一筆是今天（或最近一天）08:00，大小不是 0 | 最新一筆是好幾天前或大小為 0 → 查看 `tail -20 backups/backup.log` |
| 3 | 硬碟（`Use%`） | 低於 80% | 執行 `docker image prune -f` 清除舊映像檔 |
| 4 | 記憶體（`available`） | 還有數百 MB；`free` 很少是正常的（被拿去當快取） | `available` 低於 100Mi 或 Swap 使用超過 1Gi → 回報檢查 |

---

## 三、每月維護（打烊時段）

1. **系統安全更新**

   ```sh
   apt update && apt upgrade -y && [ -f /var/run/reboot-required ] && echo "需要重開機" || echo "不用重開機"
   ```

   顯示「需要重開機」時執行 `reboot`，約 1 分鐘後網站自動恢復，再做一次「二、每週檢查」確認。

2. **清除舊 Docker 映像檔**（每次更新程式都會留下舊版）

   ```sh
   docker image prune -f
   ```

3. **查看 Vultr 帳單**：確認信用卡扣款正常（促銷額度 2026-11-06 到期後開始實際扣款，約 $12／月）。

---

## 四、更新程式

1. 挑打烊時段。
2. 先手動備份：

   ```sh
   cd /opt/mantingeats && sh scripts/backup-db.sh
   ```

3. 拉取新版並重建（約中斷 10～60 秒；資料存在 Docker volume，不受影響；資料庫 Migration 啟動時自動套用）：

   ```sh
   git pull && docker compose -f docker-compose.prod.yml up -d --build
   ```

4. 確認：`docker compose -f docker-compose.prod.yml ps` 三個服務都是 `Up`，再用瀏覽器登入看看。

---

## 五、出問題時

### 網站打不開

```sh
cd /opt/mantingeats && docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs web --tail 50
```

常見處理：`docker compose -f docker-compose.prod.yml restart web`。仍無法恢復時，把上面兩個指令的輸出交給開發者判斷（輸出不含密碼）。

### 需要從備份還原正式資料庫

> ⚠️ 會**覆蓋**目前正式資料庫的內容，執行前務必確認要還原到哪一份備份。尚未在正式環境實際執行過，先完成「六、還原演練」。

```sh
cd /opt/mantingeats
sh scripts/backup-db.sh                       # 先備份目前狀態，還原錯了還能回來
ls -lht backups/*.sql | head                  # 選定要還原的備份檔
docker compose -f docker-compose.prod.yml stop web
set -a && . ./.env && set +a
docker compose -f docker-compose.prod.yml exec -T db mysql -uroot -p"$DB_ROOT_PASSWORD" ManTingEatsDb < backups/要還原的檔名.sql
docker compose -f docker-compose.prod.yml start web
unset DB_ROOT_PASSWORD DB_APP_PASSWORD SEED_ADMIN_PASSWORD
```

備份檔由 `mysqldump` 產生，內含 `DROP TABLE IF EXISTS`，匯入時會整批取代各資料表。

### 整台主機壞掉

到 Vultr 後台 → 主機 → **Backups**，選一份自動備份還原整台主機。還原後資料會回到那份快照的時間點，之後的訂單需人工補登。

---

## 六、還原演練（建議每季一次）

在主機上另開臨時 MySQL 還原最新備份，與正式資料庫比對各資料表筆數。**不碰正式資料庫、資料不離開主機、只比對筆數不顯示內容。** 臨時 MySQL 會多用記憶體，請在打烊時段做。

```sh
# ① 先做一份最新備份
cd /opt/mantingeats && sh scripts/backup-db.sh && LATEST=$(ls -t backups/*.sql | head -1) && ls -lh "$LATEST"

# ② 開臨時 MySQL，等待就緒（約 10～30 秒）
docker run -d --name restore-test -e MYSQL_ROOT_PASSWORD=restoretest -e MYSQL_DATABASE=ManTingEatsDb mysql:8.0 && until docker logs restore-test 2>&1 | grep -q "ready for connections.*port: 3306"; do sleep 2; done && echo "臨時 MySQL 已就緒"

# ③ 還原備份
docker exec -i restore-test mysql -uroot -prestoretest ManTingEatsDb < "$LATEST" && echo "還原完成"

# ④ 比對筆數（左：正式，右：還原）
set -a && . ./.env && set +a
Q="SELECT 'Orders',COUNT(*) FROM Orders UNION ALL SELECT 'OrderItems',COUNT(*) FROM OrderItems UNION ALL SELECT 'OrderItemAddOns',COUNT(*) FROM OrderItemAddOns UNION ALL SELECT 'MenuItems',COUNT(*) FROM MenuItems UNION ALL SELECT 'AddOns',COUNT(*) FROM AddOns UNION ALL SELECT 'Employees',COUNT(*) FROM Employees UNION ALL SELECT 'Reservations',COUNT(*) FROM Reservations UNION ALL SELECT 'Expenses',COUNT(*) FROM Expenses UNION ALL SELECT 'Migrations',COUNT(*) FROM __EFMigrationsHistory"
docker compose -f docker-compose.prod.yml exec -T db mysql -uroot -p"$DB_ROOT_PASSWORD" ManTingEatsDb -N -e "$Q" 2>/dev/null > /tmp/a.txt
docker exec restore-test mysql -uroot -prestoretest ManTingEatsDb -N -e "$Q" 2>/dev/null > /tmp/b.txt
paste /tmp/a.txt /tmp/b.txt && diff -q /tmp/a.txt /tmp/b.txt && echo "✅ 一致"

# ⑤ 收拾
docker rm -fv restore-test && rm -f /tmp/a.txt /tmp/b.txt && unset DB_ROOT_PASSWORD DB_APP_PASSWORD SEED_ADMIN_PASSWORD
```

每列左右筆數相同並顯示「✅ 一致」即通過。演練期間若有人使用系統，筆數差幾筆屬正常。

---

## 七、一次性待辦

- [ ] 第一次還原演練（見「六」）
- [ ] 網站斷線通知：設定免費監測服務（例如 UptimeRobot），每 5 分鐘檢查網站，連不上時寄信通知
- [ ] SSH 改為只允許金鑰登入：兩台電腦都設好金鑰後，關閉密碼登入
- [ ] Vultr 帳號開啟雙因素認證（Cloudflare 帳號同樣要開）
- [ ] 異地備份：每日備份檔另存一份到 Vultr 以外的地方
- [ ] 隱私權政策聯絡電話、信箱（`Views/Home/Privacy.cshtml` 頂端常數）

## 八、重要日期與帳號

| 項目 | 說明 |
|---|---|
| Vultr 促銷額度 | **2026-11-06 到期**，之後由綁定的信用卡扣款；信用卡過期或換卡時記得到 Vultr 更新 |
| 網域（Cloudflare） | 每年續約，確認 Cloudflare 已開啟自動續約 |
| HTTPS 憑證 | Caddy 自動續約，不需處理 |
