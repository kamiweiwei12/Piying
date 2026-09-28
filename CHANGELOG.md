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

## [2026-09-28] V2-P1 - 首招双展山膀

### 新增

* 新增纯 C# `PuppetV2Pose` 全身姿态契约，明确 root、头腰、双手目标、双腕、双足、鞋面、朝向和手型通道。
* 新增八拍“06 双展山膀”五阶段轨迹：起势、收肘蓄势、展开、显势、微收出口；翻腕作为内部细节，不再单列动作。
* 新增 `PlanarTwoBoneArmSolver`，分别按左右弯肘分支从腕目标反解肩、肘，避免复制 raw Euler 导致双臂串边。
* 输出五张关键帧、49 帧连续序列与循环 GIF 至 `Logs/PuppetV2/P1-DoubleMountainArm/`。

### 修改

* Presenter 新增隔离的 P1 预览入口，未接入正式歌曲。P1 姿态内将骨盆标定至 `y=-0.40`，左右腿分别向外弯，使膝角保持自然并让鞋底继续落在地平线。
* 旧舞句、旧腿解算路径、音符、判定、场景、Prefab、素材和项目设置均未修改。

### 测试

* P1 定向 EditMode `3 / 3` 通过：验证动作名称/八拍/五阶段、241 个连续时刻、双腕目标误差 `<0.003`、左右手不越过中线、双脚接地及膝弯方向。
* Unity 6000.6.2f1 EditMode 全量回归 `total=213 / passed=213 / failed=0 / skipped=0`，报告 `Logs/PuppetV2/P1-DoubleMountainArm/full-editmode-results.xml`。
* 五阶段截图人工复核：两腕分处躯干两侧且同高，肩肘腕铆钉连接连续；收肘阶段双手不串边；峰值双膝没有旧零姿的交叉菱形。

### 验收结果

* P1 自动验证和内部画面复核通过；等待用户确认实际动作观感后才进入 P2。

### Git Commit

* `feat(puppet): add v2 double mountain arm`（本工作单位提交）。

### 风险 / 已知问题

* 当前入口是开发预览，不会在正式歌曲触发；P7 才统一接线。
* 循环 GIF 是验证证据，不代表最终游戏帧率或动作时长；正式时长按八拍和歌曲拍点取样。

### 下一步

* 用户确认后进入 P2“15 提膝挂脚”，建立支撑足锁定、骨盆移重心和正确落脚顺序。

## [2026-09-28] V2-P0 - 现有十五分片绑定标定

### 新增

* 新增 `PuppetRigV2Calibration` 只读绑定契约，记录 15 张真实美术分片、14 个主关节、四段骨长、零姿、左右旋转轴、舞台安全框及手臂/腿部可达半径。
* 新增运行时标定快照，按真实 SpriteRenderer 边界读取人物可见高度、双手末端代理点和双鞋底接触点。
* 新增 P0 EditMode 回归与 `Logs/PuppetV2/P0/P0-rig-calibration-and-joint-sweeps.png` 标定证据；同目录报告记录所有关节局部坐标。

### 修改

* 仅为现有 Presenter 增加只读标定入口；没有重做动作、改变正式舞句、节奏判定、场景、Prefab、渲染设置或素材。
* V2 计划状态更新为 P0 完成、等待验收后进入 P1。

### 测试

* V2-P0 定向 EditMode：`2 / 2` 通过。逐一对 14 个主关节执行 `-12° / 0° / +12°` 扫动，验证旋转前后铆钉链长度误差小于 `0.0001` 局部单位。
* 实测可见高度 `H=4.4518`；手臂可达环 `0.10..1.54`，腿部可达环 `0.06..1.70`；双手和双鞋末端均在安全框内。
* Unity 6000.6.2f1 EditMode 全量回归 `total=210 / passed=210 / failed=0 / skipped=0`，报告 `Logs/PuppetV2/P0/full-editmode-results.xml`。

### 验收结果

* 自动结构验收与标定图人工复核通过；P0 未新增动作，因此不以旧动作观感冒充 P1 验收。

### Git Commit

* `feat(puppet): calibrate v2 segmented rig`（本工作单位提交）。

### 风险 / 已知问题

* 手尖采用当前整掌 Sprite 的最低边界中心作为代理点；真正单指/拳掌素材仍按 P4 单独审批。
* 标定图和报告位于被忽略的 `Logs/`，不会混入 Git；测试可重复生成。

### 下一步

* 用户确认 P0 后才进入 P1“06 双展山膀”，建立第一套 V2 完整 Pose 与手目标求解。

## [2026-09-28] V2 规划 - 存档旧版与十八个完整动作方案

### 新增

* `PUPPET_ACTION_V2_PLAN.md`：18 个命名大动作的拍数、肩肘腕、头腰、手型、足点、位移、区别、衔接、来源及逐步验收计划。
* 已为 `fceb62842410f3ed1a28cd73ea3a6a4cb7ea1d19` 创建 `archive/puppet-v1-20260928` 注释标签；离线历史包位于 `Logs/puppet-v1-20260928.bundle`。

### 修改

* DEVELOPMENT 记录 V2 处于规划待确认状态。本轮无运行时代码或素材变更。

### 测试

* 已检查标签解析到指定 commit，`git bundle verify` 通过，完整历史可恢复。
* 文档检查包含 18 个不同动作、来源与工程设计值区分、P0–P8 验收步骤及十项项目风险；本轮不运行 Unity 测试。

### 验收结果

* 存档和研究计划已交付；V2 动作尚未实施，也未进行视觉或实机验收。

### Git Commit

* `docs(puppet): archive v1 and plan eighteen integrated actions`（本规划工作单位提交）。

### 风险 / 已知问题

* 标签和 bundle 覆盖已提交历史，不包含未提交设置、未跟踪 Docs 或被忽略的构建/截图；这些文件保留原位。
* 来源未提供该影偶精确角度；计划中的参数是待美术验证的项目设计。整掌素材不能直接实现独立指节，侧脸也不能直接生成正面/背面五官。

### 下一步

* 确认计划后仅开展 P0 美术关节标定；后续一招通过再做下一招。

## [2026-09-28] 美术 A2-R5 候选 - 动作幅度、脚步与转身景深校准

### 新增

* 新增云手画面高度回归，以实际左手腕位置而非仅检查肩角数值，防止长袖分片再次遮住高点。
* 新增分片转身透视回归，确认原 `FacingScale` 连续轨迹在侧身中点仍保留可读宽度，并持续完成正反换面。
* 输出云手、左右脚步及转身三阶段验收截图至 `Logs/ActionAmplitude/`。

### 修改

* 保留既有舞句顺序、五段取样结构、节拍、判定、Miss 冻结和每句主动杆定义；仅放大原八式现有肩肘关键姿态。
* 云手第三段改按分片骨骼的实际负角方向抬至头部以上，末姿仍保持 `-24° / 0°`，不改变接顺风旗的边界。
* 单山膀／云手仍只带右脚，扬袖／反云手仍只带左脚；原抬落弧线由外探 `0.32`、抬高 `0.34`、移重心 `0.14` 放大为 `0.50 / 0.50 / 0.20`，未新增正式步法。
* 分片转身由整宽瞬时换面改为连续收窄，侧身最窄保持 56% 宽度，再展开到反面；足点与原朝向轨迹不变。

### 测试

* Unity 6000.6.2f1 EditMode 全量回归 `total=208 / passed=208 / failed=0 / skipped=0`，报告 `Logs/M11-action-amplitude-results.xml`。
* 截图复核确认云手手掌越过头部高度；右脚／左脚按原定舞句侧别外探抬起；转身具正面、侧身、反面三个可辨阶段。
* `StandaloneWindows64` 验证建置成功，输出 `Builds/M11-Action-Amplitude/YingYun.exe`，BuildReport 为 `Success`、0 error、1 个既有 Pipeline 配置 warning、314,503,211 bytes。
* `git diff --check` 通过；未纳入既有 URP、ProjectSettings、Docs 与 Connect Share 工作区改动。

### 验收结果

* 自动与静态视觉验收通过；仍待使用者在实际整曲播放中确认动作幅度和转身观感。

### Git Commit

* `fix(puppet): strengthen existing dance motion`（本工作单位提交）。

### 风险 / 已知问题

* 足部仍是既有结构验证弧线，不宣称为已核实的戏曲步法；M8.3 已跳过的正式步法范围没有恢复。
* 转身立体感为 2D 分片的横向透视模拟，并非新增背面贴图或 3D 骨骼。

### 下一步

* 使用者在完整歌曲中复核云手最高点、四个既有脚步舞句与连续转身；如需细调，仅校准幅度，不重排动作。

## [2026-09-28] 美术 A2-R4 候选 - 分片皮影复用既有手势骨骼

### 新增

* 新增分片手部跟随既有指掌／握拳驱动的回归，覆盖单指收窄延伸与拳掌礼右拳闭合。
* 新增分片转身显示回归，确认原 `FacingScale` 动作轨迹继续运行，但高细节贴图不再在侧身中点压成细线。
* 新增右上臂素材绑定回归，防止再次误用右前臂贴图。

### 修改

* 未修改 `DanceChoreography`、十四式动作角度、节拍、衔接或判定逻辑；只修正新分片美术对旧骨骼的显示适配。
* 新皮影左右手掌继续挂在原手腕位置，同时同步继承原指掌节点的局部旋转与缩放，使按掌、托掌、穿掌、翻腕、拳掌礼和单指重新获得换贴图前已有的手势变化。
* 单指沿用既有 `PointFingerAmount`，在分片美术层将完整手掌收窄并延长；拳掌礼沿用既有 `RightHandClosure` 驱动右手贴图闭合。
* 转身继续使用原连续朝向轨迹驱动逻辑与下肢，只在分片贴图显示层进行完整宽度换面，避免人物在半转时被横向压成一条线。
* 右上臂恢复使用专用 `puppet_right_upper_arm` 分片，右前臂继续使用 `puppet_right_forearm`，不再在两节关节重复同一前臂贴图。

### 测试

* `ShadowPuppetPresenterTests` 32/32 通过，包含 3 项新增美术骨骼适配回归。
* Unity 6000.6.2f1 EditMode 全量回归 `total=206 / passed=206 / failed=0 / skipped=0`。
* 已逐项截取十四式动作的 25%、55%、82% 三阶段，共 42 帧；总览位于 `Logs/PuppetActionFix/contact-sheet-01-07.png` 与 `contact-sheet-08-14.png`。
* `StandaloneWindows64` 验证建置成功，输出 `Builds/M11-Puppet-Art-Adapter/YingYun.exe`，BuildReport 为 `Succeeded`、0 error、1 个既有 Pipeline 配置 warning、314,500,053 bytes。

### 验收结果

* 转身中段不再消失成细线；按掌、托掌、翻腕等手部旋转已反映到新手掌贴图；拳掌礼右拳会收紧；单指会形成明显的窄长指向轮廓。
* 顺风旗、扬袖与双山膀的原动作方向保持不变，右上臂和右前臂改由各自分片显示。

### Git Commit

* `fix(art): reconnect segmented puppet gesture rig`（本工作单位提交）。

### 风险 / 已知问题

* 分片美术只有一张完整手掌贴图，单指由既有手势参数在显示层塑形，并非额外新增一张逐指素材；当前轮廓已可辨认，仍需使用者实机确认风格接受度。
* Build warning 为既有 `RuntimePipelineConfig` 未配置，不影响本次皮影显示修正。

### 下一步

* 使用者在已打开项目中实机检查转身、翻腕、拳掌礼与单指；如仍需调整，仅校准分片贴图适配参数，不改动作编排。

## [2026-09-28] 美术 A2-R3 候选 - 双臂舞句方向与残留修正

### 新增

* 新增舞句左右侧回归，明确检查“顺风旗”“扬袖”的右肩必须向人物右侧外展，不得复用左臂负角符号。
* 新增连续舞句状态回归，覆盖上一招抬起右臂、下一招未使用右臂时，右肩和右肘必须平滑回到中立位。

### 修改

* 修正“顺风旗”“扬袖”与“双山膀”衔接中的右肩角度符号：右臂由错误的负角跨到人物左侧，改为正角向人物右侧展开。
* `DancePlayback` 在舞句边界记录四个手臂关节起始角；当前舞句没有驱动的肩／肘会沿原有预编混合曲线平滑归位，不再无限继承上一招姿态。
* 保留 Miss 冻结当前姿态、命中开启舞句、转身方向、脚步落地、手腕手势及每句原有主动操纵杆定义。

### 测试

* `DanceChoreographyTests` 30/30 通过，新增右臂方向和未使用手臂归位两项回归。
* Unity 6000.6.2f1 EditMode 全量回归 `total=203 / passed=203 / failed=0 / skipped=0`。
* 对两首正式曲进行逐舞句实时取样；“顺风旗”中左肩约 `-62°`、右肩约 `+145°`，双腕分处身体两侧。
* 已检查 `Logs/M11-arm-side-fix.png`，确认原截图中两手集中在左侧的问题已消失。
* `StandaloneWindows64` 验证建置成功，输出 `Builds/M11-Arm-Recovery/YingYun.exe`，BuildReport 为 `Succeeded`、0 error、314,499,541 bytes。

### 验收结果

* 错误根源确认包含两层：右肩预编轨迹使用了左臂的角度符号；舞句播放器又让未参与下一招的手臂永久保留旧角度。两层均已在数据和播放状态机中修正。

### Git Commit

* `fix(puppet): restore bilateral arm choreography`（本工作单位提交）。

### 风险 / 已知问题

* 自动截图与全量回归已通过，正式曲完整连续表演的节奏观感仍需使用者在已打开项目中实机确认。
* 建置仍有既有 Inference Engine shader variant 警告，本次为 187 warning、0 error。

### 下一步

* 使用者实机观察“顺风旗 → 转身 → 扬袖”与“扬袖 → 单手舞句”两组衔接；若个别动作仍需风格调整，只改对应舞句轨迹，不再动关节装配。

## [2026-09-28] 美术 A2-R2 候选 - 首音平滑入场与皮影关节对齐

### 新增

