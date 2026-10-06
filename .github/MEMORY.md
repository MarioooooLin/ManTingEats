# MEMORY — 決策與變更紀錄

> 每次完成可能影響架構、規範、API 或開發流程的變更後，依下列格式在本文件**最上方**新增一筆條目（最新在前）。格式規範見 [copilot-instructions.md](copilot-instructions.md) 第 7.3 節。

---

## 📍 目前進度與下一步（交接用，每次收工前更新）

> 最後更新：2026-10-07。開發者會在公司與家裡兩台電腦間切換，**對話紀錄不會同步**，接手時以本段為準。

### 出單列印改為 iPad + PassPRNT（2026-10-04 實機測試通過）

- **規格**：`docs/prd/v5.md`（2026-10-04 定案）。重點：58mm 紙（`size=2`）、結帳不補印且有待出單品項時擋下結帳、列印失敗一律人工重按「確認訂單」、舊 LAN 直連程式已移除。出單金額：品項印總價（單價 x 數量）、同一品項的加料合併一行印加料總價、皆不印單價（2026-10-05 修訂，原為不印單價／小計）。
- **硬體**：Star mC-Print3 **MCP31L BK TC**，USB 線接 iPad 的印表機 USB-A（2.4A）孔；印表機與 PassPRNT App 紙寬須設為 58mm。
- **實作（2026-10-04）**：確認訂單／補印 → `Views/Order/Print.cshtml` 開啟 PassPRNT → 回呼 `PrintResult`（GET）→ 成功時 `PrintSucceeded.cshtml` 自動 POST `MarkPrinted` 標記送印當下的品項。出單版面在 `Views/Order/_Ticket.cshtml`。
- **已實測**：中文正常、自動切紙、印完回到同一分頁；Safari 每次都會跳 PassPRNT 確認視窗（iOS 限制，無法關閉）。
- **實機測試（2026-10-04）**：使用者以 iPad + 實體印表機測試新流程，回報**測試通過**。
  - 伺服器日誌：訂單 #2 出單兩次回報 `0 SUCCESS`；`PrintResult` 需登入才會執行並記錄，證實從 PassPRNT 回到網站時**登入狀態仍在**（`SameSite=Strict` 不影響）。
  - 過程中曾出現 PassPRNT `E004 device connection error`（回呼代碼 `4`、`ERROR_GETPORT_FAILURE`）：PassPRNT 連不到印表機，處理印表機連線後即正常；系統依規格顯示出單失敗、品項維持待出單。
- **尚未特別驗證（非必要，有機會再測）**：
  1. 15 項以上的大單能否正常列印（出單 HTML 放在網址中，過長可能失敗）。
  2. 實際缺紙時的畫面（模擬回呼已驗證會顯示失敗）。
  - 「加入主畫面」模式已於 2026-10-05 實測結案，見下方「已結案：跳過 PassPRNT 確認視窗」。
- **已決議**：作廢訂單不列印（店長在原出單手動打叉），見 2026-09-30 紀錄。

### 下一步建議

- 出單模組與結帳找零已完成，接著處理下方「Review 待辦」中標記上線前的項目（隱私權政策聯絡資訊），以及租用 VPS／網域與正式部署。

### 正式環境主機與網域（2026-10-06 已上線，待實測）

- **VPS：Vultr 東京機房，Regular Cloud Compute 2GB**（1 vCPU / 2GB，約 $10／月）。
- **Vultr 費用**：新帳號促銷額度 $250，**2026-11-06 到期**（過期作廢），期間內費用由額度扣抵；之後改由使用者綁定的信用卡扣款，主機＋自動備份約 **$12／月**。帳務頁的 Current Balance 負數代表尚有額度。
- **網域：Cloudflare Registrar 註冊 `.com`**（成本價約 $10.46／年，續約不漲）。**2026-10-06 已註冊完成**；網域名稱不寫在公開 repo，部署時填入 VPS 上的 `.env`（`DOMAIN`）。DNS 由 Cloudflare 代管，A 記錄指向 VPS IP，**proxy 設為灰色雲（DNS only）**，讓 Caddy 直接向 Let's Encrypt 申請憑證。
- **部署完成（2026-10-06）**：
  - 主機：Ubuntu LTS、時區 Asia/Taipei、Docker 29.8.2／Compose v5.6.0；Vultr 防火牆群組開放 22／80／443；swap 為 Vultr 預建的 5.3GB（不需另加，`fallocate` 會因 swapfile 使用中而失敗）。實際可用記憶體約 1.6GB（系統保留一部分）。
  - 程式位於 `/opt/mantingeats`（`git clone`），`.env` 權限 600；兩組 DB 密碼以 `openssl rand -hex 16` 產生，**第一次啟動後不可再改**（MySQL 只在資料庫初始化時讀取）。網站帳密由使用者自行設定。
  - `docker compose -f docker-compose.prod.yml up -d --build` 後 web／db／caddy 皆 Up，Caddy 已取得 Let's Encrypt 憑證，網站可正常開啟。
  - 更新程式：`cd /opt/mantingeats && git pull && docker compose -f docker-compose.prod.yml up -d --build`（資料存於 Docker volume，Migration 啟動時自動套用）。更新前可先 `sh scripts/backup-db.sh` 手動備份；**挑打烊時段更新**（約中斷 10～60 秒）。
  - 2026-10-07 01:37（打烊後）更新至 `9e8e8f4`（帳號與權限管理 v7）：更新前手動備份 `mantingeats_20261007_013754.sql`；Migration `AddEmployeeActiveAndSecurityStamp` 套用成功，店長帳號為啟用且已補上戳記；web 無錯誤、重啟次數 0，HTTPS 登入頁 200，未登入進入帳號管理會導向登入。**店長已更換初始密碼**（以密碼雜湊與更新前備份比對確認不同；`.env` 中的初始密碼已失效，僅在資料庫無任何帳號時才會用到）。
  - 2026-10-07 01:12（打烊後）更新至 `4f0d91c`（MySQL 健康檢查）：更新前手動備份 `mantingeats_20261007_011226.sql`；db 以新設定重建並 Healthy，web 重建後無錯誤、重啟次數 0，HTTPS 登入頁 200。
  - SSH：Mac 的公鑰已於 2026-10-07 以 `ssh-copy-id` 加入 VPS 的 `root` 帳號，可用金鑰直接登入（另一台電腦的登入方式未記錄）。VPS 目前仍開放密碼登入。**主機 IP 不寫在公開 repo**（見 Vultr 後台）。
- **備份（2026-10-06 設定完成）**：每日 **08:00**（台灣時間；店家營業晚餐與消夜，早上無人使用）由 root crontab 執行 `sh /opt/mantingeats/scripts/backup-db.sh >> /opt/mantingeats/backups/backup.log 2>&1`，保留 7 天。腳本在 repo 中無執行權限，故以 `sh` 呼叫；改時區後需 `systemctl restart cron` 才會依台灣時間排程。另開啟 **Vultr 自動備份**（整台主機快照，建議排在 08:00 之後），作為主機外的一層備份；異地備份（低 7）可延後。
- **下一步**：登入測試 → iPad PassPRNT 出單實測（到店裡才能做；測試單作廢、測試品項下架）→ 輸入真實菜單（**勿執行 `import-menu-seed.sh`**）。
- 程式碼不需修改：網域、密碼皆透過 `.env` 設定；PassPRNT 回呼網址以 `location.origin` 組成，自動使用正式網域。部署步驟見 2026-10-06「正式環境主機與網域選定」紀錄。

### 結帳收款與找零（2026-10-04 完成，iPad 實測通過）

- 規格 `docs/prd/v6.md`。結帳改為跳出視窗，須「收剛好」或輸入收款金額（快速金額：下一個整百／$500／$1000 中大於應收者）才能結帳；收款與找零**只在畫面計算，不存資料庫**。
- 2026-10-04 使用者以 iPad 實測結帳視窗，回報測試通過。

### 已結案：跳過 PassPRNT 確認視窗（不可行）

- 2026-10-05 使用者在點餐系統暫時加上主畫面 App 的 meta 設定實測：「加入主畫面」後雖全螢幕，但下滑仍見網址、登入狀態與 Safari 共用，**出單仍會跳 PassPRNT 確認視窗**。
- 同日第二次實測（加上 `manifest.webmanifest`（`display: standalone`）、刪除舊圖示後重新加入主畫面）：**仍跳確認視窗**；且伺服器未收到任何 `PrintResult` 回呼，研判 PassPRNT 印完開回 Safari（未登入而被導到登入頁），主畫面模式反而會讓出單結果無法回寫。
- 第三次（同一個主畫面圖示、排除印表機 E004 連線問題後）：主畫面 App 中出單**確實不會跳確認視窗**，但 PassPRNT 印完**一律開回 Safari**（iOS 無法讓其他 App 跳回主畫面網頁 App），主畫面 App 停在「正在開啟 PassPRNT」，需手動切回；出單結果只寫入 Safari 的登入狀態。
- **決議（2026-10-05，使用者選 A）：維持在 Safari 使用、每張單多按一次「打開」**，自動回到訂單頁並記錄出單結果。不採用主畫面 App（每張單需手動切回 App，且需改寫為伺服器端記錄＋輪詢）。實驗用的 meta 與 manifest 皆已移除；iPad 上的主畫面圖示已刪除。
- 若日後仍要省略，只剩：Star CloudPRNT（印表機需上網、需額外硬體並重寫出單流程）或自製 iOS App（需 Apple 開發者帳號，成本高）。

### Review 待辦（2026-09-29 全專案 review 後尚未處理）

- 低 2：`DailyNumber` 與 `TotalAmount` 併發競態——單店機率低，先觀察。
- 低 7：備份腳本（root 密碼在指令列、未壓縮、未異地備份）——確定雲端主機／資料庫形式後再處理。
- 低 8：README 與正式部署清單——買主機時一起整理。
- 隱私權政策聯絡電話、信箱仍為佔位文字（`Views/Home/Privacy.cshtml` 頂端常數）。
- ~~帳號管理~~：2026-10-07 已完成（v7）並部署，店長已更換初始密碼，見下方紀錄。

### 環境備註

- 新電腦：`git clone` → `docker compose up -d --build` → `sh scripts/dev-data/import-menu-seed.sh`（匯入測試菜單）。開發帳號 admin / 123。
- 本機若只有舊版 .NET SDK（Mac 那台為 7.0），可用 Docker 執行建置與測試：`docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:9.0 dotnet test tests/ManTingEats.Tests`
- 開發用 `docker-compose.yml` 未保存 DataProtection 金鑰（正式環境 `docker-compose.prod.yml` 有掛 volume）：每次重建 web 容器，已登入的瀏覽器會被登出，舊頁面送出表單會出現 Anti-Forgery 驗證失敗（400），重新整理／重新登入即可。
- 2026-10-07 起 db 有健康檢查、web 會等 db 就緒才啟動，不再需要手動 `docker compose restart web`。
- 若本機已裝 MySQL 佔用 3306（Mac 那台即是），需先到「系統設定 → MySQL」停用，否則 db 容器無法啟動。
- GitHub repo 為**公開**，不得 commit 真實資料或機密。

---

### [2026-10-07] 結帳折扣（v8）

**變更內容**

- 新增 `docs/prd/v8.md`（已定案）。
- `Order` 新增 `DiscountAmount`、`DiscountReason`、`DiscountNote`、`DiscountedByEmployeeId`（Migration `AddOrderDiscount`），以及唯讀的 `AmountDue`（原價 − 折扣，不對應欄位）。`TotalAmount` 維持原價。
- 新增 `Models/Enums/DiscountReason.cs`：熟客、抹零、招待、餐點問題、其他。
- 新增 `Services/OrderDiscount.cs`：
  - 抹零：去掉個位數。
  - 打折：1～9 代表幾折、10～99 代表幾幾折，四捨五入。
  - 折扣檢查：整數、0 以上且小於原價、原因必選、「餐點問題」「其他」需說明、說明最多 100 字。
- `OrderController.Checkout` 新增 `discountedAmount`、`discountReason`、`discountNote` 參數：
  - 先檢查折扣，再以折扣後應收檢查收款金額。
  - 記錄折扣金額、原因、說明與操作者。
  - 成功訊息帶出折扣與應收。
- 結帳視窗：
  - 兩個表單合併為單一表單，新增可收合的折扣區（改收金額、打折、抹零按鈕、原因與說明）。
  - 即時顯示「原價 → 折扣 → 應收」，條件不符時停用送出按鈕；快速金額依折扣後應收重新計算。
  - 改收金額、折數、收款金額只接受整數：輸入小數時提示並停用送出，不默默捨去小數。
