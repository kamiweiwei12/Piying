---
name: unity-rhythm-timing
description: >
  以 sample-accurate 的時間軸實作 Unity 6 2D 節奏遊戲核心循環（音樂 → 音符 → 玩家輸入 → 時間判定 →
  得分/連擊 → 回饋）。涵蓋 AudioSettings.dspTime、BPM/Beat/Bar 換算、音符時間軸與生成、Input System
  取樣與時間戳、Note Hit Window、Perfect/Good/Miss 判定、Combo/Score/Accuracy、音符生命週期、音訊與輸入
  延遲校準，以及 UI/動畫/音效與節奏時間同步。Use when working on rhythm game, 節奏遊戲, 音ゲー, 音樂遊戲,
  BPM, beat, 拍點, 音符, note, 譜面, chart, 判定, judgment, Perfect/Good/Miss, hit window, combo, 連擊,
  得分, dspTime, 對拍, 時間軸, 延遲校準, offset, calibration, 音遊, 打歌。
---

# Unity 2D 節奏遊戲核心時序（Rhythm Timing & Judgment Core）

本專案是**皮影戲題材的 Unity 2D 節奏遊戲**（Prototype 階段），畫面以 2D 為主、不做 3D 建模，核心循環是：

```
音樂時間軸 → 音符生成 → 玩家輸入 → 時間判定 → 得分/連擊 → 遊戲回饋
```

這個 skill 的職責就是讓上面六件事**共用同一個音樂時鐘**，並且彼此**解耦**，使日後的音符種類與操作方式可以替換而不需重寫判定核心。

## 何時使用

- 建立／修改任何與「節奏時間」、「拍點」、「音符」、「判定」、「連擊」、「準確率」相關的程式。
- 需要把音訊播放、輸入、UI、動畫、特效對齊到音樂時間軸時。
- 需要做**延遲校準（audio offset / input offset）**時。
- 需要讀譜面（chart）資料、BPM 變速、段落（bar/measure）時。

**何時不要用**（改用其他 skill）：

- 純 C# MonoBehaviour 生命週期、序列化、coroutine 寫法 → `unity-csharp-scripting`
- Input Actions 資產、action map、rebinding 細節 → `unity-input-system`
- Animator 狀態機／blend tree／2D 骨架動畫設定 → `unity-animation`
- 資料資產（ScriptableObject）與 event channel 的通用設計 → `unity-scriptableobjects`
- HUD/選單版面與解析度自適應 → `game-ui-ux`
- 打擊感、螢幕震動、hit-stop 的通用配方 → `game-feel`
- 匯流排/混音/adaptive music 的通用音訊架構 → `audio-design`
- 2D 相機運鏡與邊界 → `camera-systems`
- 出包、IL2CPP、建置腳本 → `unity-build-pipeline`

## 鐵則（Hard Rules，違反即為錯誤實作）

1. **絕不以 `Time.time` / `Time.deltaTime` 累加作為高精度音樂判定時鐘。** 唯一主時鐘是 `AudioSettings.dspTime`。
   `Time.*` 只能用在與判定無關的裝飾性視覺（例如背景特效），且不得回饋進判定。
2. **判定核心必須是純 C#、無 MonoBehaviour、無 AudioSource 依賴**，時間來源以介面注入（預設實作包 dspTime，測試實作包假時鐘）。
   理由：可在沒有音訊裝置的環境做 EditMode 測試，也能重播（replay）驗證。
3. **五個模組必須解耦**：`SongClock`（時間軸）、`ChartData`（音符資料）、`NoteInputSource`（輸入）、`Judgment`（判定）、`Feedback`（回饋）。
   任一模組只能透過介面與資料結構溝通，不得互相直接抓取對方物件。
4. **不得假定最終操作方式。** 目前只確定「類似 osu! 的節奏體驗」，最終是點擊／按鍵／拖曳／滑條／其他皆未定。
   因此輸入與音符型別一律走抽象（`INoteInputSource` + `NoteTypeDefinition`），**禁止**把滑鼠座標判定、單一按鍵判定寫死在判定核心裡。
5. **不沿用 Platformer 架構設計新遊戲。** 既有 `PlayerController`／`EnemyController`／`PatrolPath`／`KinematicObject`／`Simulation` 等
   是 Microgame 教學模板遺留物，可參考其 C# 寫法與 Input System 用法，但**新遊戲的架構不得圍繞 Player/Enemy/Jump 設計**。
