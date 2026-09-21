# 《影韵》開發規範（DEVELOPMENT.md）

> 本文件是本專案的**長期開發契約**。每次工作開始前必須重新閱讀（見附錄 A STEP 4）。
> 分工：**事實**以 `.clinerules/01-project-context.md` 為準；**已完成工作**記於 `CHANGELOG.md`；**流程與里程碑條件**記於本檔。

## 0. 文件角色與閱讀順序

| 文件 | 角色 | 更新時機 |
|---|---|---|
| `.clinerules/00~03` | Cline 每次對話自動載入的開發流程 / 專案情境 / 慣例 / 護欄 | 專案事實變動時（需使用者同意） |
| `DEVELOPMENT.md`（本檔） | 開發流程、里程碑、完成條件 | 流程或里程碑進度變更時 |
| `CHANGELOG.md` | 已完成工作記錄（只記真正完成者） | 每個工作單位完成並 commit 後 |
| `.cline/skills/` | 領域知識（節奏時序、Input、UI、音訊…） | 新增 / 修正 Skill 時（需使用者同意） |

**每次工作閱讀順序**：
`.clinerules/00 → 01 → 02 → 03` → 本次任務相關 `SKILL.md` → 該 Skill 的相關 `references/` → 本檔 →
檢查 Git → 確認目前 Milestone → 定出最小修改範圍。

## 1. 專案目標

- **《影韵》**：**2D 中國皮影戲題材節奏音樂遊戲**，PC / 鍵盤操作，最終目標為**比賽 Demo**。
- 核心體驗一句話：**「音樂給予節奏，音符給予指令，鍵盤模擬操偶，皮影完成舞蹈。」**
- 現階段唯一目標：證明核心玩法成立 ——
  `音樂 → 音符 → 玩家輸入 → 時間判定 → 得分/連擊 → 遊戲回饋`。
- 畫面以 2D 為主，**不做 3D 建模**；皮影以 2D 分層 Sprite 呈現。
- **暫時不做**：劇情、角色收集、商城、聯機、排行榜、大量曲目、複雜養成、複雜世界觀。
- 核心設計理念：把皮影戲的「**操偶方式**」轉化為玩家的「**核心操作方式**」——
  文化元素必須進入遊戲機制，而不只是美術與背景包裝。

### 1.1 與既有 Platformer 模板的關係（硬性）

- 本專案素材與程式來自 Unity 官方 **2D Platformer Microgame** 教學模板。
- **可以**參考：C# 寫法風格、Input System 用法、Animation / UI / 相機的設定方式。
- **不可以**沿用或擴充其架構：`PlayerController`、`EnemyController`、`PatrolPath`、`Jump`、`KinematicObject`、
  `Simulation`、`Platformer.*` 命名空間一律**不得**作為新遊戲的架構基礎。
- 該批檔案**保留原位、不刪除、不重構**；若判定無用，先提出清單與理由，由使用者決定。

## 2. 當前 Prototype 階段

| 項目 | 狀態 |
|---|---|
| 階段 | **Prototype 第一階段（核心循環垂直切片）** |
| 目前 Milestone | **M6.5 已結案**（數位皮影操演灰盒：操演意圖、竹製控制桿、分片剪影舞台）；下一個工作單位為使用者實機驗收，通過後進 **M7 可玩 Demo** |
| 可用基礎 | Unity 6000.6.2f1 / URP 17.6.0（**Forward Renderer**，非 Renderer2D）/ Input System 1.19 / uGUI + TMP / Cinemachine 6.6 / Audio (DSP buffer 1024, 48 kHz) |
| 重大缺口 | M6.5 已把「頂部吊線木偶」改為「數位皮影操演」：六軌表達六種操演意圖，側向／下方剛性竹桿先響應輸入再驅動分片關節；幕布、背光、戲台框、冠飾、寬袖、衣擺與鉚釘建立第一眼皮影語言。判定、計分與 dsp 時鐘保持原架構。**目前缺口**：正式分層透光皮影 Sprite、角色側臉輪廓、動作短語與 Combo 舞台光影仍待後續；M7 尚缺三難度、選曲流程、延遲校準與最終 Windows 出包體驗 |
| 版控 | Git 已初始化，`main` 為目前主線 |
| Cline 資產 | `.cline/skills/` 10 個 Skill、`.clinerules/` 4 份 Rule（`00`–`03`） |