- 原本伺服器端的 `CashPayment.QuickAmounts` 已無使用處，連同測試一併移除，改由前端計算。
- 訂單詳情顯示折扣（原因、說明、操作者）與實收；訂單列表金額欄改為實收。
- 營收報表：總營收與通路拆分改用實收，新增「已扣除折扣」；品項、加料排行維持原價。
- 新增 `tests/ManTingEats.Tests/OrderDiscountTests.cs`（25 項）。

**決策原因**

- 使用者決議：
  - 店長、員工都能給折扣，不設上限。
  - 「改收金額」與「打折」都要，並提供抹零。
  - 原因必填，「餐點問題」「其他」需說明。
  - 餐點有問題用整單折扣處理（方案 A），不做單一品項免費。
- 折扣金額要存資料庫（找零不存）：折扣影響實收與營收，需供事後對帳。
- 前端統一換算成「折扣後應收金額」送出，後端只需檢查一個數字與原因，打折、抹零的換算規則與 `OrderDiscount` 一致。
- 品項排行維持原價，才看得出各品項的實際銷售，因此「品項＋加料排行 − 折扣總額 = 總營收」。
- 整數檢查放在前端：原本以 `parseInt` 讀取會把折數 8.5 當成 8（85 折變 8 折），前端換算後送出的已是合法整數，後端擋不到，因此必須在前端擋下（使用者詢問表單檢查時發現）。

**驗證結果**

- Docker 內 .NET 9 SDK `dotnet test`：141 項全數通過（新增 25 項，移除 5 項快速金額舊測試）。
- 本機套用 Migration `AddOrderDiscount` 成功。
- 以 puppeteer-core 驅動本機 Chrome 實際操作結帳視窗（腳本放在暫存資料夾，未加入 repo）：
  - 抹零：$487 → $480，原因自動選「抹零」。
  - 打 9 折：$438；未選原因時停用，選「熟客」後收 $500 找零 $62。
  - 餐點問題：未填說明時停用；金額超過原價時停用；取消折扣後回到原價且不送出折扣。
  - 整單招待：應收 $0，收 $0。
  - 重新打開視窗會重置。
  - 報表：原價合計 $1,948、折扣 $543、總營收 $1,405。
  - 小數：折數 8.5、改收 450.5、收款 500.5 皆停用送出並提示；改為整數後正常結帳。
- 測試建立的「E2E折扣測試品」已下架；開發資料庫留有數張測試訂單。
- 尚未部署到正式環境。

---

### [2026-10-07] 帳號與權限管理（v7）

**變更內容**

- 新增 `docs/prd/v7.md`（已定案）。
- `Employee` 新增 `IsActive`、`SecurityStamp`；Migration `AddEmployeeActiveAndSecurityStamp`：既有帳號預設啟用，並以 `UUID()` 補上安全戳記。
- 新增 `Models/Enums/AppRoles.cs`（`[Authorize(Roles)]` 用的角色字串）。
- 新增 `Services/EmployeeSession.cs`：建立登入身分（含安全戳記），並於 `Program.cs` 的 `OnValidatePrincipal` 每次請求檢查帳號仍啟用、戳記一致。
- `AccessDeniedPath` 改為新的 `Account/AccessDenied`「沒有權限」頁。
- `MenuController`、`ExpenseController`、`ReportController` 改為 `[Authorize(Roles = AppRoles.Manager)]`；訂單、訂位維持登入即可使用。
- `AccountController.Login`：密碼正確但帳號停用時顯示「此帳號已停用，請洽店長」，改用 `EmployeeSession.CreatePrincipal`。
- 新增 `EmployeeController`（店長限定）與 `Views/Employee/*`：
  - 帳號列表、新增員工、重設員工密碼、停用／啟用員工。
  - 變更自己的密碼：需輸入目前密碼，完成後以新戳記重新簽發目前裝置的 Cookie。
- 新增 `Models/Commands/EmployeeCommands.cs`。三個表單需要 `[Compare]`，而 MVC 不接受 positional record 的屬性驗證，故改用一般 class。
- 導覽列與首頁卡片：員工看不到菜單管理、支出記錄、營收報表、帳號管理。
- 新增測試 `AccountTestHost.cs`、`AccountPermissionTests.cs`（25 項）。

**決策原因**

- 使用者決議：
  - 員工只能用訂單與訂位，可以作廢已結帳訂單。
  - 只有店長能設定密碼，員工不能自行修改。
  - 系統只有一個店長，不能新增店長。
- 帳號不提供刪除：歷史訂單記錄了開單與作廢的操作者，刪除會使紀錄失去對象。店長帳號也不能停用。
- 安全戳記：讓重設密碼與停用立即對已登入的裝置生效（離職員工的 iPad 不會保持登入）。代價是每次請求多一次以主鍵查詢，單店使用量可忽略。
- 停用訊息只在密碼正確時顯示，避免未知密碼的人探測帳號狀態。
- `IsActive` 不在 EF 模型設 `HasDefaultValue(true)`：bool 的 CLR 預設值 false 會被 EF 視為「未設定」，導致停用無法寫入。既有資料的預設值只在 Migration 中設定。

**驗證結果**

- Docker 內 .NET 9 SDK `dotnet test`：121 項全數通過（新增 25 項）。
- 突變檢查：移除「停用帳號擋登入」與「菜單管理店長限定」，對應測試皆失敗；檢查後已還原。
- 本機套用 Migration：admin 為啟用、戳記 32 字。
- 以 curl 走完整流程：
  - 店長：導覽列 6 項；新增員工成功；重複帳號（不分大小寫）與密碼太短被擋。
  - 員工：導覽列只有訂單與訂位；進入菜單、支出、報表、帳號管理皆導向「沒有權限」。
  - 重設員工密碼後，員工原本的登入立即失效，舊密碼不能登入、新密碼可以。
  - 停用後，員工原本的登入立即失效；正確密碼顯示「已停用」，錯誤密碼只顯示「帳號或密碼錯誤」。
  - 店長不能停用自己。
  - 店長改密碼：目前密碼錯誤會被擋；改完目前裝置保持登入，其他裝置的舊密碼失效。
- 開發資料庫：admin 密碼已還原為 `123`；留有測試帳號 `staff1`（已停用）。
- 2026-10-07 已部署到正式環境，店長已更換初始密碼（見「正式環境主機與網域」段落）；所有裝置需重新登入一次。

---

### [2026-10-07] MySQL 健康檢查，web 等資料庫就緒才啟動

**變更內容**

- `docker-compose.yml`、`docker-compose.prod.yml`：
  - db 新增 `healthcheck`：以網站使用的帳號、經 TCP（`-h 127.0.0.1`）對 `ManTingEatsDb` 執行 `SELECT 1`。
  - 健康檢查參數：`start_period` 120 秒、`start_interval` 2 秒，之後每 10 秒檢查一次。
  - web 的 `depends_on` 改為 `condition: service_healthy`。

**決策原因**

- 原本 `depends_on` 只等 db 容器啟動，不等 MySQL 可連線；web 啟動時立即執行 `MigrateAsync()`（只試一次），在 Mac 上兩次出現 `Unable to connect to any of the specified MySQL hosts` 後退出，需手動重啟。正式環境雖有 `restart: unless-stopped` 會自動重試，但會留下錯誤紀錄。
- 不用 `mysqladmin ping`：MySQL 首次初始化時會先啟動只接受 socket 的暫時伺服器（`port: 0`），socket 版 ping 會在此時誤判就緒；改走 TCP 並實際用應用程式帳號查詢，才代表帳號與資料庫都已建立。
- 密碼以 `$$` 取用容器內環境變數，不寫進 compose 指令。
- 已知限制：VPS 重開機時由 Docker daemon 依 restart policy 重啟容器，**不套用** `depends_on` 條件；此情況仍由 web 的 `restart: unless-stopped` 自動重試，最終會正常啟動。健康檢查主要改善 `docker compose up`（部署、更新）時的啟動順序。

**驗證結果**

- 現有開發資料庫：`docker compose down` 後 `up -d`，db Healthy 後 web 才啟動，登入頁 200、無錯誤。
- 全新資料卷（首次初始化，獨立專案名稱 `mte-healthtest`）：日誌可見暫時伺服器 `port: 0` 階段，健康檢查等到正式伺服器才通過；7 個 Migration 套用成功、無錯誤。
- 正式環境設定（假密碼、僅 web＋db，專案 `mte-prodtest`）：同樣等 db Healthy 才啟動 web，web 重啟次數 0、無錯誤。
- 反向檢查：健康檢查指令以正確密碼結束碼 0、錯誤密碼結束碼 1。測試用容器、映像與資料卷皆已清除，開發環境已恢復。
- **正式環境需在 VPS 執行一次更新指令才會套用**（見「正式環境主機與網域」段落）。

---

### [2026-10-06] 訂單、支出與訂位列表分頁

**變更內容**

- 新增 `Models/PagedList.cs`（`PagedList<T>` 與非泛型 `IPagination`）：每頁 20 筆，頁碼超出範圍時夾回第一頁／最後一頁。
- 新增 `Views/Shared/_Pagination.cshtml`：Bootstrap 分頁列（上一頁／頁碼／下一頁，目前頁前後各 2 頁），換頁時保留其他查詢參數；只有一頁時不顯示。
- `OrderController.Index`：改為分頁，排序改為**未結帳訂單優先**，其餘依建立時間新到舊。
- `ExpenseController.Index`：改為分頁；支出總額改以 `SumAsync` 加總所有符合條件的資料（非僅目前頁）。`ExpenseSearchViewModel.Results` 改為 `PagedList<Expense>`。
- `ReservationController.Index`：改為分頁；未指定姓名與日期時**只列今天以後**的訂位（依時段先後），畫面提示「查詢過去的訂位請輸入姓名或選擇日期」；只依姓名查詢時涵蓋過去訂位、最近的排前面。`ReservationSearchViewModel` 新增 `UpcomingOnly`，`Results` 改為 `PagedList<Reservation>`。
- 新增 `PaginationTests`（6 項）、`ReservationControllerTests` 新增列表篩選測試（3 項）。

**決策原因**

- 處理 Review 待辦「低 5」：資料逐年累積後一次載入全部會越來越慢。選擇分頁而非預設日期範圍，不改變使用者目前的查詢習慣。
- 訂位原本由舊到新全部列出，一年後打開會先看到一年前的訂位；使用者選擇（2026-10-06）預設只顯示今天以後＋分頁。依姓名查詢仍涵蓋過去，因為顧客請求刪除個資時要找得到舊資料。
- 訂單原本只依建立時間排序；分頁後忙碌時較早開的未結帳單會被擠到第二頁而漏結帳，故未結帳單一律置頂。
- 排序最後加上 `Id`，確保同時間的資料換頁時不重複、不遺漏。
- Razor 注意：`@page` 是指令保留字，迴圈變數不可命名為 `page`。

**驗證結果**

- `dotnet test`：96 項全數通過。
- 本機 MySQL 暫時插入 25 筆測試支出驗證：第 1 頁 20 筆、第 2 頁 7 筆，兩頁的支出總額皆為全部加總；加上分類條件換頁時條件保留。測試資料驗證後已刪除。
- 本機以 curl 確認訂位列表：預設顯示今天以後的訂位與提示文字；依姓名查詢、依過去日期查詢皆正常。

---

### [2026-10-06] 正式環境主機與網域選定：Vultr 東京 + Cloudflare .com

**決策內容**

- VPS：Vultr 東京 Regular Cloud Compute 2GB（1 vCPU / 2GB，約 $10／月）。
- 網域：Cloudflare Registrar `.com`（約 $10.46／年，續約不漲）。

**決策原因**

- 使用者以 CP 值為主要考量。2026-10 比價（2GB、亞洲機房）：Vultr $10 最低；Lightsail 東京 $12（2 vCPU）、DigitalOcean 僅新加坡 $12、Linode $12；GCP 台灣 e2-small $14.16 且流量另計；Hetzner 2026 年兩次漲價後新加坡最低 €15.49，已不划算。
- 記憶體選 2GB：MySQL 8 + ASP.NET + Caddy 在 1GB 上吃緊，尖峰卡頓風險不值得省下的 $5。
- 東京距台灣延遲約數十毫秒，點餐操作無感。
- 網域僅供店員使用、顧客看不到，不需 .tw；Cloudflare 為成本價且續約不漲。
- 先前的對話曾討論過主機選擇但未記錄於 repo，換電腦後無從查起，故本次明確記錄。

**部署時待辦（程式碼不需修改）**

