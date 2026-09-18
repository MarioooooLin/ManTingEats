# MEMORY — 決策與變更紀錄

> 每次完成可能影響架構、規範、API 或開發流程的變更後，依下列格式在本文件**最上方**新增一筆條目（最新在前）。格式規範見 [copilot-instructions.md](copilot-instructions.md) 第 7.3 節。

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