* 音符入场增加独立的 1.75 秒预览缓冲，并在接近路径前段渐显；三秒倒数、歌曲时钟与谱面判定时刻继续各自保持原定义。
* 新增首音入场回归，覆盖倒数期间零生成、开放边界不抢跑，以及首音从完整路径进入后按谱面时间到达判定圈。
* 新增皮影分片枢轴回归，检查头、躯干、骨盆、四肢与鞋的枢轴均落在素材可见铆钉中心。

### 修改

* 三秒倒数结束后不再把首音压缩成极短移动；倒数显示结束时，歌曲仍处于 `-1.75s` 预览阶段，首音由场外正常渐显进入，实际音频零点、`NoteTimeSec` 与判定窗口均未改变。
* 通过 Sprite Editor 数据接口把 15 个皮影分片的 pivot 对齐到图像中的可见关节，并同步校正肩、髋、颈部锚点、肢体长度与局部旋转。
* 右上臂改用同图集内直形袖片，避免原弯曲分片自身包含多段关节而与运行时骨架重复，既有 21 关节、十四式舞句、六根操纵杆和判定驱动保持不变。

### 测试

* `RadialNoteViewTests` 13/13 通过；`ShadowPuppetPresenterTests` 29/29 通过。
* Unity 6000.6.2f1 EditMode 全量回归 `total=201 / passed=201 / failed=0 / skipped=0`。
* 已检查 `Logs/M6-5-shadow-play-chord.png`、`Logs/M8-R-northern-rig-step.png` 与 `Logs/M8-1-fist-palm-salute.png`，确认头颈、肩袖、骨盆、腿与鞋的铆钉连接明显改善。
* `StandaloneWindows64` 验证建置成功，输出 `Builds/M11-Puppet-Alignment/YingYun.exe`，BuildReport 为 `Succeeded`、0 error、314,499,541 bytes。

### 验收结果

* 开头首音不再从边缘快速闪过；皮影分片按可见铆钉连接，静态站姿与既有动作截图均未再出现原先明显的关节悬空或交叉错位。

### Git Commit

* `fix(art): align puppet joints and smooth first note`（本工作单位提交）。

### 风险 / 已知问题

* 当前结论基于 EditMode、自动截图和 Windows 建置；最终舞台比例下的动作观感仍需使用者在已打开项目中实机确认。
* 建置仍有既有 Inference Engine shader variant 警告，本次为 187 warning、0 error。

### 下一步

* 使用者实机确认正式曲首音入场速度与头颈、肩、髋、膝、踝连续动作；若仍有单一关节偏差，再按部位做最小范围微调。

## [2026-09-28] 美术 A2-R1 候选 - 三秒倒数音符入场门控

### 新增

* 音符显示层增加零秒入场边界：歌曲时间为负时，即三秒开演倒数期间，音符池不会放出任何音符。
* 新增倒数边界回归，覆盖 `-3s / -2s / -1s / -0.000001s` 均无音符，以及 `0s` 才开放首批音符。

### 修改

* 首批音符若距离歌曲零点不足完整的 1.75 秒可见提前量，会从零秒的生成点开始，以缩短后的显示提前量进入。
* 显示提前量只改变移动速度，不修改谱面 `NoteTimeSec`、判定窗口或音频时钟；音符仍在原判定时刻到达判定圈。

### 测试

* `RadialNoteViewTests` 12/12 通过，包含倒数前零生成、零秒开放和生成点／判定圈时间对齐。
* Unity 6000.6.2f1 EditMode 全量回归 `total=199 / passed=199 / failed=0 / skipped=0`。
* `StandaloneWindows64` 验证建置成功，输出 `Builds/M11-Puppet-Countdown/YingYun.exe`，BuildReport 为 `Succeeded`、0 error、314,499,029 bytes。

### 验收结果

* 三秒倒数结束前音符完全不入场；倒数结束后才从场外进入，并保持显示到圈与原判定时刻一致。

### Git Commit

* `fix(gameplay): gate notes until countdown ends`（本工作单位提交）。

### 风险 / 已知问题

* 时间为 0 的原型谱面音符会在零秒直接位于判定圈，这是该音符原始判定时刻决定的；两首正式曲目的首音均在零秒之后，会从生成点进入。
* 建置仍有既有 Inference Engine shader variant 警告，本次为 187 warning、0 error。

### 下一步

* 使用者在已打开项目中实机确认倒数结束瞬间、正式曲首音移动速度，以及新皮影的关节观感。

## [2026-09-28] 美术 A2 候选 - 关节分片皮影替换

### 新增

* 依据完整皮影与左右手参考图，新增一套透明底的 15 分片皮影图集，包含头、躯干、骨盆、左右上臂／前臂／手、左右大腿／小腿／鞋。
* 通过 Sprite Editor 写入各部件矩形、关节枢轴和稳定名称，运行时从同一图集装配到既有铆钉关节。

### 修改

* `ShadowPuppetPresenter` 保留原 21 关节、十四式舞句、六根操纵杆及判定事件，只将旧几何占位外观替换为红、金、黑皮影分片。
* 双手改挂到左右腕关节，避免袖口与手掌跟随时错位；长按仍可牵动袖片。
* 转身的逻辑朝向仍连续从 1 过渡到 -1，但美术侧身宽度保留最小值，避免完整皮影在中点压成不可见的一条线。

### 测试

* `ShadowPuppetPresenterTests` 28/28 通过，覆盖分片图集装配、单指／掌式／抱拳、转身、下肢落地、长按牵杆与截图验收。
* 已人工检查 `Logs/M6-5-shadow-play-chord.png`、`Logs/M8-turn-mid.png` 与 `Logs/M8-1-fist-palm-salute.png`，确认关节驱动、双脚承重及操纵杆连接保持有效。

### 验收结果

* 皮影美术替换作为独立最小步骤通过技术与渲染验收；原动作和判定逻辑未被推翻。

### Git Commit

* `feat(art): replace puppet with jointed artwork`（本工作单位提交）。

### 风险 / 已知问题

* 生成图集与原始参考图均保留，不覆盖素材库原文件；后续如需更精细的服饰边缘，可继续按单一关节替换。
* 本步骤不包含音符入场倒数门控；该项按下一独立工作单位处理。

### 下一步

* 三秒倒数期间禁止音符进入，倒数结束边界再开放生成，并保持谱面判定时间不变。

## [2026-09-28] 美術 A1-R2 候選 - 組合音素材化端點與花紋橋

### 新增

* 透過 Sprite Editor 將既有 `note_chord.png` 拆成左側圓形端點、中央花紋橋、右側圓形端點三個 Sprite，不另造替代圖形。

### 修改

* 組合音兩端改用素材庫原圖內的紅青圓形紋樣，與單音的長條裝飾外形明確區分。
* 中央連接件改用素材原圖的白色外暈、紅色雙線與青色內芯；依兩個實際判定端點旋轉和延伸，移除上一版程式生成的純色 `LineRenderer`。
* 端點保持等比例，橋位於端點後方；組合音的兩個位置仍由各自 lane 與同一 `NoteTimeSec` 計算，不改判定邏輯。

### 測試

* `RadialNoteViewTests` 11/11 通過，驗證三個切片存在、兩端使用指定圓形 Sprite、中央使用指定花紋橋且不含 `LineRenderer`；預覽輸出為 `Logs/M11-note-art-preview-v3.png`。
* Unity 6000.6.2f1 EditMode 全量回歸 `total=198 / passed=198 / failed=0 / skipped=0`。
* `StandaloneWindows64` 驗證建置成功，輸出 `Builds/M11-Art-Notes-R3/YingYun.exe`，BuildReport 為 `Succeeded`、0 error、308,177,957 bytes。

### 驗收結果

* 組合音素材切片、生成邏輯、預覽、全量回歸與 Windows 建置技術驗收通過；等待使用者實機確認各種 lane 組合下的端點與花紋橋觀感。

### Git Commit

* `fix(art): use patterned chord components`（本工作單位提交）。

### 風險 / 已知問題

* 同一張原圖的完整組合音 Sprite 仍保留，供追溯與比較；執行期只載入三個新切片。
* 建置仍有既有 Inference Engine shader variant 警告，本次為 187 warning、0 error。

### 下一步

* 使用者實機確認圓形端點大小、橋寬與花紋延伸效果；確認後才進入皮影關節重繪。

## [2026-09-28] 美術 A1-R1 候選 - 長音隧道收束與組合音重製

### 新增

* 長音素材以 Sprite Editor 拆成大端、中段、小端三個可獨立控制的 Sprite；端部維持等比例，中段依實際頭尾距離伸縮。
* 長按成立後，對應判定圈會以歌曲時間每 0.5 秒持續循環白色打擊圓環，直到尾判完成或失敗。

### 修改

* 長音按下後，大端在判定圈處隱去；中段只沿軌道逐步變短，小端保持原比例並隨 `NoteTimeSec + DurationSec` 移向判定圈，形成進入隧道的消失效果，不再整張壓縮。
* 組合音不再顯示合成大貼圖，改由每個實際判定位置各生成一枚與單音相同的音符，並以白色外框／青色內芯的橋連接兩端。
* 所有移動、縮短與特效循環仍只讀歌曲時間；判定仍由既有 Judgment 層負責，顯示端點與按下／尾判時刻保持同源。

### 測試

* 音符定向測試 12/12 通過，新增長音持續圓環、隧道式縮短、尾端不壓縮以及組合音無合成貼圖的回歸覆蓋。
* Unity 6000.6.2f1 EditMode 全量回歸 `total=198 / passed=198 / failed=0 / skipped=0`；新版 1280×720 預覽為 `Logs/M11-note-art-preview-v2.png`。
* `StandaloneWindows64` 驗證建置成功，輸出 `Builds/M11-Art-Notes-R2/YingYun.exe`，BuildReport 為 `Succeeded`、0 error、308,165,973 bytes。

### 验收结果

* 程式、時序、渲染預覽、全量回歸與 Windows 建置技術驗收通過；現等待使用者在實際遊玩中確認長按動態與組合音觀感，確認後才進入皮影關節重繪。

### Git Commit

* `fix(art): refine hold and chord visuals`（本工作单位提交）。

### 风险 / 已知问题

* 原組合音貼圖保留為未使用參考資產，沒有刪除或覆蓋；本次不修改皮影、背景、URP／ProjectSettings。
* 建置仍會輸出既有 Inference Engine shader variant 警告，本次為 187 warning、0 error，不影響建置成功。

### 下一步

* 使用者實機確認長按期間持續圓環、尾端進入判定圈以及各類組合音的橋接觀感；通過後再按關節逐步重繪皮影。

## [2026-09-28] 美術 A1 候選 - 音符素材與長音頭尾對齊

### 新增

* 導入單音、長音、組合音符與點擊光圈四張正式透明素材，使用 Sprite Editor 裁去外圍透明留白並保留原圖色彩。
* 六個判定點新增 0.5 秒點擊光圈：前 0.2 秒擴張，後 0.3 秒收縮淡出；長音在按下成立與尾判成功時分別觸發。

### 修改

* 單音改用獨立裝飾音符，組合音符以兩個實際判定端點定向，取代灰盒圓點與連線。
* 長音大端嚴格對應 `NoteTimeSec` 的按下判定位置，小端嚴格對應 `NoteTimeSec + DurationSec` 的釋放判定位置；畫面仍只讀歌曲時間，不參與判定。
* 長音以頭尾兩點直接定向與縮放完整素材，避免 Unity `SpriteDrawMode.Sliced` 在目前導入資料下不出圖，並維持顯示端點與實際判定一致。

### 測試

* `RadialNoteViewTests` 10/10 通過，涵蓋素材裁剪載入、長音頭尾時刻／方向及 1280×720 三類音符預覽；預覽輸出為 `Logs/M11-note-art-preview.png`。
* Unity 6000.6.2f1 EditMode 全量回歸 `total=196 / passed=196 / failed=0 / skipped=0`。
* `StandaloneWindows64` 驗證建置成功，輸出 `Builds/M11-Art-Notes/YingYun.exe`，BuildReport 為 `Succeeded`、0 error、308,162,437 bytes。

### 驗收結果

* 程式、時序、預覽與 Windows 建置技術驗收通過；依「最小步驟、成功後再下一步」原則，本工作單位不修改皮影，等待使用者實機確認音符觀感後再開始關節拆分重繪。

### Git Commit

* `feat(art): integrate note visuals`（本工作單位提交）。

### 風險 / 已知問題

* 長音使用完整原圖依實際頭尾距離縮放，極短或極長 Hold 會連同端部花紋一起縱向縮放；目前優先保證顯示端點與判定端點完全一致。
* 本次未修改皮影、背景、URP／ProjectSettings，也未處理素材說明中與現行竹桿操偶設計衝突的舊版「頂部吊線」描述。

### 下一步

* 使用者在 Editor 或本次 Windows 驗證包確認單音、長音、組合音符及點擊光圈觀感；確認後才進入皮影關節拆分與接縫補繪。

## [2026-09-24] M10 候選 - Windows x64 獨立 Demo 與運行診斷

### 新增

* 選曲頁新增「運行日誌」入口，可查看最近記錄、刷新、複製完整日誌及開啟日誌資料夾；`YingYun-latest.log` 記錄遊戲／Unity 版本、Windows、CPU、GPU、記憶體、顯存、解析度、普通訊息與異常堆疊。
* 新增 `Tools/BuildCompetitionDemo.ps1`：等待 Unity 真實退出碼、驗證必要 Player／Beat This／模型／授權檔、排除備份及開發日誌，再產出 ZIP 與 SHA-256。
* 新增同伴版 `README.txt` 來源及 `DELIVERY.md`，記錄執行、自訂歌曲、問題回報、支援環境與校驗方式。

### 修改

* 正式內置選曲只顯示《象王行》與《青玉案》，預設為《象王行》；《試燈》資產與回歸測試保留，但不進入交付版選曲。玩家自訂 MP3／FLAC／WAV 功能保持可用。
* 診斷日誌採固定 120 筆記憶體環形紀錄、畫面只顯示最近 24 筆、每秒批次刷新檔案；錯誤與異常立即刷新，避免每幀重建或即時重型計算。
* 日誌檔允許寫入期間共享讀取；自動測試捕獲並修正「複製完整日誌」在 Windows 發生 sharing violation 的問題。
* 原生依賴表顯示 Beat This／ONNX Runtime 需要四個 Visual C++ x64 DLL；改由 Visual Studio 2022 官方 Redistributable 目錄採 application-local 方式隨分析器提供，避免乾淨電腦缺少 VC++ Runtime。
* 直接在中文解壓路徑分析時重現上游 `bad conversion`；固定 GitHub commit 的本地補丁改用 Windows 寬字元入口、miniaudio 寬路徑 API 與 UTF-8 ONNX 模型路徑，並固定 ONNX Runtime 官方下載 SHA-256。

