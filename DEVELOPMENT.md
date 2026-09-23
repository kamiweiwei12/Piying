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
| 目前 Milestone | **M8.1 手勢與上身**：按掌／托掌正式舞句已驗收；穿掌／翻腕已完成單手簽能力預覽與技術回歸，待剪影驗收後才決定是否接入正式循環。其餘 M8.1 手勢、M8.2–M8.4 與 M9 仍未實作。 |
| 可用基礎 | Unity 6000.6.2f1 / URP 17.6.0（**Forward Renderer**，非 Renderer2D）/ Input System 1.19 / uGUI + TMP / Cinemachine 6.6 / Audio (DSP buffer 1024, 48 kHz) |
| 重大缺口 | M7 核心 Demo、M8 預編舞段首版及第一批按掌／托掌正式舞句已通過實機驗收。現有十式仍是灰盒舞句，非完整戲曲動作庫；其餘手勢、水袖、經驗證的步法和組合套路仍列於 §4.1。目前音樂／BPM 指定於場景控制器，音符由程式生成、舞句按十式循環，**尚不能由使用者對自己的音樂指定動作**。自編舞依 §4.2 分期。 |
| 版控 | Git 已初始化，`main` 為目前主線 |
| Cline 資產 | `.cline/skills/` 10 個 Skill、`.clinerules/` 4 份 Rule（`00`–`03`） |

## 3. 開發里程碑 M0 → M8

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
| **M8** | 預編皮影舞段首版 | 載入時生成全曲舞句、成功命中啟動、Miss 保持姿態、至少 4 拍／最多 2 主動關節、緩動及可辨認轉身；完整動作庫分期完成 | 不新增套件／渲染設定；實機舞蹈觀感需使用者驗收 |
| **M9（規劃，未批准實作）** | 自訂歌曲與編舞 | 先用 Unity Editor 為自己的音樂建立音符譜與舞句譜、預覽和驗證，再評估正式遊戲內匯入音檔與自製關節軌跡 | 需先定資料契約與音樂授權；Editor 工具／資產建立另提 Plan 批准 |

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
- [x] 三難度（Easy / Normal / Hard）結構與數值差異明確
- [x] 選曲 → 遊玩 → 結算 流程無需在 Editor 手動介入
- [x] 延遲校準值可調、可保存、可即時生效
- [x] 出包（至少 Windows）可正常執行，節奏不偏移
- [x] 中文 UI 使用包内 CJK 字型，不依賴目標機器的系統字型

### M8 — 預編皮影舞段首版（已結案）
- [x] 載入時一次生成全曲舞句，運行時只按時間取樣、不臨時決定動作；輸出 `【拍數，動作名稱，激活關節，持續拍數】`
- [x] 每段至少 4 拍、最多 2 個主動關節；姿態連續緩動，無逐拍換招與瞬跳
- [x] 成功命中對應音符才啟動舞句；Miss 保持當前姿態，等待下一個成功舞句
- [x] 首批單山膀、雲手、順風旗、轉身、揚袖、雙山膀、反雲手、亮相可辨認；轉身須有正反面視覺變化
- [x] EditMode 全量測試、Windows build 及視覺驗收通過；使用者實機確認舞蹈感

### 4.1 M8 動作庫分期清單（不可因首版完成而刪除）

