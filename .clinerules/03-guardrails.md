# 協作護欄（Guardrails）

## 一律先問、不要自己動手的事

1. **刪除或移動任何既有檔案**（包含 Platformer 模板的程式、Prefab、Scene、資產）。
   看到確定無用的程式碼時 → 先列出清單與理由，等使用者決定。
2. **任何會改變 Unity 專案設定的動作**：
   `Packages/manifest.json`、`ProjectSettings/*`（含 Render Pipeline、圖形、品質、輸入、物理設定）、
   新增 asmdef、切換 URP Renderer（Forward → Renderer2D）、`AudioSettings.Reset()` 之類的執行期全域設定。
3. **建立或改寫既有場景（Scene）與 Prefab**：目前 `Assets/Scenes/` 是空的，任何場景建立都算重大變更，需先確認。
4. **大量新增資產**（圖片、音訊、字型、外部套件、Unity MCP 之類的工具鏈）。
5. **重構既有 Platformer 程式碼**（即使只是改名或換命名空間）。

## 變更前後的報告要求

- 動工前：說明「要改哪些檔案／為什麼／有什麼風險」。
- 動工後：列出實際變更檔案清單、驗證方式（編譯、Play、或測試）、以及未解的問題。
- 若同一件事有多種做法，先給選項與建議，不要自行挑一個大改。

## 現在絕對不要做的事（Prototype 第一階段）

- 不要建立 Scene、Prefab 或遊戲用的 C# 程式碼（除非使用者明確要求開始實作）。
- 不要安裝 Unity MCP、不要修改 `Packages/`、`Assets/Plugins/`。
- 不要為了「湊功能」而新增與核心循環無關的系統（劇情、存檔、商店、成就…）。

## Skill 使用規則

- 專案 Skills 位於 `.cline/skills/`（9 個通用 skill + 專案專屬 `unity-rhythm-timing`）。
- 動到節奏／判定／音符相關工作時，**必須**先載入 `unity-rhythm-timing`。
- 動到 UI／打擊感／音訊／鏡頭／輸入／動畫／資料資產／出包時，載入對應的通用 skill。
- 若某個 skill 的建議與本專案 Rules 衝突（例如 skill 假設 3D 或平台跳躍），**以本專案的 Rules 與使用者指示為準**，
  並在回覆中說明衝突點。