### 測試

* Unity 6000.6.2f1 EditMode 全量回歸 `total=193 / passed=193 / failed=0 / skipped=0`；包含正式兩首選曲白名單與診斷環境頭／寫入中完整讀取測試，結果為 `Logs/M10-editmode-results.xml`。
* Release Windows x64 建置成功；`Logs/M10-competition-demo-build.log` 記錄 `Build Finished, Result: Success.`，交付包包含 226 個檔案、Beat This 原生執行檔、ONNX 模型、Visual C++ x64 應用本地執行庫及第三方授權，且無 `BackUpThisFolder`、`Logs` 或 `.git`。
* 最終 ZIP 解壓至含空格與中文的全新路徑後隱藏啟動 15 秒，程序持續運行並輸出 `[M10] diagnostics-ready`、`[M6.5] shadow-play-ready`；無 `NullReferenceException`、缺 DLL、缺方法或崩潰標記。
* 同一中文解壓路徑中以成品分析器處理中文檔名《象王行》：退出碼 0、399 拍、105 個強拍、68,014,124-byte WAV；確認模型、音訊與輸出路徑均支援 Unicode。
* 產物：`Builds/Competition-Demo/YingYunDemo-Windows-x64.zip`，155,261,319 bytes；SHA-256 `25BB52A52A9E17EA9834556B102A18C7432A63A2ABC2F40FA631697041F1EBED`。

### 驗收結果

* 兩首內置曲目、診斷能力、乾淨打包與同機異路徑啟動已形成可交付候選；真正不同電腦的硬體／驅動相容性須由同伴實機啟動並回傳日誌後才能判定 M10 結案。

### Git Commit

* `feat(m10): package portable windows demo`（本工作單位提交）。

### 風險 / 已知問題

* 支援範圍為 Windows 10／11 x64；未驗證 Windows 以外平台、32 位元系統或不支援 DirectX 11 的舊顯卡。
* 測試版尚未程式碼簽章，Windows SmartScreen 可能顯示未知發行者；同伴應先核對 SHA-256。
* 未修改既有髒改動中的 ProjectSettings，因此視窗產品資訊與持久資料路徑仍使用目前 `DefaultCompany/My project`。正式改名、解析度與簽章需另列批准。
* 本工作單位不補 M2 三分鐘播放及暫停／續播／重開證據，也不替正式曲目完成授權判定。

### 下一步

* 將 ZIP 與 `.sha256` 一起交給同伴，在另一台 Windows 10／11 x64 電腦解壓運行；若有問題，回傳 `YingYun-latest.log`、曲目、難度與操作步驟。完成第二台實機及 M2 長時間回歸後再判定 M10 結案。

## [2026-09-24] M9-B 驗收 - 玩家自訂歌曲正式通過

### 新增

* 新增使用者實機驗收證據，確認修正版自訂歌曲流程、三難度與整曲表現通過。

### 修改

* `DEVELOPMENT.md` 將 M9-B 由技術候選更新為已完成，並把下一工作單位推進至 M10 比賽 Demo 交付規劃。
* M9 路線保留 Windows x64、格式與快取限制；不把其他平台或大型曲庫最佳化誤列為已完成。

### 測試

* 本次只同步實機驗收狀態，未修改程式，未重跑 Unity。沿用修正版有效證據：EditMode 191/191、Windows build success，以及使用者 FLAC 417 拍、Easy 105／Normal 209／Hard 625 的實包就緒紀錄。

### 驗收結果

* 使用者在完整验收 Plan 后明确回复「通过」；M9-B 的选曲、三难度、自动拍点与十四式舞句正式通过。

### Git Commit

* `docs(m9): accept runtime custom song flow`（本工作單位提交）。

### 風險 / 已知問題

* 自訂歌曲仍限 Windows x64 的 MP3／FLAC／WAV，首次分析會建立大型 WAV 快取；玩家自有音樂授權仍由玩家負責。
* M8.4《試燈》整曲觀感與 M2 Windows 長時間證據仍保持原待辦，不由本次驗收自動完成。

### 下一步

* 提出 M10 比賽 Demo 交付 Plan：先盤點正式演示曲與授權，再補 Windows 長時間／暫停／續播／重開證據、操作說明及乾淨建置。

## [2026-09-24] M9-B 修正 - FLAC／WAV 自訂歌曲掃描

### 新增

* 玩家歌曲資料夾新增 FLAC 與 WAV 支援，與 MP3 一樣使用既有 miniaudio 解碼、Beat This 分析及 SHA-256 快取；副檔名不分大小寫。
* 新增六個副檔名回歸案例，覆蓋 `.mp3`／`.MP3`／`.flac`／`.FLAC`／`.wav` 及不支援的 `.txt`。

### 修改

* 掃描由固定 `Directory.GetFiles(..., "*.mp3")` 改為列出資料夾檔案後套用支援格式白名單。
* 空資料夾提示改為「MP3／FLAC／WAV」，載入失敗日誌改用通用 Audio 文案。
* 選曲列表偵測到新增歌曲時自動跳到最後一頁，避免第四首歌曲已生成卻藏在第 2 頁。
* `DEVELOPMENT.md` 的目前里程碑、格式範圍與驗收步驟同步為三種格式。

### 測試

* 問題現場確認 `UserSongs` 內實際檔案為 `星降る海-Aqu3ra 早見沙織.flac`；舊版只掃描 `*.mp3`，因此沒有分析、快取或新按鈕。
* 原生分析器直接读取该 FLAC 成功：417 拍、105 个强拍、估算 100 BPM。
* Unity 6000.6.2f1 EditMode 全量回歸 `total=191 / passed=191 / failed=0 / skipped=0`；結果 `Logs/M9-B-audio-format-fix-results.xml`，包含新增歌曲自動翻至末頁及既有選取頁保持測試。
* Windows x64 重建成功；`Logs/M9-B-audio-format-fix-build.log` 記錄 `Build Finished, Result: Success.`。
* 修正版實包直接掃描使用者原始 FLAC，生成 417 行 `.beats`、97,321,532-byte WAV 快取，且無 `.tmp` 殘留；就緒資料為 Easy 105／Normal 209／Hard 625。

### 驗收結果

* 已重現並修正「刷新後沒有新歌曲」的根因，實際 FLAC 已通過完整分析與歌曲資料生成；畫面按鈕及整曲手感仍由使用者在修正版候選確認。

### Git Commit

* `fix(m9): recognize flac and wav custom songs`（本工作單位提交）。

### 風險 / 已知問題

* FLAC／WAV 與 MP3 同樣只支援 Windows x64；首次分析及未壓縮 WAV 快取可能占用明显磁盘空间。
* 自動生成結果仍需逐曲驗收；格式支援通過不代表每個來源檔都必然具有可辨識節拍。

### 下一步

* 使用者以修正版候選確認 `星降る海-Aqu3ra 早見沙織` 按鈕可見，並依序試玩三種難度；若卡點或舞句有具體異常，再按時間點修正。

## [2026-09-24] M9-B 候選 - 玩家 MP3 自動分析、三難度譜與隨機舞句

### 新增

* 開始畫面新增「開啟歌曲資料夾」「刷新歌曲」與分析狀態；歌曲按鈕以三首一頁顯示，玩家只需把 MP3 放入 `Application.persistentDataPath/UserSongs`。
* 新增 Windows x64 執行期分析器：固定 `beat_this_cpp` commit `07ab790a9ec2eda8093d52d249e3ec4f0510ee72`、Beat This `final0` ONNX 模型與 ONNX Runtime 1.18.0；工具來源、最小修補、SHA-256 及所有第三方授權均保存於專案及成品。
* 新增純 C# `.beats`／float WAV 解析、SHA-256 版本快取、動態歌曲定義，以及依檔案雜湊可重現的音符譜與十四式舞句生成器。

### 修改

* `SongDefinitionAsset` 與控制器改用共用可播放歌曲介面，內建三曲與執行期歌曲走同一套 DSP、判定、難度與舞句播放路徑。
* 自動譜避免固定 `ASDQWE` 輪播；Easy 僅單鍵 Tap，Normal 加入 1／2／3／4 拍 Hold 且無 Chord，Hard 才加入拍內變奏及 Chord，並只禁止 `SW`、`QD`、`AE`。
* 舞句每八拍選用現有十四式並確定性洗牌；相鄰不重複、不連續轉身，首尾保留亮相。分析與音訊解碼在遊玩前完成，遊玩幀不執行模型或即時建模。

### 測試

* 原生分析器以《象王行》與《青玉案·蘭芥》對照既有 Beat This 結果；分别为 399／399 及 381／382 个拍点在 50 ms 内匹配，直接 MP3 分析约六秒。
* Unity 6000.6.2f1 EditMode 全量回歸 `total=183 / passed=183 / failed=0 / skipped=0`；結果 `Logs/M9-B-runtime-import-editmode-results.xml`。新增測試涵蓋拍點格式、變拍、三難度契約、四種 Hold 長度、合法和弦、舞句錨點、確定性及 float WAV。
* Windows x64 建置 `Builds/M9-B-Runtime-Import/YingYun.exe`；`Logs/M9-B-runtime-import-build.log` 記錄 `Build Finished, Result: Success.`。
* 實包將《青玉案·蘭芥》臨時放入 `UserSongs`，自動生成 382 行 `.beats` 與 80,852,388-byte WAV 快取後加入歌曲；測試 MP3 副本已移除，Player log 無腳本例外。

### 驗收結果

* 自動測試、打包與真實 MP3 管線煙測通過；本項保持技術候選，等待使用者在 Windows 候選確認歌曲按鈕、首次分析提示、三難度卡點及整段舞句觀感。

### Git Commit

* `feat(m9): add runtime custom song generation`（本工作單位提交）。

### 風險 / 已知問題

* 目前只打包 Windows x64 分析器且只掃描 MP3；首次分析每首約六秒，並建立約等同未壓縮音訊大小的 WAV 快取。《青玉案·蘭芥》快取約 80.9 MB。
* 同一檔案會依 SHA-256 重現相同結果，但自動拍點、三難度與舞句仍需真人整曲驗收；玩家須自行確保音樂使用權。
* 多首長曲同時載入的記憶體最佳化、其他平台、更多音訊格式與變拍曲專門策略不屬本候選範圍。

### 下一步

* 請使用者以 Windows 候選把一首未快取 MP3 放入歌曲資料夾，驗收按鈕、等待提示、三難度與整曲卡點；依具體時點做最小修正後再結案。

## [2026-09-24] M9-A3 驗收 - 《青玉案·蘭芥》正式通過

### 新增

* 新增使用者實機驗收證據，確認 M9-A3-R1 的方向變化、長短 Hold 與困難組合規則可接受。

### 修改

* 《青玉案·蘭芥》的 `TimingStatus` 由 `AnalysisCandidate` 更新為 `Verified`；離線作者譜重建時保留正式狀態。
* `DEVELOPMENT.md` 將目前工作推進至 M9-B Unity Editor 作者工具規劃。

### 測試

* 狀態回歸驗證《象王行》《青玉案·蘭芥》《試燈》均為 `Verified`；Unity 6000.6.2f1 EditMode 全量回歸 `total=177 / passed=177 / failed=0 / skipped=0`，結果 `Logs/M9-A3-verify-editmode.xml`。

### 驗收結果

* 使用者在 M9-A3-R1 Windows 候選後明確回覆「可以」；M9-A3 不再保留為技術候選。

### Git Commit

* `chore(m9): verify qing yu an lan jie chart`（本工作單位提交）。

### 風險 / 已知問題

* M9-A3 通過不代表 Unity Editor 通用作者工具、遊戲內任意音檔匯入或新動作軌跡編輯已完成。
* M8.4《試燈》整曲舞感與先前結算可讀性驗收仍保留原紀錄，未由本次回覆自動結案。

### 下一步

* 另提 M9-B Unity Editor 作者工具 Plan，第一版只編排既有音符類型、拍點與已驗收動作，保存為可版控歌曲資產。

## [2026-09-24] M9-A3-R1 候選 - 譜面方向、長按與組合鍵手感修正

### 新增

* 新增 `KeyboardChordLayout` 共用契約，列出 Q/W/E/A/S/D 的 12 種允許雙鍵組合。
* 新增載入驗證，僅拒絕 `SW`、`QD`、`AE` 三組困難模式 Chord；`QW`、`QE`、`QS` 等其他組合均為合法。
* 新增《青玉案·蘭芥》方向動機、四種 Hold 拍長、Hold 占鍵及相鄰舞句方向簽名回歸。

### 修改

* 《青玉案·蘭芥》移除固定六鍵輪轉，改用 12 組八拍方向動機按起、承、轉、合排列；Hard 半拍音符使用獨立偏移。
* 《青玉案·蘭芥》Normal／Hard 的 Hold 由固定 2 拍改為 1／2／3／4 拍固定作者表；Hold 期間其他音符與 Chord 不會重用被占用的實體鍵。
* 《象王行》只替換違規 Chord，拍點、音符数、Hold、一般方向、舞句及 `Verified` 狀態不變。
* 《試燈》Hard 由輪替全部 15 種雙鍵改為輪替 12 種允許組合。

### 測試

* 首次資產重建在編譯測試時發現目前 NUnit 不支援整數版 `Does.Not.Contain`；改用布林集合斷言後重新執行，未修改遊戲規則。
* Unity 6000.6.2f1 EditMode 全量回歸：`total=177 / passed=177 / failed=0 / skipped=0`；結果 `Logs/M9-chart-feel-editmode.xml`。
* Windows x64 建置：`Builds/M9-Chart-Feel/YingYun.exe`；`Logs/M9-chart-feel-build.log` 記錄 `Build Finished, Result: Success.` 及 PlayerBuildInfo success。
* Windows 候選完成 20 秒啟動煙霧測試，進入 `[M6.5] shadow-play-ready`；`Logs/M9-chart-feel-player-smoke.log` 未出現腳本例外。

