# 專案情境：皮影戲題材 2D 節奏遊戲（Prototype）

## 遊戲目標（現階段）

- **類型**：Unity **2D** 音樂節奏遊戲（rhythm game），畫面以 2D 為主，**不做 3D 建模**。
- **題材**：中國傳統**皮影戲**（shadow puppetry）視覺風格。
- **操作體驗方向**：類似 osu! 的節奏體驗；**具體操作方式尚未確定**（點擊／按鍵／拖曳／滑條皆可能）。
- **現階段範圍**：只做**核心玩法 Prototype**。優先完成核心循環：
  `音樂 → 音符 → 玩家輸入 → 時間判定 → 得分/連擊 → 遊戲回饋`
- **暫時不做**：完整劇情、完整美術、大量內容與關卡。

## 目前專案的來源（重要）

本專案目前內容來自 **Unity 官方 2D Platformer Microgame 教學模板**（`Assets/Tutorials/1_GetStarted … 6_WhatsNext`、
`Assets/Mod Assets/`、`Assets/Scripts/{Core,Gameplay,Mechanics,Model,UI,View}`、`Documentation/PlatformerTemplateUserGuide.pdf`）。

因此：

- **可以**複用 Unity 專案基礎設施、C# 寫法風格、Input System 用法、Animation、UI 設定。
- **不可以**把 `PlayerController`、`EnemyController`、`Jump`、`PatrolPath`、`KinematicObject`、`Simulation`、
  `Platformer.*` 命名空間當成新遊戲的架構基礎，也不要圍繞平台跳躍／橫向捲軸／關卡地形的思路做設計。
- 既有 Platformer 程式碼若判斷已無用途，**先回報給使用者，再決定是否刪除或重構；不要自行刪除。**

## 環境事實（以此為準，不要憑印象假設）

| 項目 | 值 |
|---|---|
| Unity 版本 | 6000.6.2f1（Unity 6.x） |
| Render Pipeline | URP 17.6.0，Renderer 為 **Forward Renderer**（`Assets/Settings/URP_ForwardRenderer.asset`），**不是 Renderer2D** |
| 專案類型 | 2D（`com.unity.feature.2d` 2.0.2、Physics2D、Sprite、Tilemap） |
| 輸入 | **Input System 1.19.0**（`Assets/Settings/InputSystem_Actions.inputactions`；舊 InputManager 不使用） |
| UI | **uGUI**（`Assets/Prefabs/UI Canvas.prefab`、`MainUIController`、`MetaGameController`），不是 UI Toolkit |
| 相機 | Cinemachine 6.6.0 |
| 音訊 | `Assets/Audio/*.wav`（含 `Music.wav`）、`PlayAudioClip.cs`（StateMachineBehaviour）；**尚無 AudioMixer** |
| 組件 | 新遊戲程式已拆分為 `YingYun.Runtime`（純 C#）、`YingYun.Unity`（Unity 執行期）與 `YingYun.Tests`（EditMode 測試）asmdef；Platformer 舊碼仍維持原架構 |
| 場景 | `Assets/Scenes/YingYun_Gameplay.unity` 為目前節奏原型場景，已註冊至 `EditorBuildSettings` |
| 版控 | Git 已初始化；`main` 為主線，M0、M1、M2 已完成並提交 |
| 目前 Milestone | **M3 已結案**；下一步補齊 M2 真人驗收，完成後進入 **M4 計分與結算** |
| Cline 設定 | Skills：`.cline/skills/`（10 個）；Rules：`.clinerules/`（`00`–`03` 共 4 份） |

## 開發語言與溝通

- 與使用者溝通使用**繁體中文**。
- 程式碼命名、註解風格沿用現有 C# 慣例；UI 文案可日後再做本地化。
