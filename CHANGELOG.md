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

## [2026-09-20] M1 - 可重現的純 C# 判定核心

### 新增

* `YingYun.Runtime` asmdef：隔離新遊戲執行期程式，啟用 `noEngineReferences`，避免判定核心依賴 Unity API。
* `YingYun.Rhythm.Judgment`：加入 `ISongClock`、音符/輸入/判定結果資料契約、可調 `TimingConfig` 與
  `JudgmentEngine`。
* `YingYun.Tests` EditMode 測試程序集與 12 個測試案例。

### 修改

* `DEVELOPMENT.md`：標記 M1 完成，並將目前里程碑推進至 M2。

### 測試

* 指令：`unity test "D:\Unity\program\My project" --editor-version 6000.6.2f1 --mode EditMode --filter YingYun.Tests --timeout 180 --format json`
* Unity Test Framework / NUnit 最終回歸結果：`total=12`、`passed=12`、`failed=0`、`skipped=0`，耗時約 0.081 秒。
* 覆蓋：Perfect/Good early/late 包含邊界、超窗輸入保留、同批輸入取最近者、超時 Miss、Combo 中斷、
  Score/Accuracy、Hold tick、合法/提早釋放、段落完成/中斷、相同 chart + replay 結果一致。

### 驗收結果

* ✅ 判定程序集不引用 UnityEngine；引擎時間只讀取注入的 `ISongClock`。
* ✅ 判定窗內部以 `double` 秒運算，結果輸出毫秒，並以極小 epsilon 穩定處理浮點邊界。
* ✅ 配對從所有合法音符—輸入組合選擇絕對誤差最小者，未使用輸入留在佇列。
* ✅ Tap、Hold、Miss、Combo、Score、Accuracy 與舞蹈段結果由單一引擎狀態產生。
* ✅ Replay 測試證明同一譜面與同一輸入序列可重現相同事件簽章。
* ✅ `Advance` 寫入呼叫端可重用的結果清單，正式熱路徑不需為每幀事件配置新陣列。

### Git Commit

* **待提交**

### 風險 / 已知問題

* `TimingConfig` 目前是純 C# 值型別；M2/M4 可再由 ScriptableObject 設定資產轉換，不應讓核心直接依賴資產。
* M1 僅驗證數學與狀態機，尚未接入 `AudioSettings.dspTime`、Input System 或場景。
* Pipeline 的內建 `run_tests` 在含空格的專案路徑下曾錯誤啟動測試程序；本次改用官方 `unity test` headless 命令完成驗收。

### 下一步

* M2：建立 `DspSongClock`、ClockBridge、Input System 事件佇列與最小 Gameplay 場景，並以 `PlayScheduled` 接通單曲流程。

---

## [2026-09-20] M0 - 音訊與輸入時鐘行為基線

### 新增

* `Assets/Scripts/M0/ClockProbe.cs`：可拋棄式執行期量測工具，記錄 DSP、realtime、unscaled time、
  `AudioSource.timeSamples`、音訊 callback sample frame 與 Input System event time；可用數字鍵切換測試情境。
* `Assets/Scripts/M0/SampleCounterFilter.cs`：以音訊執行緒 callback 累計已處理 sample frame，作為音訊系統活動參考。
* `com.unity.modules.particlesystem` 明確依賴，修復 Platformer 模板程式在目前 package 組合下缺少
  `UnityEngine.ParticleSystemModule` 參考而無法編譯的問題。

### 修改

* `ClockProbe` 延後至 `AfterSceneLoad` 解析 `AudioListener`：場景已有 listener 時沿用，僅在完全不存在時建立 fallback，
  避免與 `Main Camera` 產生重複 listener。
* `DEVELOPMENT.md`：標記 M0 完成、同步 Git 現況與下一個里程碑。

### 測試

