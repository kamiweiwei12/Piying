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

## [2026-09-21] M7 候选版 - 选曲、三难度与延迟校准

### 新增

* 单曲选曲入口《试灯》，提供「入门／行当／名角」三档难度及清楚的密度说明。
* `PlayDifficulty` 与难度说明契约；Easy 减少合奏与同拍操作，Normal 保留标准谱面，Hard 加入半拍点按与双脚合奏。
* `CalibrationSettings`：音频输出与输入路径偏移独立保存，范围限制为 -300～+300 ms，可在菜单以 5 ms 步进调整。
* M7 uGUI 流程画面与 EventSystem：选曲 → 游玩 → 结算 → 返回选曲不需 Editor 介入。

### 修改

* `DspSongClock.AudioOffsetSeconds` 与 `InputSystemNoteInputSource.InputOffsetSeconds` 支持运行时即时生效；校准值写入 `PlayerPrefs`。
* 结算提示加入 Enter 返回选曲；R 仍可用当前难度重新演奏。
* 谱面生成器按时间重新排序，保证 Hard 新增的半拍音符仍符合判定引擎的有序输入契约。

### 测试

* Unity EditMode 全量测试：**96/96 通过**，0 failed／0 skipped。
* Play Mode 实测：选曲菜单正常显示，可选择 Hard 进入游玩；Unity Console 0 error。
* UI 验收图：`Logs/M7-song-select.png`。
* Connected Editor Windows build：`Builds/M7/YingYun.exe`，**Succeeded**，0 error，134,400,328 bytes，12.185 秒。

### 验收结果

* 自动化与开发机 Play Mode 验收通过；等待使用者实机确认菜单操作、三难度体感与校准方向。

### Git Commit

* `feat(m7): add playable demo flow`（本条目所在的里程碑提交）

### 風險 / 已知問題

* 当前只有一首原型曲《试灯》，选曲流程已成立但尚无第二首歌曲内容。
* 中文继续使用 Windows 动态系统字体；最终跨平台包仍应补正式 CJK TMP 字体资产。
* 音频／输入偏移的数值可即时调整与保存，但玩家仍需凭听感手动校准，尚未加入自动节拍采样向导。

### 下一步

* 使用者实机验收 M7 候选版；通过后补正式 CJK TMP 字体并将 M7 结案，或先按反馈调整三难度密度与校准交互。

---

## [2026-09-21] M6.5 修正 - Hold 持續操演與音符類型配色

### 新增

* Hold 持續操演回饋：按住期間竹桿以歌曲時間做小幅往復推拉，對應皮影片關節同步產生細微持續擺動。
* 回歸測試：Hold 杆端與前臂在兩個取樣時刻必須持續變化；Tap／Hold／Chord 必須各自維持唯一顏色且不受 lane 影響。

### 修改

* `HoldStarted` 的「按住」提示不再套用普通判定的 0.18 秒清除計時；會一直保留到 Hold 完成、早放或失敗結果取代它。
* 音符配色改為三類固定語義：Tap 紅色、Hold 金色、Chord 青綠色；六個判定點統一為深褐色，不再按方向使用六種彩色。
* Hold 的音符本体与头端在按住前后维持同一金色，只以持续脉动和长条形状表示状态，避免再次变色造成混乱。

### 測試

* Unity EditMode 全量測試：**93/93 通過**，0 failed／0 skipped。
* 類型配色視覺圖：`Logs/M6-2-note-shapes.png`，Tap 為紅色圓形、Hold 為金色長條。
* Connected Editor Windows build：`Builds/M6.5/YingYun.exe`，**Succeeded**，0 error，134,391,624 bytes，12.984 秒；187 項為既有 Inference/Shader 變體警告。

### 驗收結果

* ✅ Hold 的畫面提示現在覆蓋完整按住期間，不再只閃現一次。
* ✅ Hold 操偶在整段按住期間都有可觀察的杆端推拉和皮影片擺動，與 Tap 的單次衝量清楚區分。
* ✅ 顏色僅表達 Tap／Hold／Chord 類型，不再表達六方向。

### Git Commit

* `27cec86 fix(m6.5): sustain hold puppetry and simplify note colors`
* `docs(m6.5): record hold puppetry correction`（本條目所在的文件提交）

### 風險 / 已知問題

* 持續推拉幅度刻意保持小，避免遮蔽譜面或破壞既有姿態；最終強度仍需使用者實機確認。
* 測試期間曾遇到 Unity Licensing Client 啟動異常，重新開啟 Editor 後由 Pipeline 完成 93/93 測試與 Windows build，非程式缺陷。

### 下一步

* 實機確認 Hold 的持續拉扯是否足夠明顯；若仍偏弱，下一輪只調整推拉幅度／頻率，不再更動判定。

---

## [2026-09-21] M6.5 - 從吊線木偶重構為數位皮影操演

### 新增

* `PerformanceIntent`：六軌改以 `LeftLead / Lift / RightLead / LeftStep / Sink / RightStep` 表達操演意圖，部位映射仍由 `ActionBinding` 資料驅動。
* 灰盒皮影戲台：半透明紙幕、背光、木框、遠景剪影、冠飾、寬袖、衣擺與可見鉚釘關節。
* 六根側向／下方剛性竹製操縱桿；按鍵後杆端立即位移，再由既有彈簧－阻尼關節系統產生連續動作。

### 修改

* `ShadowPuppetPresenter` 不再建立由頂部垂下、會下墜彎曲的 `Control String`；改為直線竹桿、竹柄與關節連接點。
* `RhythmPrototypeController` 啟動紀錄改為 M6.5 `rod-driven-spring-joint`，判定、計分、時鐘、譜面與輸入契約均未修改。
* 保留 `StringCount` / `GetStringTension` 相容入口供舊測試與呼叫端過渡，正式語義改為 `RodCount` / `GetRodDrive`。

