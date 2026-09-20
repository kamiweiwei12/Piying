# 00 開發工作流程（每次工作必遵守 · always-on）

> 本檔是 `DEVELOPMENT.md` 的**強制摘要**。一切細節以 `DEVELOPMENT.md` 為準；
> 若本檔與 `DEVELOPMENT.md` 衝突，**以 `DEVELOPMENT.md` 為準**，並在回報中指出衝突。
>
> 每次工作必須依序執行下列 **16 條**，並在回報中對應說明。

## 開始前（1–6）

1. **先讀相關 `.clinerules`**：`01-project-context.md`（專案事實）、`02-unity-conventions.md`（Unity/C# 慣例）、
   `03-guardrails.md`（護欄）。
2. **讀 `DEVELOPMENT.md`**：至少 §4（目前里程碑的完成條件）、§8（最小修改原則）、§9（測試規則）、
   §12（禁止自行做出的高風險修改），並檢查附錄 F（待批准事項）中有無尚未批准的項目。
3. **判斷目前 Milestone**（M0–M7？Infra？）與該里程碑尚未滿足的條件；前置事項未經批准不得越過。
   ※ 同時檢查 Git 狀態（`git status --short`、目前 branch、上一個 commit、工作區是否乾淨）
   → 對應 `DEVELOPMENT.md` 附錄 A STEP 5。
4. **判斷本次需要哪些 Skills**（對照 `DEVELOPMENT.md` §7.1 任務→Skill 對照表）；
   若本次工作無對應 Skill（例如純文件／版控工作），必須明確說明「無」。
5. **讀相關 `SKILL.md` 與其 `references/`**：必須實際閱讀檔案，**不得只依賴對話記憶**。
6. **先提出 Plan**：輸出 `DEVELOPMENT.md` §10.1 的全部欄位（Current Milestone／Goal／Required Skills／
   Files To Change／Files NOT To Change／Implementation／Testing／Risk／Alternative／Git），
   且 Risk 必須逐項回答 §11 的十項風險檢查。

## 批准（7）

7. **輸出 Plan 後停下來等待使用者批准**；除非使用者明確說「直接執行」，否則**不得進入修改階段**。

## 修改中（8–9）

8. **遵守最小修改原則**（`DEVELOPMENT.md` §8）：只改完成任務所必需的最小範圍；
   不順便重構、美化、升級套件、動 `ProjectSettings/`、動 URP、動 Input Actions、建 Scene/Prefab、
   刪除或搬移 Platformer 舊檔。若發現「順手做會更好」→ **不要做**，列為回報中的「額外建議」。
9. **高風險修改先詢問**（§12 十二條＋附錄 F 待批准清單）：刪檔／改套件／改設定／切 Renderer／
   建 Scene／改 Input Actions／重構／加 asmdef／大量新增資產／改 `.clinerules` 或 `.cline/skills`
   → 一律先問，未獲同意不得執行。

## 完成後（10–14）

10. **完成後測試**（§9）：純邏輯 → EditMode 測試必備且涵蓋邊界值；時間／音訊 → Editor **與** Build 各一次；
    文件／設定／版控 → 檢查檔案存在、路徑與 diff 無意外檔案。
    先確認 **Unity Console 無 compile error**，有 error 先修，不得 commit。
    測試失敗不得標記完成，須回報 `【Failure】【Cause】【Attempted Fix】【Risk】【Next Action】`。
11. **Review git diff**：`git status --short` ＋ `git diff`（或 `git diff --cached`），確認只有 Plan 列出的檔案被改、
    `.meta` 無遺漏、沒有意外檔案（尤其不得出現 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`*.csproj`、`*.sln`）。
12. **更新 `CHANGELOG.md`**（§6 七個必填欄位：新增／修改／測試／驗收結果／Git Commit／風險／下一步；
    未 commit 時寫「待提交」），並依附錄 C 第 8 步更新 `DEVELOPMENT.md` §2 的「目前 Milestone」進度。
13. **Git commit**：message 依 §5.3（`type(scope): summary`，英文小寫祈使句；
    **禁止** `update`、`fix`、`test`、`change stuff` 等模糊訊息）。一個里程碑＝一個可回復的完整工作單位。
14. **回報 commit hash**：依 §10.2 回報（本次完成／驗收結果／Git Commit／CHANGELOG／目前 Milestone／
    下一步／是否需要批准／額外建議）。

## 收尾（15–16）

15. **完成後停下等待下一步**：不得自行進入下一個 Milestone、不得自行執行附錄 F 的待批准事項。
16. **禁止「假完成」**（附錄 E）：只建文件、只寫程式未測試、Console 有 error、測試未實際執行、
    只在 Editor 測卻要求 Build 驗證、未 commit、CHANGELOG 未更新、驗收條件未完成、只做閱讀分析
    → 全部**不算完成**。完成唯一標準：**實作 + 測試 + 驗收 + 文件 + Git commit**。
