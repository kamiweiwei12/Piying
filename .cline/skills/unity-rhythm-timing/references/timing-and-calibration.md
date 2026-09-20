# 時序數學與延遲校準

## 1. dspTime 基礎

`AudioSettings.dspTime` 是**音訊硬體的取樣時鐘**，以「已送出的 buffer 數 × buffer 長度」累積而成，
因此：

- 不受 frame rate 影響（掉幀也不會讓它跳動），是節奏遊戲唯一可用的判定時鐘。
- 約每 1/(buffer size) 秒更新一次（常見 buffer 1024 samples @ 48 kHz ≈ 21 ms 一跳），
  但它與**實際播放時間**一致，所以用它算出來的 `SongTime` 是準的（只是解析度有限）。
- 需要更平滑的「顯示用」時間時，可用 `Time.deltaTime` 內插 **dspTime 的預測值**，
  但這只能用於畫面，**不得**用於判定。

## 2. 標準時鐘公式

```
leadInSeconds = 3.0                          // 開場倒數
dspStart      = AudioSettings.dspTime + leadInSeconds
musicSource.PlayScheduled(dspStart)

songTimeRaw   = AudioSettings.dspTime - dspStart - pausedTotal
songTime      = songTimeRaw - audioOffsetMs / 1000.0 - inputOffsetMs / 1000.0
beat          = songTime * bpm / 60.0
bar           = beat / beatsPerBar            // 4/4 → /4
```

- `songTime` 在 lead-in 期間為負值 → 允許「倒數 3、2、1」的 UI。
- **`audioOffsetMs` 與 `inputOffsetMs` 的符號必須統一並寫在註解**：
  建議定義「正偏移＝玩家覺得音樂提早（需要延後判定）」或反之，二選一後全專案遵守。

## 3. 起播：PlayScheduled vs Play

| 做法 | 行為 | 適用 |
|---|---|---|
| `Play()` | 立即要求播放，實際起播點落在下一個 buffer 邊界，**不確定** | 非節奏用途 |
| `AudioSource.PlayScheduled(dspTime)` | 在指定 dspTime 精準取樣起播 | **節奏遊戲** |
| `AudioSource.SetScheduledStartTime` | 播放中重設起播 | 特殊情況 |

`PlayScheduled` 的 `dspStart` 至少要比現在晚 **1–2 個 buffer**，否則會直接開始播放而不是排程。

## 4. 暫停／續播

`dspTime` 不會因為 `Time.timeScale = 0` 停止，也不會因為 `AudioSource.Pause()` 停止：

```
// 暫停
pausedAtDsp = AudioSettings.dspTime;
musicSource.Pause();

// 續播
double pauseLength = AudioSettings.dspTime - pausedAtDsp;
pausedTotal += pauseLength;
musicSource.UnPause();
```

- 續播後**不要**改 `dspStart`（因為 `songTimeRaw` 已用 `pausedTotal` 扣掉）。
- 若要更精準（避免 UnPause 的 buffer 邊界誤差），改用「記錄歌曲位置 → `SetScheduledStartTime` 重新排程」。
- 效能考量：暫停時不要去動 `AudioListener.pause`（會影響全局）。

## 5. 校正（Calibration）

需要兩個獨立偏移，**不要合併成一個數字**（否則換耳機就得重新校準輸入）：

| 偏移 | 來源 | 典型量級 |
|---|---|---|
| `audioOffsetMs` | 輸出鏈：藍牙、HDMI、DAC、驅動 buffer | 0–300 ms（有線通常 < 20 ms） |
| `inputOffsetMs` | 輸入鏈：無線手把、觸控面板、驅動、事件傳遞 | 0–60 ms |

### 校準程序建議

1. **節拍聲引導法**：每拍播放一個 click，玩家跟著敲。收集 N（建議 ≥ 16）次 `hitSongTime - expectedBeatTime`。
2. 取**中位數**（不是平均，可抵抗離群值），即為合併偏移 `combinedOffsetMs`。
3. 若已知輸入端延遲（例如有線鍵盤 ≈ 0），可令 `inputOffsetMs ≈ 0`、`audioOffsetMs ≈ combinedOffsetMs`；
   否則用「已知音訊延遲裝置」的兩次測量分離兩個偏移。
4. 結果寫入設定並提供 UI 微調（±1 ms / ±5 ms），即時生效（不需重開曲目）。
5. 不同輸出裝置要分開保存（例如「喇叭」「藍牙耳機」兩組 profile）。

### 常見誤差來源檢查清單

- [ ] 音樂是用 `PlayScheduled` 起播的嗎？
- [ ] 判定用的是 `songTime`（校正後）而不是 `AudioSource.time`？
- [ ] `Application.targetFrameRate` / VSync 是否讓視覺抖動（影響操作感受但不影響判定）？
- [ ] 音效檔本身前面有沒有靜音空白（會造成「聽起來」有偏移）？
- [ ] 是否有多個 AudioSource 同時播放音樂（相位／延遲不一致）？
- [ ] 平台音訊設定：`AudioSettings.outputSampleRate`、DSP buffer size（越小延遲越低但越容易爆音）。

## 6. 變速（BPM 變化）與段落

有 BPM 變化的譜面，**不要**用 `beat = songTime * bpm/60`；改用「BPM 事件表」累加：

```
// timingPoints 依 songTime 排序：[{timeSec, bpm}, ...]
上一段結束時的 beat  = 段起始 beat
該段內 beat          = 段起始 beat + (songTime - 段起始 songTime) * 該段 bpm / 60
```

反向（beat → songTime）也必須由同一張表推導，兩者務必寫成同一組函式（單一真實來源）。