## 3. 開發里程碑 M0 → M7

| M | 名稱 | 目標產出 | 依賴 / 需批准項目 |
|---|---|---|---|
| **M0** | 時鐘行為實測 | 「`AudioSettings.dspTime` / `Time.realtimeSinceStartup` / Input System 事件時間」在 ①正常 ②`AudioSource.Pause` ③`AudioListener.pause` ④失焦（`runInBackground=0`）四情境下的行為事實表；據此定案暫停數學與 ClockBridge | 需 1 個測試場景或 Editor 臨時腳本 |
| **M1** | 判定核心 | 純 C# 判定引擎 + EditMode 測試（不需場景 / 音訊 / 美術） | 需 `YingYun.Runtime` / `YingYun.Tests` asmdef |
| **M2** | 節奏執行期 | `DspSongClock` + `ClockBridge` + `InputSystemNoteInputSource`；單曲、節拍器、輸入誤差統計；**並出一次 Windows build 對照** | 需建立 `YingYun_Gameplay.unity`、新增 Input `Rhythm` action map |
| **M3** | 音符視覺 | 接近圈音符生成 / 回收、命中消失、判定文字 | — |
| **M4** | 計分與結算 | Combo / Score / Accuracy HUD + 結算畫面（S~D 級） | — |
| **M5** | 進階音符 | Hold、組合音符（按鍵集合）、舞蹈連貫度（段落完成 / 中斷） | — |
| **M6** | 皮影角色 | `PuppetRig` + `ActionBinding` + 光幕 / 剪影 / 操偶桿；「按 Q → 左手抬起」因果一眼可見 | 皮影部位 Sprite（可先用幾何佔位） |
| **M6.5** | 數位皮影操演重構 | 六種操演意圖、側向竹桿傳動、分片剪影、幕布背光與灰盒戲台；移除頂部吊線木偶語言 | 不改 Renderer／Packages／ProjectSettings |
| **M7** | 可玩 Demo | 三難度、選曲、延遲校準、出包（Windows / WebGL 評估）、中文 UI | 中文需 CJK TMP 字型資產 |

> 順序原則：**先讓時間與數學正確（M0–M2），再讓它看得見（M3），最後才讓它好看（M6）。**

## 4. 各里程碑完成條件（Definition of Done）

> 通則：**實作 + 測試 + 驗收 + 文件 + Git commit** 缺一不可（見附錄 E）。下列為各里程碑的專屬條件。

### M0 — 時鐘行為實測
- [x] 以實驗量測並記錄四情境：正常播放 / `AudioSource.Pause()` / `AudioListener.pause = true` / 應用失焦（`runInBackground = 0`）
- [x] 每個情境記錄 `AudioSettings.dspTime`、`Time.realtimeSinceStartup`、`Time.unscaledTime`、Input System 事件時間的變化
- [x] 明確回答：**暫停期間 `dspTime` 是否前進？**（Unity 6.6 文件稱 audio system 被暫停時 dspTime 不更新，必須以實測為準）
- [x] 產出暫停數學定案（是否需要 `pausedTotal` 補償）
- [x] 產出 ClockBridge 設計結論（輸入時間戳 → songTime 的映射公式與誤差量級）
- [x] 結果寫入 `CHANGELOG.md`；若結論影響 Skill/Rules，**提出**修改建議（不自行修改）

### M1 — 判定核心
- [x] 純 C#（不依賴 `MonoBehaviour` / `AudioSource`），時間來源以介面注入
- [x] EditMode 測試全綠，至少涵蓋：判定窗邊界（early/late 對稱）、多輸入同幀配對（取最近且其餘不丟棄）、
      超時自動 Miss、Hold tick 與釋放、Combo 中斷、Score/Accuracy 累計、舞蹈段完成判定
- [x] 同一份譜面 + 同一組 replay 輸入 → **結果可重現**
- [x] 測試指令與結果貼入 `CHANGELOG.md`