### 測試

* Unity EditMode 全量測試：**91/91 通過**，含新增「六種操演意圖唯一」與「六竹桿存在、舊 Control String 不存在」回歸檢查。
* 視覺驗收圖：`Logs/M6-5-shadow-play-chord.png`（1280×720；不進版控），可見紙幕背光、戲台框、分片衣袖／衣擺、冠飾、鉚釘與側向竹桿。
* Connected Editor Windows build：`Builds/M6.5/YingYun.exe`，**Succeeded**，0 error，134,390,600 bytes，19.132 秒。187 項為既有 Inference/Shader 變體警告。
* Unity Console 編譯狀態：`compilationFailed=false`。

### 驗收結果

* ✅ 核心畫面不再使用頂部吊線木偶語言；控制裝置為硬直竹桿，杆端與關節的因果連接清楚。
* ✅ 六軌已從裸露的身體部位提升為六種操演意圖，未把輸入、判定或計分耦合進 View。
* ✅ Tap／Hold／Chord 與連續彈簧運動全部維持原有行為，全量測試無回歸。
* ✅ 不依賴 Renderer2D、Light2D、新套件或 ProjectSettings 修改即可成立灰盒皮影舞台。

### Git Commit

* `ba24746` `feat(m6.5): replace puppet strings with shadow play rods`
* `docs(m6.5): record digital shadow play redesign`（本條目所在的文件提交）

### 風險 / 已知問題

* 目前仍是程式生成的幾何灰盒；已具皮影構圖語言，但正式分層皮革／透光 Sprite、角色側臉輪廓與竹節細節仍待美術替換。
* 舊測試方法名稱與 `PuppetPose` 的 `Tension` 欄位仍帶有繩索歷史命名，為避免本輪擴大重構而保留；後續應以獨立提交清理。
* 獨立 batch build 因第二個 Unity 程序無法寫授權資料庫而失敗；改由已連接 Editor 建置成功，非程式錯誤。

### 下一步

* 使用者在 `Builds/M6.5/YingYun.exe` 實機驗收：第一眼是否像皮影戲、六杆與六方向是否容易理解、Hold 是否呈現持續操演。
* 驗收後再決定是否進一步加入動作短語／Combo 舞台光影，或進入 M7。

---

## [2026-09-21] M6.2 - 六鍵點按與長按：長條音符與持續操偶

### 新增

* 譜面改為六個部位都有點按與長按。每個 8 拍樂句固定為：`Q 點`、`E 點`、`Q+E 組合`、`W 點`、`S 長按（2 拍）`、`A 點`、`D 點`、`S 點`，再加一顆輪替長按（Q／E／W 落在第 5–7 拍，A／D 落在第 2–4 拍，每 5 個樂句輪完一輪）。180 秒共 **406 顆音符、其中 90 顆長按**。
* `JudgmentLabels`（顯示層純函式）：長按失誤細分為「早放」（提早放開，誤差為負）、「未撐住」（按住了但沒撐到最後）與一般「空引」；按住期間顯示「按住」。判定、計分、連擊完全不受影響。
* `RadialNoteView` 新增長按的頭端圓帽與長條本體：**點按是圓形、長按是沿軌道伸長的長橢圓**（圓形貼圖非等比縮放＋沿軌道旋轉），長度等於音符在軌道上的長度，尾巴必定在放開時間抵達判定點。
* 新增測試：六軌都有點按與長按、同軌音符不重疊（新不變量）、長按長度為 1 秒、長條長軸必須沿軌道、長按期間手臂維持舉起、長按與點按的姿態差異、長按失誤文字標籤，以及圓形／長橢圓對照圖。

### 修改

* `PuppetPoseEvaluator`：長按期間的持續力道改為維持在 `ActionBinding` 宣告角度（乘上 `1 / DriveAmplitudeScale` 補償），因此手會**持續舉起**、繩索持續拉緊；並修正長按起始時「按下頓拉 + 平台」相加造成過度彎折的缺陷（修正前在按住 0.4 秒時實測為 -76.5°，修正後維持在 -52° 附近）。
* 放開長按不再施加朝向拉力的頓拉：改為純慣性滑行後由彈簧拉回，回彈更自然。
* `RadialNotePresenter` 改用 `JudgmentLabels` 並新增 `ShowJudgmentText`（移除私有 `GradeText`）。

### 測試

* Unity EditMode 全回歸：**total=90 / passed=90 / failed=0 / skipped=0**，duration=0.5437 s。
  * 指令：`unity test "D:\Unity\program\My project" --editor-path "D:\Unity\Editor\6000.6.2f1\Editor\Unity.exe" --mode EditMode --filter YingYun.Tests --timeout 600 --output "…\Logs\M6-2-editmode-results.xml" --format json`。
* 純 C# 譜面驗證（Unity 內建 .NET SDK 8.0.318，直接編譯 `PrototypeDanceChart.cs` 與 `JudgmentEngine.cs`）：**11 項檢查全數通過** —— 406 顆、六軌都同時有 tap 與 hold、時間遞增、Id 唯一、**同軌零重疊**、90 顆長按皆為 1.00 秒。
* 以真實判定核心重播長按：輕點長按音符 → `NoteJudged Miss errorMs=-950`（畫面顯示「早放」）；完整按住 → 4 次 `HoldTick` 後 `Perfect`（combo=1）。
* 視覺驗收圖（`Logs/`，不進版控）：`M6-2-note-shapes.png`（1280×720，左＝圓形點按、右＝長條橢圓長按含頭端圓帽）。首次渲染即發現長條橫躺 90° 的旋轉基準錯誤，修正後補上「長軸必須沿軌道」的方向回歸測試。
* Windows x64 建置：`unity build … --target StandaloneWindows64 --output-path "Builds\M6\YingYun.exe"` → `Build Finished, Result: Success`、退出碼 0、provenance `outcome=success`；`Builds\M6\YingYun_Data\Managed\YingYun.Unity.dll` 已更新（14:45，39,936 bytes）。
* 依照先前要求**沒有自動啟動遊戲**。