1. Cloudflare 註冊網域；DNS 新增 A 記錄指向 VPS IP，**proxy 關閉（灰色雲）**。
2. Vultr 建立 Ubuntu LTS 主機；防火牆只開 22、80、443；安裝 Docker；建議加 2GB swap（在主機上 `docker compose build` 執行 `dotnet publish` 較吃記憶體）。
3. `git clone` 後依 `.env.example` 建立 `.env`：`DOMAIN`、`ACME_EMAIL`、DB 兩組密碼、`SEED_ADMIN_PASSWORD` 皆用強密碼。
4. `docker compose -f docker-compose.prod.yml up -d --build`；確認 Caddy 取得憑證、以網域登入、iPad 出單回呼正常。
5. 系統目前**沒有改密碼功能**：admin 密碼即 `.env` 的 `SEED_ADMIN_PASSWORD`，只在第一次啟動（資料庫無任何員工）時建立，之後改 `.env` 不會生效，第一次啟動前務必設為強密碼。crontab 排程 `scripts/backup-db.sh`（時間部署後再討論；異地備份仍待處理，見 Review 待辦「低 7」）。
6. 上線前完成：隱私權政策聯絡資訊。

---

### [2026-10-06] 列表頁排版一致性調整

**變更內容**

- 菜單管理：「新增品項／新增加料」按鈕由分頁內移到標題列右側（與訂單、訂位、支出一致），依目前分頁切換顯示（`Views/Menu/Index.cshtml` 內的 script）。
- 菜單管理：網址帶 `#addons` 時直接開啟加料分頁；`MenuController` 新增／編輯／上下架加料後導回 `/Menu#addons`，加料表單的「取消」也回到加料分頁。切換分頁時以 `history.replaceState` 更新網址，重新整理仍停在同一分頁。
- 訂單、菜單、訂位列表的操作欄標題由空白統一為「操作」；各列表操作按鈕加 `text-nowrap`，iPad 窄畫面不換行。
- 營收報表「查詢」按鈕改為 `btn-outline-primary`，與其他頁的查詢按鈕一致。

**決策原因**

- 使用者發現菜單管理的新增按鈕位置與其他頁不同：原因是菜單有兩個分頁、各自需要不同的新增按鈕，當初放在分頁內。改為標題列按鈕隨分頁切換。
- 原本處理完加料一律回到「品項」分頁，需再手動切換，一併改善。

**驗證結果**

- `dotnet test`：87 項全數通過。
- 本機以 curl 確認：標題列有兩個按鈕（加料按鈕預設隱藏）、加料上下架後導向 `/Menu#addons`、加料表單取消連結為 `/Menu#addons`。分頁切換的前端效果需在瀏覽器確認。

---

### [2026-10-06] 表單欄位名稱改為中文

**變更內容**

- 訂位、支出、菜單品項、加料、建立訂單的 Command 加上 `[property: Display(Name = ...)]`，表單標籤由英文屬性名（`CustomerName`、`Date`、`Price`、`Channel` 等）改為中文。
- 已在畫面手寫中文的標籤（開放加料、加價、桌號）外觀不變，但預設錯誤訊息中的欄位名也會變成中文。

**決策原因**

- 使用者測試訂位編輯頁時發現欄位名稱是英文；檢查後所有用 `<label asp-for>` 空標籤的表單都有同樣問題。
- 注意：record 主建構式參數上的 `[Display]` **必須加 `property:` 目標**，否則 MVC 讀不到、標籤仍顯示英文（第一次未加時實測無效）；驗證屬性則不需要。之後新增 Command 請照此寫法。

**驗證結果**

- `dotnet test`：87 項全數通過。
- 本機以 curl 檢查支出、菜單、加料、訂單、訂位的新增與編輯頁，標籤皆為中文；訂位編輯送出錯誤資料時中文錯誤訊息正常顯示。

---

### [2026-10-06] 訂位新增編輯與刪除

**變更內容**

- `ReservationController` 新增 `Edit`（GET／POST）與 `Delete`（POST），作法與支出模組一致；新增 `UpdateReservationCommand`、`Views/Reservation/Edit.cshtml`，列表每筆加上「編輯」「刪除」按鈕（刪除前跳確認）。
- 訂位時間檢查抽成 `ValidateReservedAt`，新增與編輯共用。
- `OrderControllerTestHost.NullTempDataProvider` 改為 `internal`，供其他 Controller 測試共用。
- 新增 `ReservationControllerTests`（6 項）。

**決策原因**

- 處理 Review 待辦：隱私權政策承諾顧客可請求刪除個資，訂位含姓名與電話，必須能刪除或更正。
- 刪除為直接刪除、不留紀錄（不做軟刪除），才符合「刪除個資」的承諾。
- 編輯時**只有改了時段**才檢查「不可早於現在」：已過的訂位仍要能更正姓名、電話。

**驗證結果**

- `dotnet test`：87 項全數通過。
- 本機以 curl 實測：編輯頁帶出原資料；改到過去時段會被擋下；正常編輯後列表顯示新資料；刪除後列表不再出現（測試資料已刪除）。

---

### [2026-10-06] 營收報表排行拆分品項與加料

**變更內容**

- `ReportController.Index`：額外載入訂單品項的加料，新增「加料銷售排行」，依下單當下的加料名稱快照（`OrderItemAddOn.AddOnName`）分組，金額為 `UnitPrice × Quantity`，與 `OrderController.ComputeTotal` 算法一致。
- `ReportViewModel` 新增 `AddOnSales` 與 `AddOnRanking`。
- `Views/Report/Index.cshtml`：品項排行標題註明「不含加料」，下方新增加料排行表。
- 新增 `ReportControllerTests`（2 項）：品項與加料分開計算、兩張排行相加等於總營收；未結帳與作廢訂單的加料不計入。

**決策原因**

- 處理 Review 待辦「低 4」：原本品項排行只算品項本身，加料金額沒有出現在任何排行。使用者決定（2026-10-06）**品項與加料分開排行**，而不是把加料併入品項金額。
- 用名稱快照分組，加料日後改名或停用時，歷史資料仍對得上當時的金額。

**驗證結果**

- `dotnet test`：81 項全數通過。

---

### [2026-10-05] 桌號限制最多 10 個字

**變更內容**

- `OrderLimits` 新增 `MaxTableNumberLength = 4`，`CreateOrderCommand.TableNumber` 長度上限由 20 改為 10（錯誤訊息「桌號最多 10 個字」，數字取自常數）。
- `Views/Order/Create.cshtml`：桌號輸入框加上 `maxlength`，直接打不進第 11 個字。
- `AmountValidationTests` 新增桌號長度測試。

**決策原因**

- 使用者實測可輸入 `111111111` 後提出；決議只限制長度、中英數皆可，上限 10 個字（日後可直接改常數）。
- 資料庫欄位維持 20，放寬上限不需 Migration。

**驗證結果**

- `dotnet test`：79 項全數通過。
- 本機以 curl 驗證：輸入框有 `maxlength="10"`；送出 11 個字被擋下，10 個字與中文桌號可建立訂單。

---

### [2026-10-05] 辣度改為數字 0～6

**變更內容**

- 移除 `Models/Enums/SpiceLevel.cs`（五級中文辣度）與對應的 `ToDisplayText`。
- `OrderItem.SpiceLevel`、`AddOrderItemCommand.SpiceLevel` 改為 `int?`；`OrderLimits` 新增 `MinSpiceLevel = 0`、`MaxSpiceLevel = 6`、`DefaultSpiceLevel = 1`，Command 加上 0～6 範圍驗證。
- 點餐畫面辣度按鈕改為 0～6，預設選 1；訂單詳情與出單改為顯示「辣度：數字」。
- `docs/prd/v4.md`、`BRIEFING.md`、PassPRNT 測試頁同步改為數字辣度。
- 測試：`OrderControllerTests` 改用數字辣度，`AmountValidationTests` 新增辣度範圍測試。

**決策原因**

- 使用者要求辣度不用中文，改為 0～6；預設值由使用者選定為 1（沿用原「微辣」的位置）。
- 原列舉在資料庫本來就存成整數，改為 `int?` 後欄位型別不變，**不需 Migration**（`dotnet ef migrations has-pending-model-changes` 確認無變更）。
- 已存在的辣度資料（原 0～4 對應不辣～大辣）會直接顯示為數字 0～4；目前尚未上線，只有開發測試資料受影響。

**驗證結果**

- Docker 內 .NET 9 SDK `dotnet test`：74 項全數通過。
- 本機以 curl 驗證：點餐畫面辣度選項為 0～6 且預設 1；送出辣度 6 正確存檔、未選辣度存為 1、送出 7 被擋下（「辣度需介於 0 到 6」）；出單顯示「辣度：6」「辣度：1」。

---

### [2026-10-05] 出單印出品項與加料金額

**變更內容**

- `Views/Order/_Ticket.cshtml`：
  - 品項行改為「品名｜數量｜金額」，金額為單價 x 數量，30px 粗體靠右。
  - 同一品項的所有加料合併一行，右側印加料總價，不加「+」號。
  - 每種加料以不換行的 span 包住，過長時只在頓號後換行。
- `docs/prd/v5.md`：修訂第 4 節版面表與第 6 節決議 3（原「不印單價／小計」）。

**決策原因**

- 使用者要求出單上每個品項都要看得到金額。
- 數量大於 1 印總價、加料只印總價，皆不印單價，節省 58mm 紙寬。
- 加料名稱先以 `HtmlEncoder` 編碼再組成一行輸出：避免 Razor 排版的換行在頓號前後產生多餘空白，也確保店員輸入的名稱不會被當成 HTML。

**驗證結果**

- 以 headless Chrome 在 384px（58mm 可印寬度）產生示意圖，經使用者確認版面。
- 本機建立實際訂單，把網站產生的出單 HTML 轉成圖片：品項金額、加料總價（蛋蛋 x5 + 瓜瓜 x6 = $1250）、總金額 $1380 皆正確，換行位置正常。
- 2026-10-05 使用者以 iPad + 印表機實際列印，確認新出單格式（品項金額、加料總價、數字辣度）沒問題。

---

### [2026-10-05] 補上 OrderController 測試（review 低 1）

**變更內容**

- 測試專案新增 `Microsoft.EntityFrameworkCore.Sqlite` 套件（僅測試使用）。
- 新增 `tests/ManTingEats.Tests/OrderControllerTestHost.cs`：SQLite 記憶體資料庫＋測試用菜單／加料、已登入使用者、TempData、產生回呼網址的假 `IUrlHelper`；每次動作用新的 DbContext 模擬獨立請求。
- 新增 `tests/ManTingEats.Tests/OrderControllerTests.cs`（33 項）：
  - 建立訂單：內用未填桌號、每日流水號遞增、同桌未結帳提醒（含桌號去空白）。
  - 金額計算與客製化：一般品項小計、客製化數量鎖 1 與加料計價、辣度預設小辣、不支援加料時忽略加料、停用加料／下架品項被擋、改價不影響已點品項、移除品項重算總額。
  - 狀態轉換：已結帳／作廢訂單不可加點或出單、未出過單取消即作廢、結帳（成功、待出單擋下、未付或金額不足、空訂單、重複結帳）、作廢（記錄原因與操作者、不可重複作廢）。
  - 出單：全單／加點單內容、確認出單時不先標記、失敗與成功回呼、只標記送印的品項、作廢不可補印、補印不帶品項 ID。

**決策原因**

- 選 SQLite 記憶體資料庫而非 EF InMemory：會檢查外鍵與連動刪除，較接近 MySQL 實際行為，且不需外部資料庫即可在任何電腦執行。

**驗證結果**

- Docker 內 .NET 9 SDK `dotnet test`：70 項全數通過（新增 33 項）。
- 突變檢查：暫時移除「客製化數量鎖 1」與「待出單擋下結帳」兩條規則，對應測試皆失敗，確認測試有效；檢查後程式已還原。

---

### [2026-10-04] 結帳收款與找零

**變更內容**

- 新增 `docs/prd/v6.md`（已定案）；`docs/prd/mvp.md` 註明找零改由 v6 提供。
- 新增 `Services/CashPayment.cs`：快速金額計算（下一個整百、$500、$1000 中大於應收者）、收款金額檢查（必填、整數、不小於應收、上限 $100,000）。
- `OrderController.Checkout` 新增 `receivedAmount` 參數，檢查不通過擋下結帳；成功訊息顯示「收 $X，找零 $Y」。收款金額不寫入資料庫。
- `Views/Order/Details.cshtml`：結帳按鈕改為開啟結帳視窗（取代原本的 confirm 對話框），含「收剛好」、快速金額按鈕、手動輸入與即時找零顯示；金額不足時停用「確認結帳」；送出後停用按鈕防止連點。
- 新增 `tests/ManTingEats.Tests/CashPaymentTests.cs`。

**決策原因**

- 使用者決議：必須「收剛好」或輸入收款金額其中之一才能結帳；收款與找零只在畫面計算、不存資料庫；需要快速金額按鈕。
- 後端仍檢查收款金額，防止繞過結帳視窗直接送出；不需 Migration。
- 防連點：第二次送出會因訂單已結帳而顯示錯誤，蓋掉含找零的成功訊息。

**驗證結果**