### M2 — 節奏執行期
- [ ] 音樂以 `AudioSource.PlayScheduled(dspTime)` 起播（**禁用 `Play()` 當判定基準**）
- [ ] 連續播放 3 分鐘以上，節拍指示與音樂不漂移
- [ ] 輸入誤差分布中位數接近 0（列出樣本數與中位數）
- [ ] **Editor 與 Windows build 各測一次**並記錄差異（不得只在 Editor 驗收）
- [ ] 暫停 / 續播 / 重開曲目行為正確
- [ ] 既有 Platformer 程式未被破壞（Console 無新增 error）

### M3 — 音符視覺
- [x] 音符依 `songTime` 正確出現與移動，物件池生效（無每幀 `Instantiate`）
- [x] 命中 / 失敗後正確消失並回收
- [x] **判定邏輯不得出現在 View 層**（程式碼審查要確認）
- [x] 判定文字與音符消失時機一致

### M4 — 計分與結算
- [x] Combo / Score / Accuracy 只在 Judgment 端計算，UI 只讀
- [x] 結算數字與執行中 Console 統計一致
- [x] 評級（S~D）門檻放於 `DifficultyConfig`

### M5 — 進階音符
- [x] Hold 音符：按住期間、提早放開、超時未放開皆有正確判定
- [x] 組合音符（Q+E 同時）以「按鍵集合 + 時間窗」判定
- [x] 舞蹈連貫度：整段完成 / 中斷皆有事件與演出

### M6 — 皮影角色
- [x] 6 鍵各對應到正確部位動作（`ActionBinding` 資料驅動，可改鍵）
- [x] 一段 `Q → E → Q+E → D` 能形成肉眼可辨的連續舞蹈
- [x] 剪影 + 光幕視覺成立，且**不依賴 Renderer2D / 2D Light**
- [x] 動畫不驅動判定（判定仍只依 dsp 時間軸）

### M6.5 — 數位皮影操演重構
- [x] 不再建立頂部垂下的控制繩，改為側向／下方剛性竹製操縱桿
- [x] 六軌同時具備唯一的 `PerformanceIntent`，身體部位只作角色對意圖的資料驅動解釋
- [x] 幕布、背光、戲台框、分片衣袖／衣擺、冠飾與鉚釘關節在灰盒階段可辨認
- [x] 輸入先造成杆端回饋，再由固定步長彈簧關節形成連續動作；Hold／Chord 維持既有行為
- [x] EditMode 全量測試與 Windows build 通過，視覺驗收圖留存於 `Logs/`

### M7 — 可玩 Demo
- [ ] 三難度（Easy / Normal / Hard）結構與數值差異明確
- [ ] 選曲 → 遊玩 → 結算 流程無需在 Editor 手動介入
- [ ] 延遲校準值可調、可保存、可即時生效
- [ ] 出包（至少 Windows）可正常執行，節奏不偏移
- [ ] 有中文 UI 需求時，CJK TMP 字型已就位

## 5. Git 規則

### 5.1 基本原則
- **一個里程碑 = 一個可回復的完整工作單位 = 一個 commit**（不要每改一行就 commit，也不要把多個里程碑混成一個 commit）。
- commit 前必須：**Review diff → 確認無意外修改 → 測試通過 → 更新 CHANGELOG**。
- 測試失敗時**不得** commit 成「完成」；必須先回報 Failure / Cause / Attempted Fix / Risk / Next Action。

### 5.2 分支
- Prototype 階段：`main` 為主線；實驗性做法可開 `exp/<主題>`，**實驗失敗即丟棄**，不合併回 `main`。
- 不使用 force push；不重寫已推送的歷史。

### 5.3 Commit message 規範

格式：`<type>(<scope>): <summary>`

| type | 用途 |
|---|---|
| `feat` | 新功能 |
| `fix` | 修正錯誤 |
| `docs` | 文件（含 CHANGELOG / DEVELOPMENT / Rules / Skills） |
| `test` | 測試程式或測試資料 |
| `chore` | 建置 / 設定 / 雜項（不含遊戲邏輯） |
| `refactor` | 不改變行為的重構（Prototype 階段盡量避免） |

