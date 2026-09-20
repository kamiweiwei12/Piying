# Unity 與 C# 開發慣例（本專案）

## 時間與節奏（最高優先）

- 高精度音樂／節奏判定**一律使用 `AudioSettings.dspTime`**；**禁止**以 `Time.time`、`Time.deltaTime` 累加或
  `AudioSource.time` 作為判定主時鐘。
- 音樂播放使用 `AudioSource.PlayScheduled(dspTime)`，不使用 `Play()` 起播。
- 節奏判定核心應為**不依賴 MonoBehaviour / AudioSource 的純 C# 類別**，時間來源以介面注入，以便測試。
- 「音樂時間軸 / 音符資料 / 輸入 / 判定 / 回饋」必須是彼此解耦的模組。
- **尚未確定最終音符操作方式**（類似 osu! 的體驗，但點擊／按鍵／拖曳／滑條未定）→ 不要寫死操作，保留抽象層。
- 詳細規範見 skill：`.cline/skills/unity-rhythm-timing/SKILL.md`。

## 程式碼風格

- 語言／框架：C#（Unity 6000.6.2f1）、Unity **2D**。
- 命名空間：既有程式為 `Platformer.*`（模板遺留）。**新遊戲程式碼請使用獨立命名空間**，不要把新功能塞進 `Platformer.Mechanics`。
  建議（**待使用者確認**）`Rhythm.*` 或 `ShadowPlay.*`，確認後才建立檔案。
- 新檔案的資料夾建議（**待確認後才建立**）：`Assets/Scripts/Rhythm/{Core,Data,Input,Judgment,View,UI,Config}`。
- 欄位慣例沿用現有風格；設計新程式碼時優先 `[SerializeField] private` 而非 public 欄位，並在 `Awake` 快取元件。
- 沒有 asmdef 的情況下，新程式碼會進 `Assembly-CSharp`；如需 asmdef 拆分，**先與使用者討論**。
- 註解：沿用現有 XML `<summary>` 風格，內容用繁體中文亦可。

## 使用已安裝的技術（不要另起爐灶）

- **輸入**：Input System 1.19（`Assets/Settings/InputSystem_Actions.inputactions`）；需要的 action 以資產為準，
  新增 action map 前先報告。
- **UI**：uGUI（Canvas / Prefab 形式），與 `game-ui-ux` skill 的版面規則一致；不要引入 UI Toolkit。
- **相機**：Cinemachine 6.6.0；2D 運鏡沿用 `camera-systems` skill 的死區／前瞻／邊界夾制做法。
- **動畫**：Animator + 2D Sprite 動畫（`com.unity.2d.animation` 已安裝）；2D 皮影戲骨架可日後使用，
  但**不得讓動畫驅動判定**。
- **音訊**：`AudioSource` / AudioClip；要做混音與 bus 架構時參考 `audio-design` skill（目前專案尚無 AudioMixer）。
- **渲染**：URP 17.6.0 目前使用 **Forward Renderer**。若要 Light2D／2D 影子效果需改用 Renderer2D
  → 這會變更 `Assets/Settings` 與 `GraphicsSettings`，**必須先取得使用者同意**。
- **建置**：出包相關參考 `unity-build-pipeline` skill；`Assets/BurstAotSettings_WebGL.json` 顯示可能做 WebGL 版。

## 效能（Prototype 也要守）

- 熱路徑（每幀、每音符）不產生 GC：使用物件池、struct、避免 LINQ 與字串串接。
- 音符時間軸以「已排序清單 + 前進指標」掃描，不要每幀掃整份譜面。
- 不在 `Update()` 中呼叫 `GetComponent` / `FindObjectOfType` / `Camera.main`。
