using System;
using System.IO;
using System.Linq;
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
                    string index = File.ReadAllText(indexPath)
                        .Replace("<html lang=\"en-us\">", "<html lang=\"zh-CN\">")
                        .Replace("Unity Web Player | My project", "影韵 · 皮影节奏游戏")
                        .Replace("<div id=\"unity-build-title\">My project</div>",
                            "<div id=\"unity-build-title\">影韵 · 皮影节奏游戏</div>")
                        .Replace("companyName: \"DefaultCompany\"", "companyName: \"影韵\"")
                        .Replace("productName: \"My project\"", "productName: \"影韵 · 皮影节奏游戏\"");
                    File.WriteAllText(indexPath, index);
                    File.WriteAllText(Path.Combine(report.summary.outputPath, ".nojekyll"), string.Empty);
                }

                UnityEngine.Debug.Log(
                    $"[YingYunWebBuild] Success: {report.summary.outputPath}, " +
                    $"{report.summary.totalSize} bytes, {report.summary.totalTime}.");
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
    }
}