### 驗收結果

* S 鍵「永遠空引」的問題根源已消除：S 現在同時有點按與長按，點按會被判定為點按；長按若提早放開，畫面會顯示「早放」而不是「空引」。
* 長按在畫面上是長條橢圓、在皮影上是持續舉起／持續拉緊（回歸測試：按住 1 秒期間肩膀維持在 -45° 以上、繩索張力 ≥ 0.95；放開後回到 -20° 以內、張力歸零）。
* 既有 90 項測試（含 M0–M6.1 的判定、計分、音符、操偶）全數維持通過。
* 測試與建置造成的 URP／Unity Connect 自動改動已還原，工作區只留下本次工作單位的變更。

### Git Commit

* `3ef1813` `feat(m6.2): add tap and hold notes on all six lanes`
* `docs(m6.2): record tap and hold note work`（本條目所在的提交）

### 風險 / 已知問題

* 譜面密度由每樂句 7 顆提高到 9 顆（120 BPM 下約 2.25 顆/秒），尚未經實機手感確認；若覺得太密，可調整輪替長按的頻率或位置。
* 長按固定為 1 秒（2 拍）；若想要更長的持續操偶，只需調整 `PrototypeDanceChart.HoldBeats`。
* 顯示層以 `JudgmentResult.ErrorMs` 的正負區分「早放／未撐住」，未來若判定改版需同步此推論。
* 建置造成的 URP 與 Unity Connect 自動改動已備份於 `Logs/M6-2-reverted-autochanges/` 後還原。
* `ProjectSettings/ProjectSettings.asset` 仍顯示為 modified，但內容雜湊等於 `HEAD`，是 CRLF／stat cache 假訊號。

### 下一步

* 實機驗收：圓形點按與長橢圓長按是否一眼可辨、按住時手是否持續舉起、按住 1 秒的手感與密度是否合適。
* 通過後再進 **M7**（三難度、選曲流程、延遲校準、最終 Windows Demo）。

---

## [2026-09-21] M6.1 - 連續操偶（彈簧－阻尼關節）

### 新增

* `PuppetPoseEvaluator` 改為連續操偶核心：10 個關節各自是帶慣性的彈簧－阻尼系統，以固定 1/240 秒步長積分，每個關節有自己的自然頻率、阻尼比與角度上限。
* 每個輸入事件都保留成獨立的「拉繩」事件（每軌 4 個環形槽位），相鄰輸入的力量會自然疊加，不再互相覆蓋。
* 同鍵連按會在「抬手／伸手／收肘」三種手勢間循環：幅度（1.00／0.86／1.12）、繩索張緊時間（0.20／0.14／0.26 秒）與前臂比例（1.00／0.78／1.22）都不同。
* 手臂與腿的拉力以較小比例連動頭與軀幹（重心轉移），左右手的軀幹連動方向相反。
* `ShadowPuppetPresenter` 新增肘、膝、髖旋轉讀值與骨盆位置讀值，讓驗收程式能直接量測姿態。
* 10 個新 EditMode 測試：三幀內起動、峰值幅度、連鎖延遲、繩索回鬆後仍滑行、回彈、連按不斷線、幀率無關、同鍵手勢變化、無輸入靜止、失勢姿態、玩家層序列與軌跡輸出。

### 修改

* 移除舊的無狀態模型（按鍵 → 固定 0.68 秒曲線 → 歸零）；改為「拉繩 → 角速度 → 慣性滑行 → 回彈 → 與下一次拉繩疊加」。單次動作約 0.9–1.2 秒，120 BPM 的相鄰拍自然交疊。
* 控制索張力仍在按下瞬間就變為 1（繩索先繃緊），關節則在約 3 幀（50 ms）內明顯被拉動、約 0.33 秒到達峰值；Q 的峰值為 **-52.45°**（`ActionBinding` 宣告值 -52°）。
* 連鎖延遲：肩峰值 0.333 秒、肘峰值 0.383 秒（相差約 50 ms），軀幹與頭更慢，形成「肩膀先起、前臂跟上、身體再被帶動」的次序。
* 姿態只取決於「事件清單 + 歌曲時間」：回溯或重複取樣會從最早事件重新模擬，不會遺失已累積的姿態（修正實作過程中發現的缺陷：舊寫法回溯時會連事件一起清除，導致木偶靜止）。
* `RhythmPrototypeController` 的啟動診斷字串改為 `motion=spring-joint`。
* 事件走訪移除每次呼叫配置委派的 `Action<int>`，改為固定迴圈；熱路徑仍不產生配置。

### 測試

* Unity EditMode 全回歸：`total=76 / passed=76 / failed=0 / skipped=0`，duration=0.6831737 s。
  * 指令：`unity test "D:\Unity\program\My project" --editor-path "D:\Unity\Editor\6000.6.2f1\Editor\Unity.exe" --mode EditMode --filter YingYun.Tests --timeout 600 --output "…\Logs\M6-1-editmode-results.xml" --format json`。
