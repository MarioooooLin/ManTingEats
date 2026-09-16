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
- [ ] Menu / Order / Reservation / Reporting 的 Controller、Service、View 實作（下一步）
- [ ] 店長登入（Auth）UI 與流程實作

## 已知待確認問題

- 目前尚無 Repository/Service 層的實作，Controller 直接使用 `AppDbContext` 尚可行；待模組數量增加後視情況再抽出 Service 層，避免過早抽象。

## 相關文件

- [copilot-instructions.md](copilot-instructions.md) — 開發規範
- [MEMORY.md](MEMORY.md) — 決策與變更紀錄
