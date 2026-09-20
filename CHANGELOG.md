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

## [2026-09-21] Infra - 同步開發契約與專案事實

### 新增

* 無。

### 修改

* `.clinerules/01-project-context.md`：同步 Git、節奏原型場景、asmdef、目前 Milestone 與 Rule 數量。
* `DEVELOPMENT.md`：將 Rules 閱讀範圍更新為 `00`–`03`，同步 Cline 資產數量，並標記附錄 F 中已完成的 F1–F4、F10。
* 將附錄 G 從「尚未建立的建議」改為已建立的工作流程 Rule 說明。

### 測試

* 文件／版控驗證：`git diff --check`。
* 檢查 `git status --short` 與 `git diff --stat`，確認只有本條目列出的三份文件發生變更。

### 驗收結果

* `git diff --check` 通過；僅出現既有換行策略的 LF→CRLF 提示，沒有 whitespace error。
* `git status --short` 與 `git diff --stat` 確認只有 `.clinerules/01-project-context.md`、`DEVELOPMENT.md`、`CHANGELOG.md` 三份文件變更。

### Git Commit

* 待提交。

### 風險 / 已知問題

* 本次只同步既有事實，不修改 Unity 程式、Scene、Prefab、Packages、ProjectSettings 或 Input Actions。
* F5–F9 仍維持原本狀態；未經另行批准不得執行。

### 下一步

* 提出 M3 音符視覺的完整 Plan；涉及改寫 `YingYun_Gameplay.unity` 時仍須另行確認具體修改範圍。

---

## [2026-09-21] M2 - 節奏執行期（DSP 時鐘 / 輸入橋接 / Windows build）

### 新增

* `YingYun.Unity` asmdef（引用 `YingYun.Runtime`、`Unity.InputSystem`）：新遊戲的 Unity 端執行期程式。
* `YingYun.Rhythm.Timing.DspSongClock`：以 `AudioSettings.dspTime` 為唯一基準的歌曲時鐘；音樂以
  `AudioSource.PlayScheduled(dspStart)` 起播，暫停期間累加 `pausedTotal`（不使用 `Play()`、不用 `Time.time`）。
* `YingYun.Rhythm.Timing.ClockBridge`：取樣 `dspTime − realtimeSinceStartup`（平滑係數 0.1），
  把 Input System 事件時間映射為 DSP 時間與 song time。
* `YingYun.Rhythm.Input.InputSystemNoteInputSource`：訂閱 `Rhythm` action map，保留事件時間戳後入列，
  不做「這幀才處理」的近似。
* `YingYun.Rhythm.Prototype.RhythmPrototypeController`：單曲原型整合（180 秒、120 BPM、每拍一顆、
  lane 以 1→6 循環）；Console 逐拍輸出 `songTime` / `dsp` / `bridgeMs` / `inputMedianMs`，
  逐判定輸出 `grade` / `errorMs` / `combo` / `score` / `accuracy`；P = 暫停／續播、R = 重開。
* `Assets/Scenes/YingYun_Gameplay.unity`：專案第一個可版控場景（Main Camera + Rhythm Prototype），
  並註冊進 `EditorBuildSettings`。
* `Assets/Settings/InputSystem_Actions.inputactions`：新增 `Rhythm` action map（Q/W/E/A/S/D → Lane1–6）。
* `YingYun.Tests` 新增 `ClockBridgeTests`（3 個案例）。
* Windows build 產物 `Builds/M2/YingYun.exe`（StandaloneWindows64、Mono；`Builds/` 不進版控）。

### 修改

* `Assets/Tests/EditMode/YingYun.Tests.asmdef`：新增 `YingYun.Unity` 參考。
* `ProjectSettings/EditorBuildSettings.asset`、`ProjectSettings/ProjectSettings.asset`、
  `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`：Unity 自動產生的設定變更原樣收錄。

### 測試

* **EditMode 回歸**（Unity 6000.6.2f1）：
  `unity test "D:\Unity\program\My project" --editor-path "D:\Unity\Editor\6000.6.2f1\Editor\Unity.exe" --mode EditMode --filter YingYun.Tests --timeout 180 --output "…\Logs\M2-editmode-results.xml" --format json`
  → **total=15 / passed=15 / failed=0 / skipped=0**（`JudgmentEngineTests` 12、`ClockBridgeTests` 3），
  `duration=0.0881676 s`，`result=Passed`。報告檔 `Logs/M2-editmode-results.xml`（未進版控）。
  備註：`--editor-version 6000.6.2f1` 與直接呼叫 `Unity.exe -batchmode -runTests` 皆失敗
  （前者 CLI 找不到安裝、後者 headless 授權不足），改用 `--editor-path` 後成功。
* **Editor 真人實測**（音訊 48000 Hz、DSP buffer 1024 samples × 4 buffers）：
  * 判定 467 筆：Perfect 19 / Good 21 / Miss 427；**非 Miss 樣本 n = 40**。
  * 誤差分布：median **+11.314 ms**、mean −0.853 ms、min −99.413 ms、max +83.354 ms、標準差 55.72 ms；
    平均值的 95% 信賴區間 **[−18.12, +16.41] ms（涵蓋 0）**。
  * 誤差對時間的線性趨勢：+17.0 ms/min（標準誤 11.7、t = 1.46，**不顯著**）。
  * 播放區段：R 重開前 143.5 秒（beats 0–287）、重開後 89.0 秒（beats 0–178），合計 232.5 秒。
    `dsp − songTime` 漂移 **0.000 s**（兩段）；相鄰拍 `dsp/songTime` 比值中位數 **1.000000**；
    `bridgeMs` 區間 34115.3–34139.3 / 34112.1–34136.9，趨勢漂移 −2.80 ms / −1.69 ms。
  * Miss 的 `errorMs` 全部落在 101.3–121.3 ms（中位數 112）= late 窗超時自動 Miss，代表未輸入，非判定錯誤。
  * Pause：`paused | songTime=89.453333 | dsp=270.720000`，且之後**無任何 beat/judgment 行**（時鐘確實凍結）。
  * Restart：第二次 `scheduled | dspStart=181.266667`。
  * Console 無 error（僅 Unity 授權訊息 `Licensing::Client Error: Code 404`，與專案無關）。