* 純 C# 獨立驗證（Unity 內建 .NET SDK 8.0.318，直接編譯本專案的 `PuppetPoseEvaluator.cs` 與 `ActionBinding.cs`）：**23 項檢查全數通過**，關鍵量測為峰值 `-52.45° @ +0.33 s`、肩／肘峰值 `0.333 s / 0.383 s`、繩索回鬆後仍位移 `-49.72°`、回彈 `+4.01°`、1.8 秒歸零 `-0.014°`、同鍵連按三次從不回到靜止（`-44.71 / -30.54 / -52.77`）、幀率無關（30 fps 與 240 fps 差異 `< 0.001°`）、序列最大每幀變化 `4.389°`、序列平均位移 `20.03°`。
* 視覺與軌跡產物（`Logs/`，不進版控）：`M6-1-puppet-chord.png`（1280×720、Q+E 峰值定格，65,152 bytes）、`M6-1-puppet-trajectory.csv`（151 格 × 11 欄、60 fps、Q→E→Q+E→A 序列，11,067 bytes）。
* Windows x64 建置：`unity build … --target StandaloneWindows64 --output-path "Builds\M6\YingYun.exe"` → `Build Finished, Result: Success`、退出碼 0、`YingYun.provenance.json` 的 `outcome=success`（耗時約 68 秒）。
* 產物內容核對：新建的 `Builds\M6\YingYun_Data\Managed\YingYun.Unity.dll`（14:12:10）以位元組比對確認含有新診斷字串 `motion=spring-joint`，證明玩家端確實帶有連續操偶版本。
* 依照先前要求**沒有自動啟動遊戲**；Play 模式的手感驗收留給使用者執行。

### 驗收結果

* M6 的完成條件在體感層面補齊：連續按鍵不再「打一下動一下」，手臂在兩顆音符之間維持位移並持續變化。
* 所有新舊判定、計分、輸入與音符測試維持全綠（76/76），判定仍只讀 `dspTime` 時間軸，動畫不驅動判定。
* 幾何皮影仍只以旋轉關節運動，骨盆位置在整段序列中完全不移動（回歸測試斷言位移 `< 0.0001`）。
* 無任何輸入時木偶的所有關節與張力皆為 0（既有回歸測試維持通過）。
* 測試與建置造成的 URP／Unity Connect 自動改動已還原，工作區只留下本次工作單位的變更。

### Git Commit

* `97a4a33` `feat(m6.1): make shadow puppet motion continuous`
* `docs(m6.1): record continuous puppetry acceptance`（本條目所在的提交）

### 風險 / 已知問題

* 手感（回彈軟硬、拉扯幅度是否過大、控制線是否夠明顯）尚未由使用者實機確認；數值已量化，但好不好看仍需人眼判斷。
* 動作由 `dspTime` 衍生的 `songTime` 驅動，視覺取樣約 21 ms 一格（DSP buffer 1024 @ 48 kHz）；若要更平滑需改用插值顯示時鐘（`unity-rhythm-timing` 允許，但那會動到共用時鐘，未經同意不做）。
* `PuppetPoseEvaluator.Fail()`（失勢姿態）仍未被 Presenter 接入，只有測試覆蓋，保留給未來的明確失敗演出。
* `ProjectSettings/ProjectSettings.asset` 仍顯示為 modified，但內容雜湊與 `HEAD` 相同（`08c1b52c…`），是 CRLF／git stat cache 的假訊號，未納入提交。
* 測試／建置造成的 URP 與 Unity Connect 自動改動已備份於 `Logs/M6-1-reverted-autochanges/` 後還原。

### 下一步

* 使用者在 Editor 或 `Builds\M6\YingYun.exe` 實機驗收：Q → E → Q+E → D 是否形成不斷線的表演、回彈幅度、控制線明顯度、Q+E 同時按的難度。
* 驗收通過後再進 **M7**（三難度、選曲流程、延遲校準、最終 Windows Demo）。

---

## [2026-09-21] M6 fix - 自動 Miss 不再誤驅動木偶

### 新增

* 新增 `AutomaticMissWithoutInput_DoesNotMovePuppet` 回歸測試，鎖定無輸入時頭部、軀幹與 S 操偶線均保持靜止。

### 修改

* `ShadowPuppetPresenter` 不再把普通 `NoteJudged/Miss` 轉成全身失勢動作；木偶姿態現在只由真實 Q/W/E/A/S/D 輸入與有效 Hold 狀態驅動。
* Miss 仍由判定、分數、HUD 與舞台視覺正常呈現，不影響原有失誤規則。

### 測試

* Unity EditMode 全回歸：**total=66 / passed=66 / failed=0 / skipped=0**。
* Windows x64：`Builds/M6/YingYun.exe` 重新構建成功，**0 errors**。

### 驗收結果

* 無任何按鍵時，譜面自動 Miss 不再令頭部或身體重複旋轉，S 控制線張力維持 0。
* 真實按鍵即使最終判為 Miss，仍會按先前規格立即拉動相應部位。

### Git Commit

* `1a7d68b` `fix(m6): stop automatic misses moving puppet`

### 風險 / 已知問題

* `PuppetPoseEvaluator` 內仍保留未接入 Presenter 的失勢曲線，供未來明確設計獨立失敗演出時使用；目前不會被一般 Miss 觸發。
* `ProjectSettings/ProjectSettings.asset` 的既有用戶改動未納入提交。

### 下一步

* Play Mode 靜置觀察一段譜面，確認木偶不自行動作；再逐一按 Q/W/E/A/S/D 驗收只有對應部位受拉。

---

## [2026-09-21] M6 fix - 底部身體判定與全按鍵操偶回饋

### 新增

* 新增身體判定點底排位置回歸測試，以及 Miss 按鍵仍立即拉動木偶的 Presenter 回歸測試。

### 修改