6. **未經使用者確認，不刪除、不移動、不重構既有程式與資產。** 發現無用程式碼時先回報，由使用者決定。
7. **不主動修改** `Assets/`、`Packages/`、`ProjectSettings/` 中與本任務無關的內容；新增檔案前先說明路徑與用途。

## 核心時鐘模型

```csharp
// 設計示意（尚未建立任何專案檔案）：以 dspTime 為唯一主時鐘
double dspStart = AudioSettings.dspTime + LEAD_IN_SECONDS;   // 預約起播時間
musicSource.PlayScheduled(dspStart);                          // 由音訊執行緒精準起播

double SongTimeRaw  = AudioSettings.dspTime - dspStart;       // 未校正的歌曲時間（秒）
double SongTime     = SongTimeRaw - audioOffsetSeconds;       // 校正後的歌曲時間
double Beat         = SongTime * (bpm / 60.0);                // 節拍位置（含小數）
```

重點：

- **務必用 `PlayScheduled(dspTime)`**，不要用 `Play()`：`Play()` 的實際起播點不確定，會造成固定偏移。
- `dspTime` 是音訊硬體取樣時鐘（以 buffer 為單位前進），**不受 frame rate 影響**，因此跨裝置穩定。
- `AudioSource.time` 會因緩衝／解碼而跳動，**不可**當判定時鐘（只可用於顯示或除錯）。
- 音樂時間可為**負值**（LEAD_IN 期間）→ 時間軸設計必須允許「開場倒數」的負時間區段。
- 節奏遊戲的「暫停」不是 `Time.timeScale = 0`：`dspTime` 不會因 timeScale 停止。必須
  `AudioSource.Pause()` 並把「暫停期間的 dspTime 差」累加進 `pausedDuration`，恢復時 `UnPause()` 並修正 `dspStart`。
- 換曲／重開：以新的 `dspStart` 重建時間軸，不要嘗試用 `AudioSource.time` 對齊。
- 若需要改變輸出取樣率或裝置，`AudioSettings.Reset()` 之後所有 dspTime 基準都會改變 → 必須重建時鐘。

## 標準工作流程（每次動到節奏核心都照這個順序）

1. **先讀參考檔**：`references/timing-and-calibration.md`（時序與校準）、`references/architecture-decoupling.md`（解耦與擴充點）、
   `references/judgment-and-scoring.md`（判定／計分）、`references/chart-format-and-timeline.md`（譜面與時間軸）。
2. **先定資料契約**：`ChartData` / `NoteData` / `HitInput` / `JudgmentResult` 的欄位與單位（秒 vs 毫秒、beat vs songTime）先寫清楚。
3. **用假時鐘寫判定**：以可注入的假時鐘在 EditMode 驗證判定邊界（不需要音訊裝置）。
4. **再接真實音訊**：`PlayScheduled(dspTime)` → `ISongClock` 實作。
5. **最後接回饋**：UI／動畫／特效／音效全部**只讀節奏時間**，不自己累加計時。
6. **回報**：列出改了哪些檔案、判定邊界如何驗證、尚待確認的決策點。

> 這個順序的理由：判定能不能正確，只跟「時間與數學」有關；先把它隔離出來測試，才不會被音訊裝置與畫面問題干擾。

## 模組契約（介面草案）

```csharp
// 設計示意：實際命名待使用者確認後再建立檔案
public interface ISongClock                 // 音樂時間軸（唯一真實來源）
{
    double SongTime { get; }                // 校正後歌曲時間（秒，可為負）
    double Beat { get; }                    // 節拍位置（含小數）
    bool   IsRunning { get; }
    double DspTime { get; }
}

public interface INoteInputSource           // 輸入抽象：不假設是點擊/按鍵/滑條
{
    bool TryDequeueHit(out HitInput hit);   // hit 內含：lane/noteType 提示、輸入時間戳
}

public interface IJudgmentStrategy          // 判定策略：可替換成不同規則
{
    JudgmentResult Evaluate(in NoteState note, double hitSongTime, in TimingConfig cfg);
}

public interface IFeedbackSink              // 回饋匯流：UI/動畫/SFX 都掛在這裡
{
    void OnNoteSpawned(in NoteState note);
    void OnJudged(in JudgmentResult result);
}
```

