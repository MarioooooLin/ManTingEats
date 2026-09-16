# ManTingEats

單店家餐廳管理系統：店家前台點餐、記錄訂位，後台可編輯菜單、查看營收與庫存。

## 技術棧

- ASP.NET Core 9.0 (MVC)
- MySQL 8.0（透過 Docker Compose 提供）
- Docker / Docker Compose

## 目前開發階段

早期工程基礎建設階段，尚未實作業務邏輯（Menu / Order / Reservation / Inventory / Reporting / Auth 模組規劃中）。
詳見 [.github/BRIEFING.md](.github/BRIEFING.md)。

## 本機啟動方式

### 使用 Docker Compose（含 MySQL）

```powershell
docker-compose up --build
```

- Web 服務：http://localhost:8080
- MySQL：localhost:3306（帳密見 `docker-compose.yml`，僅供本機開發使用）

### 直接使用 .NET SDK（不含資料庫）

```powershell
dotnet run
```

## 執行測試

```powershell
dotnet test
```

## 專案文件

- [.github/copilot-instructions.md](.github/copilot-instructions.md) — 開發規範與 AI 協作準則
- [.github/BRIEFING.md](.github/BRIEFING.md) — 專案定位、技術棧、開發階段
- [.github/MEMORY.md](.github/MEMORY.md) — 重要決策與變更紀錄