* S／身體音符的判定點由畫面中央下移到雙腳判定点同一底排中央；音符維持由下方向上接近，不再穿入木偶主体。
* `RhythmPrototypeController` 将每一个原始 `HitInput` 先转发给木偶演出，再交给判定引擎；动作使用输入事件自身的校正后 `InputTimeSec`。
* `ShadowPuppetPresenter` 的 Tap 拉线从判定结果解耦：命中、空按、早按、晚按或最终 Miss 都会即时拉动对应部位；判定结果只追加失败姿态，并负责 Hold 建立／释放。

### 測試

* Unity EditMode 全回归：**total=65 / passed=65 / failed=0 / skipped=0**。
* Windows x64：`Builds/M6/YingYun.exe` 重新构建成功，**0 errors**，产物 134,380,168 bytes。

### 驗收結果

* 身体 receptor 的 Y 坐标与左右脚严格一致，X=0；S 音符路径固定在画面下方。
* 原始 Q+E 输入可同时拉紧双线；即使同一输入随后得到 Miss，左臂拉动和线张力仍保持可见。

### Git Commit

* `d7a89a4` `fix(m6): align body lane and animate every input`

### 風險 / 已知問題

* 本轮按先前要求未自动启动 Player；构建及自动测试均通过，实际键盘手感留给人工验收。
* `ProjectSettings/ProjectSettings.asset` 的既有用户改动继续保留，未纳入提交。

### 下一步

* 在 Unity Play Mode 中按 Q/W/E/A/S/D 做人工验收，重点确认空按也有拉扯、S 音符只在底部，以及 Hold 按下持续绷紧、松开回弹。

---

## [2026-09-21] M6 - 關節操偶與幾何皮影舞台

### 新增

* `ActionBinding`／`PrototypeActionBindings`：將六個 lane 資料化映射為 Q 左手、W 頭部、E 右手、A 左腳、S 軀幹、D 右腳，並分別配置主／次關節拉動角度；實體按鍵仍由 Input System 與 lane 解耦，可日後重綁。
* `PuppetPoseEvaluator`：純 C# 絕對歌曲時間姿態計算器；Tap 產生拉起、反向回彈、回中曲線，Hold 維持張力直到尾判釋放，Miss 產生失勢姿態。
* `ShadowPuppetPresenter`：執行期建立骨盆、頸、肩、肘、髖、膝父子關節鏈，六根操偶線與木製控制柄，以及暖色紙幕／深色剪影／紅色關節銷。
* `PuppetPoseEvaluatorTests`、`ShadowPuppetPresenterTests`：驗證六部位映射、關節旋轉、組合拉線、Hold 張力、回彈、確定性與離屏視覺輸出。

### 修改

* `RhythmPrototypeController` 將同一份 `JudgmentResult` 與絕對 `songTime` 單向轉發給皮影 Presenter；角色动画不读取输入、不计算判定，也不回写 Combo／Score。
* `RadialNotePresenter` 移除中央“皮影偶”文字占位，保留低层级段落反馈光晕，让真实几何关节偶位于画面中心。
* 肢体动作全部绕肩、肘、髋、膝枢轴旋转；不以整体 Transform 横移模拟动作。控制线会从松弛弧线变为绷直高亮，释放后产生一次小幅反向回弹。

### 测試

* EditMode 全回归：`YingYun.Tests` → **total=63 / passed=63 / failed=0 / skipped=0**，duration=0.5696555 s。
  * 六个 lane 与六个部位均唯一；键位顺序为 Q/W/E/A/S/D；绑定数组重排后仍按 lane 正确解析。
  * Q 只旋转左肩／左肘，E 只旋转右肩／右肘，Q+E 同时拉动双臂，D 只旋转右髋／右膝且躯干不滑动。
  * Hold 跨 3 秒仍保持 >95% 控制线张力；尾端释放后 0.6 秒内回中。
  * 同一事件序列与同一 `songTime` 产生完全相同姿态。
* 离屏视觉验收：1280×720 输出 `Logs/M6-puppet-chord.png`；检查暖色幕布、深色剪影、十个旋转关节、六根控制线和 Q+E 双臂受拉姿态均清晰可辨。
* Windows x64：`Builds/M6/YingYun.exe` 构建成功，退出码 0；实际启动 10 秒输出 `[M6] puppet-ready | joints=10 | strings=6 | motion=joint-rotation`，Player log 无脚本异常。

### 驗收結果

| M6 条件 | 结果 |
|---|---|
| 六键对应正确部位、ActionBinding 数据驱动 | ✅ lane 与实体按键解耦，六映射唯一且测试覆盖 |
| `Q → E → Q+E → D` 形成连续动作 | ✅ 左肩链、右肩链、双肩链、右腿链依序受拉并带回弹 |
| 剪影 + 光幕，不依赖 Renderer2D／Light2D | ✅ 仅使用 SpriteRenderer、LineRenderer 与运行时纹理 |
| 动画不驱动判定 | ✅ Presenter 只消费 `JudgmentResult` 与 `songTime` |
| 操偶拉扯感而非整体跳舞 | ✅ 固定关节旋转、控制线张力／松弛、Hold 持续拉力 |

* **结论：M6 完成。** 几何替身已验证操偶机制；未来替换正式分层 Sprite 时不需要修改判定或姿态事件接口。

### Git Commit

* `95109d2` `feat(m6): add joint-driven shadow puppet performance`

### 風險 / 已知問題