資料流（單向，避免互相依賴）：

```
AudioSettings.dspTime ──► ISongClock ──┬──► ChartScheduler ──► 音符生命週期（生成/前進/回收）
                                       │
Input System 事件 ──► INoteInputSource ─┴──► Judgment（用 SongTime 比對）
                                                 │
                                                 └──► IFeedbackSink ──► UI / 動畫 / 特效 / SFX
```

### 五個模組的責任邊界

| 模組 | 負責 | 不負責 |
|---|---|---|
| `SongClock` | dspTime、`PlayScheduled`、暫停／續播、換曲、校正偏移 | 判定、UI、音符內容 |
| `ChartData` | BPM 事件、音符清單、難度參數、譜面時間單位轉換 | 何時生成（那是 Scheduler 的事） |
| `NoteInputSource` | 取樣輸入、附上**輸入時間戳**、lane/type 提示 | 判定的寬容度與分數 |
| `Judgment` | 判定窗、Perfect/Good/Miss、Combo、Score、Accuracy | 顯示、音效播放方式 |
| `Feedback` | 依判定結果與節奏時間播放 UI/動畫/SFX/粒子 | 決定分數 |

## 核心循環的六個對應點

| 玩家看得到的循環 | 由哪個模組負責 | Prototype 最小驗收 |
|---|---|---|
| 音樂 | `SongClock` | 音樂與節拍指示器不會漂移（跑 3 分鐘以上仍對齊） |
| 音符 | `ChartData` + `ChartScheduler` | 譜面音符能依時間軸準時出現在預定位置 |
| 玩家輸入 | `INoteInputSource` | 輸入帶有正確時間戳（不是「這幀才處理」） |
| 時間判定 | `Judgment` | 邊界可測：±N ms 內外分別得到預期判定 |
| 得分/連擊 | `Judgment`（單一真實來源） | Combo/Score/Accuracy 由判定事件驅動，UI 只顯示 |
| 遊戲回饋 | `IFeedbackSink` 實作 | 打擊特效／音效與拍點同步，不慢半拍 |

## 輸入取樣：一定要帶時間戳

- 節奏判定的精度上限＝輸入時間戳精度。**不要**在 `Update()` 裡「看到按鍵就當作現在才按下」。
- Input System 提供事件時間：`InputAction.CallbackContext.time`（以及互動事件中的 `time`），
  該時間與 `dspTime` 同屬「真實時間軸」，可直接與 `SongTime` 比較。
- 建議把輸入事件**入列（queue）**，在判定步驟一次消化；佇列元素必須攜帶時間戳與來源。
- 若使用 `InputAction` 的 polling（`WasPressedThisFrame`），精度只有「一個 frame」→ 只用於非判定用途（例如選單）。

## 判定、計分與延遲校準（重點摘要）

- **判定窗以「校正後的歌曲時間差」為準**（`|hitSongTime - noteSongTime|`），不是以 frame 數或螢幕時間。
- Prototype 起始建議（**可調參數，勿寫死**，之後依實際手感調整）：

| 判定 | 容許誤差（絕對值） |
|---|---|
| Perfect | ≤ 40 ms |
| Good | 40–100 ms |
| Miss | > 100 ms（或漏打超過 late 窗） |

- 判定要有**優先序**：同一顆音符只被吃一次；多筆輸入同時到達時，取「距離音符最近」的那一筆，
  其餘輸入不可被丟棄（它們可能屬於其他音符），要放回佇列或對應到最近的未判定音符。
- `Combo` 只在非 Miss 時累加；Miss 斷連與否由設定決定（Prototype 先斷連）。
- `Score`、`Accuracy`、`Combo` 的公式只能存在**一個地方**（`Judgment`），UI 只讀取結果。
- 音符生命週期建議狀態機：`Pending → Active（進入可見/可打範圍）→ Judged（Perfect/Good/Miss）→ Released（回收至 Pool）`，
  並保留 `HoldHeld` 之類的中間狀態以利未來擴充長按／滑條。
- **延遲校準要分成兩個獨立偏移**：
  - `audioOffsetMs`：音訊輸出鏈延遲（藍牙耳機可達 150–300 ms）
  - `inputOffsetMs`：輸入路徑延遲（裝置／驅動）
  兩者不可混為一談；校準流程與量測方法見 `references/timing-and-calibration.md`。