### 驗收結果

* 自動驗證確認三首 Hard 均不含 `SW`、`QD`、`AE`，且其餘 12 種組合全部能通過共用載入契約；《試燈》實際覆蓋 12 種。
* 《青玉案·蘭芥》仍為 Easy 96／Normal 191／Hard 572 顆音符與 48 段舞句，Normal／Hard 實際覆蓋 1／2／3／4 拍 Hold，沒有 Hold 實體鍵衝突。
* 方向變化、長短 Hold 視覺與困難組合的最終手感待使用者實機確認。

### Git Commit

* `fix(chart): improve lane variety and chord ergonomics`（本工作單位提交）。

### 風險 / 已知問題

* 自動測試能封鎖固定循環、錯誤拍長及禁用組合，不能代替完整歌曲的打譜吸引力與手部舒適度驗收。
* 《青玉案·蘭芥》保持 `AnalysisCandidate`；《象王行》的拍點驗收狀態不因單纯替換違規 Chord 而降級。
* 煙霧日誌仍有既有 Unity Connect 憑證訊息；本輪未修改 Unity Connect、ProjectSettings 或 URP。

### 下一步

* 請使用者實機測試 `Builds/M9-Chart-Feel/YingYun.exe`，重點確認《青玉案·蘭芥》的方向變化、四種 Hold 長度，以及三首 Hard 不再出現 `SW`、`QD`、`AE`。

## [2026-09-24] M9-A3 候選 - 《青玉案·蘭芥》資料驅動關卡

### 新增

* 新增《青玉案·蘭芥》離線作者譜，直接使用已保存的 382 個 Beat This 拍點，不在執行時分析音訊或生成譜面。
* 寫入 Easy 96、Normal 191、Hard 572 顆音符；三難度共用每 8 拍一次的 48 個舞句觸發錨點。
* 新增 48 段固定套路：起 8、承 14、轉 14、合 12 段，最後亮相依歌曲可播放結尾計算為 11 拍。
* 新增《青玉案·蘭芥》的數量、難度類型、六方向、排序、錨點、主動控制點及尾奏邊界回歸測試。

### 修改

* 開始介面由兩首選曲擴為《試燈》《象王行》《青玉案·蘭芥》三首，移除「待製譜」提示。
* `SongAssetBootstrap` 重建歌曲資產時一併套用《青玉案·蘭芥》作者譜；歌曲狀態保持 `AnalysisCandidate`。
* Easy 僅含單鍵 Tap；Normal 含單鍵 Tap／Hold 且沒有 Chord；Hard 才加入多鍵 Chord。

### 測試

* Unity 6000.6.2f1 EditMode 全量回歸：`total=175 / passed=175 / failed=0 / skipped=0`；結果 `Logs/M9-A3-editmode-2.xml`。
* Windows x64 建置：`Builds/M9-A3-QingYuAnLanJie/YingYun.exe`；`Logs/M9-A3-build.log` 記錄 `Build Finished, Result: Success.` 及 PlayerBuildInfo success。
* Windows 候選完成 20 秒啟動煙霧測試，進入 `[M6.5] shadow-play-ready`；`Logs/M9-A3-player-smoke.log` 未出現腳本例外。

### 驗收結果

* 自動驗證確認三檔數量與操作類型、48 個共享錨點、至少四拍、最多兩個主動控制點及尾奏範圍均符合資料契約。
* 節拍分析仍是候選資料；歌曲開頭、變速段、轉折、尾奏卡點、三難度手感、轉身承接、Miss 恢復與最後收勢待使用者完整實機確認。

### Git Commit

* `feat(m9): add qing yu an lan jie playable chart`（本工作單位提交）。

### 風險 / 已知問題

* 自動測試和建置不能判定音樂卡點與整段舞感；通過實機驗收前不得把 `AnalysisCandidate` 改成 `Verified`。
* 煙霧日誌仍有既有 Unity Connect 憑證訊息；本輪未修改 Unity Connect、ProjectSettings 或 URP。
* Unity Editor 通用作者工具仍未實作；本輪作者資料為可維護的歌曲專用離線腳本。

### 下一步

* 請使用者在 Windows 候選完整測試《青玉案·蘭芥》三難度，特別檢查開頭、中段節奏變化、轉身後承接、Miss 恢復與最後 11 拍亮相；通過後另以驗收提交轉為 `Verified`。

## [2026-09-24] UI／難度 - 結算可讀性與操作類型分級

### 新增

* 結算四項判定統計格增加半透明淺色宣紙底片，讓指定的藍黑墨字在深色結算面板上保持可讀。
* `SongChartValidation` 新增難度操作類型契約：Easy 只允許單鍵 Tap；Normal 必須有 Hold 且禁止 Chord；Hard 必須同時有 Hold 與 Chord。
* 新增契約正反例、三難度生成結果、結算底片與文字顏色的回歸測試。

### 修改

* Easy 移除原有教學 Hold，固定為每秒一顆單鍵 Tap。
* Normal 將原有 Chord 位置改為單鍵 Tap，保留單鍵 Tap／Hold；Hard 才使用多鍵同時按。
* 《象王行》離線作者譜依新契約重建，仍維持 Easy 100／Normal 200／Hard 598 顆音符及 50 段舞句錨點。
* 選曲難度說明改為「單鍵點按」、「單鍵點按與長按」、「點按長按與多鍵合奏」。
* 结算次数数字与书法标签统一使用 `#071B1F`；判定时间窗、计分、DSP 时钟、拍点与舞句均未改变。

### 測試

* 第一次全量回歸為 `172/173`：產品色值正確，但測試直接比較 `Color` 與 `Color32` 類型而失敗；統一斷言類型後重跑。
* Unity 6000.6.2f1 EditMode 全量回歸：`total=173 / passed=173 / failed=0 / skipped=0`；結果 `Logs/difficulty-contract-editmode-2.xml`。
* Windows x64 建置：`Builds/M9-Difficulty-Contract/YingYun.exe`；`Logs/difficulty-contract-build.log` 記錄 PlayerBuildInfo 成功並以 return code 0 結束。
* Windows 候選完成 20 秒啟動煙霧測試，進入 `[M6.5] shadow-play-ready`；`Logs/difficulty-contract-player-smoke.log` 無腳本例外。

### 驗收結果

* 自動驗證確認三檔難度的音符種類已嚴格分離，且《象王行》的數量、排序、六方向、舞句錨點與結束範圍均未回歸。
* 結算四格已具有宣紙底、藍黑書法字與藍黑次數；最終畫面可讀性和三檔實際手感待使用者實機確認。

### Git Commit

* `feat(difficulty): separate note mechanics by level`（本工作單位提交）。

### 風險 / 已知問題

* 《青玉案·蘭芥》尚未製譜；新驗證契約會在其 M9-A3 作者譜中強制套用相同分級。
* 啟動煙霧日誌仍有既有 Unity Connect 憑證驗證訊息；本輪未修改 Unity Connect、ProjectSettings 或 URP。

### 下一步

* 請使用者實機確認結算四項文字與數字可讀，並各試 Easy／Normal／Hard 的 Tap、Hold、Chord 分級；通過後進入 M9-A3《青玉案·蘭芥》製譜 Plan。

## [2026-09-24] UI - 四项判定固定蓝黑墨色

### 新增

* 新增四个判定等级的逐项显示回归，覆盖「契合、协律、应拍、空引」的 Sprite 映射、固定墨色与原始比例。
* 新增舞句完成／中断事件不得覆盖当前判定的回归，以及结算面板四项书法标签的统一样式检查。

### 修改

* 即时判定与结算标签统一使用参考图方向的蓝黑墨色 `#071B1F`；次数数字继续使用既有清晰字体与颜色。
* 即时判定 Sprite 改为等比显示，取消纵向压缩，并将书法判定可见时间固定为 0.32 秒。
* `SegmentCompleted`／`SegmentInterrupted` 事件继续供舞句逻辑使用，但不再显示「合势／断势」，因此不会覆盖真正的四项判定。
* 判定窗口、计分、Combo、DSP 时间轴、Hold 提示与书法图集内容均未改变。

### 測試

* Unity 6000.6.2f1 EditMode 全量回歸：`total=171 / passed=171 / failed=0 / skipped=0`；結果 `Logs/M9-A2-calligraphy-ink-editmode.xml`。
* Windows x64 建置：`Builds/M9-A2-Calligraphy-Ink/YingYun.exe`；`Logs/M9-A2-calligraphy-ink-build.log` 記錄 PlayerBuildInfo 成功並以 return code 0 結束。
* Windows 候選完成 20 秒啟動煙霧測試，進入 `[M6.5] shadow-play-ready`；`Logs/M9-A2-calligraphy-ink-player-smoke.log` 無腳本例外。

### 驗收結果

* 四項指定判定均已有同一藍黑墨色、正確字形與等比顯示的自動驗證；「合勢／斷勢」覆蓋問題已由回歸測試封鎖。
* Windows 技術候選已完成；最終畫面辨識度仍待使用者實機確認。

### Git Commit

* `feat(ui): unify judgment ink feedback`（本 UI 工作單位提交）。

### 風險 / 已知問題

* 深色墨字在不同顯示器與遊戲背景上的最終可讀性仍屬視覺驗收項，不能只由 EditMode 測試代替。
* 啟動煙霧日誌仍有既有 Unity Connect 憑證驗證訊息；本輪未修改 Unity Connect、ProjectSettings 或 URP。

### 下一步

* 請使用者實機確認四項判定均可見、顏色符合參考且畫面不再出現「合勢／斷勢」；通過後進入 M9-A3《青玉案·蘭芥》製譜 Plan。

## [2026-09-24] UI - 四項判定墨筆字

### 新增

* 依使用者提供的視覺參考，以 imagegen 製作透明白色墨筆圖集，四格固定對應「契合、協律、應拍、空引」，可由 Unity 依判定等級著色。
* 新增 `JudgmentCalligraphyAtlas`，在載入時建立四個固定 Sprite；不在每次判定或每幀切割圖片。
* 新增图集资源完整性与即时判定切换测试。

### 修改

* 即时判定的四项成绩改用墨笔 Sprite；“按住、早放、未撑住、合势、断势”继续使用现有清晰字体，因为参考图未提供这些字形。
* 结算面板将四项判定名称改为墨笔图形，次数数字继续使用包内中文字体以保持可读性。
* 判定、计分、Combo、DSP 时间轴和判定文字内容均未改变。

### 测试

* Unity 6000.6.2f1 EditMode 全量回归：`total=167 / passed=167 / failed=0`；结果 `Logs/M9-A2-calligraphy-editmode.xml`。
* 第一次新测试因 EditMode `AddComponent` 不自动调用运行时 `Awake` 而得到 `166/167`；测试夹具显式初始化后全量通过，未修改游戏逻辑。
* Windows x64 建置：`Builds/M9-A2-Calligraphy/YingYun.exe`；`Logs/M9-A2-calligraphy-build.log` 记录 `Build Finished, Result: Success.`。
* Windows 候选完成 20 秒启动烟雾测试，墨笔资源成功从 Resources 载入并进入 `shadow-play-ready`；日志为 `Logs/M9-A2-calligraphy-player-smoke.log`。

### 验收结果

* 四项指定判定已按参考图的墨笔方向完成程式接入、资源验证和 Windows 候选建置；最终画面大小与辨识度待使用者实机确认。

### Git Commit

* `feat(ui): render judgment calligraphy`（本 UI 工作单位提交）。

### 风险 / 已知问题

* 墨笔字为位图 Sprite；极端高分辨率下的锐利度受 1536 × 1536 图集限制，但运行时不产生字体图集扩张或即时建模成本。
* 启动烟雾日志仍有既有 Unity Connect 证书验证讯息；本轮未修改 Unity Connect、ProjectSettings 或 URP。

### 下一步

* 请使用者运行 Windows 候选确认即时判定与结算面板的字形大小；通过后进入 M9-A3《青玉案·兰芥》制谱 Plan。

## [2026-09-24] M9-A2 驗收 - 《象王行》正式通過

### 新增

* 新增使用者完整實機驗收證據，涵蓋歌曲卡點、三難度手感、轉身承接、Miss 恢復及最終亮相。

### 修改

* 《象王行》的 `TimingStatus` 由 `AnalysisCandidate` 更新為 `Verified`；離線作者譜重建時保留正式狀態。
* `DEVELOPMENT.md` 將目前工作推進至 M9-A3《青玉案·蘭芥》製譜。

### 測試

* 狀態测试验证《象王行》为 `Verified`、《青玉案·兰芥》仍为 `AnalysisCandidate`；与随后墨笔字 UI 一起执行的全量 EditMode 为 `167/167` 通过。

### 验收结果

* 使用者明确回复「通过」；M9-A2 不再保留为技术候选。

### Git Commit

* `chore(m9): verify xiang wang xing chart`（本验收状态提交）。

### 风险 / 已知问题

* 《象王行》通过不代表《青玉案·兰芥》的自动拍点、谱面或舞句已经验收。
* 音乐授权资料仍须在比赛交付前整理。

### 下一步

* 先完成使用者指定的四项判定墨笔字 UI，再另提 M9-A3 制谱 Plan。

## [2026-09-24] M9-A2 候選 - 《象王行》資料驅動關卡

### 新增

* 新增《象王行》離線作者譜：沿用 Beat This 的 399 個拍點，序列化 Easy 100、Normal 200、Hard 598 顆音符；三難度均覆蓋六方向並共用每 8 拍舞句錨點。
* 新增 50 段明確排列的起、承、轉、合套路；以現有十四式編排，四次轉身只放在段落方向變化處，最終亮相延長至尾奏前。
* 開始介面新增《試燈》／《象王行》双曲选择與当前曲目显示；《青玉案·兰芥》标示待制谱。

### 修改

* `SongDefinitionAsset` 可输出作者舞句并在载入时验证整数拍位、共同 Tap 锚点、舞句重叠、可玩范围及最多两支主动操纵杆。
* `DanceChoreography` 新增分段 Timing Map 作者舞句编译路径；运行时只采样 129 点预编轨迹，不做音频分析、实时建模或临时编舞。
* 正式控制器从 Song Catalog 选择资料驱动曲目，节拍器统一使用 `SongTimingMap`；新曲在最后音符判定后继续播放尾奏，至曲目结束点才结算。
* 音乐 `AudioSource.loop` 关闭；《试灯》继续使用旧程式谱相容路径，不在本轮迁移旧谱。

