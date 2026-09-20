# 判定、計分與音符生命週期

## 1. 判定窗（Hit Window）

判定窗以**毫秒的節奏時間誤差**定義，且左右（early / late）可不同：

```
errorMs = (hitInputTimeSec - note.timeSec) * 1000.0     // 負 = 早，正 = 晚
```

建議 Prototype 起始值（**必須是 ScriptableObject 上的可調參數**）：

| 等級 | early 窗 | late 窗 | 說明 |
|---|---|---|---|
| Perfect | −40 ms | +40 ms | 手感核心，之後可依玩家回饋收窄 |
| Good | −100 ms | +100 ms | 涵蓋範圍較寬，避免挫折 |
| Miss（未打） | — | > lateGood | 音符越過 late 窗即自動 Miss |

- 若要更細（例如 osu! 的 300/100/50），把 `grade` 改成列舉並允許每個等級一組窗。
- 進階：窗值可隨 combo、HP、或段落縮放（但 Prototype 先固定）。
- **不要**用「frame 差」或像素距離當窗（前者受幀率影響，後者受解析度影響）。

## 2. 輸入－音符配對演算法（可重現、可測試）

```
for each 音符 in 已進入判定範圍的音符（依 timeSec 排序）:
    if 音符已判定 → 跳過
    在輸入佇列中找 |error| 最小且落在窗內的輸入 i
    if 找到:
        以 i 判定該音符；從佇列移除 i
    else if songTime > 音符.timeSec + lateMissWindow:
        判為 Miss（未命中）
```

要點：

- **一顆音符只吃一筆輸入**；一筆輸入原則上只用於一顆音符。
- 尋找時優先「最近時間」而非「佇列順序」——因為輸入抵達順序可能因裝置而異。
- 被吃掉以外的輸入**不要丟棄**（可能屬於下一顆音符或同拍的其他音符）；
  若確定無音符可配對，才依規則處理（吃掉 / 忽略 / 當成空白鍵）。
- 這個演算法是純函式：輸入（音符陣列＋輸入佇列＋目前 songTime）→ 輸出（判定結果陣列）。
  **必須可單元測試**（見 `architecture-decoupling.md` 的 `FakeSongClock`）。

## 3. 連擊、分數、準確率

三個數字都只能有一個計算來源（`JudgmentEngine`），UI 只讀：

```
combo        = 非 Miss 時 +1；Miss 時歸 0

// 簡易加權分（Prototype）
weight       = Perfect ? 1.0 : Good ? 0.5 : 0.0
score       += baseScorePerNote * weight * (1 + min(combo, maxComboBonus) / maxComboBonus)

accuracy     = 累計權重 / 已判定音符數         // 顯示用百分比（可另計 Miss 為 0）
```

- 若要「顯示分數看起來很爽」的常見做法：分數另開一條（如 osu! 的固定總分），
  但**Accuracy 與 Combo 必須獨立**，否則無法用於難度平衡。
- Miss 是否斷連、Good 是否斷連 → 全部集中成設定旗標，不要散落在各處 `if`。

## 4. 音符生命週期（狀態機）

```
Pending ──(songTime 進入 可見範圍 之前沿) ──► Active ──(判定完成/超時) ──► Judged ──(回饋播放完/離開畫面) ──► Released
                                                                                     ▲
長按/滑條擴充：  Active ──► Holding ──► (成立/中斷) ──► Judged
```

- `Pending`：資料存在但未生成顯示物件（節省效能）。
- `Active`：生成／從池取出，開始依節奏時間插值位置。
- `Judged`：已產生 `JudgmentResult`，但視覺回饋（爆擊特效）可能還在播。
- `Released`：回收至池，狀態重置（**務必清空所有欄位**，避免殘留造成鬼影判定）。

生命週期事件一律走 `IFeedbackSink`，讓 View 層決定要播什麼，判定層不需要知道。

## 5. 邊界情況檢查清單

- [ ] 同一幀到達多筆輸入 → 依 §2 演算法配對，不可只取第一筆。
- [ ] 音符時間落在 lead-in（負 songTime）→ 允許，且不應被視為「超出範圍」。
- [ ] 暫停／續播後，判定窗不得因 `pausedTotal` 未扣而整體位移。
- [ ] 變速段落（BPM 改變）→ 判定仍以「秒」比對，不受 beat 換算影響（beat 只用於顯示與譜面編輯）。
- [ ] 結算畫面：Combo/Score/Accuracy 由引擎最終狀態輸出，不重新計算。