- 校準值必須**可即時調整、可保存**（`PlayerPrefs` 或 `Application.persistentDataPath` 下的 JSON），並提供 ±ms 微調 UI。

## 效能與穩定性

- 音符一律物件池化；判定與輸入資料用 struct，避免每幀配置。
- 譜面先排序，判定時用「指標前進」找出「已進入判定範圍」的音符，不要每幀掃全譜。
- 大量音符的顯示（osu! 類玩法可能有數百～數千顆）：只讓「可見時間窗內」的音符 active。
- 不要用協程逐一計時每個音符；用**相對節奏時間的插值**計算位置（可自然處理變速與延遲校正）。
- 幀率不穩不會影響判定正確性（因為判定只看 dspTime），但會影響**視覺平滑度** → 視覺可用 `Mathf.Lerp`/插值補償。

## 與其他已安裝 skill 的分工

| Skill | 在節奏遊戲中負責 |
|---|---|
| `unity-rhythm-timing`（本 skill） | 節奏時鐘、譜面時間軸、判定、計分、延遲校準、核心循環整合 |
| `unity-csharp-scripting` | MonoBehaviour 生命週期、序列化、coroutine 的正確寫法（不含判定邏輯） |
| `unity-input-system` | Input Actions 資產、action maps、玩家裝置與 rebinding |
| `unity-animation` | 打擊成功／失敗的 Animator 狀態與 2D 骨架動畫（由判定事件驅動） |
| `unity-scriptableobjects` | 歌曲設定、難度、判定參數、事件通道的資料資產化 |
| `game-ui-ux` | 分數／連擊／準確率 HUD、選曲與結算畫面的版面與縮放 |
| `game-feel` | 打擊感：hit-stop、畫面震動、放大縮小、粒子，全部同步於節奏時間 |
| `audio-design` | 混音架構、音樂 ducking、打擊音效變體、adaptive music |
| `camera-systems` | 2D 鏡頭運鏡（例如跟隨節奏的輕推、shake 的實作方式） |
| `unity-build-pipeline` | Prototype 出包（桌面／WebGL）、IL2CPP、建置腳本 |

## 皮影戲題材的技術備註

- 視覺一律 2D：Sprite、Sprite Shape（已安裝 `com.unity.2d.spriteshape`）、2D 骨架動畫（已安裝 `com.unity.2d.animation`）。
- 皮影戲的「光影」效果若要用 **Light2D**，需要 URP 的 **Renderer2D**；本專案目前是 Forward Renderer
  → 這是會影響 `Assets/Settings` 與 `GraphicsSettings` 的變更，**必須先取得使用者同意**。
  在未切換前，可用 Sprite 疊圖、加法混合材質、粒子與後處理模擬剪影光感。
- **判定與視覺必須分離**：即使皮影戲的美術之後大幅翻新，判定核心不應改動。

## 反模式（Anti-patterns）

- ❌ 用 `Time.time` 累加或 `AudioSource.time` 當判定時鐘。
- ❌ 用 `Play()` 起播音樂，之後才想辦法補償偏移。
- ❌ 以 `Time.timeScale = 0` 暫停節奏遊戲。
- ❌ 把判定寫進 UI 腳本或 Canvas 的 `Update()`。
- ❌ 每顆音符一個 `AudioSource`、每次打擊 `Instantiate` 特效。
- ❌ 把「osu! 的滑鼠座標判定」或「單一按鍵」寫死在判定核心。
- ❌ 每個音符用一個 coroutine 計時。
- ❌ 直接沿用 `Platformer.Mechanics` 的架構與命名來包裝新遊戲。

## 參考檔

- `references/timing-and-calibration.md` — dspTime 數學、暫停／續播、音訊與輸入延遲量測與校準程序
- `references/architecture-decoupling.md` — 模組介面、資料契約、擴充點（新音符型別／新操作方式）與資料夾規劃
- `references/judgment-and-scoring.md` — 判定窗、輸入配對演算法、Combo/Score/Accuracy 公式、音符生命週期
- `references/chart-format-and-timeline.md` — BPM/Beat/Bar ↔ 秒的換算、譜面資料格式、生成與回收策略