### 测试

* Unity 6000.6.2f1 EditMode 全量回归：`total=165 / passed=165 / failed=0`；结果 `Logs/M9-A2-editmode.xml`。
* 新增测试覆盖三难度 100／200／598 数量、排序、六方向、Hold／Chord、50 个共同舞句锚点、最少 4 拍、最多 2 支主动杆及末句不越界。
* Windows x64 建置：`Builds/M9-A2-XiangWangXing/YingYun.exe`；`Logs/M9-A2-build.log` 记录 `Build Finished, Result: Success.`。
* Windows 候选完成 20 秒启动烟雾测试；场景进入 `shadow-play-ready`，没有本次程式异常。`Logs/M9-A2-player-smoke.log` 留存启动日志。

### 验收结果

* 音符、舞句、资料验证、选曲入口、DSP 播放接线、尾奏结算及 Windows 序列化已形成可运行技术候选。
* 自动分析拍点仍保持 `AnalysisCandidate`。完整歌曲的卡点、三难度手感、连续命中／Miss、转身承接及整段舞感尚待使用者实机确认，因此本条目不将《象王行》标为正式关卡完成。

### Git Commit

* `feat(m9): add xiang wang xing playable chart`（本技术候选提交）。

### 风险 / 已知问题

* 目前谱面依据 Beat This 拍点和明确离线规则制作，尚未由真人逐段实听；自动测试只能证明资料一致性，不能证明音乐重音选择和舞感正确。
* 启动烟雾日志仍出现既有 Unity Connect 证书验证讯息；本轮未修改 Unity Connect 或 ProjectSettings。
* 《试灯》仍走旧程式生成谱；两种载入路径会在后续旧谱迁移时再统一。
* 音乐授权资料尚未整理进比赛交付包；URP、ProjectSettings 与 Unity Connect 的既有未提交变动未纳入本候选。

### 下一步

* 请使用者运行 `Builds/M9-A2-XiangWangXing/YingYun.exe`，选择《象王行》逐项确认开头／中段／尾段卡点、三难度密度、转身后承接、Miss 恢复和最终亮相；记录具体秒数后做最小修正并决定是否转为 `Verified`。
* 《象王行》验收后另提 M9-A3《青玉案·兰芥》制谱 Plan；通用 Unity Editor 编舞工具继续保持独立工作单位。

## [2026-09-24] M9-A0／A1 - 導入歌曲並建立可追溯時間資料

### 新增

* 將 `象王行（特别版）.mp3` 與本地版 `青玉案·兰芥.mp3` 導入 `Assets/Audio/Music/`，保留 Unity `.meta`。
* 新增純 C# `SongTimingMap`、`SongChartValidation` 與可序列化 `SongDefinitionAsset`、`SongCatalogAsset`；支援分段速度、複合小節、三難度音符、舞句 Cue、可玩區間及候選／已驗證狀態。
* 新增三首歌曲資料資產；《試燈》保留已驗證 120 BPM，新曲均標為 `AnalysisCandidate`。
* 新增 `Tools/BeatAnalysis/`：保存 Beat This 1.1.0 `final0` 的标准 `.beats` 输出、完整依赖锁定、重建步骤及输入／模型 SHA-256。
* 新增 Editor 导入器，只把外部逐拍时间戳和小节标签转换为 Unity Timing Point，不在游戏运行时分析音乐。

### 修改