- `scope`：里程碑編號或模組，例如 `m0`、`m1`、`clock`、`judgment`、`input`、`chart`、`repository`。
- summary 用**英文小寫祈使句**，描述「做了什麼」。
- **禁止**模糊訊息：`update`、`fix`、`test`、`change stuff`、`temp`、`aaa`。
- 範例（本專案採用）：
  - `chore(repository): add git ignore rules and development workflow docs`
  - `feat(m0): establish audio and input clock baseline`
  - `feat(m1): add deterministic judgment core`
  - `feat(m2): add dsp song clock and clock bridge`
  - `fix(m2): correct input clock mapping`
  - `docs: update development log`

### 5.4 什麼一定要進版控
`Assets/`（**所有 `.meta` 必進**）、`Packages/`（含 `packages-lock.json`）、`ProjectSettings/`、
`.cline/`、`.clinerules/`、`CHANGELOG.md`、`DEVELOPMENT.md`、`.gitignore`、`.vsconfig`。

### 5.5 什麼一定不進版控
`Library/`、`Temp/`、`Logs/`、`UserSettings/`、`obj/`、`Build/`、`Builds/`、`.vs/`、
`*.csproj`、`*.sln`、IDE 暫存檔、建置產物（`.apk` / `.aab` / `.unitypackage` / `.app` / `.ipa`）。
詳細規則見 `.gitignore`（每個區塊皆有註解說明）。

### 5.6 提交節奏
```
Plan → Implement → Test → Review diff → CHANGELOG → Commit → 回報 commit hash
```

## 6. CHANGELOG 規則

- 檔案：`CHANGELOG.md`（專案根目錄）。
- **只記錄真正完成並通過驗收的工作**。閱讀、分析、提出方案**不得**寫成完成條目（可寫入「下一步」）。
- 每個里程碑完成 → 新增一筆，標題：`## [YYYY-MM-DD] <Milestone> - <標題>`；**最新條目放最上方**。
- 每個條目**必須**包含以下欄位（缺一不可）：

```
### 新增
### 修改
### 測試
### 驗收結果
### Git Commit
### 風險 / 已知問題
### 下一步
```

- 未 commit 的工作：`Git Commit` 欄位寫「**待提交**」，且**不得**標記為完成。
- 修正類工作：另立 `### 修正` 或在標題標示 `fix` 對應的里程碑。
- 更新 CHANGELOG 是 commit 前的必要步驟（順序：測試通過 → 更新 CHANGELOG → commit）。

## 7. Skill 使用規則

### 7.1 任務 → Skill 對照表

| 本次工作內容 | 必須載入的 Skill |
|---|---|
| 節奏時間 / 判定 / 音符 / BPM / 校準 / 核心循環 | **`unity-rhythm-timing`**（＋對應 references） |
| 玩家輸入、Input Actions、按鍵、rebinding | `unity-input-system` |
| C# / MonoBehaviour / 生命週期 / 序列化 / coroutine | `unity-csharp-scripting` |
| 音符視覺、HUD、選單、結算、版面縮放 | `game-ui-ux` |
| 打擊感（hit-stop、震動、縮放、粒子時機） | `game-feel` |
| 音訊（混音、ducking、打擊音效、音樂處理） | `audio-design` |
| 動畫（Animator、2D 骨架、狀態機） | `unity-animation` |
| 資料資產（ScriptableObject、event channel） | `unity-scriptableobjects` |
| 相機（Cinemachine、運鏡、shake） | `camera-systems` |
| 出包 / IL2CPP / 建置腳本 | `unity-build-pipeline` |

### 7.2 使用規定
1. **必須實際閱讀 `SKILL.md`**，不得只依賴對話記憶。
2. 任務相關時，**必須續讀該 Skill 的 `references/`** 對應檔案（例如節奏工作 → `timing-and-calibration.md`）。
3. 專案專屬 `unity-rhythm-timing` 的 7 條硬規則（dspTime 為唯一判定時鐘、判定為純 C#、五模組解耦、
   不假定操作方式、不沿用 Platformer 架構、不未經確認刪檔、不動無關設定）**優先於**任何通用 Skill 的建議。
4. Skill 與本專案 Rules 衝突時：**以 Rules 與使用者指示為準**，並在回報中說明衝突點。
5. 需要新增/修改 Skill 或 Rule：**先提出方案，經使用者同意**才動。

## 8. 最小修改原則

每次修改**只做完成當前任務所必需的最小改動**。明確禁止「順手做」：

