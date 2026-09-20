# 譜面格式與時間軸

## 1. 單位換算（單一真實來源）

```
songTime(秒)  = beat * 60 / bpm
beat          = songTime * bpm / 60
bar           = beat / beatsPerBar            // 4/4 → beatsPerBar = 4
barBeat       = beat % beatsPerBar            // 用於節拍器與段落判定
```

- **所有換算寫在同一組函式內**（例：`TimingUtility`），禁止在 UI／譜面工具各寫一份。
- 內部資料存**秒**；顯示與編輯器才顯示 beat/bar（音樂人比較習慣）。
- 有 BPM 變化時，用 `TimingPoint` 表分段累加（見 `timing-and-calibration.md` §6）。

## 2. 建議的譜面資料結構

```csharp
// 設計示意（尚未建立檔案）
[CreateAssetMenu(menuName = "Rhythm/Chart")]
public class ChartData : ScriptableObject
{
    public AudioClip music;
    public string    title, artist, charter;
    public float     previewStartSec;
    public int       beatsPerBar = 4;
    public TimingPoint[] timingPoints;   // 依 timeSec 排序，第一筆為起始 BPM
    public NoteData[]    notes;          // 依 timeSec 排序
    public DifficultyConfig difficulty;  // 判定窗、scroll speed 等
}
```

- **`notes` 必須保持依 `timeSec` 排序**（載入時驗證一次，並在匯入工具中強制）。
- `typeId` 對應 `NoteTypeDefinition`（見 `architecture-decoupling.md`），讓同一份譜面能承載未來的長按／滑條。
- 譜面來源可為：手寫 ScriptableObject、JSON（`TextAsset`）、或日後做 `.osu` 轉檔器。
  建議**內部以 ScriptableObject 為執行期格式，JSON 為匯入/交換格式**（方便版本控管與外部編輯）。
- 存檔/匯入的 JSON 用固定欄位名與秒為單位，並帶 `version` 欄位以便日後升級。

## 3. 生成與回收策略（osu! 類玩法的關鍵）

參數：

| 參數 | 意義 | Prototype 起始 |
|---|---|---|
| `scrollSpeed` | 音符從生成到判定線的時間（秒）或速度倍率 | 由「可見時間」反推 |
| `visibleLeadSec` | 音符提前多久生成（= 旅行時間，含變速考量） | 1.5–2.0 秒 |
| `releaseGraceSec` | 判定後多久回收 | 0.3 秒 |

流程：

```
每幀（或每固定步）:
  while (nextNoteIndex < notes.Length && notes[nextNoteIndex].timeSec - songTime <= visibleLeadSec)
      生成／取出池物件 → 設定 noteId/type/lane/目標時間 → nextNoteIndex++

  對所有 Active 音符:
      依「(目標時間 - songTime) / visibleLeadSec」插值位置與透明度（或依 scrollSpeed 線性位移）

  對所有 Judged 且超過 releaseGraceSec 的音符:
      播放完畢 → 回收
```

- **指標前進**（`nextNoteIndex`）而不是每幀掃全譜；因為 `songTime` 單調遞增，指標不會回溯
  （唯一例外是**重新開始/倒帶**，此時重建索引）。
- 位置一律由**節奏時間**推導 → 校正偏移與變速會自然正確，不需要為每個音符額外計時。
- **不要**為每顆音符使用 coroutine 或 `Invoke`；那是效能與正確性的雙重陷阱。

## 4. 節拍器與對拍顯示

- 節拍指示器（皮影戲可用「鼓點」「影子閃動」表示）：以 `bar`、`barBeat` 的整數變化觸發，不靠計時器。
- 因 `dspTime` 更新有 buffer 粒度（約 10–20 ms），瞬間事件（閃光／音效）可能有些微抖動 →
  可提前**預測**下一個拍點時間做準備，但**判定**仍以實際 `songTime` 為準。

## 5. 譜面編輯的最小方案（Prototype 建議）

1. 先用純資料方式：`TextAsset`(JSON) 或直接在 Inspector 編輯 `ChartData`。
2. 再做簡易編輯器：以目前的 `Time` 為基準即時「按鍵寫入音符」（Editor-only 腳本放 `Assets/Editor/`）。
3. 最後才考慮外部工具（例如借用 osu! 的譜面再轉檔）。

> 注意：任何 Editor-only 腳本仍屬於 Unity 專案檔案，建立前需先向使用者確認（見 `03-guardrails.md`）。