* 依使用者指示移除本轮曾建立的自制节拍分析器，改用 GitHub [CPJKU/Beat This!](https://github.com/CPJKU/beat_this) 官方工具；Python、PyTorch、FFmpeg 和模型只存在隔离工具环境，不进入 Unity 仓库或 Player。
* 《象王行》保存 399 个逐拍点，首强拍 0.86 秒；《青玉案·兰芥》保存 382 个逐拍点，首强拍 0.52 秒。两曲均保留模型输出的速度变化和最长 6／8 拍小节标签，不压成单一 BPM。
* 两首长音乐的 Unity 导入方式固定为 `Streaming` 且不预载完整音频，避免曲目目录载入时一次解压整首歌曲。
* 更新目前 Milestone 为 M9-A0／A1 技术基础完成；M8.4 整曲观感仍待验收，新曲尚未接入正式游玩。

### 测试

* Unity 6000.6.2f1 EditMode 最终全量回归：`total=163 / passed=163 / failed=0 / skipped=0`；结果 `Logs/M9-A-final-results.xml`。
* 测试涵盖固定／分段 Timing Map 双向换算、连续性、无判定区间、音符越界、歌曲 ID／资源完整性，以及外部结果的 399／382 拍、0.86／0.52 秒首拍和 6／8 拍小节资料。
* Windows x64 建置：`Builds/M9-A-Song-Data/YingYun.exe`；`Logs/M9-A-build.log` 记录 `Build Finished, Result: Success.`。

### 验收结果

* 两首音频、外部分析来源、可重建环境、原始逐拍证据、Unity 时间资料和载入验证均已建立，Editor 与 Windows 构建通过。
* 本工作单元只完成歌曲与时间轴基础。两首新曲尚无三难度音符谱、舞句谱、选曲 UI 或 Windows 实机游玩，节拍资料仍须在后续制谱时逐段实听，未标记为关卡完成。

### Git Commit

* `feat(m9): add traceable song timing data`（本条目与实现同一提交）。

### 风险 / 已知问题

* Beat This 是自动分析来源，最长 6／8 拍小节及速度加倍／减半区段仍须人工听辨；`AnalysisCandidate` 状态会阻止后续把它误当成已验收谱面。
* 两首 MP3 的使用授权尚未写入项目交付资料；竞赛出包前必须确认授权。
* MP3 属二进制文件且目前未批准 `.gitattributes`／Git LFS，本次按普通 Git 文件提交。
* `.clinerules/01-project-context.md` 仍停在 M4；URP、ProjectSettings 与 Unity Connect 的既有未提交变动未纳入本次成果。

### 下一步

* 另提 M9-A2 Plan：先以《象王行》逐段实听校正候选拍格，编写 Easy／Normal／Hard 音符与现有十四式舞句；完成实机验收后再处理节奏变化更复杂的《青玉案·兰芥》，随后才接选曲 UI。

## [2026-09-23] M8.4 候選 - 《試燈》固定起承轉合套路

### 新增

* 新增《試燈》固定45段套路表：起8段、承14段、轉13段、合10段；末句固定為亮相，所有十四式至少使用一次。
* 新增固定順序、非取模循環、短曲收尾、可重現性及完整45段相鄰邊界測試。

### 修改

* `DanceChoreography.Create` 不再以 `index % 14` 機械輪播，載入時依固定套路表選擇既有預編舞句；運行時仍只取樣，不臨時計算編舞。
* 新相鄰組合從前句實際末姿開始，並在半拍內進入下一句預編曲線，避免雲手→按掌等舊循環未覆蓋的邊界首幀瞬移。
* 音符、判定、DSP 時鐘、十四式曲線、正式動作名稱、Miss 凍結及恢復規則均保持不變。

### 測試

* 首次完整回歸發現雲手→按掌左肩由 `-24°` 瞬切至 `0°`；加入半拍直連後，將連續性檢查擴展至45段全部邊界。
* Unity 6000.6.2f1 EditMode 最終全量回歸：`total=154 / passed=154 / failed=0 / skipped=0`；結果 `Logs/M8-4-routine-results.xml`。
* Windows x64 建置：`Builds/M8-4-Routine-Preview/YingYun.exe`；`Logs/M8-4-routine-build.log` 記錄 `Build Finished, Result: Success.`。

### 驗收結果

* 固定順序、完整動作覆蓋、末句亮相、相鄰首幀連續、轉身邊界、Miss 與重啟可重現性已通過自動回歸。
* 本次是技術候選；整曲是否具有可辨的起承轉合、是否出現不自然重複或收尾呼吸不足，仍待使用者在 Windows 完整播放後確認，M8.4 尚未結案。

### Git Commit

* `feat(m8): compose fixed trial light routine`（本候選提交）。

### 風險 / 已知問題

* 本套路只重排現有十四式，無法表現已跳過的水袖、步法、武將起霸或跳大架；段落層次完全依靠次序、方向、收勢和過門形成。
* 自動測試能證明順序和數值連續，不能代替真人對整段舞感、重複密度與表演呼吸的判斷。
* `.clinerules/01-project-context.md` 仍停在 M4；URP、ProjectSettings 與 Unity Connect 的既有未提交變動均未納入。

### 下一步

* 請使用者運行 M8.4 Windows 候選完整播放一曲，重點確認起8段、承14段、轉13段、合10段的分段感、轉身後的方向穩定、重複動作密度、Miss 後恢復及最終亮相；按具體時點修正後再決定 M8.4 結案。

## [2026-09-23] M8.3 收尾 - 跳過身段與步法並進入 M8.4

### 新增

* 新增 M8.3 範圍決定紀錄：依使用者指示跳過身段與步法，下一階段為 M8.4 組合套路。

### 修改

* `DEVELOPMENT.md` 將目前 Milestone 推進至 M8.4，並將站相、重心、弓箭步、丁字步、七星步等 M8.3 項目標為跳過而非完成。
* M8.4 範圍收斂為只使用現有十四式已驗收舞句安排起、承、轉、合；不加入水袖、步法、武將起霸、跳大架或其他未驗收動作。

### 測試

* 本工作單位只修改 `DEVELOPMENT.md` 與 `CHANGELOG.md`，未改 Runtime、測試、Scene、Prefab 或設定；以 Git 差異確認程式維持提交 `2193f91` 的無水袖十四式版本。
* 最近一次有效技術基線保持 EditMode `152/152` 通過，Windows 包 `Builds/M8-2-No-Water-Sleeve/YingYun.exe` 建置成功；本次文件範圍決定不重跑 Unity。

### 驗收結果

* 使用者明確要求「跳過步法，進入 8.4」；M8.3 因範圍決定收尾，不記為已完成的步法能力。

### Git Commit

* `docs(m8): skip body movement and enter routine phase`（本條目與範圍同步同一提交）。

### 風險 / 已知問題

* M8.4 無法使用未建立的足位、重心及舞台位移；套路差異必須由現有十四式的次序、時間、方向、收勢與過門形成。
* `.clinerules/01-project-context.md` 仍停在 M4；本輪未獲修改批准，因此未更動。
* URP、ProjectSettings 與 Unity Connect 的既有未提交變動未納入本次成果。

### 下一步

* 另提 M8.4-A 組合套路骨架 Plan：先用現有舞句編排完整起、承、轉、合及漏擊恢復，再由使用者實機驗收整段舞感。

## [2026-09-23] M8.2 收尾 - 移除水袖候選並跳過水袖

### 新增

* 新增 M8.2 範圍決定紀錄：水袖依使用者實機觀感判定跳過，下一階段改為 M8.3 身段與步法。

### 修改

* 完整反向套用水袖候選提交 `66d5e1e`，移除 `WaterSleevePlate.png`、`WaterSleeveChoreography`、四個被動袖節的 Presenter 預覽及其專用測試。
* 正式程式回到 M8.1 收尾的十四式版本；不保留水袖 Sprite、額外旋轉關節、甩袖軌跡或實驗入口。
* 將 M8.2 記為依使用者決定跳過；此範圍決定不代表水袖動作已完成或驗收。

### 測試

* Unity 6000.6.2f1 EditMode 全量回歸：`total=152 / passed=152 / failed=0 / skipped=0`；結果 `Logs/M8-2-no-water-sleeve-results.xml`。
* Windows x64 建置：`Builds/M8-2-No-Water-Sleeve/YingYun.exe`；`Logs/M8-2-no-water-sleeve-build.log` 記錄 `Build Finished, Result: Success.`。

### 驗收結果

* 使用者明確判定水袖效果不佳，要求直接回檔且後續不要水袖；目前程式、測試及建置已回到無水袖版本。

### Git Commit

* `revert(m8): remove water sleeve and advance to body movement`（本條目與反向變更同一提交）。

### 風險 / 已知問題

* M8.2 是跳過而非完成；完整戲曲動作庫仍缺身段、步法與組合套路。
* `.clinerules/01-project-context.md` 仍停在 M4；本輪未獲修改批准，因此未更動。
* URP、ProjectSettings 與 Unity Connect 的既有未提交變動未納入本次成果。

### 下一步

* 另提 M8.3-A「站相、重心與丁字步」最小 Plan；先驗證足底接地、膝胯一致與重心轉移，再決定是否接入正式舞句。

## [2026-09-23] M8.1 收尾 - 移除劍指並凍結手勢範圍

### 新增

* 新增 M8.1 收尾範圍紀錄：正式保留已實機驗收的按掌、托掌、穿掌、翻腕、拳掌禮與單指六式。

### 修改

* 反向套用 `0e43977` 與 `18f8bb6` 的劍指候選及正式舞句變更，移除 `HandGesture.SwordFinger`、第二指片、`DanceAction.SwordFinger`、劍指播放狀態、HUD 循環與專用測試。
* 正式循環由十五式回到已驗收的十四式；拳掌禮→單指→單山膀的既有收勢與承接保持不變。
* 依使用者決定跳過劍指、點雲手與提甲，M8.1 以六式已驗收手勢收尾；三個跳過項不記為已完成。

### 測試

* Unity 6000.6.2f1 EditMode 全量回歸：`total=152 / passed=152 / failed=0 / skipped=0`；結果 `Logs/M8-1-closure-results.xml`。
* Windows x64 建置：`Builds/M8-1-Closure/YingYun.exe`；`Logs/M8-1-closure-build.log` 記錄 `Build Finished, Result: Success.`。

### 驗收結果

* 使用者明確要求刪除劍指、回到沒有劍指的版本，並跳過劍指、點雲手與提甲後結束 M8.1；目前程式與文件已依此範圍收尾。

### Git Commit

* `revert(m8): remove sword finger and close gesture scope`（本條目與反向變更同一提交）。

### 風險 / 已知問題

* M8.1 的「收尾」以使用者核准範圍為準；被跳過的三式仍未實作，不構成完整戲曲手勢庫。
* `.clinerules/01-project-context.md` 的「目前 Milestone」仍停在 M4；依既有規則需另行批准，本次未修改。
* URP、ProjectSettings 與 Unity Connect 的既有未提交變動未納入本次成果。

### 下一步

* 另提 M8.2 水袖能力的最小 Plan，先以少數代表動作驗證袖端軌跡、延遲、收袖與遮擋，再決定是否擴充完整清單。

## [2026-09-23] M8.1 正式舞句 - 單指接入整曲

### 新增

* `DanceAction.SingleFinger` 加入第十四式，HUD 正式名稱為「單指」；成功錨點才播放，Miss 不憑空伸指。
* `HandGesturePhrase` 預編指扇寬度、長度與獨立指片伸出量；正式版本最後兩拍同步收回手臂、腕、指扇及指片。
* `DancePlayback` 保存上述指型狀態，使 Miss 能凍結當前剪影，下一次成功舞句再沿既有恢復曲線承接。
* `ShadowPuppetPresenter` 將正式播放狀態映射到指扇比例與獨立指片，不再只有實驗預覽可見。

### 修正與驗證

* 正式循環由十三式擴為十四式；拳掌禮先完整收勢，再起單指，單指末段回中後接下一輪單山膀。
* 新增兩個舞句邊界的肩、肘、腕、指扇比例及伸指量連續性測試，避免起拍瞬移。
* Unity 6000.6.2f1 EditMode：151/151 通過，0 failed；結果 `Logs/M8-1-single-finger-formal-results.xml`。
* Windows 建置成功：`Builds/M8-1-Single-Finger-Formal/YingYun.exe`；日誌 `Logs/M8-1-single-finger-formal-build.log`，`Build Finished, Result: Success.`。

### 驗收結果

* 單指完整比例、近距離剪影及正式歌曲均獲使用者確認；名稱、拳掌禮→單指→單山膀承接、末段收指及 Miss 後恢復通過實機驗收。

### Git Commit

* `b160ccc feat(m8): integrate single finger phrase`。
* `docs(m8): accept formal single finger phrase`（實機驗收同步提交）。

### 下一步

* 下一式先核實劍指的指型構成與現有分片能力；點雲手與提甲繼續等待可核對的動作分解。

## [2026-09-23] M8.1 手勢候選 - 單指剪影能力預覽

### 新增

* `HandGesture.SingleFinger` 提供 129 點預編實驗取樣；只用一支左手簽，雙足維持接地。
* 左腕增加隨腕剛性運動的細長單指片，既有指扇在末姿縮短收攏，使完整角色比例仍可區分伸指與掌部。
* `ShadowPuppetPresenter.PreviewHandGesture` 只在單指實驗末段顯示新指片；正式十三式、判定與 HUD 未接線。
* 產出 `Logs/M8-1-single-finger.png` 與 `Logs/M8-1-single-finger-closeup.png` 供剪影驗收。

### 研究邊界

* [上海戲曲學校戲曲韻律操](https://sh-xiquschool.sta.edu.cn/wmzx/77/cf/c4106a96207/page.htm)明列單指（蘭花指）及劍指；[中國戲曲學院手眼身法步](https://bo.nacta.edu.cn/py/yf/byf/index.htm)指出不同行當的指法規格不同。
* 本候選因此只稱「單指」，展示手位與數值是剪影能力實驗，不作為特定行當蘭花指的教材角度。

### 測試

* Unity 6000.6.2f1 EditMode：149/149 通過，0 failed；結果 `Logs/M8-1-single-finger-results.xml`。
* 新測試確認單指片啟用、指扇收攏、雙足不移位，並輸出完整與近距離圖片。

### Git Commit

* `feat(m8): preview single finger silhouette`（本候選提交）。

### 下一步

* 由使用者先確認單指在完整角色及近距離圖中的辨識度；通過後另提正式舞句的起勢、收勢與前後承接。

## [2026-09-23] M8.1 正式舞句 - 拳掌禮接入整曲

### 新增

* `DanceAction.FistPalmSalute` 置於穿掌／翻腕之後，正式循環由十二式擴為十三式；HUD 使用中性名稱「拳掌禮」。
* `DancePlayback` 增加右腕、右指扇與拳形收攏狀態，雙手簽舞句仍計為兩個主動控制點。

### 修改

* 拳掌禮成功錨點起勢後，以左右兩支手簽在胸前完成左掌包右拳；最後兩拍將雙臂、雙腕、指扇與右拳比例收回中立位。
* Presenter 在正式播放時套用右手拳形；離開雙手舞句後平滑恢復原掌片比例。Miss 沿用既有規則，凍結當前雙手姿態。

### 測試

* Unity EditMode 全量 **148/148 通過**，報告 `Logs/M8-1-fist-palm-formal-results.xml`；涵蓋十三式錨點、兩次轉身、全部舞句邊界、雙手簽、右拳比例、Miss 凍結、末兩拍收勢及正式 Presenter 路徑。
* Windows Standalone `Builds/M8-1-Fist-Palm-Formal/YingYun.exe` 建置成功，`Logs/M8-1-fist-palm-formal-build.log` 記錄 `Build Finished, Result: Success`。

### 驗收結果

* 拳掌禮能力剪影及正式歌曲均獲使用者確認；名稱、翻腕→拳掌禮→單山膀承接及 Miss 後恢復通過實機驗收。

### Git Commit

* `9f3f78f feat(m8): integrate fist palm salute`。
* `docs(m8): accept formal fist palm salute`（實機驗收同步提交）。

### 風險 / 已知問題

* 「拳掌禮」是來源名稱尚有歧義時的中性顯示；角色化為抱拳或拱手前仍需對應戲曲行當與演出參考。
* 右拳仍由既有指扇縮放形成，兩支操縱簽會在胸前交叉；本輪不新增拳片素材或更改操縱簽顯示規則。

### 下一步

* 下一式先核對點雲手、提甲與指掌的來源及現有剪影能力；角色化抱拳／拱手與獨立拳片保留後續決策。

## [2026-09-23] M8.1 手勢候選 - 雙手拳掌禮能力預覽

### 新增

* `FistPalmSalutePhrase` 以 129 點預編左右肩、肘、腕與指扇軌跡，使用兩支手簽在胸前完成左掌包右拳。
* `ShadowPuppetPresenter.PreviewFistPalmSalute` 收縮右指扇形成較緊湊拳形，並輸出 `Logs/M8-1-fist-palm-salute.png`。

### 修改

* Presenter 新增右腕位置與右指扇縮放的唯讀檢查值；開始播放及其他單手預覽會恢復雙方指扇比例，避免實驗拳形殘留。
* 依[上海戲曲學校戲曲韻律操](https://sh-xiquschool.sta.edu.cn/wmzx/77/cf/c4106a96207/page.htm)的拱手禮及[國家體育總局抱拳禮資料](https://www.sport.gov.cn/wszx/n14665/c978050/part/628084.pdf)記錄命名差異，正式定名前只稱「拳掌禮」。

### 測試

* Unity EditMode 全量 **146/146 通過**，報告 `Logs/M8-1-fist-palm-final-results.xml`；驗證兩支手簽、胸前會合、右拳剪影收攏、雙足固定及十二式既有回歸。
* Windows Standalone `Builds/M8-1-Fist-Palm/YingYun.exe` 建置成功，`Logs/M8-1-fist-palm-build.log` 記錄 `Build Finished, Result: Success`。
* 第一版雙手落在腹前，未達到資料的胸前位置；調整肘部收合角度後單項視覺測試與最終全量回歸均通過。

### 驗收結果

* 使用者已確認胸前雙手會合與左右拳掌差異；能力預覽通過。抱拳／拱手正式命名仍待角色化來源核實，正式歌曲接線另列新候選。

### Git Commit

* `3ae642a feat(m8): preview two-hand fist palm salute`。

### 風險 / 已知問題

* 右拳以縮放現有橢圓指扇表現，仍不是獨立拳形素材；兩支側向操縱簽在胸前交叉，可能降低手形可讀性。
* 不同資料對抱拳／拱手名稱與手型關係並不一致；正式名稱必須結合後續採用的戲曲行當與演出參考，不由本灰盒自行定案。

### 下一步

* 能力預覽通過後已另做正式舞句候選；整曲接線驗收與後續安排見上方最新條目。

## [2026-09-23] M8.1 正式舞句 - 穿掌與翻腕接入整曲

### 新增

* `DanceAction.ThreadPalm`、`DanceAction.TurnWrist` 正式舞句置於按掌／托掌之後，原十式循環擴為十二式；HUD 顯示「穿掌」「翻腕」。
* 正式播放與 Presenter 回歸覆蓋穿掌→翻腕首幀連續、腕面翻換及下一輪收勢。

### 修改

* 翻腕正式取樣從穿掌終姿直接開始，肩肘在翻腕階段保持手位；最後兩拍將肩、肘、腕與指扇收回中立位，再接下一輪單山膀。
* 穿掌／翻腕沿用成功錨點起舞、Miss 凍結與預編譜面時間取樣；判定、DSP、音符資料、場景及設定未修改。

### 測試

* Unity EditMode 全量 **144/144 通過**，報告 `Logs/M8-1-thread-wrist-formal-results.xml`；涵蓋十二式錨點、所有舞句邊界、正反晚擊轉身、穿掌→翻腕手位連續、末拍回中及正式 Presenter 路徑。
* Windows Standalone `Builds/M8-1-Thread-Wrist-Formal/YingYun.exe` 建置成功，`Logs/M8-1-thread-wrist-formal-build.log` 記錄 `Build Finished, Result: Success`。

### 驗收結果

* 穿掌／翻腕能力預覽及正式歌曲均獲使用者確認；名稱、穿掌→翻腕→單山膀承接及 Miss 後恢復通過實機驗收。

### Git Commit

* `08844d5 feat(m8): integrate thread palm and wrist turn`。
* `docs(m8): accept formal thread and wrist sequence`（實機驗收同步提交）。

### 風險 / 已知問題

* 十二式仍是固定程式循環；本候選不包含雙穿掌、眼隨手動或完整翻托掌，也沒有改善橢圓掌片的精細指型限制。

### 下一步

* 下一批先核對抱拳／拱手的雙手語義與現有掌片能力，再決定是否需要補拳形分片；不直接接入正式循環。

## [2026-09-23] M8.1 手勢候選 - 穿掌與翻腕能力預覽

### 新增

* `HandGesture.ThreadPalm` 以身前聚手後向外穿出的 129 點軌跡驗證單手穿掌路徑；`HandGesture.TurnWrist` 在肩肘定點後翻換腕面。
* 四張剪影對照圖：`Logs/M8-1-thread-palm-gather.png`、`Logs/M8-1-thread-palm.png`、`Logs/M8-1-turn-wrist-before.png`、`Logs/M8-1-turn-wrist-after.png`。

### 修改

* `HandGestureChoreography.Get` 改為明確分派四種手勢；`GetFormal` 只接受已驗收的按掌／托掌，穿掌／翻腕在通過剪影與正式接線驗收前會拋出明確錯誤。
* 依[上海戲曲學校戲曲韻律操](https://sh-xiquschool.sta.edu.cn/wmzx/77/cf/c4106a96207/page.htm)記錄穿掌、雙穿掌接翻托掌及眼隨手動的教學語義；本候選僅做單手簽與腕面能力轉譯。

### 測試

* Unity EditMode 全量 **142/142 通過**，報告 `Logs/M8-1-thread-wrist-final-results.xml`；驗證兩式各用一支手簽、穿掌先聚後伸、翻腕前後手位不移及腕面差異超過 150 度，雙足保持原位。
* Windows Standalone `Builds/M8-1-Thread-Wrist/YingYun.exe` 建置成功，`Logs/M8-1-thread-wrist-build.log` 記錄 `Build Finished, Result: Success`。

### 驗收結果

* 四張剪影圖已獲使用者確認「聚手→穿出」方向感及定點翻腕清楚；本條能力預覽通過，正式歌曲接線另列新候選。

### Git Commit

* `98c00bd feat(m8): preview thread palm and wrist turn`。

### 風險 / 已知問題

* 現有灰盒沒有眼神控制，也只演示單手；不能把本候選稱為教材中的雙穿掌、翻托掌完整組合。
* 穿掌終姿單看近似平伸臂，必須連同聚手中段或動態路徑驗收；翻腕的橢圓掌片只能表現腕面方向，不能呈現精細指型。

### 下一步

* 能力預覽通過後已另做正式舞句候選；整曲接線驗收與後續安排見上方最新條目。

## [2026-09-23] M8-R 接續修正 - 晚擊轉身保持朝向

### 新增

* 新增晚擊轉身回歸用例，分別覆蓋正向與反向兩次轉身，模擬錨點晚擊 80 ms 後在譜面邊界接續下一式。

### 修改

* `DancePlayback` 的舞句取樣起點固定使用預編譜面起拍，不再以實際命中時刻將整段向後平移；下一式準時啟動時，轉身已完成並提交新朝向。
* 判定窗口、DSP 時鐘、音符時間及舞句曲線均未修改；晚擊仍依既有判定結果決定是否起舞。

### 測試

* Unity EditMode 全量 **140/140 通過**，報告 `Logs/M8-turn-late-hit-results.xml`；既有所有準點舞句邊界連續測試保持通過，新增測試驗證兩個方向的晚擊轉身接續不翻回。
* Windows Standalone `Builds/M8-Turn-Boundary/YingYun.exe` 建置成功，`Logs/M8-turn-boundary-build.log` 記錄 `Build Finished, Result: Success`。

### 驗收結果

* 已定位使用者回報的瞬移路徑並完成技術回歸；使用者在 Windows 候選包完成實機複驗並確認修正通過。

### Git Commit

* `cfff11e fix(m8): align late turn at phrase boundary`。
* `docs(m8): record turn boundary acceptance`（實機驗收同步提交）。

### 風險 / 已知問題

* 晚擊後會直接從該舞句已經過的譜面時間取樣；80 ms 相對 8 拍舞句的位移很小，現已通過實機觀察。
* 本修正處理播放時間對齊，不改動轉身造型或下一式的編舞曲線；其他新動作仍需各自驗收。

### 下一步

* 回到 M8.1 正式舞句候選，確認整曲中的按掌→托掌→下一輪承接與 Miss 後恢復；通過後再選下一批少量手勢。

## [2026-09-23] M8.1 正式舞句 - 按掌與托掌接入整曲

### 新增

* `DanceAction.PressPalm`、`DanceAction.SupportPalm` 正式舞句，置於原八式後形成十式循環；HUD 直接顯示「按掌」「托掌」。
* 舞句播放層保存已預編的腕與指扇角度，Presenter 在手勢舞句期間直接依歌曲時間取樣，不以 frame 累加。

### 修改

* 托掌由按掌末姿直接展開，避免相鄰成功舞句首幀歸零；最後兩拍收勢回中，以銜接下一輪單山膀。
* 按掌／托掌維持一支左手簽主動，腕與指扇為同杆被動分片；判定核心、音符譜與 DSP 時鐘未改。

### 測試

* Unity EditMode 全量 **139/139 通過**，報告 `Logs/M8-1-formal-results.xml`；涵蓋正式錨點、HUD 動作資料、單杆限制、Miss 凍結手位、所有舞句邊界連續及正式 Presenter 路徑。
* Windows Standalone `Builds/M8-1-Formal/YingYun.exe` 建置成功，日誌 `Logs/M8-1-formal-build.log` 記錄 `Build Finished, Result: Success`。
* 首次回歸為 138/139，定位按掌→托掌首幀歸零；修正後第二次仍為 138/139，定位托掌→單山膀末姿未收回；加入直連起點與末兩拍收勢後 139/139。

### 驗收結果

* 按掌／托掌預覽剪影及正式歌曲均獲使用者確認；按掌→托掌→下一輪三段承接、左側名稱及 Miss 後恢復通過實機驗收。

### Git Commit

* `e0e57be feat(m8): integrate palm gestures into dance cycle`。
* `docs(m8): accept formal palm gesture sequence`（實機驗收同步提交）。

### 風險 / 已知問題

* 十式仍是固定程式循環，不是可編舞資料；其他 M8.1 手勢與精細指型尚未完成。現有橢圓掌片的細節能力限制不變。

### 下一步

* 下一批先核對穿掌／翻腕的教學語義與現有腕掌分片能力，再做小量剪影候選；不直接擴充整份手勢清單。

## [2026-09-23] M8.1 手勢候選 - 按掌與托掌實驗預覽

### 新增

* `HandGestureChoreography` 預編按掌、托掌的肩、肘、腕及指扇軌跡；每式 129 點取樣，同一支手簽驅動，腕與掌形不另算主動控制點。
* `ShadowPuppetPresenter.PreviewHandGesture` 實驗入口及 `Logs/M8-1-press-palm.png`、`Logs/M8-1-support-palm.png` 剪影對照圖；不接入正式譜面或判定。

### 修改

* `DEVELOPMENT.md` 記錄現有腕／掌能力、按掌與托掌的資料來源、灰盒轉譯限制及「實驗候選」狀態。

### 測試

* Unity EditMode 全量 **136/136 通過**，報告 `Logs/M8-1-palm-results.xml`；新增用例驗證兩式預編、只使用一支手簽、腕面方向差異超過 120 度、托掌高於按掌且雙足不移位。
* Windows Standalone `Builds/M8-1-Palm/YingYun.exe` 建置成功，日誌記錄 `Build Finished, Result: Success`。
* 兩張 1280×720 對照圖均成功輸出；按掌與托掌的手位及腕面方向可區分。第一次以 `-nographics` 跑既有 Camera.Render 測試導致 Unity 崩潰，改用有圖形 batchmode 後全量通過。

### 驗收結果

* 技術候選通過；仍待使用者判斷兩式灰盒剪影是否足以辨認。通過前保持「實驗」，不加入正式歌曲。

### Git Commit

* `feat(m8): add experimental palm gesture previews`（本候選提交）。

### 風險 / 已知問題

* 現有橢圓掌片只能呈現掌位與腕面方向，無法表示蘭花指、拳等精細指型；按掌與托掌角度是依教學語義製作的數位灰盒，並非傳統教材量測值。

### 下一步

* 請使用者比較按掌／托掌剪影；若可辨，另提接入正式舞句的最小 Plan。若不可辨，先設計手掌／手指剪影分片，不繼續擴充其他手勢。

## [2026-09-23] M8 結案 - 八式整段舞蹈實機通過

### 新增

* 無新增程式或資產；本工作單位記錄 M8 首版的最終實機驗收結果。

### 修改

* `DEVELOPMENT.md` 將 M8 五項完成條件勾選結案，並把八式相鄰過門與命中／連續命中／漏擊後恢復觀察表標為通過。
* 目前 Milestone 推進至 M8.1 手勢與上身；M8.2 水袖、M8.3 身段與步法、M8.4 組合套路及 M9 自編舞仍保持未完成。

### 測試

* 沿用 M8-H 候選的 Unity EditMode 全量 **133/133 通過**及 `Builds/M8-H/YingYun.exe` 建置成功記錄。
* 使用者於 Windows 實機完整遊玩，包含連續命中、漏擊後恢復與八式相鄰承接，回覆整段通過且未提出需修正的異常。
* 文件檢查執行 `git diff --check`，並確認提交僅包含 `DEVELOPMENT.md`、`CHANGELOG.md`。

### 驗收結果

* M8 預編皮影舞段首版結案：八式剪影、轉身、收勢、相鄰直連及 Miss 後恢復的整段舞感均獲使用者實機確認。

### Git Commit

* `docs(m8): close first dance milestone after playtest`（本結案提交）。

### 風險 / 已知問題

* 本次通過的是既有八式灰盒首版，不代表 M8.1–M8.4 完整戲曲動作庫已完成。`.clinerules/01-project-context.md` 的 Milestone 仍是 M4，須另獲明確批准才能修改。

### 下一步

* 另提 M8.1 手勢與上身 Plan，先核對被動腕／手形能力與動作參考，再分批實作可由剪影辨認的手勢。

## [2026-09-23] M8 驗收記錄 - 解說可見性與整段觀察表

### 新增

* `DEVELOPMENT.md` §4.1 加入八式相鄰過門及命中／連續命中／漏擊後恢復的整段實機觀察表；各項舞感仍待觀察。

### 修改

* 同步使用者本次確認：M8-H 左側動作名稱已於實機顯示。M8-S 舞句銜接、八式整段舞感與 M8 首版仍未結案。

### 測試

* 文件檢查：核對 `DEVELOPMENT.md`、`CHANGELOG.md` 的驗收措辭，執行 `git diff --check` 並確認僅暫存本工作單位的文件。未更改程式，未重跑 EditMode 或 Windows 建置。

### 驗收結果

* M8-H 動作名稱可見性依使用者本次實機回覆判為通過；其他版面細節及整段舞蹈觀感未據此判為通過。

### Git Commit

* `docs(m8): record hud visibility and full-dance review checklist`（本記錄提交）。

### 風險 / 已知問題

* `.clinerules/01-project-context.md` 的「目前 Milestone」仍停在 M4，改 Rule 須另獲明確批准。既有 URP、ProjectSettings 與 Unity Connect 未提交變動未納入本工作單位。

### 下一步

* 使用 M8-H Windows 版觀察整曲，逐項填寫拍數、輸入路徑及畫面證據；只有觀察到具體問題後才提出最小舞句修正。

## [2026-09-22] M8-H 演出解說候選 - 左側顯示實際舞句

### 新增

* 左側安全區內的「影戲身段」面板，顯示實際舞句名稱、起始拍、持續拍數及主動關節；待拍、演出、收勢、保持與漏擊各有明確文案。
* 純 C# 舞句狀態事件與 EditMode 回歸測試。漏擊舞句標「未演」，不當作目前皮影正在做的動作。
* `DEVELOPMENT.md` §4.2 自訂音樂／編舞分期路線：先完成動作庫與舞句資料契約，再做 Unity Editor 作者工具；全新關節動作與遊戲內任意音檔匯入另議。

### 修改

* 由舞句播放層發出狀態，經皮影呈現層轉送到 HUD；不讓 UI 依拍數猜測演出，也不改判定或原有動作曲線。
* HUD 沿用既有 uGUI Canvas、安全區與中文字型；EditMode 直接呼叫 `Begin` 時也能安全建立 HUD。M8.1–M8.4 未完成項目保持原計畫，不因新增名稱顯示就視為動作完成。

### 測試

* Unity EditMode 全量 **133/133 通過**，報告 `Logs/M8-H-hud-results.xml`；包括漏擊未演、早擊待拍、收勢、返回等待及 HUD 文案。第一次測試因 EditMode 未呼叫 HUD 的 `Awake` 而 132/133，改由 `Begin` 安全初始化後全綠。
* Windows Standalone `Builds/M8-H/YingYun.exe` 建置成功，`Build Finished, Result: Success`；Unity 授權服務訊息未阻斷建置。
* 左上 Combo 與新面板在參考解析度下保留間隔；不同視窗比例及實際游玩時的視覺可讀性仍待實機驗收。

### 驗收結果

* 技術候選通過；使用者於 2026-09-23 確認左側動作名稱已顯示（見上方驗收記錄）。其他版面細節及 M8 舞蹈觀感未因這項確認而結案。

### Git Commit

* `feat(m8): show live shadow dance cue in gameplay hud`（本候選提交）。

### 風險 / 已知問題

* 目前僅顯示已實作八招的灰盒舞句；M8.1 手勢、M8.2 水袖、M8.3 步法、M8.4 組合套路尚未完成。自訂歌曲編輯器也尚未建立；本輪沒有新增曲目或匯入格式。

### 下一步

* 請使用者在 Windows build 實機確認左側面板與中央音符的距離、字級及 Miss／收勢文案。通過後，依 `DEVELOPMENT.md` §4.1–4.2 另提動作庫與自編舞資料契約的 Plan。

## [2026-09-22] M8-S 舞句銜接候選 - 承接與收勢分明

### 新增

* 相鄰成功舞句的預編直連曲線，以及漏擊／跳句後的預編恢復曲線；不在運行時生成動作。
* 收勢、承接、漏擊恢復和逐句交界姿態連續性的 EditMode 回歸測試。

### 修改

* 雲手在轉入順風旗前收肘；雙山膀定勢後放下左臂，右臂承接反雲手；反雲手收右臂後亮相；亮相定住再收身。保留順風旗經轉身到揚袖的高臂連續性。並將同向中間關鍵姿態改為連續切線，避免每兩拍機械停住。
* `DEVELOPMENT.md` 記錄使用者已確認 M8-R 轉身，並將 M8-S 明列為**技術候選、待實機舞蹈觀感驗收**；M8.1–M8.4 動作庫與經影像核實的過門仍保留。

### 測試

* Unity EditMode 全量 **130/130 通過**，報告 `Logs/M8-S-transition-results.xml`。
* Windows Standalone `Builds/M8-S/YingYun.exe` 建置成功，`Build Finished, Result: Success`；授權服務訊息未阻斷建置。
* `git diff --check` 無內容錯誤；未將原有 URP／ProjectSettings 工作區改動納入本輪範圍。

### 驗收結果

* 自動化與建置通過；**整段連起來是否像有呼吸的皮影舞蹈，尚待使用者實機驗收**。不把程式連續性等同戲曲動作完成。

### Git Commit

* `feat(m8): blend consecutive shadow dance phrases`（本候選提交）。

### 風險 / 已知問題

* 現階段的收勢拍點及關節角度屬灰盒編舞設計，非資料來源給出的傳統定式；若觀感欠佳，需先比對教學／演出影像再調整。完整腕指、水袖、足法和套路仍未實作。

### 下一步

* 請使用者實機看相鄰舞句，特別檢查「該放下時有放下、該承接時不硬歸零」。若通過，再另提 Plan 處理經參考核實的步法與更長的組合套路。

## [2026-09-22] M8-R 轉身修正候選 - 下身分片隨影人改向

### 新增

* 依皮影操演資料新增裙片偏側飾帶，讓下身翻面能從剪影辨認；`DEVELOPMENT.md` §4.1 明記轉身仍待實機驗收與完整動作庫待辦。
* 轉身雙足接地、髖位與裙片同向、Miss 不翻面、反向轉回的 EditMode 回歸用例，以及前／中／後截圖 `Logs/M8-turn-*.png`。

### 修改

* `轉身` 舞句改由身簽與一側手簽主動；胸腰、裙片、髖位與足片同步改向，兩腳不套用其他舞句的抬腳弧線。骨盆根節點不縮放，足點由兩節腿約束保持於舞台地平線。
* 無譜面、判定、時鐘、Input Actions、Scene、Prefab、URP 或 ProjectSettings 的計畫內修改；工作區原有的設定檔改動未納入本次提交。

### 測試

* Unity EditMode 全量 **127/127 通過**，報告 `Logs/M8-R-full-turn-results.xml`；含 61 個時點雙足接地檢查及第二次轉身還原朝向。
* Windows Standalone `Builds/M8-R-full-turn/YingYun.exe` 建置成功、退出碼 0；建置工具報告 Unity 授權服務警告，但未阻斷編譯或出包。
* 比對 `Logs/M8-turn-before.png`、`M8-turn-mid.png`、`M8-turn-after.png`：前後幀頭與下身飾帶方向相反，中幀呈狹窄側影；**使用者實機觀感尚未驗收**。

### 驗收結果

* 技術候選通過；修正了原先只有上身翻面的接線缺口。M8 轉身的皮影觀感與整套舞蹈仍未結案。

### Git Commit

* `feat(m8): choreograph grounded full-body shadow turn`（本候選提交）。

### 風險 / 已知問題

* [皮影操演教材](https://www.minjianyishu.net/index.php?act=app&appid=244&leibie=460&page=24)要求整體回轉及雙足貼地，[黑龍江省文旅廳訪談](https://wlt.hlj.gov.cn/wlt/c115580/202606/c00_31949483.shtml)指出懸腳／飄起會破壞轉身效果；本作仍為程式生成幾何灰盒。傳統三簽同步在本遊戲的兩主動控制點限制下只作身簽＋一手簽的數位改編，不宣稱逐式還原。使用者可能仍覺得窄影過細或腿部交疊不自然。

### 下一步

* 請使用者實機確認正式歌曲的轉身是否能看出下半身一起改向、雙腳是否穩定、下一舞句是否連貫。若不合格，先按具體觀察修正本候選；若通過，再另提有來源的水袖、連續步法與組合套路 Plan，未完成動作不得刪除或默認完成。

---

## [2026-09-22] M8-R 候選補正 - 正式游玩下半身動作

### 新增

* 載入譜面時為單側手簽舞句預取樣足點外探、抬落與骨盆移重心軌跡；執行時只按歌曲時間取樣。右手舞句配左腳、左手舞句配右腳，雙手或身頭舞句不額外啟動腳簽；每句最多兩支主動操縱杆。
* 正式播放路徑測試與截圖 `Logs/M8-R-song-step-mid.png`，直接走 `Begin(DancePhrase[])`／`OnJudged`／`Tick`，不再只驗獨立 `PreviewStructure`。

### 修改

* `ShadowPuppetPresenter.ApplyDancePose` 不再每幀給雙足固定座標；改讀預編的左右足點與骨盆位移，現有兩節腿約束器帶動膝踝。腳簽張力由抬腳高度顯示。
* 判定、譜面難度、輸入、歌曲時鐘、Scene、Prefab、URP 與 ProjectSettings 未改；`DEVELOPMENT.md` 留存真正戲曲步法與套路待辦。

### 測試

* Unity EditMode 全量 **125/125 通過**，報告 `Logs/M8-R-gameplay-step-results.xml`；涵蓋成功錨點、Miss、支撐腳不滑、抬腳高度、膝彎、回落、逐幀位移及兩杆上限。
* Windows Standalone `Builds/M8-R-step/YingYun.exe` 建置成功，退出碼 0；Unity 授權服務警告未阻斷建置。實機操作觀感仍待使用者確認。

### 驗收結果

* 修正了「只在結構預覽抬腳、正式游玩下半身不動」的接線問題；技術驗證通過，**M8 舞蹈感仍未驗收**。

### Git Commit

* `feat(m8): drive grounded steps during song playback`（本候選補正提交）。

### 風險 / 已知問題

* 目前為結構性抬落腳弧線，並非經演出影像核對的正式戲曲步法；舊舞句的歸位停頓、套路辨識度與縮放翻面依然待解。只有成功的指定錨點舞句觸發步子，Miss 不會憑空踏步。

### 下一步

* 請使用者實機確認正式歌曲第一段命中後可見抬腳、左腳踩地與膝彎；通過後再按 `DEVELOPMENT.md` §4.1 提出經參考核對的連續步法、轉身、水袖與套路 Plan，不自動視作 M8 完成。

---

## [2026-09-22] M8-R 候選版 - 北方影人三簽分片結構

### 新增

* 胸部主簽接點、雙手主簽、胸腰獨立鉚接片、分片鉚釘腕與手形、袖口及袖尾、踝與足片；三支主簽與遊戲六方向的頭／足輔助杆以粗細及透明度區分。
* 兩節腿足點約束器及獨立結構預覽：支撐腳固定、另一腳抬落、膝隨足點折彎。這是灰盒結構檢查，不命名為戲曲套路。

### 修改

* 舊 M8 播放鏈路加入被動腕、手形、袖尾及雙足約束，限制過大腰片偏轉；判定、譜面、歌曲時間、輸入與計分未改。
* `DEVELOPMENT.md` 記明使用者否決舊舞蹈感，以及傳統十一件／三簽與數位增加關節的區別，保留全部未完成動作。

### 測試

* Unity EditMode 全量 **121/121 通過**（含腰胸翻面時雙足定點），報告 `Logs/M8-R-editmode-results.xml`。
* 結構畫面 `Logs/M8-R-northern-rig-step.png` 已檢查；Windows Standalone `Builds/M8-R/YingYun.exe` 建置成功（退出碼 0）。

### 驗收結果

* 僅為技術候選：足底約束、膝彎、腕與分段袖的結構可見；尚待使用者實機驗收。不得將原 M8 舞句標為已驗收。

### Git Commit

* `f151aa6 feat(m8): add northern shadow puppet articulated rig`；胸腰補正提交 `fix(m8): separate waist from planted legs`。

### 風險 / 已知問題

* 手形／水袖仍為幾何灰盒；舊舞句的歸位停頓、套路辨識度和縮放翻面未解決。傳統資料的腿足一體；本遊戲膝踝屬數位改編。Unity 授權服務仍輸出非阻斷性警告。

### 下一步

* 先請使用者驗收結構與單步支撐；若通過，另提連續步法、可支撐的轉身、無歸位停頓串場與逐式核對的手／袖／身段套路 Plan；`DEVELOPMENT.md` §4.1 全部待辦保留。

---

## [2026-09-22] M8 候選版 - 預編皮影舞句與轉身首版

### 新增

* 載入譜面時一次生成全曲 45 段、每段 8 拍的取樣舞句；首批含單山膀、雲手、順風旗、轉身、揚袖、雙山膀、反雲手、亮相，並以 `【拍數，動作名稱，激活關節，持續拍數】` 記錄。
* `DancePlayback` 只在舞句錨點音符判定成功後播放預算軌跡；Miss 凍結姿態，下一個成功錨點再銜接。轉身收窄剪影並翻轉側臉朝向。
* M8 EditMode 舞句確定性、早擊起拍、Miss、轉身連續性、Presenter Hold 牽引及轉身截圖測試。

### 修改

* 遊戲中原始按鍵不再逐次使皮影抽動；判定、得分及譜面保留原有流程，舞蹈層從判定結果獨立讀取。
* Hold 在舞句模式仍維持竹桿張力；手部 Hold 加上持續袖片拉伸，結束後緩動回復，不額外啟動舞句關節。
* `DEVELOPMENT.md` 新增 M8 條件及附件全部未實作動作的 M8.1–M8.4 清單，不把首版等同完整動作庫。

### 測試

* Unity EditMode 全量 **115/115 通過**，報告 `Logs/m8-dance-results.xml`。
* 轉身朝向截圖：`Logs/M8-turn-before.png`、`Logs/M8-turn-after.png`；確認側臉由右轉左。
* Windows Standalone：`Builds/M8/YingYun.exe` 建置成功；無圖形 Player 隱藏啟動煙測載入 Unity 6000.6.2f1、Input System 與腳本程序集，日誌 `Logs/m8-player-smoke.log`。選曲與實際鍵盤舞蹈仍待人手測試。

### 驗收結果

* 程式層符合「載入時預編、至少 4 拍、每段最多 2 主動關節、命中觸發、Miss 保持」；幾何灰盒的舞蹈觀感及動作辨識度仍待使用者實機確認，因此標為候選版，不標 M8 結案。

### Git Commit

* `feat(m8): add precomputed shadow dance phrases`（本候選版提交）

### 風險 / 已知問題

* 首版僅八個示範舞句，肩／肘／頭／軀幹及輪廓翻面仍是幾何轉譯，不代表戲曲動作的精確還原；腕、袖尾、腳踝、移位及附件其餘項目尚未實作。
* 非錨點 Tap／Chord 仍參與判定與得分，但不各自開新舞句；Hold 保留持續操桿回饋，手部額外拉伸袖片，其餘部位尚只有竹桿張力。後續按實機感受調整錨點密度與全身牽引。
* Unity Licensing 404 類訊息為既有環境警告；以最終 build 結果為準。

### 下一步

* 請使用者實機看整曲節奏、八式辨識度及轉身效果；若接受首版，再按 `DEVELOPMENT.md` §4.1 分期提出 M8.1–M8.4 計畫，不默默略過附件動作。

---

## [2026-09-21] M7.3 - 内置中文字体并完成可玩 Demo 收尾

### 新增

* 随 Windows 包发布的 Noto Sans SC 可变 TrueType 字体与 SIL Open Font License 1.1 文本。
* `ChineseFontProvider` 统一提供包内中文字型，并仅在资源缺失时回退至系统字体。
* 字形覆盖回归测试，逐字验证当前选曲、HUD、判定、暂停与结算中文文案。

### 修改

* 选曲／校准、游戏 HUD／结算与六轨标签／判定文字不再依赖玩家电脑安装微软雅黑等系统字体。
* 包内 `Resources` 字体由 Unity 管理生命周期；只有临时创建的系统回退字体会在 Presenter 销毁时释放。
* `DEVELOPMENT.md` 将 M7 标记为结案，并同步 CJK 字体待批准项状态。

### 測試

* Unity EditMode 全量测试：**104/104 通过**，0 failed／0 skipped；报告为 `Logs/m7-cjk-font-results.xml`。
* 字形覆盖测试确认当前原型所需简体中文字符均可由包内字体直接显示。
* Windows Standalone 构建：`Builds/M7/YingYun.exe`，**Build Successful**。
* `git diff --check` 与提交前状态检查：仅纳入本次字体、代码、测试与文档；保留既有未提交的 Unity 设置噪音。

### 驗收結果

* 中文 UI 已不依赖目标机器的系统字体，Windows 构建包含字体资源并通过完整编译。
* 使用者已在上一轮实机确认放宽后的判定与 Hold 行为成功；M7 可玩 Demo 的三难度、完整流程、校准、Windows 出包与中文 UI 条件均已满足。

### Git Commit

* `feat(m7): bundle cjk font for demo ui`（本条目所在的结案提交）

### 風險 / 已知問題

* Noto Sans SC 字体约 17 MB，会相应增加构建体积；这是换取离线、跨机器中文显示一致性的成本。
* 当前界面沿用既有 uGUI `Text`／`TextMesh`，没有为了字体收尾额外迁移到 TMP；后续若整体升级 TMP，应另立计划并生成受控字形集。
* Windows 构建日志仍含既有 Unity Licensing 404 警告，但最终结果为 Success。

### 下一步

* 由使用者决定进入正式皮影美术替换、增加第二首曲目，或回补自动节拍采样校准；本次不自行展开下一阶段。

---

## [2026-09-21] M7 修正 - 放宽判定窗口并修复长按必定失败

### 新增

* Hold 回归测试：覆盖同帧旧 Release、普通 Tap 松开污染、持续按过尾端、尾端后松开与下一颗同轨 Hold。

### 修改

* 原型判定窗由 Perfect／Great／Good 的 ±40／±70／±100 ms 放宽为 ±50／±90／±150 ms。
* Hold 只接受发生在本次按下之后的 Release；每帧清理无法属于有效 Hold 的孤立 Release。
* Hold 持续按到尾端即自动完成，不再要求玩家必须在尾端附近精准松开；明显提前松开仍判为 Miss。

### 测試

* 判定核心独立反射测试：**27/27 通过**。
* Unity EditMode 全量测试：**103/103 通过**，0 failed／0 skipped。
* Windows Standalone 构建：`Builds/M7/YingYun.exe`，**Build Successful**；构建内 `YingYun.Runtime.dll` 时间戳为 2026-09-21 21:55:07。
* `git diff --check`：代码与文件结构无错误；Unity 自动生成的 `.meta` 尾随空格维持既有序列化格式。

### 驗收結果

* 已复现并消除“早先普通按键的 Release 被后续 Hold 误认为提前松开”的根因。
* 自动化与 Windows 构建通过；仍需使用者实机确认中心点后的 150 ms 容错与长条持续按压手感。

### Git Commit

* `fix(m7): relax hit windows and stabilize holds`（本条目所在的修正提交）

### 風險 / 已知問題

* 当前判定窗三种难度共用；若 Hard 实机显得过宽，后续可改为按难度分别配置。
* Windows 构建日志仍包含既有 Unity Licensing 404 与 Pipeline 配置警告，但构建结果为 Success，未阻断产物。

### 下一步

* 使用者实机复测 Hold 与晚按；若仍有体感偏移，再依据判定日志中位数调整输入／音频校准，而不是继续盲目扩大窗口。

---

## [2026-09-21] M7 修正 - 暂停流程、三秒倒数与入门谱面降难

### 新增

* 游戏内暂停菜单，提供「继续演出／重新开始／返回选曲」三个操作；P 与 Esc 均可暂停或继续。
* 由校正后 `SongTime` 驱动的「3、2、1、开演」倒数；音乐仍由 `PlayScheduled` 在 DSP 时间轴上起播。
* Easy 谱面硬性约束测试：无 Chord、每颗音符只要求一个按键、相邻音符起始时间至少间隔 1 秒。

### 修改

* 起播 lead-in 固定为 3 秒，选择难度与重新开始都会完整执行倒数。
* Easy 改为每秒一颗音符，六方向循环出现；仅间隔加入少量 0.75 秒 Hold，取消全部组合键。
* Normal 维持完整 Tap／Hold／Chord 教学组合；Hard 才加入半拍音符与额外双脚合奏。
* 暂停使用既有 `DspSongClock.Pause/Resume`，不使用 `Time.timeScale`；暂停期间停止处理输入与判定。
* 返回选曲时停止音乐、清空输入队列、回收音符并重置皮影姿态。

### 测试

* Unity EditMode 全量测试：**100/100 通过**，0 failed／0 skipped。
* Play Mode：三秒倒数与暂停菜单可见；继续、再次暂停、返回选曲后状态为 `menu=True / pause=False / audio=False`。
* Unity Console：0 compile error、0 console error。
* 视觉验收图：`Logs/M7-countdown.png`、`Logs/M7-pause-menu.png`。
* Connected Editor Windows build：`Builds/M7/YingYun.exe`，**Succeeded**，0 error，134,403,936 bytes，13.587 秒；187 项为既有 Inference／Shader 警告。

### 驗收結果

* 自动化、Play Mode 与 Windows 构建通过；等待使用者实机确认 Easy 的一秒一音符手感、暂停恢复连续性与返回选曲流程。

### Git Commit

* `feat(m7): add pause flow and rebalance difficulty`（本条目所在的修正提交）

### 風險 / 已知問題

* Easy 的 Hold 起点仍遵守一秒间隔，但 Hold 需要额外的释放动作；若实机仍觉得忙，可在下一轮将 Easy 改为纯 Tap。
* 正式 CJK TMP 字体仍未加入，维持 M7 既有待批准项。

### 下一步

* 使用者实机验收新的 Windows 包；通过后再决定是否保留 Easy 的少量 Hold，并进入 M7 结案准备。

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