1. 不順便重構其他程式。
2. 不順便美化 / 重新命名既有程式碼。
3. 不順便更新 Unity package 或版本。
4. 不順便修改 `ProjectSettings/`。
5. 不順便修改 URP / Render Pipeline 設定。
6. 不順便刪除或搬移舊 Platformer 程式與資產。
7. 不順便修改 `InputSystem_Actions.inputactions`。
8. 不順便建立或改寫 Scene / Prefab。
9. 不順便加入第三方套件（含 Unity MCP）。
10. 不順便調整 `Assets/` 內與任務無關的資產。

**若發現「順手做」會讓架構更好**：不要直接做，改為在回報中列出「**額外建議**」段落，由使用者決定。

## 9. 測試規則

| 工作類型 | 最低測試要求 |
|---|---|
| 純邏輯（判定、計分、譜面計算） | EditMode 單元測試**必備**；邊界值必須涵蓋（窗內 / 窗外 / 剛好等於 / 同幀多輸入 / 超時） |
| 時間與音訊相關 | 需實測（Editor **與** build 各一次）；記錄量測方式與數值（中位數、樣本數） |
| 視覺 / 輸入手感 | 需在 Play 模式觀察並記錄步驟；若可量化則量化（例如誤差分布） |
| 文件 / 設定 / 版控 | 檔案存在性、路徑、git diff 無意外檔案；不需程式測試 |

**共通要求**

1. 執行測試前先確認 **Unity Console 無 compile error**；有 error 先修，不得繼續。
2. 測試失敗時**不得**標記完成，必須回報：`【Failure】【Cause】【Attempted Fix】【Risk】【Next Action】`。
3. 測試方法與結果必須寫入 `CHANGELOG.md`（**不得只寫「已測試」**）。
4. 若驗收條件要求 build 驗證，**只測 Editor 不算通過**。
5. 確認既有 Platformer 程式未被破壞（Console 無新增 error、既有 Prefab/Scene 未被誤改）。

## 10. 回報格式

### 10.1 工作開始前（Plan，必填，輸出後**停下來等確認**）

```
【Current Milestone】目前在哪個 M？
【Goal】這次只要完成什麼？
【Required Skills】這次讀了哪些 Skill（含 references）？
【Files To Change】準備修改哪些文件？
【Files NOT To Change】明確列出哪些不碰
【Implementation】準備怎麼做？
【Testing】做完如何驗證？
【Risk】有什麼風險？
【Alternative】有沒有更簡單 / 更安全 / 更合理的方案？
【Git】完成後準備使用什麼 commit？
```

> 例外：使用者明確說「直接執行」時可跳過等待，但**仍須**輸出上述 Plan 供事後對照。

### 10.2 工作完成後（必填）

```
【本次完成】實際做了什麼
【驗收結果】測試方式與結果（含數值 / 指令 / 觀察步驟）
【Git Commit】hash + message（未提交則寫「待提交」與原因）
【CHANGELOG】已更新 / 未更新及原因
【目前 Milestone】M?
【下一步】建議的下一步（不自行執行）
【是否需要我批准】是 / 否
【額外建議】非必要但值得考慮的改善（若無則寫「無」）
```

### 10.3 測試失敗時（必填）

```
【Failure】失敗現象（含錯誤訊息 / 量測數值）
【Cause】判定出的原因
【Attempted Fix】已嘗試的修正（含結果）
【Risk】目前狀態的風險（是否破壞既有功能）
【Next Action】建議下一步（等你指示）
```

**禁止**：未測試就回報「完成」、把分析寫成完成、跳過 CHANGELOG、跳過 commit。

## 11. 風險管理

每次工作都必須主動逐項檢查並在 Plan 中回答：