* 当前是几何人体比例与剪影占位，不是最终角色美术；正式素材需要按头、躯干、上／前臂、大／小腿拆层并提供正确关节 pivot。
* 自动视觉图验证布局和姿态，实际按键拉扯手感仍可在正式美术进入后微调角度、回弹周期与张力颜色。
* `.clinerules/01-project-context.md` 的里程碑文字仍停在 M4，与 DEVELOPMENT／Git 实况不一致；Rule 修改属于需批准事项，本轮未改。
* `ProjectSettings/ProjectSettings.asset` 的 Unity AI 插件 define 变化为既有用户改动，未纳入 M6。

### 下一步

* 进入 M7：设计 Easy／Normal／Hard 数据差异、选曲到结算流程、延迟校准保存，以及最终 Windows Demo 出包；正式皮影分层素材可在任意时间替换几何部件。

---

## [2026-09-21] M5 - Hold、組合音符與舞蹈連貫度

### 新增

* `PrototypeDanceChart`：以 120 BPM、8 拍樂句產生 180 秒核心玩法譜面，包含單鍵、雙手組合音符與身體 Hold；終點音符固定落在 180 秒，總計 316 顆邏輯音符。
* `NoteData.RequiredLanesMask`：用位元集合描述單鍵或組合音符；雙手組合為 Q+E，仍只是一顆邏輯音符。
* `HoldStarted` 判定事件，以及「合勢／斷勢」段落演出事件。
* `PrototypeDanceChartTests`、`RadialNoteViewTests`，並擴充 `JudgmentEngineTests` 的 Hold、組合音符與段落邊界案例。

### 修改

* 六部位鍵位改為空間對應：Q 左手、W 頭部、E 右手、A 左腳、S 身體、D 右腳。
* Hold 尾端採嚴格對稱 ±100 ms 釋放窗；提早、過晚或未釋放皆為 Miss，最終成績取頭尾較差者。
* 組合音符要求完整按鍵集合在 70 ms spread window 內到齊，完成後原子消耗輸入、只計一次 Combo；缺鍵或超窗不消耗部分輸入。
* 段落第一次 Miss 立即且只發出一次 `SegmentInterrupted`；全部音符成功才發出 `SegmentCompleted`。
* `RadialNoteView` 以預建 marker、連線與 Hold 軌跡顯示進階音符；位置仍只由絕對 `songTime` 推導，遊戲熱路徑不新增 `Instantiate`。

### 測試

* EditMode 全回歸：`YingYun.Tests` → **total=51 / passed=51 / failed=0 / skipped=0**，duration=0.1500176 s。
  * 指令：`unity test "D:\Unity\program\My project" --editor-path "D:\Unity\Editor\6000.6.2f1\Editor\Unity.exe" --mode EditMode --filter YingYun.Tests --timeout 300 --output "…\Logs\M5-final-results.xml" --format json`。
  * Hold：±100 ms 邊界包含、±101 ms 排除、未釋放超時、頭尾取較差成績。
  * 組合：按鍵順序無關、完整集合原子消耗、70 ms 邊界包含、超窗與缺鍵 Miss 且保留部分輸入；同軌多候選時仍能選到整體合法的完整集合。
  * 段落：首次 Miss 立即中斷且只發一次事件；全成功才完成。
  * 視圖：雙手組合同時啟用兩個 marker 與連線；Hold 顯示軌跡、進入按住狀態並正確結束。
* Windows x64 建置：`Builds/M5/YingYun.exe`（Mono）成功，Unity `Build Finished, Result: Success`、退出碼 0。
* Windows Player 隱藏煙測 12 秒：成功載入並播放；首樂句輸出 mask 1／4／5／2／16／8／32，其中雙手組合為 mask 5；首次 Miss 立即輸出一次 `[M5] segment | id=0 | state=SegmentInterrupted`；Player log 無腳本例外。

### 驗收結果

| M5 條件 | 結果 |
|---|---|
| Hold 按住、提早釋放、過晚／未釋放 | ✅ 純 C# 邊界與狀態測試完整通過 |
| Q+E 組合以按鍵集合 + 時間窗判定 | ✅ 70 ms spread window、原子消耗、一次 Combo |
| 舞蹈段完成／中斷事件與演出 | ✅ `SegmentCompleted` 顯示「合勢」；首次 Miss 立即顯示「斷勢」 |
| 三分鐘核心玩法譜面 | ✅ 316 顆、8 拍循環、終點 180.0 秒 |
| Editor 邏輯與 Windows build | ✅ 51/51 EditMode；Windows build 成功並實際啟動 |

* **結論：M5 完成。** 三項專屬完成條件均有自動測試，Windows 玩家已驗證真實譜面與段落中斷路徑。

### Git Commit

* `d23fcfb` `feat(m5): add advanced rhythm notes and continuity`

### 風險 / 已知問題

* 組合音符目前只支援 Tap；Hold 組合會明確拒絕，避免在 M5 引入未定義的多鍵尾判語義。
* 自動化 Player 使用無圖形、無鍵盤裝置模式，因此 build 端只驗證啟動、時間軸、自動 Miss 與「斷勢」；實體鍵盤的 Hold／Q+E 手感仍適合在後續 Playtest 微調 70 ms spread window。
* 正式皮影肢體動作與「合勢／斷勢」動畫屬 M6；M5 使用幾何 marker、連線、軌跡與舞台變色作可驗證佔位演出。
* `ProjectSettings/ProjectSettings.asset` 的 Standalone define 變化來自先前安裝的 Unity AI 插件，非 M5 改動，未納入本次提交。

### 下一步

* 進入 M6：建立資料驅動 `PuppetRig`／`ActionBinding`，讓 Q、W、E、A、S、D 與組合／Hold 結果驅動肉眼可辨的皮影六部位連續舞蹈。

---

## [2026-09-21] M4 - 国风判定 HUD 与结算

### 新增