- Docker 內 .NET 9 SDK：`dotnet build` 成功，`dotnet test` 37 項全數通過（新增 10 項）。
- 本機以 curl 驗證：應收 $280 的快速金額為 $300／$500／$1000；未帶金額與金額不足（還差 $80）被擋下；收 $500 顯示找零 $220；收剛好顯示找零 $0。
- 使用者以 iPad 透過 Cloudflare 快速通道實測結帳視窗，回報測試通過。

---

### [2026-10-04] 出單列印改為 iPad + PassPRNT，移除 LAN 直連印表機

**變更內容**

- 新增 `docs/prd/v5.md`（已定案），取代 v3 的連線方式、結帳補印與失敗處理。
- `OrderController`：
  - `ConfirmPrint`／`Reprint` 不再由伺服器列印，改回傳 `Print` 頁面，由前端開啟 `starpassprnt://` 列印。
  - 新增 `PrintResult`（GET，接收 `passprnt_code`／`passprnt_message`）與 `MarkPrinted`（POST，含 Anti-Forgery），只標記送印當下記錄的品項 ID。
  - `Checkout` 移除結帳前自動補印，改為有待出單品項時擋下結帳。
  - 列印結果寫入應用程式日誌。
- 新增 `Views/Order/Print.cshtml`、`PrintSucceeded.cshtml`、`_Ticket.cshtml`（58mm 出單版面，字級同測試頁定案值，不印單價／小計），`Models/PrintTicketViewModel.cs`、`PrintSucceededViewModel.cs`。
- 新增 `Services/OrderTicket.cs`（待出單判斷、全單／加點判斷、標記已出單、品項 ID 序列化）與 `tests/ManTingEats.Tests/OrderTicketTests.cs`。
- `Views/Order/Details.cshtml`：有待出單品項時停用結帳按鈕並顯示提示。
- 移除 `LanReceiptPrinterService`、`IReceiptPrinterService`、`PrinterOptions`、`appsettings*.json` 的 `Printer` 區塊、Big5 編碼註冊與 `System.Text.Encoding.CodePages` 套件。

**決策原因**

- 主機將在雲端、店內無法讓印表機上網，伺服器無法直連印表機；改由接著印表機的 iPad 透過 PassPRNT 列印，並以回呼得知成功與否。
- 使用者決議：結帳不補印（以每次確認訂單的出單為準，加點單底部印累加總金額）；未成功一律人工重印；出單不印單價。
- 回呼為 GET，為避免 GET 修改資料，改由自動送出的 POST 標記已出單。
- 只標記送印當下的品項 ID，避免列印期間加點的品項被誤標為已出單。
- 出單時間改用 `TaipeiTime.Now`（舊程式用 `DateTime.Now`，在 UTC 容器中會差 8 小時）。

**驗證結果**

- 以 Docker 內 .NET 9 SDK `dotnet build` 成功，`dotnet test` 27 項全數通過（新增 7 項）。
- 本機以 curl 走完整流程：待出單時結帳被擋、全單內容正確（含辣度／加料／備註，備註中的 HTML 被編碼）、失敗回呼顯示警告且品項維持待出單、成功回呼標記已出單、加點單僅列新品項且總金額累加、補印不改變出單狀態、未帶 Anti-Forgery Token 的 `MarkPrinted` 回 400。
- iPad + 實體印表機實機測試通過（使用者回報；伺服器日誌兩次 `SUCCESS`，回呼時登入狀態保留）。測試中的 `E004`（`ERROR_GETPORT_FAILURE`）為印表機連線問題，處理後正常。

---

### [2026-10-04] PassPRNT 測試頁改為 58mm 紙寬並放大品項字級

**變更內容**

- `wwwroot/dev/passprnt-test.html`：
  - PassPRNT 紙寬參數 `size=3`（80mm）改為 `size=2`（58mm）。
  - 全單版面整體縮小以符合 58mm 可印寬度（約 48mm／384 dots），並加上 `word-break:break-all` 避免長字串被裁切。
  - 新增 `itemRow`：品名／數量 36px 粗體，數量固定靠右、品名過長於左欄換行；新增長品名範例測試換行。
  - 新增 `detailRow`：辣度／加料／備註 30px，內縮於品名下方。

**決策原因**

- 實測發現時間那行右側被切掉：店內實際用 58mm 紙，原測試頁依 80mm 排版。
- 品名、數量與客製化內容是廚房製作時最需要一眼看清的資訊，依使用者要求放大；其餘資訊維持小字以免整張過長。
- 新電腦首次 `docker compose up` 時，本機 Mac 已安裝的 MySQL 佔用 3306，使用者選擇停用本機 MySQL（不改專案設定）。另觀察到首次啟動 MySQL 初始化較慢，web 的自動 Migration 會先失敗，重啟 web 即可；尚未加 healthcheck。

**驗證結果**

- 使用者以 iPad + PassPRNT 實測：時間那行完整印出、會自動切紙，最終字級已確認可用。
- Safari 每次跳確認視窗、印完回到同一分頁（使用者回報）；「加入主畫面」模式尚未測。
- 測試完成後已移除 Cloudflare 快速通道容器。

---

### [2026-10-01] 交接準備：CLAUDE.md、測試菜單匯入腳本、PassPRNT 測試頁

**變更內容**

- 新增根目錄 `CLAUDE.md`：引導 Claude Code 先讀本檔「目前進度」與 `.github/` 規範，並記錄本機開發指令、Cloudflare 快速通道用法與注意事項。
- 新增 `scripts/dev-data/menu-seed.sql`（6 筆測試品項、2 筆加料）與 `import-menu-seed.sh`（`INSERT IGNORE`，可重複執行）。
- 新增 `wwwroot/dev/passprnt-test.html`：PassPRNT 出單實測頁（簡單中文、模擬全單兩種測試；自動判斷列印結果與是否開新分頁）。
- `Program.cs`：非 Development 環境下 `/dev` 路徑一律回 404。
- `.github/BRIEFING.md`：出單列印模組狀態更新為 PassPRNT 方向，並修正兩處換行黏行。

**決策原因**

- 開發者需在兩台電腦間切換，Claude Code 對話不會同步，進度改以 repo 內文件交接。
- repo 為公開：資料庫只匯出無個資的菜單／加料，不匯出訂單、訂位、員工。
- 測試頁需納入版控以便在家延續測試，但不應出現在正式環境，故以環境判斷封鎖。

**驗證結果**

- 匯入腳本於現有資料庫重複執行，筆數不變。
- Development 環境 `/dev/passprnt-test.html` 回 200；以 Production 環境啟動同一 image，`/dev` 回 404、登入頁回 200。

---

### [2026-09-30] 取消作廢通知列印

**變更內容**

- `OrderController.Void`：作廢後不再列印作廢通知，查詢簡化為 `FindAsync`（不再需要載入品項）。
- `IReceiptPrinterService`／`LanReceiptPrinterService`：移除 `PrintVoidNoticeAsync` 與 `BuildVoidTicket`。
- `docs/prd/v3.md`、`docs/prd/v4.md`：同步更新規格，作廢訂單改為不列印。

**決策原因**

- 使用者表示作廢時直接在原出單紙本上手動打叉即可，不需另印一張；同時解決「作廢已結帳訂單仍印出『請立即停止製作』」的語意問題（review 低 3）。
- 作廢仍保留操作者與原因的稽核紀錄，不受影響。

**驗證結果**

- `dotnet build` 成功，`dotnet test` 通過。

---

### [2026-09-30] 首頁卡片按鈕對齊、導覽列順序調整、補齊隱私權政策

**變更內容**

- `wwwroot/css/site.css`：`.dashboard-card .card-body` 改為直向 flex，按鈕以 `margin-top: auto` 貼齊卡片底部；新增 `.privacy-content` 長文排版樣式。
- `Views/Shared/_Layout.cshtml`：導覽列順序改為與首頁卡片一致（首頁 → 訂單 → 菜單管理 → 訂位記錄 → 支出記錄 → 營收報表）。
- `Views/Home/Privacy.cshtml`：依《個人資料保護法》補齊隱私權政策，內容依系統實際行為撰寫（蒐集資料類別、利用方式、Cookie、當事人權利、資料安全、聯絡方式、政策修訂）；店家名稱為「香港爆辣冷麵」。

**決策原因**

- 首頁卡片說明文字行數不同，導致按鈕高低不一。
- 隱私權政策僅寫系統實際蒐集的資料與實際使用的 Cookie（無追蹤／分析工具），避免與實際行為不符。

**驗證結果**

- Docker 重建後頁面正常回應 200。
- 待辦：聯絡電話與信箱仍為佔位文字（`Privacy.cshtml` 頂端常數）；訂位資料目前無刪除功能，當事人請求刪除時需直接操作資料庫；正式上線前建議由熟悉個資法者審閱。

---

### [2026-09-30] 表單欄位限制：金額整數化與上限、訂位／支出日期範圍、桌號去空白、品項名稱不重複

**變更內容**

- 新增 `Models/Validation/WholeAmountAttribute.cs`：金額需為整數。
- 金額上限（皆需整數）：品項售價 1～100,000；加料加價 0～10,000（開放 0 元免費加料）；支出金額 1～10,000,000。對應 View 的輸入框改為 `type="number" step="1"`。
- 訂位：人數 1～50；`ReservationController.Create` 檢查訂位時間不可早於現在、不可超過一年後（以台灣時間比對）。
- 支出：`ExpenseController` 新增／編輯時日期不可晚於今天（台灣時區）。
- `OrderController.Create`：桌號去除前後空白。
- `MenuController.Create`／`Edit`：品項名稱去除前後空白，且不可與其他品項重名（含已下架）。
- `Services/TaipeiTime.cs` 新增 `Now`。
- 新增 `tests/ManTingEats.Tests/AmountValidationTests.cs`：驗證上述上下限、整數規則與 `TaipeiTime` 換算。

**決策原因**

- 資料庫金額欄位為 `decimal(10,2)`，原上限 `double.MaxValue` 超過即存檔失敗（500）。
- 台幣不收小數，且出單以 F0 列印，允許小數會造成畫面與出單金額不一致。
- 營收報表品項排行以名稱分組，同名品項會被合併；僅於應用層檢查、不加資料庫唯一索引，避免既有重複資料使啟動時的 Migration 失敗。
- Command 為 positional record，驗證屬性掛在建構子參數上；測試需比照 MVC 讀取參數屬性，`Validator.TryValidateObject` 會漏掉。

**驗證結果**

- `dotnet build` 成功。
- 既有資料若有小數售價或重名品項，編輯該筆時需一併修正才能儲存。

---

### [2026-09-30] 點餐數量加上限、印表機寫入逾時

**變更內容**

- `Models/Commands/OrderCommands.cs`：新增 `OrderLimits.MaxQuantity = 99`，`AddOrderItemCommand.Quantity` 與 `AddOnSelectionCommand.Quantity` 改為 `Range(1, 99)`。
- `Views/Order/Details.cshtml`：品項與加料數量輸入框加上 `max`，「＋」按鈕不會超過上限。
- `Services/LanReceiptPrinterService.cs`：`TimeoutMs` 由僅限連線階段改為涵蓋連線＋寫入＋Flush 整段。

**決策原因**

- 原本數量上限為 `int.MaxValue`，極端值會使金額超出 `decimal(10,2)`，存檔時資料庫報錯導致 500。
- 印表機卡紙或緩衝區滿時，寫入可能無限等待，出單／結帳頁面會一直轉圈。

**驗證結果**

- `dotnet build` 成功，`dotnet test` 通過；尚未連接實體印表機驗證逾時行為。

---

### [2026-09-30] 登入防暴力破解強化、訂單詳情頁 JS 錯誤修正、新增 .dockerignore

**變更內容**

- `AccountController.Login`：失敗次數鎖定的 key 改為「正規化帳號（Trim + 大寫）＋來源 IP」；POST 套用 `[EnableRateLimiting("login")]`。
- `Program.cs`：註冊 `AddRateLimiter`，登入端點依來源 IP 限制每分鐘 10 次，超過回傳 429 與中文訊息；`UseRateLimiter()` 置於 `UseRouting()` 之後。
- `Views/Order/Details.cshtml`：`#customizeForm` 不存在時（已結帳／作廢、無上架品項）略過事件綁定，避免 TypeError 中斷後續「防重複送出」腳本。
- 新增 `.dockerignore`：排除 `bin/`、`obj/`、`.env`、`backups/`、`.git/`、`tests/` 等。

**決策原因**

- MySQL collation 不分大小寫（且不分重音），原本以原始輸入當 key，可用 `Admin`／`ADMIN` 等變化各取得 5 次嘗試；加上 IP 維度可避免外部攻擊者故意輸錯把店內管理者鎖住。總嘗試量改由 IP 限流把關。
- 未排除本機 `obj/` 時，`COPY . ./` 會以含 Windows 路徑的 `project.assets.json` 覆蓋容器內 restore 結果。