* Unity 6000.6.2f1 Editor 實測（48 kHz、DSP buffer 1024、`runInBackground = 0`）：
  * 正常播放 2.99 秒：realtime +2.99 s、DSP +2.99 s，`AudioSource.timeSamples` 持續前進。
  * `AudioSource.Pause()` 5.00 秒：realtime +5.00 s、DSP +5.01 s，`timeSamples` 固定於 1408。
  * `AudioListener.pause = true` 5.00 秒：realtime +5.00 s、DSP +0.00 s，`timeSamples` 固定於 48000。
  * 應用失焦約 2.60 秒：realtime +2.60 s、DSP +0.00 s；重新取得焦點後恢復。
* Input System 事件樣本 `n=119`：`eventTime - realtime` 中位數約 -1.257 ms、平均 -2.008 ms、
  p95 約 -0.871 ms（極值 -23.479 ms 至 -0.272 ms）。
* Listener 驗證：沿用 `Main Camera` 上既有 listener，`found=1`、`addedListener=false`，無重複 listener 警告。
* 移除臨時自動情境序列後，以 Unity CLI 強制重編譯：`completed`、`failed=false`、`compilationFailed=false`。

### 驗收結果

* ✅ 四種情境均已取得實測資料；正常播放時 DSP 與 realtime 同步前進。
* ✅ `AudioSource.Pause()` 不會凍結 DSP，因此正式暫停策略採 `AudioSource.Pause()`，並在 song time 中扣除
  `pausedTotal`：`songTime = dspNow - dspStart - pausedTotal - outputOffset`。
* ✅ `AudioListener.pause` 與本次失焦實測會凍結 DSP；若採該策略不得再扣同一段 `pausedTotal`，否則會重複補償。
* ✅ ClockBridge 定案：在相近時刻取樣 `bridgeOffset = dspSample - realtimeSample`，將 Input System 事件時間映射為
  `inputDspTime = eventTime + bridgeOffset`，再算
  `inputSongTime = inputDspTime - dspStart - pausedTotal - inputOffset`；本機一般誤差為毫秒級。
* ✅ callback sample frame 在兩種 pause 下仍可能前進，故僅作診斷，不得作實際播放進度或判定時鐘。
* ✅ 現有 timing Skill 的 `dspTime`、`PlayScheduled`、`pausedTotal` 與輸入時鐘橋接原則符合實測，暫無修改建議。

### Git Commit

* `7c868c6` `feat(m0): establish audio and input clock baseline`

### 風險 / 已知問題

* CSV 原始量測檔位於被 `.gitignore` 排除的 `Logs/`，版控保留本條目的關鍵數值與結論，不納入執行期輸出。
* M0 工具為自動安裝的診斷程式，進入正式執行期前應移除或以開發條件編譯隔離。
* 專案仍沒有可版控的 `.unity` 場景；`EditorBuildSettings` 仍指向不存在的 `Assets/Scenes/SampleScene.unity`。

### 下一步

* M1：建立 `YingYun.Runtime` / `YingYun.Tests` asmdef，實作不依賴 Unity 場景的純 C# 判定核心與 EditMode 測試。

---

## [2026-09-20] Infra-2 - 開發工作流程 Rule（`.clinerules/00-development-workflow.md`）

### 新增

* `.clinerules/00-development-workflow.md`：always-on 強制流程 Rule，把 `DEVELOPMENT.md` 的
  附錄 A（STEP 0–7）、§8（最小修改）、§9（測試）、§10（回報）、§12（高風險）、§5（Git）、§6（CHANGELOG）、
  附錄 C / E 摘要為 **16 條必遵守流程**（開始前 6 條、批准 1 條、修改中 2 條、完成後 5 條、收尾 2 條）。

### 修改

* `CHANGELOG.md`（本檔）：補記 F1 baseline commit hash；新增本條目。

### 測試