| # | 檢查項 | 若為「是」的處理 |
|---|---|---|
| 1 | 是否會修改 Unity `ProjectSettings/`？ | 先停下來問（附錄 F） |
| 2 | 是否會修改 `Packages/`（含新增/升級套件）？ | 先停下來問 |
| 3 | 是否會修改 `InputSystem_Actions.inputactions`？ | 先停下來問（新增 action map 屬高風險） |
| 4 | 是否會建立 / 改寫 Scene 或 Prefab？ | 先停下來問 |
| 5 | 是否會影響既有 Platformer 程式或資產？ | 先停下來問；不得刪除 / 搬移 |
| 6 | 是否會增加第三方依賴（含 Unity MCP）？ | 先停下來問 |
| 7 | 是否影響 Git 可回復性（大量檔案變動、無法用單一 commit 還原）？ | 拆成多個 commit 或先確認 |
| 8 | 是否可能造成 `.meta` 問題（在 `Assets/` 外手動增刪檔、或移動資產未連同 `.meta`）？ | 使用 Unity 內操作或連同 `.meta` 一起處理 |
| 9 | 是否可能造成 **Editor 與 Build 行為不同**（音訊延遲、繪製、時間）？ | 驗收條件納入 build 測試 |
| 10 | 是否可能造成**音訊時間與輸入時間不同步**？ | 依 `unity-rhythm-timing` 的 ClockBridge 設計，並以量測數據驗收 |

**原則**：任何無法完全確定後果的操作 → 先問，不要試。

## 12. 禁止自行做出的高風險修改

以下行為**一律須先取得使用者明確同意**（沒有同意就是不做）：

1. 刪除、搬移、重新命名任何既有檔案（含 Platformer 程式、Prefab、資產、`.meta`）。
2. 修改 `Packages/manifest.json` / `packages-lock.json`，或安裝/升級任何 Unity 套件（含 Unity MCP）。
3. 修改 `ProjectSettings/` 任何設定（含 Render Pipeline、圖形、品質、輸入、物理、音訊、Time、Player）。
4. 切換 URP Renderer（Forward → Renderer2D）或變更 `Assets/Settings/` 內渲染資產。
5. 建立 / 改寫 / 刪除 Scene 與 Prefab。
6. 修改 `InputSystem_Actions.inputactions`（新增 action map、改鍵位）。
7. 重構既有程式碼（含改名、換命名空間、抽出共用類別）。
8. 新增 asmdef（會改變 Assembly 結構）。
9. 大量新增資產（圖片、音訊、字型、外部套件）。
10. 任何全域執行期設定變更（例如 `AudioSettings.Reset()`、`Application.targetFrameRate` 的全域調整）。
11. `git push`、force push、重寫歷史、刪除分支。
12. 修改 `.clinerules/` 或 `.cline/skills/`（須先提出方案）。

> 若任務非得動到上述項目，正確做法是：**在 Plan 中標明、說明理由與風險、等待批准**。

## 附錄 A：每次工作的強制流程（STEP 0–7）

| STEP | 動作 | 產出 |
|---|---|---|
| 0 | 閱讀 Rules：`.clinerules/01-project-context.md`、`02-unity-conventions.md`、`03-guardrails.md` | 確認專案事實與護欄 |
| 1 | 判斷本次工作需要的 Skill（對照 §7.1） | Skill 清單 |
| 2 | **實際閱讀**相關 `SKILL.md`（不得依賴記憶） | 規則確認 |
| 3 | 依任務閱讀該 Skill 的相關 `references/` | 技術細節確認 |
| 4 | 閱讀本檔 `DEVELOPMENT.md` | 流程與完成條件確認 |
| 5 | 檢查 Git 狀態（`git status`、目前 branch、是否乾淨） | 可回復性確認 |
| 6 | 檢查目前 Milestone 與其完成條件 | 明確目標 |
| 7 | 確認本次任務的**最小修改範圍** | Files To Change / Files NOT To Change |

完成 STEP 0–7 後，輸出 §10.1 的 Plan，並**停下來等待確認**。

## 附錄 B：Plan 模板（複製使用）

```
【Current Milestone】M?
【Goal】
【Required Skills】讀了 ★ 哪些 SKILL.md 與 references
【Files To Change】
【Files NOT To Change】
【Implementation】
【Testing】
【Risk】逐項檢查 §11 十項風險
【Alternative】
【Git】type(scope): summary
```

## 附錄 C：每次修改後的強制流程