**驗證結果**

- `dotnet build` 成功，`dotnet test` 通過；`docker build` 成功，image 內容僅含 publish 產物。
- 限流與鎖定尚未實際啟動網站驗證（需資料庫）。

---

### [2026-09-30] 啟動時自動套用 Migration、修正營收報表時區偏差與作廢訂單補印問題

**變更內容**

- `Program.cs`：種子帳號建立前呼叫 `db.Database.MigrateAsync()`，啟動時自動套用尚未執行的 Migration。
- 新增 `Services/TaipeiTime.cs`：集中處理台灣時區「今天」與「台灣日期 → UTC 邊界」換算；`OrderController.GetNextDailyNumberAsync` 改用此 helper。
- `ReportController`：已結帳訂單查詢改以 UTC 邊界比對 `CompletedAt`（原本以本地日期直接比對 UTC 欄位，台灣時間 00:00–08:00 結帳的訂單會被算到前一天）；支出 `Expense.Date` 為台灣日期，維持原比對方式。`Views/Report/Index.cshtml` 快捷日期按鈕改用 `TaipeiTime.Today`。
- `OrderController.Reprint`：已作廢訂單拒絕補印；`ConfirmPrint`：僅允許 `Open` 狀態訂單出單。`Views/Order/Details.cshtml` 作廢訂單不再顯示「補印出單」按鈕。

**決策原因**

- 正式環境 runtime image 不含 SDK／`dotnet-ef`，無法手動執行 `database update`；單店單一 web 實例，啟動時自動 Migrate 最簡單且不易遺漏。替代方案（idempotent SQL script、migration bundle）需額外部署步驟，暫不採用。
- 作廢訂單補印「全單」可能讓廚房誤以為需要製作，後端與前端雙重阻擋。
- 伺服器與資料庫待客戶測試完成後才購買；若改用雲端代管 MySQL，應用程式帳號需具備 CREATE／ALTER 權限才能執行 Migration。

**驗證結果**

- `dotnet build` 成功（0 警告 0 錯誤），`dotnet test` 通過。
- 尚未連線資料庫實際啟動驗證，建議確認：程式可正常啟動（本機已是最新 schema，Migrate 應無動作）、報表「今日」包含早上 8 點前結帳的訂單。

---

### [2026-09-23] UI/UX 優化：點餐客製化面板與加料管理套用焦糖橘 Design System

**變更內容**

- `Views/Order/Details.cshtml` 客製化 Offcanvas：辣度改為 `.spice-btn-group`（選中態焦糖橘實心底、44px 觸控高度）；加料勾選改為整列變色（`.addon-row-selected`）取代單純小 checkbox；小計＋加點按鈕合併為 `.offcanvas-sticky-footer` 固定於底部。
- `Views/Menu/Index.cshtml` 加料頁籤由純表格改為卡片網格（`.addon-card`），與品項卡片視覺風格一致。
- `Views/Menu/Create.cshtml`/`Edit.cshtml` 的兩個客製化旗標改用 `.customization-card` 包裹＋`form-switch` 開關樣式，並加上說明文字。
- `wwwroot/css/site.css` 新增對應 CSS：`.spice-btn-group`、`.addon-row`/`.addon-row-selected`、`.offcanvas-sticky-footer`、`.addon-card`、`.customization-card`、`.text-brand`。

**決策原因**

- 沿用既有焦糖橘 Design Token（`--brand-primary` 系列），維持全站視覺一致性，不引入新配色。
- 加料維護維持原本整頁跳轉互動（未改為 Modal），僅做視覺換皮，範圍與風險可控。

**驗證結果**

- `dotnet build` 成功，`get_errors` 無錯誤；未變動 Controller/Model 邏輯，純 View/CSS 異動。

---

### [2026-09-23] 實作點餐客製化（加料／辣度），依 docs/prd/v4.md 落地

**變更內容**

- **Model**：新增 `AddOn`（全店共用加料清單）、`OrderItemAddOn`（加料快照：`AddOnName`/`UnitPrice`/`Quantity`）、`SpiceLevel` enum（不辣/微辣/小辣/中辣/大辣）；`MenuItem` 新增 `SupportsAddOns`/`SupportsSpiceLevel`；`OrderItem` 新增 `SpiceLevel?` 與 `AddOns` 集合。
- **Data**：`AppDbContext` 註冊新 `DbSet` 與關聯設定；新增 Migration `AddOrderCustomization` 並已套用至本機開發資料庫。
- **Command**：`MenuItemCommands` 新增兩個客製化旗標；新增 `AddOnCommands`（Create/Update）；`OrderCommands.AddOrderItemCommand` 新增 `SpiceLevel`/`AddOns`（`List<AddOnSelectionCommand>`）。
- **Controller**：`MenuController` 新增加料 CRUD（`CreateAddOn`/`EditAddOn`/`ToggleAddOnActive`），`Index` 改回傳 `MenuIndexViewModel`（品項+加料）；`OrderController.AddItem` 依品項旗標分流客製化邏輯，客製化品項後端強制 `Quantity=1`（不信任前端），驗證所選加料是否仍上架，寫入快照；新增 `ComputeTotal` 統一總額計算（含加料金額），並同步修正 `RemoveItem`/`CancelPendingItems` 的加總邏輯與 Include 查詢（皆需 `.ThenInclude(i => i.AddOns)`）。
- **Service**：`LanReceiptPrinterService.AppendItemLine` 出單內容補印辣度與加料明細。
- **View**：`Views/Order/Details.cshtml` 加點面板依品項旗標分流開啟簡易 Offcanvas 或客製化 Offcanvas（辣度按鈕列、加料勾選＋展開式數量調整、捲動區塊、即時小計）；`Views/Menu/Index.cshtml` 改為頁籤（品項／加料）；`Views/Menu/Create.cshtml`/`Edit.cshtml` 新增兩個客製化 checkbox；新增 `Views/Menu/CreateAddOn.cshtml`/`EditAddOn.cshtml`。

**決策原因**

- 詳見 [docs/prd/v4.md](../docs/prd/v4.md)：加料採全店共用清單（非品項專屬）、辣度固定五級預設微辣、客製化品項數量鎖定 1 以避免計價/出單歧義。
- View 層先求功能正確、堪用即可，樣式細節（焦糖橘 Design System 套用）留給後續 UI/UX 優化階段。

**驗證結果**

- `dotnet build` 成功（0 錯誤）。
- `dotnet ef migrations add AddOrderCustomization` + `dotnet ef database update` 成功套用至本機開發資料庫（新增 `AddOns`/`OrderItemAddOns` 表，`MenuItems`/`OrderItems` 新增欄位皆有安全預設值，不影響既有資料）。
- 尚未進行瀏覽器手動點餐流程實測，建議下次操作時實際测试客製化加點、金額計算、出單內容三項。

---

### [2026-09-23] 新增 PRD：點餐客製化（加料／辣度）規格定案

**變更內容**

- 新增 [docs/prd/v4.md](../docs/prd/v4.md)，定義 `MenuItem` 客製化旗標（`SupportsAddOns`/`SupportsSpiceLevel`）、全店共用 `AddOn` 清單、`SpiceLevel` 五級 enum（不辣/微辣/小辣/中辣/大辣，預設微辣）、`OrderItemAddOn` 快照計價規則，以及客製化品項「數量恆為 1」的互動規則。
- 本次僅完成規格定稿與文件落地，**尚未進行程式碼實作**（Model/Migration/Controller/View 待後續開發階段執行）。

**決策原因**

- 加料清單採「全店共用」而非「品項專屬」（Option B），降低維護與開發成本，經使用者確認接受此限制。
- 客製化品項強制數量為 1、多份需分行加點，避免「加料/辣度是否套用到每一份」的計價與出單歧義。
- 加料維護頁面附屬於現有「菜單管理」頁籤，不另開獨立模組入口，符合單店長操作情境的精簡原則。

**驗證結果**

- 純文件變更，未觸碰程式碼；下一階段實作時需搭配 `dotnet build`/`dotnet ef migrations add` 驗證。

---

### [2026-09-23] UI/UX 美化：焦糖橘 Design System、點餐頁 Offcanvas、清單頁統一化

**變更內容**

- **Phase A（全域）**：`wwwroot/css/site.css` 新增焦糖橘（`#d97706`）Design Token，覆寫 Bootstrap `--bs-primary` 系列變數，讓 `.btn-primary`/`.btn-outline-primary`/`.nav-pills`/連結/focus-ring 全站連動換色；`Views/Shared/_Layout.cshtml.css` 移除舊 MVC 範本殘留的寫死藍色（該檔載入順序在 site.css 之後，原本會蓋掉新配色）；Navbar（`Views/Shared/_Layout.cshtml`）加品牌 icon（🐶🐱）、active 底線指示、使用者名稱改 chip 樣式；首頁（`Views/Home/Index.cshtml`）五張功能卡片加圖示、「訂單」設為主打卡片、grid 改為 RWD（`row-cols-1/sm-2/xl-5`）。
- **Phase B（點餐頁）**：`Views/Order/Details.cshtml` 加點面板由 Modal 改為底部 Offcanvas，數量輸入改 +/- 步進器（`.qty-stepper`）；訂單/通路/出單狀態 badge 改用 soft-color 樣式（`.badge-soft-*`）；危險操作卡片（取消/作廢）加淺紅底色警示。
- **Phase C（清單頁）**：`Views/Order/Index.cshtml`、`Views/Menu/Index.cshtml` 狀態 badge 統一改 `.badge-soft-*`；Order/Menu/Reservation/Expense 四個清單頁表格皆加 `table-responsive`，避免手機橫向溢出。

**決策原因**

- 原專案為 Bootstrap 5 預設樣式未經客製，無品牌識別、資訊層級扁平；經使用者確認採焦糖橘為主色，分三階段（全域 → 核心操作頁 → 清單頁）漸進套用，降低單次變動風險。
- 點餐為現場最高頻操作，Offcanvas + 數量步進器比 Modal + number input 更適合觸控情境。

**驗證結果**

- 每階段套用後皆執行 `dotnet build` 確認成功，並以 `dotnet run` 實機預覽三階段畫面無誤。
- 本次變更僅涉及 CSS/Razor 視圖層，未觸碰 Controller/Model/資料庫結構。

---

### [2026-09-23] 訂單新增「當日流水號」（內用/外帶合併計數）

**變更內容**

- `Models/Entities/Order.cs` 新增 `DailyNumber`（int），僅供人員溝通顯示用，非資料庫主鍵；`Id` 保留作為內部路由/關聯用途，不對外顯示。
- `Controllers/OrderController.cs` 新增 `GetNextDailyNumberAsync`：以 `Asia/Taipei` 時區（固定 UTC+8，台灣無日光節約時間）明確計算「今天」邊界，取當日最大 `DailyNumber` +1；不依賴容器系統時區（目前 Dockerfile 未設 `TZ`，容器預設 UTC，若直接用系統本地時間判斷會在台灣時間早上 8 點才跨日）。內用/外帶**合併計數**（不分通路各自流水）。
- 併發保護：採簡單的「查當日最大值 +1」，未加悲觀鎖／唯一索引重試機制——依店內單一收銀情境評估，過度設計不符合專案最小改動原則；未來若真的出現多終端同時建單需求再補強。
- 新增 Migration `AddOrderDailyNumber`：新增欄位並用 SQL 回填既有 7 筆訂單的 `DailyNumber`（依台灣時區分組、依 `CreatedAt` 排序編號）。
- 顯示端改為 `DailyNumber`：`Views/Order/Index.cshtml`、`Views/Order/Details.cshtml`（標題/H1）、`Views/Order/Create.cshtml`（併桌提示連結文字）、`Services/LanReceiptPrinterService.cs`（出單內容的「訂單編號」）；所有 `asp-route-id` 路由參數維持使用 `Id`，不受影響。

**決策原因**

- `Id` 是 DB auto-increment 主鍵，只增不減、取消訂單也不刪列（軟取消保留稽核），長期會變得很大且不利人員溝通；改用每日重置的流水號給人看，`Id` 仍留給系統內部使用，兩者關注點分離。
- 時區改用 `Asia/Taipei`（IANA ID，.NET 9 跨平台皆可辨識）明確計算，避免依賴容器系統時區設定造成「今天」邊界跑掉。

**驗證結果**

- `dotnet build` 成功；`dotnet ef database update` 套用成功，既有 7 筆訂單已回填流水號；已重建並重啟 Docker `web` 容器。

---

### [2026-09-23] 補齊 RWD：訂單詳情與營收報表表格加上 table-responsive

**變更內容**

- `Views/Order/Details.cshtml` 品項清單表格、`Views/Report/Index.cshtml` 通路營收拆分與品項銷售排行兩張表格，補上 `table-responsive` 包裝；手機上這幾個表格原本會被壓縮或橫向溢出。
- 報表頁「今日/本週/本月」`.btn-group` 加上 `flex-wrap gap-2`，避免窄螢幕手機（約 320px）三個按鈕擠在一起。