* 文件類工作，依 `DEVELOPMENT.md` §9「文件 / 設定 / 版控」要求驗證：
  * 檔案存在與路徑：`.clinerules/00-development-workflow.md` ✅
  * 編碼：UTF-8 無 BOM、CRLF（與 `01~03` 一致）✅
  * 內容一致性：逐條對照 `DEVELOPMENT.md` §5／§6／§8／§9／§10／§12／附錄 A／附錄 C／附錄 E，
    確認 16 條無與規範衝突之敘述 ✅
  * diff 檢查：`git status --short` 僅出現本條目涉及之檔案 ✅

### 驗收結果

* ✅ Rule 覆蓋使用者要求的 16 項強制事項（逐條可對應）
* ✅ 未修改 `DEVELOPMENT.md`（依指示不修改其餘內容；附錄 G 的草稿為精簡版，尚待使用者決定是否同步為完整版）
* ✅ 未新增其他 Rule
* ⚠️ Cline UI 內是否實際載入本 Rule（Rules 面板是否出現 `00-development-workflow`）**待使用者於 UI 確認**

### Git Commit

* `bad91f80bbebddff4c76f7b17a149feaf6059a7a`（short `bad91f8`）
  `docs(rules): add mandatory development workflow rule`
  （本條目內容與 `.clinerules/00-development-workflow.md` 同屬此提交）

### 風險 / 已知問題

* `DEVELOPMENT.md` 附錄 G 仍保留 7 點精簡草稿，與已建立的 16 條完整版內容方向一致但詳略不同；
  如需一致，須經使用者同意後修改 `DEVELOPMENT.md`（本輪未動）。
* `DEVELOPMENT.md` §2「目前 Milestone」尚未更新為 Infra 完成（依指示本輪不修改）。

### 下一步

* 依 `DEVELOPMENT.md` 附錄 C 第 8 步更新 §2 進度（需批准）。
* M0 前置：附錄 F2（建立測試場景）與 F4（asmdef）需批准後才可進入 M0。

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
* `.gitignore` 功能測試（於系統暫存目錄建立拋棄式 repo，測完刪除，未觸碰本專案）：
  建立 26 個代表性檔案 → `git init -b main` → `git add -A` → `git diff --cached --name-only`：
  必須保留者 **10/10 全部被追蹤**、必須忽略者 **15/15 全部被忽略**。

### 驗收結果

* ✅ 三份文件建立完成且可讀（`.gitignore` / `CHANGELOG.md` / `DEVELOPMENT.md`）
* ✅ 附錄 E 判定「不算完成」的項目已於 Infra-2 補齊（Git 初始化 + baseline commit）
* ✅ Git 已初始化（branch `main`）；baseline commit 內容稽核：**957 檔**
  （`Assets` 894、`ProjectSettings` 31、`.cline` 23、`.clinerules` 3、`Packages` 2、根目錄 4），
  其中 `.meta` 480 檔；**無** `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`*.csproj`、`*.sln`、`*.pdb` 等

### Git Commit

* `8bd25fc7d8ca8b1c66f15073efe85785d9512bf3`（short `8bd25fc`）
  `chore(repository): add git ignore rules and development workflow docs`

### 風險 / 已知問題

* `core.autocrlf=true` 來自 **System 層級** `C:/Program Files/Git/etc/gitconfig`（Git for Windows 預設），
  非本專案設定；`git add` 產生大量「LF will be replaced by CRLF」警告，但**不會改寫工作區檔案**；
  `git add` 後 3 個新增檔仍為 CRLF、Unity 檔案未被更動。
* 未建立 `.gitattributes`（使用者指示不執行 F5）→ 跨平台換行正規化尚待決定。
* 本專案無任何 `.unity` 場景檔（`Assets/Scenes/` 為空，`EditorBuildSettings` 仍指向不存在的 `SampleScene.unity`）。

### 下一步

* 依 `DEVELOPMENT.md` 附錄 C 第 8 步更新 §2 進度（需批准）。
* 之後才可進入 M0（時鐘行為實測），M0 前置為附錄 F2（測試場景）與 F4（asmdef）。

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