1. **檢查 Git diff**（`git status` + `git diff`）→ 確認只有 Plan 中列出的檔案被改。
2. **檢查是否有意外修改**：是否有 `.meta` 遺漏、是否有無關檔被動到、是否有 `Assets/` 外的散檔。
3. **編譯 / 測試**：Unity Console 無 compile error；執行對應驗收（見 §9）。
4. **Console error 先處理**：有 error 不得繼續，不得 commit。
5. **執行該里程碑的驗收標準**（§4）並記錄數據。
6. **確認沒有破壞原 Platformer 程式**：Console 無新增 error，既有 Prefab / Scene 未被改動。
7. **更新 `CHANGELOG.md`**：依 §6 格式，含測試方法與結果。
8. **更新 `DEVELOPMENT.md` 的進度**（§2 當前階段表格的「目前 Milestone」）。
9. **Git commit**：使用 §5.3 規範的 message。
10. **回報 commit hash**（見 §10.2）。

## 附錄 D：Git 實務指令（僅供參考，實際執行需依 Plan 批准）

```bash
# 初始化（✅ 已核准後才執行；建議指定 main）
git init -b main

# 檢查狀態
git status --short
git diff --stat

# 建立 baseline commit（Infra 階段，涵蓋現有專案）
git add .gitignore CHANGELOG.md DEVELOPMENT.md .cline .clinerules Assets Packages ProjectSettings
git commit -m "chore(repository): add git ignore rules and development workflow docs"

# 里程碑 commit 範例
git add Assets/Scripts/YingYun/Timing Assets/Scripts/YingYun/Judgment
git commit -m "feat(m1): add deterministic judgment core"

# 檢視最近紀錄
git --no-pager log --oneline -10
```

**注意**
- `git add .` 在 `.gitignore` 完成後才可使用；使用前先 `git status` 確認 **沒有** `Library/`、`Temp/`、`UserSettings/`。
- 不得 `git add -f` 強制加入 `.gitignore` 內的可重建檔案。
- 每次 commit 前後都要確認 `Assets/**/*.meta` 沒有遺漏（未 commit 的 `.meta` 會讓他人的專案壞掉）。

## 附錄 E：禁止「假完成」

以下任一情況**都不算完成**，不得在 `CHANGELOG.md` 標記為完成、不得回報「完成」：

1. 只建立了文件。
2. 只寫了程式但沒有測試。
3. Unity 有 compile error。
4. 測試沒有實際執行（或沒有記錄結果）。
5. 驗收條件要求 build 驗證，卻只在 Editor 測試。
6. Git 沒有 commit（`Git Commit` 必須是實際 hash）。
7. `CHANGELOG.md` 沒有更新。
8. 驗收條件沒有全部達成。
9. 破壞了既有 Platformer 程式或既有資產。
10. 只做了「閱讀、分析、提出方案」。

**完成的唯一標準：實作 + 測試 + 驗收 + 文件 + Git commit，五者全部到位。**

## 附錄 F：待批准事項清單（高風險，未經同意不得執行）

| # | 項目 | 影響 | 現況 |
|---|---|---|---|
| F1 | `git init -b main` + baseline commit | 建立版控基線，讓所有修改可回溯 | **已批准並完成（M0）** |
| F2 | 建立第一個 Scene（`YingYun_Gameplay.unity`） | 建立可 Play 驗收的節奏原型場景 | **已批准並完成（M2）** |
| F3 | 在 `InputSystem_Actions.inputactions` 新增 `Rhythm` action map（6 鍵） | 修改既有輸入資產；暫停／重開由原型控制器直接讀取 P／R | **已批准並完成（M2）** |
| F4 | 新增 asmdef：`YingYun.Runtime` / `YingYun.Unity` / `YingYun.Tests` | 隔離純 C# 核心、Unity 執行期與 EditMode 測試 | **已批准並完成（M1／M2）** |
| F5 | `.gitattributes`（`* text=auto`、LF 規範） | 避免跨平台換行造成 diff 噪音 | 建議採用，等待批准 |
| F6 | CJK TMP 字型資產（中文 UI） | 目前 TMP 只有 `LiberationSans`（英文），無法顯示中文 | 等待批准（M7 需要） |
| F7 | `ProjectSettings` 調整（產品名《影韵》、解析度 1920×1080、`runInBackground`） | 影響 PC 節奏遊戲體驗 | 等待批准 |
| F8 | 切換 URP Renderer → Renderer2D（啟用 Light2D） | 影響渲染設定 | **建議延後**，M6 先用 Sprite/材質/粒子替代 |
| F9 | 清除無用的 Platformer 程式 / 資產 | 專案乾淨度 | 需先提出清單，等待批准 |
| F10 | 新增 `.clinerules/00-development-workflow.md` | 讓 STEP 0–7 每次自動生效 | **已批准並完成** |

