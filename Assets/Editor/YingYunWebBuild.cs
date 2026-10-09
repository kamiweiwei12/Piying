using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace YingYun.EditorTools
{
    /// <summary>Builds a broadly compatible WebGL competition demo for ordinary static hosting.</summary>
    public static class YingYunWebBuild
    {
        public static void BuildCompetitionDemo()
        {
            string buildVersion = CreateBuildVersion();
            UnityEditor.WebGLCompressionFormat originalCompression = PlayerSettings.WebGL.compressionFormat;
            bool originalFallback = PlayerSettings.WebGL.decompressionFallback;
            bool originalCaching = PlayerSettings.WebGL.dataCaching;
            bool originalStripEngineCode = PlayerSettings.stripEngineCode;
            ManagedStrippingLevel originalStripping =
                PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL);

            try
            {
                // Gzip plus Unity's decompression fallback works on static hosts that cannot set
                // Content-Encoding headers, which is safer for a competition review link.
                PlayerSettings.WebGL.compressionFormat = UnityEditor.WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(
                    NamedBuildTarget.WebGL,
                    ManagedStrippingLevel.Medium);

                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenes.Length == 0)
                {
                    throw new InvalidOperationException("No enabled scenes are configured for the WebGL build.");
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = "Builds/WebGL-Demo",
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"WebGL build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
                }

                string windowsAnalyzerOutput = Path.Combine(
                    report.summary.outputPath,
                    "StreamingAssets",
                    "YingYunBeatAnalyzer",
                    "Windows-x64");
                if (Directory.Exists(windowsAnalyzerOutput))
                {
                    Directory.Delete(windowsAnalyzerOutput, true);
                    UnityEngine.Debug.Log(
                        "[YingYunWebBuild] Removed the Windows-only beat analyzer from the WebGL output.");
                }

                string indexPath = Path.Combine(report.summary.outputPath, "index.html");
                if (File.Exists(indexPath))
                {
                    PatchWebShell(report.summary.outputPath, indexPath, buildVersion);
                }

                long publishedSize = Directory.EnumerateFiles(report.summary.outputPath, "*", SearchOption.AllDirectories)
                    .Sum(path => new FileInfo(path).Length);
                UnityEngine.Debug.Log(
                    $"[YingYunWebBuild] Success: {report.summary.outputPath}, version={buildVersion}, " +
                    $"publishedBytes={publishedSize}, buildTime={report.summary.totalTime}.");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = originalCompression;
                PlayerSettings.WebGL.decompressionFallback = originalFallback;
                PlayerSettings.WebGL.dataCaching = originalCaching;
                PlayerSettings.stripEngineCode = originalStripEngineCode;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, originalStripping);
            }
        }

        private static string CreateBuildVersion()
        {
            string revision = "local";
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short=8 HEAD",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process != null && process.WaitForExit(3000) && process.ExitCode == 0)
                    {
                        string candidate = process.StandardOutput.ReadToEnd().Trim();
                        if (!string.IsNullOrWhiteSpace(candidate)) revision = candidate;
                    }
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[YingYunWebBuild] Git revision unavailable: {exception.Message}");
            }

            return $"P0-{DateTime.UtcNow:yyyyMMddHHmm}-{revision}";
        }

        private static void PatchWebShell(string outputPath, string indexPath, string buildVersion)
        {
            string escapedVersion = buildVersion.Replace("\\", string.Empty).Replace("\"", string.Empty);
            const string loadingMarkup =
                "<div id=\"yingyun-loading-title\">皮影声律</div>" +
                "<div id=\"yingyun-loading-status\">首次加载约需十几秒，请勿关闭页面</div>" +
                "<div id=\"yingyun-loading-percent\">0%</div>";

            string index = File.ReadAllText(indexPath)
                .Replace("<html lang=\"en-us\">", "<html lang=\"zh-CN\">")
                .Replace("Unity Web Player | My project", "皮影声律 · 节奏游戏")
                .Replace("<link rel=\"stylesheet\" href=\"TemplateData/style.css\">",
                    $"<link rel=\"stylesheet\" href=\"TemplateData/style.css?v={escapedVersion}\">")
                .Replace("<div id=\"unity-build-title\">My project</div>",
                    $"<div id=\"unity-build-title\">皮影声律 · 节奏游戏　{escapedVersion}</div>")
                .Replace("<div id=\"unity-loading-bar\">",
                    $"<div id=\"unity-loading-bar\">{loadingMarkup}")
                .Replace("var loaderUrl = buildUrl + \"/WebGL-Demo.loader.js\";",
                    $"var loaderUrl = buildUrl + \"/WebGL-Demo.loader.js?v={escapedVersion}\";")
                .Replace("/WebGL-Demo.data.unityweb\"", $"/WebGL-Demo.data.unityweb?v={escapedVersion}\"")
                .Replace("/WebGL-Demo.framework.js.unityweb\"", $"/WebGL-Demo.framework.js.unityweb?v={escapedVersion}\"")
                .Replace("/WebGL-Demo.wasm.unityweb\"", $"/WebGL-Demo.wasm.unityweb?v={escapedVersion}\"")
                .Replace("companyName: \"DefaultCompany\"", "companyName: \"皮影声律\"")
                .Replace("productName: \"My project\"", "productName: \"皮影声律 · 节奏游戏\"")
                .Replace("const progressBarFull = document.querySelector(\"#unity-progress-bar-full\");",
                    "const progressBarFull = document.querySelector(\"#unity-progress-bar-full\");\n" +
                    "        const loadingPercent = document.querySelector(\"#yingyun-loading-percent\");\n" +
                    "        const loadingStatus = document.querySelector(\"#yingyun-loading-status\");")
                .Replace("progressBarFull.style.width = 100 * progress + \"%\";",
                    "progressBarFull.style.width = 100 * progress + \"%\";\n" +
                    "            loadingPercent.textContent = Math.round(100 * progress) + \"%\";")
                .Replace("loadingContainer.style.display = \"none\";",
                    "loadingStatus.textContent = \"戏台已就绪，正在开幕\";\n" +
                    "            loadingContainer.style.display = \"none\";")
                .Replace("alert(message);",
                    "loadingStatus.textContent = \"加载失败，请刷新页面重试\";\n" +
                    "                unityShowBanner(\"加载失败。请刷新页面；若仍失败，请使用最新版 Chrome 或 Edge。<br>\" + message, 'error');")
                .Replace("if (type == 'error') div.style = 'background: red; padding: 10px;'",
                    "if (type == 'error') div.style = 'background: #641b13; color: #f7e5b8; padding: 14px 18px; border: 1px solid #c99c4b;'");
            File.WriteAllText(indexPath, index, new UTF8Encoding(false));

            string stylePath = Path.Combine(outputPath, "TemplateData", "style.css");
            if (File.Exists(stylePath))
            {
                const string competitionStyles = @"
#unity-loading-container { display: block; background: radial-gradient(circle at center, rgba(89,30,20,.82), rgba(24,10,8,.96)); backdrop-filter: none; }
#unity-loading-bar { width: 360px; color: #f2d68c; text-align: center; font-family: 'Microsoft YaHei', 'Noto Sans SC', sans-serif; }
#unity-logo { display: none; }
#yingyun-loading-title { margin-bottom: 16px; font-family: 'STKaiti', 'KaiTi', serif; font-size: 38px; letter-spacing: 10px; color: #f4d58b; text-shadow: 0 2px 8px rgba(0,0,0,.7); }
#yingyun-loading-status { min-height: 24px; font-size: 16px; letter-spacing: 1px; color: #ead8b2; }
#yingyun-loading-percent { margin-top: 12px; font-size: 15px; color: #cfaa5d; }
#unity-progress-bar-empty { width: 320px; height: 12px; margin: 18px auto 0; border: 1px solid #bd8a3d; background: rgba(23,8,6,.72); box-sizing: border-box; }
#unity-progress-bar-full { width: 0%; height: 10px; margin: 0; background: linear-gradient(90deg, #85301f, #e2b75d); transition: width .12s linear; }
#unity-warning { z-index: 20; width: min(760px, 86%); color: #f7e5b8; font-family: 'Microsoft YaHei', sans-serif; }
";
                File.AppendAllText(stylePath, competitionStyles, new UTF8Encoding(false));
            }

            string versionJson =
                "{\n" +
                $"  \"version\": \"{escapedVersion}\",\n" +
                $"  \"builtAtUtc\": \"{DateTime.UtcNow:O}\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(outputPath, "version.json"), versionJson, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);
        }
    }
}
