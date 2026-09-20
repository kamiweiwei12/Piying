# CHANGELOG

《影韵》（Unity 2D 皮影戲題材節奏遊戲）專案更新日誌。

## 使用規則（重要）

1. **只記錄「真正完成並通過驗收」的工作**。閱讀、分析、提出方案**不算完成**，不得寫成本條目。
2. 每個里程碑完成時新增一筆，標題格式：`## [YYYY-MM-DD] <Milestone> - <標題>`。
3. 「完成」的定義見 `DEVELOPMENT.md` 附錄 E：必須 **實作 + 測試 + 驗收 + 文件 + Git commit** 全部完成。
4. 尚未 commit 的工作，`Git Commit` 欄位必須寫「**待提交**」，且不得標記為完成。
5. 每個條目必須包含以下欄位：

```
### 新增
### 修改
### 測試
### 驗收結果
### Git Commit
### 風險 / 已知問題
### 下一步
```

最新條目置於最上方。

---

## [2026-09-20] Infra-1 - 工程管理基礎設施（Git 規範 / CHANGELOG / DEVELOPMENT）

### 新增

* `.gitignore`：Unity 專案版控規則，忽略 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`obj/`、`Build/`、`Builds/`、`.vs/`、IDE 暫存檔與建置產物；
  明確保護 `Assets/`、`Packages/`、`ProjectSettings/`、`.cline/`、`.clinerules/`、`*.meta`。
* `CHANGELOG.md`：本檔，含使用規則與條目格式。
* `DEVELOPMENT.md`：本專案長期開發規範（目標、階段、里程碑 M0–M7、Git/CHANGELOG/Skill 規則、
  最小修改原則、測試規則、回報格式、風險管理、禁止事項，以及每次工作的強制流程 STEP 0–7）。

### 修改

* 無。（未修改任何 Unity 專案檔，未修改 `Assets/`、`Packages/`、`ProjectSettings/`）

### 測試

* 無程式測試（本條目僅新增文件）。已驗證：Git 未初始化（無 `.git`，上層目錄亦無 repo）；
  `git config --global user.name` / `user.email` 已設定，具備 commit 條件。

### 驗收結果

* ✅ 三份文件建立完成且可讀
* ⏳ **尚未驗收**：`git init` 與首次 baseline commit 未執行（依規定需使用者批准 → 見 `DEVELOPMENT.md` 附錄 F）
* ⚠️ 依 §11 定義，本條目**尚未構成完成的里程碑**，Git Commit 欄位為「待提交」

### Git Commit

* 待提交（Repo 尚未初始化）

### 風險 / 已知問題

* Git 尚未初始化 → 目前任何修改都無法回溯。
* `init.defaultBranch` 未設定（建議以 `git init -b main` 初始化）。
* `core.autocrlf` 未設定 → 建議後續評估加入 `.gitattributes`（尚未建立，需批准）。
* 本專案無任何 `.unity` 場景檔（`Assets/Scenes/` 為空，`EditorBuildSettings` 仍指向不存在的 `SampleScene.unity`）。

### 下一步

* 取得批准後執行 `git init -b main` 並建立 baseline commit（涵蓋現有 Unity 專案 + Cline Skills/Rules）。
* 之後才可進入 M0（時鐘行為實測）。

---

## [2026-09-20] Infra-0 - Cline Skills 與 Rules 建置（前置工程）

### 新增

* `.cline/skills/` 共 **10 個 Skill**：
  * 專案專屬：`unity-rhythm-timing`（含 4 份 references：timing-and-calibration / architecture-decoupling /
    judgment-and-scoring / chart-format-and-timeline）
  * 通用（來源 `gamedev-skills/awesome-gamedev-agent-skills`，Apache-2.0）：
    `unity-csharp-scripting`、`unity-input-system`、`unity-animation`、`unity-scriptableobjects`、
    `unity-build-pipeline`、`game-ui-ux`、`game-feel`、`audio-design`、`camera-systems`
* `.clinerules/` 共 **3 份 Rule**：`01-project-context.md`、`02-unity-conventions.md`、`03-guardrails.md`

### 修改

* 無。（未修改任何 Unity 專案資產）

### 測試

* 以程式解析每個 `SKILL.md` 的 YAML frontmatter：資料夾名 = `name`（10/10 通過）、
  description 長度 351–554 字（上限 1024，全數通過）、每個 Skill 均含 `SKILL.md` 與 `references/`。

### 驗收結果

* ✅ 路徑符合 Cline 4.1.19 掃描規則（`.cline/skills/`）
* ✅ 未產生任何 `Assets/` 檔案變更（2 小時內 `Assets/` 變更數 = 0、新增 `.cs` = 0）
* ⏳ Cline UI 內的 Skills 啟用與 `/unity-rhythm-timing` 手動調用**待使用者於 UI 確認**

### Git Commit

* 無（當時尚未初始化 Git）

### 風險 / 已知問題

* Skills 功能在 Cline 中仍標示 experimental，需於 Settings → Features 啟用。
* 通用 Skill 基準版本為 Unity 6.3 LTS，本專案為 6000.6.2f1，少數 API 需以專案實況驗證。

### 下一步

* 建立工程管理基礎設施（已於 Infra-1 執行）。