**決策原因**

- 抽查全站頁面後發現，先前另一個工作階段做的焦糖橘 Design System 改版已涵蓋 4 個列表頁的 `table-responsive`，但漏了訂單詳情（外場人員最常用的頁面）與營收報表這兩處。

**驗證結果**

- `docker compose build/up` 成功，已重建容器。

---

### [2026-09-23] 修正容器時區為 Asia/Taipei + 清空測試訂單資料

**變更內容**

- `Dockerfile` 新增 `ENV TZ=Asia/Taipei`：容器原本預設 UTC，導致 `Views/Order/Index.cshtml` 的 `CreatedAt.ToLocalTime()`、`ReportController` 的 `DateTime.Today` 等所有依賴伺服器本地時間的邏輯全部偏差 8 小時；改在容器層級設定時區，一次修正所有相關顯示與日期判斷，不需逐一改程式碼。已驗證容器內 `date` 指令顯示為正確的台灣時間（CST）。
- 清空開發資料庫的 `OrderItems`、`Orders` 資料表並重置 `AUTO_INCREMENT`，供使用者重新從頭測試訂單流程（開發/測試環境操作，非正式環境）。

**決策原因**

- 時區問題影響範圍廣（訂單建立時間顯示、每日流水號邊界、營收報表區間），在容器層級一次修正比在各處程式碼加時區轉換更不容易遺漏。

**驗證結果**

- 已重建並重啟 Docker `web` 容器，`docker exec ... date` 確認顯示 `CST` 且時間正確；`Orders`/`OrderItems` 已確認清空為 0 筆。

---

### [2026-09-23] 修正加點/送出表單重複提交（double-submit）

**變更內容**

- `Views/Order/Details.cshtml` 新增全域 JS：頁面上所有 `<form>` 送出時立即停用其 submit 按鈕並改文字為「處理中...」，避免網路較慢時使用者重複點擊造成同一動作送出兩次（例如加點被重複記錄）；若表單有 `onsubmit="return confirm(...)"` 且使用者取消，會透過 `event.defaultPrevented` 判斷跳過，不影響原本的取消行為。
- 同時把加點 Modal 的數量欄位補上 `required` + `step="1"`（延續上次的欄位檢查修正）。

**決策原因**

- 純前端防呆，屬於低風險、不影響後端邏輯的修正；伺服器端本身沒有重複品項的檢查機制，長期若要更嚴謹可考慮加 idempotency key，但目前規模下前端鎖按鈕已足夠解決回報的問題。

**驗證結果**

- `dotnet build` 成功，已重建並重啟 Docker `web` 容器。

---

### [2026-09-23] 加點流程新增「取消」（免理由）+ 修正版面間距

**變更內容**

- `Controllers/OrderController.cs` 新增 `CancelPendingItems`：刪除訂單中所有**尚未出單**（`IsPrinted == false`）的品項並重算金額，免填理由；若刪除後訂單品項歸零（代表這張單從未出過單、廚房從沒收到通知），連同訂單一併標記 `Voided`（系統自動填入 `VoidReason`），不需使用者輸入原因。
- `Views/Order/Details.cshtml`：待出單區塊原本只有「確認出單」，改為「確認訂單」+「取消」兩顆並排按鈕；「新增訂單」與「加點」共用同一組按鈕與 action，依當下是否已出過單自動決定「取消」的實際效果（整單作廢 or 只丟棄本次加點）。
- 版面調整：「結帳」按鈕與下方「取消訂單」卡片原本緊貼，替 `Checkout` 表單加上 `mb-4` 補上間距。

**決策原因**

- 「取消訂單」（`Void`，需填理由）保留給「已出單、需留稽核痕跡」的情境；新的「取消」只處理「還沒出單」的部分，兩者職責分開，避免使用者被要求為根本沒發生過的事填寫理由。

**驗證結果**

- `dotnet build` 成功，已重建並重啟 Docker `web` 容器。

---

### [2026-09-23] Open 訂單可直接取消（擴充 Void 動作）

**變更內容**

- `Controllers/OrderController.cs` 的 `Void` 動作：允許條件由「僅 `Completed`」擴充為「`Open` 或 `Completed`」，讓使用者可在結帳前直接取消訂單（例如誤按新增訂單、未點餐）。
- 作廢通知列印邏輯改為「僅當訂單內有任一品項已出單（`IsPrinted == true`）才列印」，避免對空訂單或尚未送廚房的訂單列印無意義的作廢通知。
- `Views/Order/Details.cshtml`：原本僅 `Completed` 狀態顯示的「作廢訂單」卡片，改為 `Open` 或 `Completed` 皆顯示；`Open` 狀態下文案改為「取消訂單/取消原因」，`Completed` 維持「作廢訂單/作廢原因」，共用同一個 `Void` 表單與稽核紀錄（`VoidReason`/`VoidedByEmployeeId`）。

**決策原因**

- 對照方案 A/B/C：選擇擴充既有 `Void` 動作（方案 A），統一「訂單不成立」的稽核入口，避免日後「取消已加點但未結帳的訂單」還要再開一條新流程；理由欄位維持必填以符合稽核要求（copilot-instructions 4.2 / 5）。

**驗證結果**

- `dotnet build` 成功。
- 已重建並重啟 Docker `web` 容器套用變更。

---

### [2026-09-23] 點餐介面優化：下拉選單改為分類頁籤 + 卡片點選

**變更內容**

- `Views/Order/Details.cshtml` 點餐/加點區塊改版：原本單一 `<select>` 品項下拉選單，改為依 `MenuCategory` 分組的頁籤（吃/喝/其他），每個分類下以卡片方式列出品項（顯示名稱與價格），點擊卡片跳出 Modal 輸入數量與備註後再送出。
- 僅修改 View 層（Razor + 內嵌 JS），`AddItem` 表單欄位（`MenuItemId`/`Quantity`/`Note`）與 `OrderController.AddItem`、`AddOrderItemCommand` 皆未變動，無 Controller / Model / DB 異動。

**決策原因**

- 品項一多，下拉選單需捲動查找，操作效率差且不直觀；改為分類頁籤 + 卡片點選，符合實體 POS 操作習慣，外場加點更快速。
- 選擇沿用既有 `AddItem` 單筆送出流程（而非批次一次送出多品項），維持最小改動、不動後端邏輯與資料流。

**驗證結果**

- `dotnet build` 成功，無編譯錯誤。
- 出單列印相關流程本次未變動（使用者反映紙張浪費疑慮，待與內部討論定案後再調整，暫不處理）。

---

### [2026-09-22] 出單列印模組實作（v3）+ 改為明確「確認出單」觸發

**變更內容**

- 新增 `Services/IReceiptPrinterService.cs`、`LanReceiptPrinterService.cs`：透過 raw TCP socket 直連印表機 `IP:9100`，內容以 Big5 編碼送出；新增 `Models/Options/PrinterOptions.cs` 與 `appsettings.json`/`appsettings.Development.json` 的 `Printer` 設定區塊（預設 `Enabled: false`）；`Program.cs` 註冊 `CodePagesEncodingProvider`（Big5 需要）與服務 DI。
- `Models/Entities/OrderItem.cs` 新增 `Note`（給廚房看的單項備註）與 `IsPrinted`（是否已隨出單列印過）兩個欄位，皆已建立 EF Core Migration 並套用至本機資料庫。
- `Controllers/OrderController.cs`：
  - `AddItem` 僅寫入品項（`IsPrinted = false`），**不再於加點當下自動列印**。
  - 新增 `ConfirmPrint`：由店長主動按「確認出單」按鈕，批次列印自上次確認後新增的所有品項（首次為「全單」，之後為「加點單」），成功後才標記 `IsPrinted = true`。
  - `Checkout` 新增安全網：結帳前若仍有未出單品項，自動嘗試補列印，避免忘記按「確認出單」導致廚房漏單；列印失敗僅顯示警告，不阻擋結帳。
  - `Void` 維持原本作廢時自動列印「作廢通知」（作廢本身已是明確的單一動作，不受本次調整影響）。
  - 新增 `Reprint`：手動重印全單，供印表機故障後備用。
- `Views/Order/Details.cshtml`：品項清單新增「待出單/已出單」badge、加點區塊新增「確認出單（N 項待出單）」按鈕；`_StatusMessage.cshtml` 新增 `TempData["Warning"]` 黃色警告樣式。
- `docs/prd/v3.md`：2.1 節更新為實際採用的 raw TCP socket 方案（非 Star SDK，考量 Linux Docker 容器可攜性）；2.2 節改為「確認出單」批次觸發模型；第 5 節技術確認紀錄同步更新。

**決策原因**

- 原規格「加點時自動列印」會導致每次加點都各自觸發一次列印，多品項訂單會產生大量零碎紙條（使用者反映「亂印」問題），改為使用者主動按「確認出單」統一批次列印較符合實際出餐流程（一次性把當下要送去廚房的品項一起交代清楚）。
- 為避免使用者忘記按「確認出單」導致品項從未送達廚房，於結帳動作加上自動補列印的安全網，僅在真正離開「未結帳」狀態前做最後把關，不影響原本「不阻擋操作」的失敗處理原則。
- 指令串接改採 raw TCP socket 直送（印表機自我測試頁已確認 `TCP#9100: ENABLE`），而非原規劃的 Star 官方 SDK：專案以 Docker Linux 容器部署（見 `docker-compose.prod.yml`），Star 官方 .NET SDK 主要面向 Windows/行動裝置平台，跨平台相容性不明；raw socket 方案功能等價且不受平台限制，風險更低。

**驗證結果**

- `dotnet build`、`dotnet test`（1 個測試）：成功，無 regression。
- `dotnet ef database update`：成功套用 `AddOrderItemNote`、`AddOrderItemIsPrinted` 兩個 migration。
- 尚未有實體印表機可連線（IP 未取得），列印功能僅完成程式邏輯與畫面，實際吐紙效果待印表機接上網路後由使用者實機驗證。

---

### [2026-09-22] 出單列印模組實機確認：改採 Star SDK + Big5 編碼

**變更內容**

- 實機列印印表機自我測試頁確認：LAN 功能正常（`ASB(LAN)`/`NSB(LAN)`: Valid）、字元模式為 `T-Chinese(Big5)`；測試當下尚未取得網路 IP（DHCP 未配發），待實際接上網路後續測連線。
- `docs/prd/v3.md`：2.1 節更新為確定採用 **Star 官方 SDK**（不賭 ESC/POS 相容模式）；新增「中文內容需轉換為 Big5 編碼」風險與因應；新增第 5 節「技術確認紀錄」。

**決策原因**

- 自我測試頁未明確列出 Emulation 欄位，無法 100% 確認 ESC/POS 相容模式是否可用；既然印表機已確認為 Star 原廠機種，直接採用官方 SDK 可跳過此不確定性，不需要再花時間賭測試。
- 字元模式為 Big5 而非 UTF-8，須在送出列印內容前做編碼轉換，否則中文品項名稱會亂碼，故列為需在開發時驗證的風險項目。

**驗證結果**

- 僅完成印表機規格確認與文件更新，尚未進入程式碼實作。

---

### [2026-09-22] 出單列印模組 PRD 定案（v3，尚未開發）

**變更內容**

- 新增 `docs/prd/v3.md`：出單列印模組 PRD（連線方式、觸發時機與內容、失敗處理、明確排除、風險與因應）。
- `BRIEFING.md`：開發階段清單新增本模組為待辦項目。

**決策原因**

- 印表機連線方式選定網路熱感印表機（LAN + ESC/POS），確認實際設備為 Star Micronics mC-Print3（MCP31L，LAN 介面），與方案相符；若機器僅支援原生 StarPRNT，開發時改用 Star 官方 SDK 串接，屬實作細節不影響規格。
- 出單聯內容原本規劃不含金額（僅供廚房參考），因使用者說明目前為店長一人兼點餐/出餐/收銀，改為建單/加點時皆列印訂單目前總金額，方便直接作為結帳依據；作廢通知維持不含金額（用途僅為通知廚房停止製作，非結帳用途）。
- 印表機離線不阻擋操作（僅顯示警告 + 提供補印按鈕），避免單點故障影響現場點餐運作。

**驗證結果**

- 僅完成 PRD 文件，尚未進入程式碼實作，無需編譯/測試驗證。
- 待開發前需在現場確認印表機是否已啟用 ESC/POS 相容模式（PRD 已列為風險項目，非阻塞）。

---

### [2026-09-21] 新增支出記錄（Expense）模組 + 營收報表淨利串接

**變更內容**

