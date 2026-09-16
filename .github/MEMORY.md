# MEMORY — 決策與變更紀錄

> 每次完成可能影響架構、規範、API 或開發流程的變更後，依下列格式在本文件**最上方**新增一筆條目（最新在前）。格式規範見 [copilot-instructions.md](copilot-instructions.md) 第 7.3 節。

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