**M8-R 結構先行（M8 首版已實機驗收）**：依[陝西非遺的影人製作說明](https://www.sxfycc.com/index.php/home/Index/library_detail.html?id=706)以身、左右手三支主操縱簽建模；頭和雙足仍保留遊戲六方向對應的輔助杆，視覺上與主簽區分。胸腰分片鉚接、頸與肩接胸片、髖與腿接骨盆片，避免上身轉動直接扭動支撐腿。該資料的傳統影人為 11 主要部件，腿與足一體；本遊戲因要做步法演出而額外加入腕、手形扇片、分段水袖、膝、踝及足片，**屬數位改編，不冒稱傳統製作規格**。兩節腿足點約束驗證承重、抬腳與膝彎。這是可觀察的工程灰盒，不聲稱還原特定皮影劇種或完成戲曲套路。M8 首版舞句已獲使用者整段實機確認；此結論不延伸為完整戲曲動作庫已完成。

**M8-R 游玩接線補正（M8 首版已實機驗收）**：此前抬腳僅存在 `PreviewStructure`，正式 `DancePlayback` 每幀覆寫為固定雙足落點。現將單側手簽舞句的對側輔助腳簽足點、抬落高度與重心軌跡在載入時預取樣；成功錨點才播放，Miss 保持姿態。每句最多兩支主動操縱杆：同一手簽連動肩肘算一支，配一支腳簽；雙手或身頭同時動作不再額外啟動腳簽。膝踝是足點約束的被動結果。此軌跡只作結構運動檢查，**不是已核實的戲曲步法**；[北京東城皮影資料](https://www.bjdch.gov.cn/mldc/bglj/fwzwhyc/ctxj/202401/t20240123_3543098.html)僅支持特殊跨步影人可加腳杆，不提供本作精確關節軌跡。

**M8-R 轉身修正（含晚擊邊界，使用者已實機確認）**：上一版 `轉身` 只有胸腰／頭剪影翻面，骨盆及腿足仍正向。依[皮影回轉身操演教材](https://www.minjianyishu.net/index.php?act=app&appid=244&leibie=460&page=24)的可核對文字，影人轉身應整體左右翻轉，雙足保持在影幕地平線；[黑龍江省文旅廳傳承人訪談](https://wlt.hlj.gov.cn/wlt/c115580/202606/c00_31949483.shtml)也特別指出轉身時需避免腳懸空及身子飄起。因此不把單側手簽舞句的抬腳弧線硬套進轉身，而讓胸腰、裙片、鉚接髖位與足片同向改向、足點不離地。傳統教材稱三簽同步，但本遊戲沿用每段最多兩個主動控制點，只使身簽與一支手簽主動，另一手與腿足被動連帶；**這是數位改編，不宣稱完整復刻傳統三簽轉身**。使用者後續發現轉身接下一式可能瞬移；定位為晚擊時整段曾以實際命中時間向後平移，導致下一句準時開始時轉身尚未提交新朝向。播放現固定對齊譜面起拍，正、反兩次轉身的晚擊邊界已有自動回歸，Windows 候選亦獲使用者實機確認。現有幾何片形仍是灰盒；完整戲曲動作庫仍未完成。

**M8-S 舞句銜接（使用者已實機確認）**：中國戲曲學院的[手眼身法步說明](https://bo.nacta.edu.cn/py/yf/byf/index.htm)強調各部位的聯繫與協調；附件僅提供動畫轉譯方向，未給出逐拍關節角度。本輪因此只修現有八句的銜接，不新增或宣稱還原新套路：雲手收肘後入順風旗、順風旗高臂經轉身承接揚袖、雙山膀定勢後左臂收下再以右臂接反雲手、反雲手收右臂後亮相、亮相定住再收身。收勢屬**前一句自身最後兩拍**，不在下一句額外啟動第三支簽；相鄰且成功完成的舞句選用預編直連，漏擊或跳句則選用預編恢復曲線。此為受現有 2D 灰盒與雙主動杆限制的舞句銜接設計，**不是來源證實的傳統逐招連法**。M8 首版的連續舞感、收勢與表演呼吸已獲使用者實機確認；後續新增套路仍須重新查證與驗收。

**M8-H 左側演出解說（動作名稱可見性已獲使用者實機確認）**：從實際舞句播放狀態更新名稱、主動關節及拍數；待拍、收勢、保持與漏擊分別標示。漏擊的譜面舞句必須寫「未演」，不能按拍點冒稱皮影已做該動作。使用者本次確認左側動作名稱已顯示；其他文案、不同視窗比例與整段舞蹈觀感未由此確認。現有八招皆僅為灰盒可播放舞句；腕指被動跟隨、袖片及抬落腳只是結構能力，不把它們列成已驗收的手勢／袖功／戲曲步法。

**M8 整段實機觀察表（含晚擊轉身回歸，使用者已確認通過）**：以 M8-H Windows 版完整播放一曲，記錄各項發生的拍數／時間、命中狀態、可辨剪影、支撐腳與收勢；有異常時附畫面或影片及重現輸入。使用者先前已完整遊玩並確認整段，後續回報的晚擊轉身瞬移亦已修正並完成 Windows 實機複驗。

| 觀察段 | 需核對的畫面與承接 | 狀態 |
|---|---|---|
| 單山膀 → 雲手 | 單臂起勢、對側足點；轉入雲手時肩肘不硬歸位 | 通過 |
| 雲手 → 順風旗 | 雲手末段收肘，順風旗雙臂方向可辨，抬落腳自然 | 通過 |
| 順風旗 → 轉身 | 高臂承接；轉身時上下身同向、雙足接地，無莫名偏轉 | 通過；轉身局部與整段均已確認 |
| 轉身 → 揚袖 | 翻面後方向穩定，揚袖接勢不卡頓、袖片軌跡可見 | 通過；晚擊轉身的朝向邊界補正亦已複驗 |
| 揚袖 → 雙山膀 | 揚袖收勢與雙臂定勢有呼吸，足底不滑 | 通過 |
| 雙山膀 → 反雲手 | 左臂收下、右臂接反雲手；無突然回中或短暫停住 | 通過 |
| 反雲手 → 亮相 | 右臂收回後亮相，剪影與前式可區分 | 通過 |
| 亮相 → 下一輪 | 定勢後收身，下一句起勢不瞬跳 | 通過 |
| 命中／連續命中／漏擊後恢復 | 命中錨點才起舞；相鄰成功直連；Miss 不憑空演出，後續命中自然恢復 | 通過 |

**後續明確保留**：另提 Plan 建立經影像／教學核對的連續步法與轉身軌跡、逐招過門及水袖延遲動力學；再逐項完成下表手勢、袖功、身段和組合套路。每式均需有參考來源、影人剪影對照、可見動作與實機觀感驗收；本輪收勢與直連只是銜接候選，不能代替真實舞蹈編排或默認後續已完成。

附件的動作描述是動畫轉譯參考，不是精確關節角度標準。先依[戲曲學院身段教材](https://bo.nacta.edu.cn/py/yf/byf/index.htm)、[皮影操演資料](https://www.ihchina.cn/art/detail/id/8848.html)與[水袖教學](https://music.hgnu.edu.cn/2021/0818/c1698a72006/page.htm)核對動作語義；不明確者先查教學或演出影像，再設計軌跡，不憑想像補細節。下列「本輪」均待驗收，不代表已完成；後續項目須另提 Plan、由使用者批准後實作。

| 階段 | 附件動作／套路 | 前置能力與驗收重點 |
|---|---|---|
| 本輪 M8 首版 | 單山膀、雲手、順風旗、轉身、揚袖、雙山膀、反雲手、亮相（山膀以單／雙變體呈現） | 現有肩、肘、頭、軀幹及側臉剪影；先驗證舞句與翻面，不宣稱完整戲曲動作還原 |
| M8.1 手勢與上身 | 點雲手、提甲、按掌、托掌、穿掌、翻腕、抱拳、拱手、指掌 | 補腕、手掌／手指可見形狀；研究手眼身法步及角色行當，逐式錄參考與剪影比對 |
| M8.2 水袖 | 抖袖、挑袖、甩袖、打袖、勾袖、掸袖、绕袖、涮袖、盘袖、翻袖、抛袖、投袖、拂袖、搭袖、背袖、双抖袖、双直抛袖、双后抛袖 | 分層袖端／拖尾與軌跡控制；「∞」及圓周動作先查可核對的教學影像，測試停頓與收袖，不用單塊袖片冒充水袖 |
| M8.3 身段與步法 | 站相、弓箭步、丁字步、七星步、挂单脚、骑马蹲裆、下腰、翻身、车身、踢腿、小跳、跑圆场 | 腳踝、腳片、腰／胯與舞台位移；區分本輪「轉身」與翻身／車身；依教學資料驗證足位及重心 |
| M8.4 組合套路 | 雲手套路、順風旗套路、水袖圓舞、武將起霸型、跳大架型；其中附件提及的撮步、俏步、雲步、踢甲、洗面、走圓台也逐項保留待查 | 在單式驗收後預編完整段落、加入角色／曲風編舞規則；套路內仍守最少 4 拍單元與最多 2 主動關節限制，必要時分解為相連舞句 |

**M8.1 第一批按掌／托掌（正式舞句已實機驗收）**：現有影人已具左右腕、掌片與指扇分片，但 M8 首版正式舞句只讓腕隨前臂被動偏轉，沒有獨立手勢語義。第一批按掌與托掌的 129 點預編灰盒姿態已獲使用者確認可辨，現置於原八式後形成十式循環；成功錨點才播放，Miss 凍結當前手位。只由一支左手簽帶動肩、肘，腕與指扇為同杆被動分片，不增加主動控制點。[中國戲曲學院手眼身法步](https://bo.nacta.edu.cn/py/yf/byf/index.htm)說明手勢、握拳、出掌及山膀／雲手會依行當有不同規格；[上海戲曲學校戲曲韻律操](https://sh-xiquschool.sta.edu.cn/wmzx/77/cf/c4106a96207/page.htm)把穿掌、翻托掌及眼隨手動列為教學內容；[西北工業大學中國舞教學記錄](https://youth.nwpu.edu.cn/info/1114/7268.htm)提供按掌位於胃前、沉肩圓肘的可核對描述。本作角度僅按上述語義轉譯成可辨剪影，並非教材量測值。正式托掌由按掌末姿直接展開，最後兩拍收勢回中；漏擊或跳句沿用恢復曲線。使用者已在 Windows 整曲確認按掌→托掌→下一輪承接、左側名稱及 Miss 後恢復。現有橢圓掌片仍不足以表現蘭花指、拳等精細指型。

**M8.1 第二批穿掌／翻腕（實驗預覽待驗收）**：[上海戲曲學校戲曲韻律操](https://sh-xiquschool.sta.edu.cn/wmzx/77/cf/c4106a96207/page.htm)將穿掌列為手臂訓練，兒童版以雙穿掌接翻托掌，並要求眼隨手動；本輪只驗證現有左手簽的「身前聚手後向外穿出」與「手位固定時翻換腕面」，未加入雙手、眼神或翻托掌完整組合。兩式各為 129 點預編取樣，肩、肘、腕與指扇由同一支手簽連動，雙足不移位。`GetFormal` 明確拒絕未驗收的兩式，避免它們被誤接正式舞句。對照圖為 `Logs/M8-1-thread-palm-gather.png`、`Logs/M8-1-thread-palm.png`、`Logs/M8-1-turn-wrist-before.png`、`Logs/M8-1-turn-wrist-after.png`；角度屬本作剪影實驗，不是教材量測值。

若附件中的「掸／绕／涮／盘／抛／双」等名稱與後續採用的正式戲曲教材有異，以核實來源訂正名稱並保留對照表，不默默刪項。旋轉、袖尾或腳步需要新增關節與分層素材時，先做能力設計與使用者確認。

### 4.2 自訂音樂與編舞路線（規劃，尚未實作）

1. **先定資料契約，再做工具**：將固定八招循環替換為獨立的「音符譜」與「舞句譜」。每段舞句記開始拍、動作 ID、持續拍數、觸發音符、承接／收勢／過門類型與來源狀態；音樂另記 BPM、首拍偏移與曲長。載入時驗證錨點對應、至少 4 拍、最多 2 主動關節、連續性與接地，然後一次預編，不改判定核心。
2. **M8.1–M8.3 擴動作庫，M8.4 建可組合套路**：只有經參考與實機剪影驗收的動作標為可正式選用；未驗收的動作標「實驗」，不默默視為完成。編舞可明確指定何時承接、收勢或插入過門，不將所有姿態硬接。
3. **M9 第一版：Unity Editor 作者工具**：匯入自有 AudioClip，輸入／校準 BPM 與首拍偏移，在拍格放置音符與舞句、用現有音樂時鐘預覽，通過驗證後存為可版本控管的資料資產。這一版讓作者**編排已完成的動作**，不要求玩家在正式遊戲中匯入任意檔案。
4. **M9 後續另議**：要「創造新動作」須做關節／足點／袖尾軌跡編輯與安全驗證；要在 Windows 遊戲內直接載入任意外部音檔，須另處理音訊格式、讀取錯誤、授權及不同 BPM 段。兩者均另提 Plan，不與第一版作者工具混為已完成。

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
| F6 | 包内 CJK 字型資產（中文 UI） | 讓既有 uGUI `Text`／`TextMesh` 不依賴系統字型 | **已批准並完成（M7.3）** |
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
| 1.12 | 2026-09-21 | M6.5 Hold 修正：按住提示延續至結束，竹桿與皮影片全程持續推拉；音符顏色統一為 Tap／Hold／Chord 三類，EditMode 93/93、Windows build 通過 |
| 1.13 | 2026-09-21 | M7 候选版：单曲选曲、Easy／Normal／Hard、双偏移校准保存与完整返回流程；EditMode 96/96、Windows build 通过，等待实机验收与正式 CJK TMP 字体 |
| 1.14 | 2026-09-21 | M7 修正：新增 DSP 暂停菜单、返回选曲与三秒倒数；Easy 改为无 Chord 且每秒一颗音符，EditMode 100/100、Windows build 通过 |
| 1.15 | 2026-09-21 | M7 判定修正：判定窗放宽为 ±50／±90／±150 ms；清理孤立 Release、Hold 只认本次按下后的松开并在持续按到尾端时自动完成；EditMode 103/103、Windows build 通过 |
| 1.16 | 2026-09-21 | M7 结案：内置 Noto Sans SC 中文字体并统一三处运行时 UI 字体入口；字形覆盖测试与 EditMode 104/104、Windows build 通过 |
| 1.17 | 2026-09-22 | 建立 M8 預編舞句首版條件及附件完整動作庫分期清單；首版仍待實機舞蹈觀感驗收，不把後續動作視為已完成 |
| 1.18 | 2026-09-22 | 記錄 M8-R 轉身下身同向候選、參考依據及數位改編限制；完整轉身觀感與後續動作庫均保留待驗收 |
| 1.19 | 2026-09-22 | 記錄使用者已確認 M8-R 轉身；M8-S 分別預編承接與收勢，保留整段舞蹈觀感及其餘動作庫待驗收 |
| 1.20 | 2026-09-22 | 增列 M8-H 左側演出解說與實際／預定動作區分；規劃 M9 自訂音樂、譜面、舞句譜與作者工具，不將未完成動作列為可用 |
| 1.21 | 2026-09-23 | M8 首版實機結案；M8.1 先以按掌／托掌驗證腕與掌片的剪影辨識能力，通過使用者驗收前保持實驗狀態 |
| 1.22 | 2026-09-23 | 記錄使用者確認按掌／托掌剪影；兩式接入正式十式循環，托掌末兩拍收勢回中，保留整曲實機驗收 |
| 1.23 | 2026-09-23 | 修正晚擊轉身接下一式時朝向瞬移，記錄 140/140 回歸、Windows 建置及使用者實機複驗通過 |
| 1.24 | 2026-09-23 | 記錄按掌／托掌正式整曲、相鄰承接、左側名稱及 Miss 後恢復已獲使用者實機驗收 |
| 1.25 | 2026-09-23 | 新增穿掌聚手至穿出的路徑與定點翻腕能力預覽；保留雙手、眼神、正式舞句及完整翻托掌待後續驗收 |