* **Windows build 真人實測**（48000 Hz、DSP buffer 1024 × 4）：
  * 判定 51 筆：Perfect 1 / Good 7 / Miss 43；**非 Miss 樣本 n = 8**。
  * 誤差分布：median −47.690 ms、min −99.956 ms、max +85.166 ms。
  * 播放 25.5 秒（beats 0–50）；`bridgeMs` 區間 7574.6–7590.4 ms；
    Pause 有記錄（`songTime=25.133333`）；**未測 Resume 與 Restart**。
  * Player log 無 error。
* 輔助證據（headless，非真人）：`Logs/M2-player-3min.log` 連續 194.5 秒、漂移 0.000 s、
  比值 1.000000、`bridgeMs` 趨勢漂移 −1.55 ms；但全程無鍵盤輸入（n = 0），僅供節奏穩定性參考。

### 驗收結果

| M2 完成條件 | 結果 |
|---|---|
| 音樂以 `AudioSource.PlayScheduled(dspTime)` 起播（禁用 `Play()` 當判定基準） | ✅ |
| 連續播放 3 分鐘以上，節拍指示與音樂不漂移 | ⚠️ 已量測區段的漂移為 **0.000 s**，但**最長連續僅 143.5 秒（Editor）**，未達 180 秒 |
| 輸入誤差分布中位數接近 0（列出樣本數與中位數） | ⚠️ Editor median **+11.314 ms（n=40）**、Build median −47.690 ms（n=8）；**兩邊 n 皆未達 60** |
| Editor 與 Windows build 各測一次並記錄差異 | ⚠️ 兩邊皆已完成實測與記錄，但 Build 端樣本極小且未測 Resume / Restart |
| 暫停 / 續播 / 重開曲目行為正確 | ⚠️ Editor：Pause ✅、Restart ✅、Resume 於首次實測有記錄（`pausedTotal=1.984`）、第二次未測；Build：僅 Pause |
| 既有 Platformer 程式未被破壞（Console 無新增 error） | ✅ EditMode 15/15、Editor log 與 player log 皆無 error |

* **結論：依使用者指示，以現有驗收數據結案（不再補測）。** 程式、測試、文件與版控皆已完成；
  上表 ⚠️ 三項為已知驗收缺口，詳見下方「風險 / 已知問題」。
* 未達項**不是程式缺陷**：原型目前完全沒有音符視覺（音符生成／顯示屬 M3），真人只能在無畫面提示下
  盲打，因此命中樣本數與連續播放時長無法達標；時間與判定本身經實測無系統性偏移。

### Git Commit

* `3e30379` `feat(m2): integrate dsp rhythm prototype`
* `af83301` `chore(repository): track unity generated settings and ignore ide upgrade log`
  （`ProjectSettings` 三處收錄、`.gitignore` 新增 `UpgradeLog*.htm`／`UpgradeLog*.xml`、刪除 `UpgradeLog.htm` 與
  `Assets/New Folder.meta`＋空資料夾）
* `9006142` `docs(m2): record dsp rhythm prototype acceptance`（本條目與 `DEVELOPMENT.md` §2 更新）

### 風險 / 已知問題

* **驗收缺口（未達判讀線，非程式錯誤）**：
  1. Editor 非 Miss 樣本 n = 40（判讀線 60）；Windows build n = 8。
  2. 未取得單次連續 ≥180 秒的播放區段（最長 143.5 秒）。
  3. Windows build 端未測 Resume 與 Restart。
  4. 無音符視覺下只能盲打：Build 的 median −47.690 ms（n=8）不具統計意義，不可作為偏移結論。
* **未發現系統性時間偏移**：Editor 平均誤差 −0.853 ms（95% CI 涵蓋 0）、趨勢斜率不顯著（t = 1.46）、
  兩段漂移 0.000 s、`bridgeMs` 趨勢漂移 < 3 ms；`Music.wav` 與譜面 120 BPM 無逐漸脫節跡象。
* `bridgeMs` 絕對值偏大（Editor 約 34.1 s、Build 約 7.6 s）屬正常現象：`dspTime` 與
  `realtimeSinceStartup` 起算點不同，差值為常數並在映射時抵消；程式於 `Awake` 每次重新取樣。
* 分 lane 中位數（Q −1.70 / W −64.34 / E −81.94 / A +57.78 / S +30.15 / D +33.21 ms，每 lane 僅 4–10 筆）
  不具統計意義；六鍵共用同一條 `actionTriggered → ClockBridge → Judgment` 路徑，無 per-lane 差異邏輯。
* `Assets/Scripts/M0/ClockProbe.cs` 仍以 `RuntimeInitializeOnLoadMethod` 自動執行並在 `Logs/` 產生 CSV；
  進入正式執行期前應移除或以條件編譯隔離（需使用者批准）。數字鍵 0–5／空白鍵屬該探針專用。

### 下一步

* M3 音符視覺：接近圈音符生成／回收（物件池）、命中消失、判定文字與音符消失時機一致；
  完成後再回頭補齊 M2 遺留的真人驗收（n ≥ 60、單次連續 ≥180 秒、Windows build 的 Resume/Restart）。

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

* `bac26de` `feat(m1): add deterministic judgment core`

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