- 新增 `docs/prd/v2.md`：支出記錄模組 PRD（範圍、欄位、固定分類清單、明確排除項目）。
- 新增 `Models/Enums/ExpenseCategory.cs`（食材採購/水電瓦斯/房租/人事薪資/設備耗材/其他，固定 6 類）。
- 新增 `Models/Entities/Expense.cs`、`Models/Commands/ExpenseCommands.cs`（Create/Update，`decimal` 金額、`[StringLength]` 備註驗證）、`Models/ExpenseSearchViewModel.cs`。
- `Models/Extensions/EnumDisplayExtensions.cs`：新增 `ExpenseCategory` 中文顯示文字。
- `Data/AppDbContext.cs`：新增 `DbSet<Expense>`，設定 `Amount` 精度 `(10,2)`、`Note` 長度上限 200。
- 新增 EF Core Migration `AddExpense`，已在本機資料庫套用（`dotnet ef database update`）。
- 新增 `Controllers/ExpenseController.cs`（`[Authorize]`）：新增、依日期區間/分類查詢、編輯、刪除（刪除/編輯不做操作稽核，比照 PRD v2 決策）。
- 新增 `Views/Expense/Index.cshtml`、`Create.cshtml`、`Edit.cshtml`，刪除採用 `onsubmit="return confirm(...)"` 比照既有 Order 作廢的確認對話框慣例。
- `Controllers/ReportController.cs`、`Models/ReportViewModel.cs`、`Views/Report/Index.cshtml`：新增「本期支出總額」與「淨利（= 營收 − 支出）」，淨利為負時以紅字顯示。
- `Views/Shared/_Layout.cshtml`、`Views/Home/Index.cshtml`：導覽列與首頁儀表板新增「支出記錄」入口。

**決策原因**

- 分類清單固定為 6 類（不開放自訂），避免自由輸入造成報表統計失真；已與使用者確認清單內容。
- 支出的編輯/刪除不做操作者/原因稽核（與訂單作廢的高風險稽核需求不同），降低這輪開發複雜度，符合 PRD v2 明確排除項目。
- 沿用既有 Reservation/Menu 模組的 Controller/View 慣例（Command record + `[Authorize]` + `TempData["Success"]`），維持專案風格一致。

**驗證結果**

- `dotnet build`：成功。
- `dotnet ef migrations add AddExpense` + `dotnet ef database update`：成功建立 `Expenses` 資料表。
- `docker compose -f docker-compose.yml up -d --build`：重新建置並套用新程式碼。
- 實機以種子帳號登入測試：`/Expense`、`/Report`（含新增的總支出/淨利欄位）皆回應 200；實際透過表單新增一筆支出記錄後於列表正確顯示，測試資料已清除。
- `dotnet test`：1 個測試全數通過，無 regression。

---

### [2026-09-18] 新增正式環境（VPS）部署設定

**變更內容**

- 新增 `docker-compose.prod.yml`：獨立的正式環境部署檔（`web` + `db` + `caddy`），與既有本機開發用 `docker-compose.yml` 分開維護，避免互相干擾。
  - `db` 服務移除對外 port 映射（原開發檔的 `3306:3306` 僅保留於本機開發用檔案），僅供 `web` 透過 Compose 內部網路連線。
  - `web`／`db` 機密資訊（DB 密碼、`SeedAdmin` 帳密、網域、ACME 信箱）改由 `.env`（不進版控，已被 `.gitignore` 的 `*.env` 規則排除）以環境變數注入，新增 `.env.example` 作為範本。
  - `ASPNETCORE_ENVIRONMENT` 設為 `Production`。
  - 新增 `caddy` 服務作為反向代理，綁定 80/443，透過 `Caddyfile` 自動向 Let's Encrypt 申請憑證並轉發至 `web:8080`。
- 新增 `Caddyfile`，網域與憑證信箱皆透過 `{$DOMAIN}`/`{$ACME_EMAIL}` 環境變數帶入。
- `Program.cs`：
  - 新增 `AddDataProtection().PersistKeysToFileSystem(...)`，Key 目錄改為 `/app/keys`（可掛載 volume 持久化），解決容器重啟導致所有登入 session 失效的已知問題。
  - 新增 `UseForwardedHeaders`（信任 `X-Forwarded-For`/`X-Forwarded-Proto`），確保 Caddy 終止 TLS 後，ASP.NET Core 仍能正確判斷原始請求為 HTTPS，讓 Cookie 的 `SecurePolicy = SameAsRequest` 正確生效、並避免 `UseHttpsRedirection` 造成的重導向問題。
- `Dockerfile`：新增 `RUN mkdir -p /app/keys`，確保掛載 DataProtection Key volume 時沿用映像檔內既有目錄權限。
- 新增 `scripts/backup-db.sh`：以 `mysqldump` 備份正式環境資料庫，僅保留最近 7 天備份，供 VPS 排程 cron 呼叫。
- `.gitignore`：新增 `backups/` 排除規則。

**決策原因**

- 正式環境與本機開發環境分成兩份 compose 檔（而非用 override 疊加），避免 Compose 陣列型設定（如 `ports`）合併語意複雜、難以確保 DB port 確實被移除的風險。
- 反向代理選用 Caddy 而非 Nginx + Certbot：Caddy 內建自動 HTTPS 憑證管理，設定檔更精簡，適合小型單店家專案降低維運複雜度。
- DataProtection Key 目錄選在 `/app/keys`（沿用既有 `/app` 權限）而非容器內其他路徑，避免非 root 執行使用者對新掛載目錄無寫入權限的常見問題。

**驗證結果**

- `dotnet build`：成功，無編譯錯誤。
- `docker compose -f docker-compose.prod.yml config --quiet`：語法驗證通過（未設定 `.env` 時僅出現預期的環境變數未設定警告）。
- `docker compose -f docker-compose.yml config --quiet`：確認既有本機開發用設定未受影響，仍可正常解析。
- 尚未在實體 VPS 上完整驗證（憑證申請、防火牆規則、DNS 綁定），待實際租用主機後於現場執行。

---

### [2026-09-18] 安全性複查修正（OWASP Top 10 導向）

**變更內容**

- `Program.cs`：Cookie Authentication 補上 `HttpOnly`、`SecurePolicy = SameAsRequest`、`SameSite = Strict`、`ExpireTimeSpan = 8 小時` + `SlidingExpiration`；新增 `AddMemoryCache()`。
- `Controllers/AccountController.cs`：登入新增失敗次數鎖定機制（同帳號 5 次失敗鎖定 15 分鐘，以 `IMemoryCache` 儲存，鎖定訊息不透露剩餘嘗試次數或帳號是否存在）。
- `Models/Commands/MenuItemCommands.cs`、`OrderCommands.cs`、`ReservationCommands.cs`、`Models/LoginViewModel.cs`：補上 `[StringLength]`，與 DB 欄位長度上限對齊，避免超長輸入直接在 DB 層丟未處理例外。
- `docker-compose.yml`：`db` 服務新增 `MYSQL_USER`/`MYSQL_PASSWORD` 建立最小權限帳號 `mantingeats_app`（僅授權於 `ManTingEatsDb`，非 global），`web` 服務與 `appsettings.Development.json` 的連線字串改用此帳號取代 `root`。

**決策原因**

- Cookie `SecurePolicy` 選用 `SameAsRequest` 而非 `Always`：`Always` 會導致本機 HTTP 開發環境（`dotnet run` 無 HTTPS）無法正常登入；`SameAsRequest` 在正式環境走 HTTPS（`UseHttpsRedirection` 已強制）時會自動套用 Secure，開發環境不受影響。
- 登入鎖定以「帳號」為 key（而非 IP），因內部單店家工具情境下 IP 常見 NAT 共用會誤鎖多人；帳號鎖定的代價（可被用來針對已知帳號做 DoS）在此規模下可接受。
- DB 最小權限帳號改採 MySQL 官方 image 的 `MYSQL_USER`/`MYSQL_PASSWORD` 自動建立機制，避免手寫初始化 SQL script 增加維運複雜度。

**驗證結果**

- `dotnet build`、`dotnet test`：成功，無 regression。
- 重建 `db` 容器（`docker-compose down -v` + `up -d db`）套用新的最小權限帳號設定，`SHOW GRANTS` 確認 `mantingeats_app` 僅有 `ManTingEatsDb.*` 權限，無 global 權限。
- `dotnet ef database update` 用新帳號成功建表，確認權限足夠執行 DDL。
- 實機測試：連續 6 次錯誤密碼登入，第 6 次起正確回傳「登入嘗試次數過多」且不再查詢資料庫（伺服器 log 僅 5 筆 SELECT）；重啟服務清除鎖定快取後，正確密碼登入成功（302 導向）。

---

### [2026-09-18] 修正 MySQL 資料無持久化問題 + 完整 Docker Compose 驗證

**變更內容**

- `docker-compose.yml`：`db` 服務新增具名 volume `mysql-data:/var/lib/mysql`；移除已棄用的 `version: '3.8'` 屬性。
- 執行 `docker-compose up -d --build` 完整啟動 `web` + `db` 兩個服務（首次完整驗證，先前僅驗證過 `db` 單獨啟動）。

**決策原因**

- 完整 Docker Compose 驗證時發現：原本 `db` 服務未掛載任何 volume，資料庫資料存在容器可寫層；執行 `docker-compose down` 後容器被移除，資料庫內所有資料（含種子帳號、菜單、訂單、訂位測試資料）全數遺失。這在正式環境會是嚴重的資料遺失風險，故立即修正。
- 同時發現 Cookie Authentication 的 DataProtection Key 未持久化（`/root/.aspnet/DataProtection-Keys` 僅存在容器內），容器重啟會導致所有登入 session 失效；影響僅為需重新登入，非資料遺失，列入待辦、非本次必要修正。

**驗證結果**

- 修正後重新執行 `docker-compose up -d db` + `dotnet ef database update` 重建資料表。
- 執行 `docker restart mantingeats-db-1` 後以 `SHOW TABLES` 確認資料表仍存在，證實 volume 持久化生效。
- 執行 `docker-compose up -d --build` 完整啟動 `web`+`db`，`GET http://localhost:8080/Account/Login` 回應 200，確認容器化服務可正常運作並連線資料庫。
- 驗證後已 `docker-compose stop web`，僅保留 `db` 容器供本機 `dotnet run` 開發使用。

---

### [2026-09-18] Reporting 模組實作（MVP 五大模組全部完成）

**變更內容**

- 新增 `Models/ReportViewModel.cs`（`ChannelRevenue`/`MenuItemSales`/`ReportViewModel`）。
- 新增 `Controllers/ReportController.cs`（`[Authorize]`）：依日期區間查詢總營收、通路拆分、品項销售排行（依金額降序）。
- 新增 `Views/Report/Index.cshtml`：自訂日期區間表單 + 今日/本週/本月快速篩選。
- 導覽列與首頁儀表板新增「營收報表」連結；首頁儀表板卡片由 `col-md-4` 調整為 `col-md-3` 以容納四個模組入口。

**決策原因**

- 報表統計基準日期采 `Order.CompletedAt`（而非 `CreatedAt`），符合「營收場時點」的語意，且只有 `Completed` 狀態訂單才會有 `CompletedAt`。
- 查詢四層篷選僅限 `Status == Completed`，符合 PRD「報表不含已作廢訂單」的驗收標準。
- 今日/本週/本月快速篩選直接在 View 端計算日期並產生連結，不引入額外 JS 依賴。

**驗證結果**

- `dotnet build`、`dotnet test`：成功，無 regression。
- 實機測試：使用者使用先前建立的訂單資料驗證總營收、通路拆分、品項排行數字正確，確認無誤。

---

### [2026-09-18] 全站 UI/UX 第一輪優化

**變更內容**

- 新增 `Views/Shared/_StatusMessage.cshtml`，統一顯示 `TempData["Success"]`/`TempData["Error"]`，並在 `_Layout.cshtml` 引入，取代原本僅 Order 頁面才有的錯誤提示。
- `MenuController`/`OrderController`/`ReservationController` 的寫入動作（新增、編輯、上下架、加點、移除、結帳、作廢、訂位建立）補上 `TempData["Success"]` 成功訊息。
- `Views/Shared/_Layout.cshtml`：導覽列依目前 Controller 加上 `active` 高亮樣式；移除無關的預設 Privacy 連結。
- `wwwroot/css/site.css` 新增少量自訂樣式（導覽列 active 樣式、頁首間距、dashboard 卡片 hover 效果、表格垂直置中）。
- `Views/Home/Index.cshtml` 改為登入後的模組導覽儀表板（卡片連結至 Menu/Order/Reservation），未登入則顯示登入提示。
- Menu/Order/Reservation/Account 的 Index、Create、Edit、Details 頁面全面改用 Bootstrap `card` 包裝，狀態改用彩色 `badge`（Menu 上下架、Order 三種狀態），金額統一 `$` 前綴格式化；高風險操作（移除品項、結帳、作廢）加上 `confirm()` 對話框；清單為空時顯示提示文字而非空白表格。

