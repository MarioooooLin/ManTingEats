# CLAUDE.md

本專案的開發規範與背景都在 `.github/` 底下，Claude Code 開始工作前請先閱讀：

1. **`.github/MEMORY.md` 最上方的「目前進度與下一步」**：上次停在哪裡、接下來要做什麼、待使用者決定的事項。
2. `.github/copilot-instructions.md`：開發規範（命名、架構、Commit 格式、文件維護義務）。所有規範同樣適用於 Claude Code。
3. `.github/BRIEFING.md`：專案定位、技術棧、開發階段。
4. `docs/prd/`：各版本需求規格。

## 溝通與工作慣例

- 一律使用**繁體中文**與使用者溝通，程式碼註解也用繁體中文，說明「為什麼」而不只是「做什麼」。
- 每次程式碼變更後，依 `copilot-instructions.md` 7.3 節在 `MEMORY.md` 最上方新增紀錄；commit 分兩個：功能本身＋`docs(memory): ...`。
- Commit／push 前先詢問使用者。

## 本機開發環境

```sh
docker compose up -d --build                  # 啟動 web(:8080) + MySQL；啟動時自動 Migrate、建立 admin/123 帳號
sh scripts/dev-data/import-menu-seed.sh       # 匯入測試菜單與加料（新電腦第一次啟動後執行）
dotnet build && dotnet test tests/ManTingEats.Tests
```

- Windows 上用 **Git Bash** 執行 `.sh` 腳本與含中文的重新導向；PowerShell 5.1 的 `<`／`>` 會破壞 UTF-8 中文。
- 讓 iPad 從外部連到本機網站（測試用）：
  `docker run -d --name mantingeats-tunnel --network mantingeats_default cloudflare/cloudflared:latest tunnel --no-autoupdate --url http://web:8080`
  再用 `docker logs mantingeats-tunnel` 找 `https://xxx.trycloudflare.com` 網址。快速通道網址會變、可能被收回；**網址是公開的且帳密為 admin/123，測完務必 `docker rm -f mantingeats-tunnel`**。
- `wwwroot/dev/` 為開發測試頁，僅 Development 環境可存取（正式環境回 404）。

## 注意事項

- **GitHub repo 為公開**：不得 commit 任何真實顧客／員工資料、正式環境密碼或 `.env`。
- 資料庫中的訂位、訂單、員工資料可能含個人資料，非必要不要讀取或匯出。
