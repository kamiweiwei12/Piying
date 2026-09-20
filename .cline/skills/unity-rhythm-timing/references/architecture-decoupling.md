# 架構解耦與擴充點

> 目的：**音樂時間軸 / 音符資料 / 輸入 / 判定 / 回饋** 五者解耦，讓「最終操作方式」與「新音符型別」可以後加而不改判定核心。

## 1. 分層與目錄（建議，尚未建立，需使用者確認後才落地）

```
Assets/Scripts/Rhythm/            # 新遊戲程式碼獨立於 Platformer.*
├── Core/        # ISongClock、SongClock（dspTime 實作）、Schedule/Timeline、Pool
├── Data/        # ChartData(SO)、NoteData、TimingPoint、DifficultyConfig
├── Input/       # INoteInputSource、InputSystem 事件佇列與時間戳轉換
├── Judgment/    # 純 C#：JudgmentEngine、判定窗、Combo/Score/Accuracy（無 UnityEngine 依賴）
├── View/        # 音符顯示、打擊特效、皮影戲視覺（只讀節奏時間）
├── UI/          # HUD、選曲、結算、校準畫面
└── Config/      # ScriptableObject 設定資產（判定參數、偏移、難度）
```

- `Judgment/` 內**只允許** `System.*`、`UnityEngine.Mathf` 之類的純數學依賴；這樣才能 EditMode 測試與 replay。
- 命名空間建議（**待確認**）：`Rhythm.*` 或 `ShadowPlay.*`；不要放進 `Platformer.*`。

## 2. 資料契約（單位一次講清楚）

| 型別 | 欄位（建議） | 單位 |
|---|---|---|
| `TimingPoint` | `timeSec`, `bpm`, `beatsPerBar`, `metronome` | 秒 |
| `NoteData` | `id`, `typeId`, `lane`, `timeSec`, `durationSec`, `params` | 秒（`durationSec = 0` 表示單點音符） |
| `HitInput` | `inputTimeSec`（時間戳）, `laneHint`, `sourceId`, `payload` | 秒 |
| `JudgmentResult` | `noteId`, `grade`, `errorMs`, `comboAfter`, `scoreAfter` | 毫秒 |

**規則**：內部運算與資料一律用**秒（double）**，只在 UI 顯示時轉毫秒。避免兩種單位混用。

## 3. 擴充點：新音符型別

```
NoteTypeDefinition (ScriptableObject)     // 一種音符型別一份
├─ typeId                 : string        （"tap" / "hold" / "slide" / "chain" …）
├─ requiredInputKinds     : flags         （這型別接受哪些輸入種類）
├─ shape / prefabRef      : 顯示與碰撞形狀
├─ defaultWindows         : 判定窗覆寫（可空 → 用難度預設）
└─ extensionStrategy      : IJudgmentStrategy 參照或識別碼
```

- 判定核心**只認 `typeId` 與策略介面**，不認具體型別。
- 新增型別＝新增一份 `NoteTypeDefinition` + 一個 `IJudgmentStrategy` 實作，**不動判定引擎**。

## 4. 擴充點：新操作方式（目前未定）

```
INoteInputSource（單一介面）
├─ KeyboardKeyInputSource      （按鍵）
├─ PointerTapInputSource       （滑鼠／觸控點擊）
├─ DragOrSlideInputSource      （拖曳／滑條）
└─ ReplayInputSource           （重播錄製的輸入，用於測試與驗證）
```

- 每個實作只負責「把某種操作轉成 `HitInput`（含時間戳）」，**不含任何判定邏輯**。
- 切換操作方式＝換一個 `INoteInputSource` 實作（或依 `NoteTypeDefinition.requiredInputKinds` 混合多個）。
- 若之後要做「osu! 式游標移動 + 點擊」，需要的是：游標位置也走 `HitInput.payload`，
  判定策略改成「位置 + 時間」雙條件 → 這仍然不用改 `JudgmentEngine` 的時間軸處理。

## 5. 依賴方向（禁止反向依賴）

```
Data ◄── Core ──► Judgment ──► Feedback(interface)
                 ▲
Input ───────────┘
View / UI ──► (只讀 Core/Judgment 的公開狀態，且不得被它們反向引用)
```

- `Core` 不知道 `View` 存在；`Judgment` 不知道 UI／Animator／AudioSource 存在。
- 回饋一律透過 `IFeedbackSink`（或 event channel）往外送，實作者可以是 UI、Animator、粒子、音效，
  也可以是「錄影／測試用的假實作」。

## 6. 測試策略（不需音訊裝置）

- `FakeSongClock`：可 `Advance(deltaSec)` 的假時鐘 → 在 EditMode 測判定邊界、連擊、計分。
- `ReplayInputSource`：餵入預錄 `HitInput` 序列 → 驗證同一份譜面得到可重現的結果（regression）。
- 這是**驗收節奏核心的主要手段**；Play 模式的音樂只在最後接上時驗證。

## 7. Prototype 實作順序（建議）

1. `Judgment`（純 C#）＋ `FakeSongClock` ＋ EditMode 測試 ← 先做這個
2. `SongClock`（dspTime、PlayScheduled、暫停續播）
3. `ChartData` + Scheduler（音符生成/回收、時間軸掃描）
4. `INoteInputSource`（先用最簡單的按鍵實作打通流程）
5. `View` + `IFeedbackSink`（音符顯示、打擊特效、SFX）
6. HUD（Combo/Score/Accuracy）＋ 校準畫面
7. 之後才做音符型別擴充與皮影戲視覺替換
