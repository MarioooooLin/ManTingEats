# ADR: 選用 EF Core 作為 ORM

- 狀態：已採納
- 日期：2026-09-16

## 背景

ManTingEats 需要一套 ORM 存取 MySQL 8.0。候選方案為 EF Core 與 Dapper。

## 決策

採用 **EF Core**（搭配 Pomelo.EntityFrameworkCore.MySql provider）。

## 理由

- 專案為標準 CRUD 為主（Menu、Order、Reservation）＋少量報表彙總查詢，資料量以單店規模來說不大，EF Core 的開發效率優勢明顯大於效能劣勢。
- 內建 Migrations，資料表結構變更可版控追蹤，不需手寫 SQL 腳本。
- 多表 transaction（例如建立 Order 同時寫入多筆 OrderItem）可用 `DbContext.SaveChangesAsync()` 搭配 EF Core 內建 transaction 處理，符合 `copilot-instructions.md` 第 4.2 節「多表更新必須在同一個 transaction 內完成」的規範。
- 若未來報表查詢效能不足，仍可用 EF Core 的 `FromSqlRaw`/Raw SQL 混用，不需整套替換為 Dapper。

## 替代方案（已排除）

- **Dapper**：效能較佳、SQL 可控性高，但沒有內建 Migrations，資料表變更需手寫 SQL 腳本，且樣板程式碼較多，對目前團隊規模（單一開發者/資淺維運）開發效率較低。

## 影響

- 新增 NuGet 套件：`Microsoft.EntityFrameworkCore`、`Pomelo.EntityFrameworkCore.MySql`、`Microsoft.EntityFrameworkCore.Design`（Migrations 工具用，不會進入執行期）。
- 資料存取層以 EF Core `DbContext` 為核心，Repository（如需要）僅作為 DbContext 查詢的薄封裝，不引入額外的資料存取抽象。