* `GameplayStatistics`：由 `JudgmentResult` 单向累积四档中文判定统计、最高连击、得分与准确率。
* `DifficultyConfig`：集中保存「天成／传神／入韵／初成」准确率门槛；`ResultGradeCalculator` 只读此配置。
* `GameplayHudPresenter`：运行时 uGUI 中文 HUD 与结算面板，使用安全区域锚点与 1920×1080 缩放基准。
* `GameplayStatisticsTests`：新增四档统计、配置化评价边界、重置，以及同一条 `JudgmentResult` 流下与 `JudgmentEngine` 最终数值一致的测试。

### 修改

* 判定核心增加 `Great` 档：40 ms 内「契合」、40–70 ms「协律」、70–100 ms「应拍」、超过 100 ms「空引」。
* 协律权重为 0.75；契合、应拍、空引权重保持 1.0、0.5、0.0；Combo／Score／Accuracy 仍只有 `JudgmentEngine` 计算。
* `RadialNotePresenter` 与六部位标签完全中文化；不再向玩家显示 Perfect／Great／Good／Miss 或英文部位名称。
* `RhythmPrototypeController` 在最后一颗音符判定后暂停歌曲并显示结算；`R` 重新开始时清空 HUD 与统计。
* `YingYun.Unity.asmdef` 仅增加 `Unity.ugui` 引用；未新增 Scene、Prefab、Packages 或 ProjectSettings 变更。

### 测试

* Unity 编译：`compilationFailed=false`，Console error=0。
* EditMode 回归：`YingYun.Tests` → **total=32 / passed=32 / failed=0 / skipped=0**，duration=0.1533774 s。
  * 指令：`unity test "D:\Unity\program\My project" --editor-path "D:\Unity\Editor\6000.6.2f1\Editor\Unity.exe" --mode EditMode --filter YingYun.Tests --timeout 240 --output "…\Logs\M4-editmode-results.xml" --format json`。
* 结算数字一致性：`Statistics_MatchJudgmentEngineForMixedJudgments` 以同一组谱面与输入跑出
  Perfect／Great／Good／Miss 各 1 颗，断言 `GameplayStatistics` 的四档计数、JudgedCount、MaxCombo、Score、Accuracy
  全数与 `JudgmentEngine` 一致。
* 结算画面：以 EditMode 离屏渲染真实 uGUI Canvas 至 1280×720 RenderTexture，画面显示总评「入韵」、
  总分 156,256、准确率 85.42%、最高连击 115、契合 75／协律 30／应拍 10／空引 5、完成 120/120 与「按 R 再奏」；
  中文字体完整，无缺字或裁切。
* Windows build：`Builds/M4/YingYun.exe`（StandaloneWindows64、Mono；`Builds/` 不进版控）。实际启动 Player 后，
  361 颗音符在无输入情况下依次 Miss，最后输出 `[M4] result | score=0 | accuracy=0.00 % | maxCombo=0`，
  证明结算分支在 Windows build 正常触发；玩家日志无脚本异常。
* 中文字体：使用 Windows 动态中文字体作为占位显示；未复制系统字体文件进项目。

### 验收结果

| M4 条件 | 结果 |
|---|---|
| Combo／Score／Accuracy 只在 Judgment 端计算，UI 只读 | ✅ `GameplayStatistics` 只消费 `JudgmentResult`；View 层无计分公式 |
| 结算数字与执行中 Console 统计一致 | ✅ 同一结果流逐项断言 Engine 最终 Score／Accuracy／MaxCombo／计数一致 |
| 评级（S~D）门槛放于 `DifficultyConfig` | ✅ 门槛已集中配置，并用自定义门槛测试边界 |
| 中文实时 HUD 与结算面板 | ✅ 离屏 uGUI 1280×720 截图检查；四档文字、评级与明细完整 |
| Windows build 结算 | ✅ Player 实际跑到最后判定并输出 `[M4] result` |

* **结论：M4 完成。** 三项专属完成条件全部达成，EditMode 回归、结算画面与 Windows build 触发均有记录。

### Git Commit

* `21da038` `feat(m4): add scoring hud and result screen`

### 风险 / 已知问题

* 结算截图使用注入的确定性样本，用途是布局、字体与评级显示验收；实时数值一致性由 EditMode 判定回放测试独立证明。
* 自动化会话中的 Windows Player 无可见窗口句柄，因此未能对 build 画面做人工截图；已以 Editor uGUI 离屏图与
  Windows Player 的 `[M4] result` 日志组成双重证据。
* `R` 重开逻辑已接入并通过代码路径检查；本次未在不可交互的自动化 Player 会话中注入实体按键。

### 下一步

* 进入 M5 进阶音符：Hold、组合音符与舞蹈连贯度。

---

## [2026-09-21] M2 補充驗收 - 三分鐘譜面邊界修正與真人採樣

### 新增

* 無。

### 修改

* `RhythmPrototypeController.CreatePrototypeChart()` 的音符數量納入 180 秒終點，將最後音符由 179.5 秒修正為 180.0 秒。
* 此修改只修正驗收譜面的右開區間邊界；不變更 BPM、判定窗、輸入映射、Scene、Packages 或 ProjectSettings。

### 測試

* Unity recompile：`completed`、`compilationFailed=false`、errors=0。
* 邊界探針：`count=361`、`lastTimeSec=180`。
* EditMode 回歸：`YingYun.Tests` → **total=20 / passed=20 / failed=0 / skipped=0**。
* Windows 修正版建置：StandaloneWindows64、`Succeeded`、errors=0、warnings=1；警告為未設定可選的 Runtime Pipeline Config。
* Editor 真人採樣：非 Miss **n=404**（Perfect 214、Good 190）、Miss 107、最長 `songTime=194.52s`；Pause 2、Resume 1、Schedule/Restart 5。
* Windows 真人採樣：非 Miss **n=46**（Perfect 25、Good 21）、Miss 44；Pause 1。Player 日誌另有 6 次 Unity TLS 憑證驗證失敗，未造成遊戲流程例外。