## 附錄 G：Cline 開發工作流程 Rule（已建立）

`.clinerules/00-development-workflow.md` 已建立為 always-on 摘要；實際內容以該檔案為準，核心要求如下：

```markdown
# 00 開發工作流程（每次工作必遵守）

1. 工作開始前，執行 DEVELOPMENT.md 附錄 A 的 STEP 0–7。
2. 必須先輸出 DEVELOPMENT.md §10.1 的 Plan，並停下等待使用者確認；
   除非使用者明確說「直接執行」。
3. 遵守 §8 最小修改原則：只改完成任務所需的最小範圍，不做「順手」的事；
   若發現更好做法 → 列為「額外建議」，由使用者決定。
4. 高風險動作（DEVELOPMENT.md §12 / 附錄 F）一律先問，不得自行執行。
5. 完成後依 §10.2 回報，並完成附錄 C 的 10 個步驟（含 CHANGELOG 與 Git commit）。
6. 禁止「假完成」（附錄 E）。
7. 詳細規範一律以 DEVELOPMENT.md 為準。
```

---

## 文件版本紀錄

| 版本 | 日期 | 變更 |
|---|---|---|
| 1.0 | 2026-09-20 | 建立本規範：專案目標、階段落點、M0–M7 里程碑與完成條件、Git / CHANGELOG / Skill / 測試 / 回報規範、最小修改原則、風險管理、禁止事項、附錄 A–G |
| 1.1 | 2026-09-21 | M2 收尾：更新 §2「目前 Milestone」與「重大缺口」（`.unity` 場景與 asmdef 已就位、音符視覺尚未實作、真人驗收樣本缺口），M2 驗收數據記於 `CHANGELOG.md` |
| 1.2 | 2026-09-21 | 同步 M0–M2 完成後的專案事實、Cline Rule 數量與附錄 F 批准狀態 |
| 1.3 | 2026-09-21 | M3 結案：六部位放射式音符視覺、物件池與判定回饋通過驗收；下一步回補 M2 真人數據 |
| 1.4 | 2026-09-21 | M4 結案：四檔判定、`DifficultyConfig` 評級門檻、中文 HUD／結算面板、EditMode 32/32 與 Windows build 驗收 |
| 1.5 | 2026-09-21 | M5 結案：嚴格 Hold 尾判、原子組合音符、段落合勢／斷勢回饋、180 秒舞蹈譜面、EditMode 51/51 與 Windows build 煙測 |
| 1.6 | 2026-09-21 | M6 結案：資料驅動六部位關節操偶、六根控制線張力、Hold 持續拉扯、幾何剪影光幕、EditMode 63/63 與 Windows build 煙測 |
| 1.7 | 2026-09-21 | M6 驗收修正：S／身體判定移至底排中央；木偶改由每個原始按鍵即時拉動，判定結果不再限制動作；EditMode 65/65 與 Windows build 通過 |
| 1.8 | 2026-09-21 | M6 自動 Miss 修正：普通 Miss 不再觸發頭部／軀幹失勢旋轉，木偶只由真實輸入與有效 Hold 驅動；EditMode 66/66 與 Windows build 通過 |
| 1.9 | 2026-09-21 | M6.1 結案：關節改為帶慣性的彈簧－阻尼連續操偶（固定步長積分、拉繩事件疊加、連鎖延遲 30–90 ms、回彈與同鍵手勢變化）；EditMode 76/76、獨立 C# 驗證 23/23、Windows build 通過 |
| 1.10 | 2026-09-21 | M6.2 結案：六鍵皆有點按與長按（圓形 vs 長條橢圓）、長按期間維持宣告角度與繩索張力、長按失誤細分「早放／未撐住」；EditMode 90/90、獨立 C# 譜面驗證 11/11、Windows build 通過 |
| 1.11 | 2026-09-21 | M6.5 結案：六軌改為六種操演意圖，頂部控制繩改為側向／下方剛性竹桿；補上幕布背光、戲台框、分片衣飾與鉚釘，EditMode 91/91、Windows build 通過 |

