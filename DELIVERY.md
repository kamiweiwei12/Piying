# 《皮影声律》独立 Demo 交付

## 支持范围

- Windows 10 / 11 64 位。
- 内置曲目仅为《象王行》和《青玉案》；《试灯》继续保留为工程回归资料，不进入交付版选曲界面。
- 玩家自定义 MP3、FLAC、WAV 的功能继续保留。
- 运行端不依赖 Unity Editor、Python、Git、开发工程或固定盘符。
- `beat_this_cpp` 的四个 Visual C++ x64 动态运行库采用应用本地方式随分析器提供，避免干净电脑缺少 VC++ Redistributable 时无法分析自定义歌曲。
- 原生分析器使用 Windows Unicode 路径入口，支持中文用户目录、解压目录、歌曲名及输出路径。

## 构建

在项目根目录执行：

```powershell
& .\Tools\BuildCompetitionDemo.ps1
```

脚本使用 Unity 6000.6.2f1 生成 Release Windows x64 Player，检查必要运行文件，排除 Unity 明示不可发布的备份目录及开发日志，再生成：

- `Builds/Competition-Demo/YingYunDemo-Windows-x64.zip`
- `Builds/Competition-Demo/YingYunDemo-Windows-x64.zip.sha256`

实际 Build 产物和日志受 `.gitignore` 排除，不提交进 Git。

## 操作键位

- 上排：`T` 左手、`Y` 头部、`U` 右手。
- 下排：`G` 左脚、`H` 身体、`J` 右脚。
- `P` 暂停／继续，`R` 重新开始当前曲目。

六个键按判定圈的上三／下三空间位置排列。名角难度的组合音符只会要求两个键同时按下；不会出现
三键组合，也不会在同一个八拍乐句中混排组合音符和长按音符。

## 同伴问题回报

选曲页的“运行日志”窗口会显示最近记录，并提供：

- 刷新显示；
- 复制完整日志；
- 打开日志文件夹。

`YingYun-latest.log` 位于 Unity 的当前用户持久资料目录下 `Diagnostics` 文件夹，记录系统、Unity、CPU、GPU、显存、分辨率、运行消息和异常堆栈。同伴应同时提供发生问题前的曲目、难度与操作步骤。

Unity 自身的 `Player.log` 仍会由系统正常生成，可作为第二层诊断证据。