### 驗收結果

| M2 補充驗收項目 | 結果 |
|---|---|
| 測試譜面覆蓋完整 180 秒 | ✅ 最後音符時間為 180.0 秒 |
| Editor 非 Miss n ≥ 60 | ✅ n=404 |
| 單次連續播放 ≥180 秒 | ✅ Editor 最長 194.52 秒 |
| Editor Pause／Resume／Restart | ✅ 日誌均有記錄 |
| Windows build 非 Miss n ≥ 60 | ⚠️ n=46，尚差 14 |
| Windows build Resume／Restart | ⚠️ Pause 已記錄；Resume／Restart 未進入日誌 |

* **結論：邊界修正與 Editor 補充驗收通過；M2 補充驗收仍未結案。** 依使用者收尾指示，本輪不再要求重複操作，也不把未達項記為通過。

### Git Commit

* `05c8c4f` `fix(m2): include three-minute chart endpoint`

### 風險 / 已知問題

* Windows 樣本不足與 Resume／Restart 缺證據屬驗收缺口，不等同已定位的程式缺陷。
* Windows Player 的 Unity TLS 憑證警告來自外部連線驗證；目前未觀察到對離線節奏核心的影響，後續若導入雲端服務需另行處理。

### 下一步

* 下次只需在 Windows build 補足至少 14 次非 Miss，並確認 Resume 與 Restart 日誌，即可關閉 M2 補充驗收；完成前不推進 M4。

---

## [2026-09-21] M3 - 六部位放射式音符視覺

### 新增

* `RadialNoteGeometry`：由絕對 `songTime`、音符時間與可見提前量計算音符進度／位置，不使用 `deltaTime` 累加。
* `RadialNotePresenter`：六個身體部位判定環、六方向音符生成、24 個 View 的預熱物件池、判定文字與回收流程。
* `RadialNoteView`：單顆音符的純畫面表示，只接受 Presenter 給予的位置與判定結果，不包含判定窗或分數邏輯。
* `RadialNoteGeometryTests`：新增 5 個位置／進度邊界測試。

### 修改

* `RhythmPrototypeController`：每次 Restart 只建立一份 `NoteData[]`，同時交給既有 `JudgmentEngine` 與新的 Presenter；將既有 `JudgmentResult` 單向轉發至 View。
* M3 畫面採執行期幾何佔位（`SpriteRenderer`／`LineRenderer`／`TextMesh`），不新增正式美術、Prefab、第三方套件、asmdef 參考或 ProjectSettings 變更。
* `YingYun_Gameplay.unity` 最終維持原狀；Presenter 由 Controller 在執行期以最小整合掛載。

### 測試

* **編譯**：Unity Editor recompile → `completed`、`compilationFailed=false`、errors=0。
* **EditMode 回歸**：`YingYun.Tests` → **total=20 / passed=20 / failed=0 / skipped=0**（既有 15 + M3 幾何 5）。
* **Play Mode**：1280×720、1366×768（16:9）、1280×800（16:10）皆可完整顯示六個判定環、鍵位／部位標籤、中央佔位與六方向音符。
* **物件池**：穩態量測 `active=4 / pooled=20 / created=24`；持續播放後 `created` 仍為 24，沒有逐拍建立物件。
* **命中／失敗回收**：注入單次 `Perfect` 結果後 `noteId=2` 於回饋時間後回收，`created=24`；自動 Miss 同樣顯示文字並回收。
* **暫停／續播／重開**：Pause 3 秒期間同一音符位置與 `active=4 / created=24` 不變；Resume 正常；Restart 後 `active=0 / pooled=24 / created=24`。
* **Console**：最終 Play Mode `error=0 / warn=0`，無 compile error。

### 驗收結果

| M3 完成條件 | 結果 |
|---|---|
| 音符依 `songTime` 出現／移動，物件池無每幀 Instantiate | ✅ 位置只由絕對歌曲時間計算；24 個 View 預熱後數量保持不變 |
| 命中／失敗後消失並回收 | ✅ Perfect 注入與實際自動 Miss 皆完成回收 |
| View 層不含判定邏輯 | ✅ View 只接收 `NoteData`、位置與 `JudgmentResult`；判定仍由純 C# `JudgmentEngine` 負責 |
| 判定文字與音符消失時機一致 | ✅ 兩者共用 `releaseDelaySeconds=0.18` 與同一 `songTime` 清除時點 |

* **結論：M3 驗收通過。** 六部位環形操偶介面的核心可讀性已建立；正式皮影角色與部位動作仍保留至 M6。

### Git Commit

* `4ca3007` `feat(m3): add radial body-part note visualization`

### 風險 / 已知問題

* 目前為幾何佔位，不代表最終皮影美術；中央角色僅用舞台佔位表示。
* 六軌固定為 Q 頭部、W 身體、E 右手、A 左手、S 左腳、D 右腳；仍需真人 Playtest 觀察誤按分布與直覺性。
* 本次只完成 M3 視覺驗收，沒有把 M2 遺留的真人樣本／Windows build 補測混入同一工作單位。

### 下一步

* 使用已有視覺提示補齊 M2 真人驗收：Editor 與 Windows build 各累積非 Miss 樣本 n ≥ 60、取得單次連續 ≥180 秒播放，並驗證 Windows build Resume／Restart；完成後再進入 M4。

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

* `690976a` `docs(repository): synchronize project development contract`

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