**決策原因**

- 使用者反映原本純預設 Bootstrap 樣式的表格/表單「超級陽春」，且缺乏操作回饋（例如新增成功後沒有任何提示）。
- 沿用既有 Bootstrap 5（已內建於 `wwwroot/lib`），不引入額外前端套件/CDN，避免不必要的相依性與 OWASP 風險（第三方 CDN 資源）。
- 高風險操作（結帳、作廢、移除）加確認對話框，降低誤觸風險，屬低成本高效益的 UX 改善。

**驗證結果**

- `dotnet build`、`dotnet test`：成功，無 regression。
- 實機測試：使用者確認菜單/訂位頁面滿意；Order 相關畫面仍反映部分不順手，列入待辦，本輪先不繼續深修。

---

### [2026-09-18] Order 模組實作

**變更內容**

- 新增 `Models/Commands/OrderCommands.cs`（`CreateOrderCommand`/`AddOrderItemCommand`/`VoidOrderCommand`）、`Models/Extensions/EnumDisplayExtensions.cs`（中文化顯示 `OrderChannel`/`OrderStatus`）、`Models/OrderDetailsViewModel.cs`。
- 新增 `Controllers/OrderController.cs`（`[Authorize]`）：`Index`、`Create`、`Details`、`AddItem`、`RemoveItem`、`Checkout`、`Void`。
- 新增 `Views/Order/Index.cshtml`、`Create.cshtml`、`Details.cshtml`。
- `Views/_ViewImports.cshtml` 新增 `ManTingEats.Models.Enums`/`ManTingEats.Models.Extensions` using；`Views/Shared/_Layout.cshtml` 導覽列新增「訂單」連結。

**決策原因**

- 狀態機嚴格按 PRD 2.2：`Open`（可加點）→ `Completed`（鎖定）→ `Voided`（僅限已結帳訂單作廢），不提供 Open 訂單直接作廢/取消的入口（PRD 未提到此需求）。
- `AddItem`/`RemoveItem` 均在單次 `SaveChangesAsync` 內完成品項變更與總額重新計算，符合「多表更新需在同一 transaction」規範。
- `OrderItem.UnitPrice` 於加點當下從 `MenuItem.Price` 快照，不受後續調價影響（已符合 Entity 設計）。
- 作廢需輸入原因且記錄操作者 `VoidedByEmployeeId`，對應 PRD 稽核需求。
- 錯誤處理采 `TempData["Error"]` + redirect 回 Details 頁，避免重建完整 ViewModel 的額外複雜度（內部管理工具，非面對大量使用者的需求）。

**驗證結果**

- `dotnet build`、`dotnet test`：成功，無 regression。
- 實機測試：使用者實際跑過建單、加點、結帳、作廢流程並確認通過。

---

### [2026-09-18] 修正 Menu record Command 驗證屬性位置錯誤

**變更內容**

- `Models/Commands/MenuItemCommands.cs`：移除 `CreateMenuItemCommand`/`UpdateMenuItemCommand` 上 `[property: ...]` 屬性標註的 `property:` target，改為直接標註在 primary constructor 參數上。

**決策原因**

- ASP.NET Core MVC 對 record 型別驗證要求 Data Annotations 必須關聯到建構子參數，而非產生的 property；`[property: ...]` 語法導致執行期丟出 `InvalidOperationException`（新增菜單品項時實測發現）。

**驗證結果**

- `dotnet build`：成功。
- 實機測試：登入、新增菜單品項功能皆正常運作（使用者手動驗證通過）。

---

### [2026-09-18] Auth 最小可用登入 + Menu CRUD 實作

**變更內容**

- `Program.cs`：新增 Cookie Authentication（`LoginPath=/Account/Login`）與 `AddAuthorization()`；註冊 `IPasswordHasher<Employee>`；啟動時呼叫 `DbSeeder.SeedManagerAsync`。
- 新增 `Data/DbSeeder.cs`：啟動時若 `Employees` 表為空，從設定檔 `SeedAdmin:Username`/`SeedAdmin:Password` 種一組 Manager 帳號（密碼雜湊儲存）；`appsettings.json` 留空、`appsettings.Development.json` 提供開發用預設值。
- 新增 `Models/LoginViewModel.cs`、`Controllers/AccountController.cs`（Login/Logout）、`Views/Account/Login.cshtml`。
- 新增 `Models/Commands/MenuItemCommands.cs`（`CreateMenuItemCommand`/`UpdateMenuItemCommand`）、`Controllers/MenuController.cs`（Index/Create/Edit/ToggleActive，皆加 `[Authorize]`）、對應 Views。
- `Views/Shared/_Layout.cshtml` 新增導覽列：已登入顯示帳號與登出、菜單管理連結；未登入顯示登入連結。

**決策原因**

- 密碼雜湊採用 ASP.NET Core 共用框架內建的 `PasswordHasher<TUser>`，不引入完整 ASP.NET Core Identity 系統（PRD 僅需帳密登入，不需要完整使用者管理功能），符合「不過早抽象」原則。
- 種子帳號採「啟動時檢查空表 + 設定檔給值」（選項 A），避免帳密寫死於程式碼或 Migration；正式環境未設定 `SeedAdmin` 時會直接略過種子邏輯，不會產生預設密碼風險。
- Menu 僅支援 Create/Edit/上下架，不做刪除功能，對應 PRD 2.1「已被歷史訂單使用過的品項禁止硬刪除」的驗收標準（本階段尚無 Order，故所有品項皆不提供刪除入口，行為一致且簡單）。
- `MenuItemCommands.cs` 採 `sealed record` + Data Annotations，符合 4.1 節 Command 命名慣例。

**驗證結果**

- `dotnet build`：成功。
- `dotnet test tests/ManTingEats.Tests/ManTingEats.Tests.csproj`：成功，1 個測試通過（無 regression）。
- 實機啟動驗證：種子帳號成功寫入 `Employees` 表；`GET /Account/Login` 回 200；未登入存取 `GET /Menu` 正確 302 導向登入頁，確認 `[Authorize]` 生效。

---

### [2026-09-18] 新開發機環境建置與首次 Migration 套用

**變更內容**

- 於新電腦上安裝 Docker Desktop（含 WSL2）與 `dotnet-ef` 全域工具。
- 執行 `docker-compose up -d db` 啟動本機 MySQL 容器。
- 首次執行 `dotnet ef database update`，將 `20260916081447_InitialCreate` Migration 實際套用到資料庫，建立 `Employees`、`MenuItems`、`Reservations`、`Orders`、`OrderItems` 五張表。
- 未變更任何程式碼或設定檔。

**決策原因**

- 上次（2026-09-16）僅產生 Migration 檔案，尚未實際執行 `database update`；本次為換機重建環境時補做，屬於延續性待辦事項，非新決策。

**驗證結果**

- `dotnet restore`、`dotnet build`：成功。
- `dotnet test tests/ManTingEats.Tests/ManTingEats.Tests.csproj`：成功，1 個測試通過。
- `dotnet ef database update`：成功，資料表已建立於本機 MySQL 容器（`ManTingEatsDb`）。

---

### [2026-09-16] Phase C：資料模型與 EF Core 建置

**變更內容**

- 新增 ADR：[docs/adr/20260916-orm-selection.md](../docs/adr/20260916-orm-selection.md)，決定採用 EF Core（Pomelo.EntityFrameworkCore.MySql provider）。
- `ManTingEats.csproj` 新增套件：`Pomelo.EntityFrameworkCore.MySql`、`Microsoft.EntityFrameworkCore.Design`、`Microsoft.EntityFrameworkCore.Tools`（皆鎖定 9.0.0，對齊 net9.0）。
- 新增 Entities：`Models/Entities/Employee.cs`、`MenuItem.cs`、`Order.cs`、`OrderItem.cs`、`Reservation.cs`；Enums：`Models/Enums/EmployeeRole.cs`、`OrderChannel.cs`、`OrderStatus.cs`。
- 新增 `Data/AppDbContext.cs`，含關聯設定（Order-Employee、Order-OrderItem、OrderItem-MenuItem）與欄位長度/精度限制。
- `Program.cs` 註冊 `AppDbContext`，連線字串改由設定檔讀取。
- `appsettings.json` 新增空白 `ConnectionStrings:DefaultConnection`（正式環境另行管理）；`appsettings.Development.json` 補上本機開發用連線字串（對應 docker-compose 的 MySQL）。
- `docker-compose.yml` 的 `web` 服務新增 `ASPNETCORE_ENVIRONMENT=Development` 與 `ConnectionStrings__DefaultConnection`（指向服務名稱 `db`，區別於本機直接執行 `dotnet run` 時使用的 `localhost`）。
- 新增初版 Migration：`Migrations/20260916081447_InitialCreate.cs`。
- 更新 `copilot-instructions.md` 第 4 節，補上實際資料夾慣例（4.4 節）；更新 `BRIEFING.md` 勾選進度。

**決策原因**

- ORM 選型理由見上述 ADR。
- `OrderItem.UnitPrice` 採快照設計（下單當下寫入售價），避免菜單日後調價影響歷史訂單金額，符合報表資料一致性要求。
- `Order.TableNumber` 為單純字串欄位（非正式桌位管理），對應 PRD 2.2 的加點識別需求。
- 目前不建立 Repository/Service 層，Controller 未來會直接使用 `AppDbContext`；待後續模組數量增加、業務邏輯變複雜時再抽出 Service 層，避免現階段過度設計。

**驗證結果**

- `dotnet build ManTingEats.csproj`：成功。
- `dotnet test tests/ManTingEats.Tests/ManTingEats.Tests.csproj`：成功，1 個測試通過。
- Migration 已產生對應的資料表結構（`Employees`、`MenuItems`、`Orders`、`OrderItems`、`Reservations`），尚未實際對資料庫執行 `dotnet ef database update`（建議下次啟動 `docker-compose up` 後執行）。

---

### [2026-09-16] Phase B：PRD 需求盤點定案

**變更內容**

- 完成與使用者的需求訪談，產出 [docs/prd/mvp.md](../docs/prd/mvp.md)，涵蓋 Menu、Order、Reservation、Reporting、Auth 五大模組的使用者故事與驗收標準。
- 更新 `.github/copilot-instructions.md` 第 7.1／7.2 節，新增 PRD 文件的必讀清單項目與維護規則。
- 更新 `.github/BRIEFING.md`：專案定位改為已確認版本、開發階段勾選 PRD 完成、移除已解決的待確認問題。

**決策原因**

- PRD 屬於產品文件，範本規則未涵蓋此類型，決定放在 `docs/prd/` 資料夾（比照技術文件慣例），以資料夾＋版本檔名管理，避免未來模組擴充時覆蓋歷史版本。
- 確定 MVP 範圍：不含庫存管理、外送 API 串接、多元支付、發票、桌位衝突檢查、訂位提醒、結帳找零計算，避免過度設計。
- 內用訂單加入「桌號」欄位以解決加點時無法辨識訂單對象的風險。

**驗證結果**

- 本次僅為文件變更，無程式碼異動，無需重新建置/測試。

---

### [2026-09-16] Phase A：工程基礎建設

**變更內容**

- 修正 `Dockerfile` 的 SDK/Runtime image 版本，由 8.0 改為 9.0，與 `ManTingEats.csproj` 的 `net9.0` 對齊。
- 新增 `.gitignore`、`.editorconfig`、`README.md`。
- 新增 `.github/copilot-instructions.md`（依團隊提供範本改編）、`.github/BRIEFING.md`、本文件 `.github/MEMORY.md`。
- 新增測試專案骨架 `tests/ManTingEats.Tests`（xUnit）。

**決策原因**

- Dockerfile 與 csproj 版本不一致會導致容器建置/執行期不相容，優先修正。
- 專案目前處於範本階段，先建立文件與規範骨架，讓後續 PM／工程師 Agent 的產出有一致依循基準。
- 暫緩建立 `CONTRIBUTING.md`、`docs/architecture.md`、`docs/api-spec.md`、`docs/adr/`，避免產生無內容的空殼文件，待對應階段有實際內容時再建立。

**驗證結果**

- `dotnet build ManTingEats.csproj`：成功。
- `dotnet test tests/ManTingEats.Tests/ManTingEats.Tests.csproj`：成功，1 個測試通過。
- 修正過程中發現：`ManTingEats.csproj` 預設會遞迴 glob 到 `tests/` 下的原始碼，導致與測試專案重複編譯（`CS0579`）並缺少 `xunit` 套件（`CS0246`）。已在 `ManTingEats.csproj` 加入 `<Compile Remove="tests/**/*.cs" />` 等排除規則修正。
- `docker-compose up` 尚未實機驗證（本機無 Docker 執行環境可驗證，建議使用者本機執行 `docker-compose up --build` 確認）。
