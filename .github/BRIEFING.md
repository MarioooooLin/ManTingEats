# BRIEFING — ManTingEats

## 專案定位

單店家餐廳管理系統。核心情境：

- 店家前台點餐（內用含桌號／外帶）
- 記錄訂位（純登記查詢）
- 後台編輯菜單
- 查看營收
- 庫存管理列為未來擴充，本次 MVP 不實作

> 已確認為店家內部操作系統（無顧客自助點餐介面），僅店長操作，未來可能新增服務生角色。詳細規格見 [docs/prd/mvp.md](../docs/prd/mvp.md)。

## 技術棧

| 項目     | 內容                                                                                                                                       |
| -------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| 框架     | ASP.NET Core 9.0 (MVC)                                                                                                                     |
| 資料庫   | MySQL 8.0（Docker Compose 提供）                                                                                                           |
| ORM      | **EF Core**（Pomelo.EntityFrameworkCore.MySql provider），詳見 [docs/adr/20260916-orm-selection.md](../docs/adr/20260916-orm-selection.md) |
| 容器化   | Docker / Docker Compose                                                                                                                    |
| 站台數量 | 單一 Web 站台，非多站台架構                                                                                                                |
| 多語系   | 目前僅繁體中文（zh-TW），未規劃多語系機制                                                                                                  |

## 目前開發階段

- [x] 修正 Dockerfile 與 csproj 的 .NET 版本不一致（8.0 → 9.0）
- [x] 建立 `.gitignore`、`.editorconfig`、`README.md`
- [x] 建立 `.github/copilot-instructions.md`、本文件、`.github/MEMORY.md`
- [x] 建立測試專案骨架 `tests/ManTingEats.Tests`
- [x] PRD 需求盤點與模組拆解（見 [docs/prd/mvp.md](../docs/prd/mvp.md)）
- [x] ORM 選型：EF Core（見 ADR）、Entities（Employee/MenuItem/Order/OrderItem/Reservation）、`AppDbContext`、初版 Migration、DI 註冊
- [x] 首次實際執行 `dotnet ef database update`，資料表結構已落地
- [x] 店長登入（Auth）：Cookie Authentication、`PasswordHasher<Employee>` 雜湊、啟動時種子 Manager 帳號（`SeedAdmin` 設定）
- [x] Menu 模組 CRUD（新增/編輯/上下架，不支援硬刪除），`[Authorize]` 保護
- [x] Order 模組：建單、加點/移除品項、結帳、作廢（狀態機 Open → Completed → Voided）
- [x] Reservation 模組：新增訂位、依姓名/日期查詢
- [x] 全站 UI/UX 第一輪優化：卡片化、狀態 badge、全域成功/錯誤訊息、高風險操作加確認對話框
- [x] Report 模組：指定日期區間總營收、通路拆分、品項销售排行（今日/本週/本月快速篩選），僅計入已結帳訂單
- [x] 完整 Docker Compose 驗證（`web`+`db`）：修正 `db` 服務缺少 volume 導致資料無持久化的問題，並補上具名 volume `mysql-data`
- [x] 安全性複查修正：Cookie 硬化（HttpOnly/SecurePolicy/SameSite/有效期限）、登入失敗鎖定機制、Command 輸入長度上限驗證、資料庫改用最小權限帳號（非 root）
- [x] 正式環境部署設定：新增 `docker-compose.prod.yml`（web + db + Caddy 反向代理自動 HTTPS）、機密改用 `.env` 注入、DataProtection Key 持久化、`ForwardedHeaders` 中介軟體、資料庫備份腳本
- [x] 支出記錄（Expense）模組（見 [docs/prd/v2.md](../docs/prd/v2.md)）：新增/編輯/刪除支出、依日期區間與分類查詢；營收報表新增本期支出與淨利
- [ ] 實際租用 VPS 主機與網域，於現場完成部署（防火牆規則、DNS 綁定、憑證申請驗證）並執行平板點餐實地演練
- [ ] 出單列印模組（見 [docs/prd/v3.md](../docs/prd/v3.md)）：**方向已改為「iPad USB 直連 MCP31L + Star PassPRNT App」**（主機在雲端、店內無法拉線讓印表機上網，原 LAN 直連方案不適用）。iPad 已可透過 PassPRNT 以 USB 連上印表機；正以 `wwwroot/dev/passprnt-test.html` 實測網頁呼叫 PassPRNT 的行為，結果確認後再撰寫 `docs/prd/v5.md` 並改寫列印流程。詳見 MEMORY.md 最上方「目前進度」
- [x] 點餐客製化模組（見 [docs/prd/v4.md](../docs/prd/v4.md)）：Model/Migration/Controller/View 已實作（全店共用加料清單、五級辣度、客製化品項數量鎖定 1），已套用至本機開發資料庫，尚待瀏覽器手動實測與 UI/UX 細節優化
> PRD MVP 五大模組（Auth/Menu/Order/Reservation/Reporting）均已實作並實機驗證。

## 已知待確認問題

- 目前尚無 Repository/Service 層的實作，Controller 直接使用 `AppDbContext` 尚可行；待模組數量增加後視情況再抽出 Service 層，避免過早抽象。
- Order 相關畫面（訂單詳情、加點）UI/UX 使用者反映仍有部分不順手之處，已做過一輪卡片化/badge/確認對話框優化，細節待後續再調整。
- Cookie Authentication 的 DataProtection Key 未持久化，容器重啟會導致所有登入 session 失效（僅需重新登入，非資料遺失），待後續視需要掛載 volume 持久化。
- 登入失敗鎖定機制目前以 `IMemoryCache` 實作（純記憶體，服務重啟會清除鎖定狀態），若未來多實例部署需改用分佈式快取（如 Redis）。
## 相關文件

- [copilot-instructions.md](copilot-instructions.md) — 開發規範
- [MEMORY.md](MEMORY.md) — 決策與變更紀錄
