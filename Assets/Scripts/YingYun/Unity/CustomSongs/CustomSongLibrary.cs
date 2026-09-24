using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using YingYun.Rhythm.Audio;
using YingYun.Rhythm.Chart;
using Debug = UnityEngine.Debug;

namespace YingYun.Rhythm.Unity.CustomSongs
{
    /// <summary>Scans supported user audio files, runs the bundled analyzer off the main thread, and caches beat grids.</summary>
    public sealed class CustomSongLibrary : MonoBehaviour
    {
        private const string AnalyzerVersion = "beat-this-cpp-07ab790-final0-c5c1466e-decodewav1";
        private readonly List<RuntimeSongDefinition> _songs = new List<RuntimeSongDefinition>();
        private Coroutine _scanRoutine;
        private Process _activeProcess;

        public event Action SongsChanged;
        public event Action<string> StatusChanged;

        public IReadOnlyList<RuntimeSongDefinition> Songs => _songs;
        public string UserSongsPath => Path.Combine(Application.persistentDataPath, "UserSongs");
        public bool IsScanning => _scanRoutine != null;

        public void BeginScan()
        {
            if (_scanRoutine == null) _scanRoutine = StartCoroutine(ScanRoutine());
        }

        public void OpenSongsFolder()
        {
            Directory.CreateDirectory(UserSongsPath);
            Application.OpenURL(new Uri(UserSongsPath).AbsoluteUri);
        }

        private IEnumerator ScanRoutine()
        {
            Directory.CreateDirectory(UserSongsPath);
            string cachePath = Path.Combine(Application.persistentDataPath, "GeneratedSongCache", AnalyzerVersion);
            Directory.CreateDirectory(cachePath);

            string[] files = Array.FindAll(
                Directory.GetFiles(UserSongsPath, "*", SearchOption.TopDirectoryOnly),
                IsSupportedAudioFile);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            _songs.Clear();
            SongsChanged?.Invoke();

            if (files.Length == 0)
            {
                StatusChanged?.Invoke("自定义歌曲：把 MP3／FLAC／WAV 放入歌曲文件夹后按刷新");
                _scanRoutine = null;
                yield break;
            }

            int failures = 0;
            var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < files.Length; index++)
            {
                string file = files[index];
                StatusChanged?.Invoke($"正在准备 {index + 1}/{files.Length}：{Path.GetFileNameWithoutExtension(file)}");
                yield return null;

                string hash;
                try
                {
                    hash = ComputeSha256(file);
                }
                catch (Exception exception)
                {
                    failures++;
                    Debug.LogWarning($"[M9-B] Cannot hash custom song: {file}\n{exception.Message}");
                    continue;
                }

                if (!seenHashes.Add(hash)) continue;
                string beatsPath = Path.Combine(cachePath, hash + ".beats");
                string audioCachePath = Path.Combine(cachePath, hash + ".wav");
                if (!File.Exists(beatsPath) || !File.Exists(audioCachePath))
                {
                    StatusChanged?.Invoke($"正在分析 {index + 1}/{files.Length}：{Path.GetFileNameWithoutExtension(file)}");
                    string analysisError = null;
                    yield return Analyze(file, beatsPath, audioCachePath, value => analysisError = value);
                    if (!string.IsNullOrEmpty(analysisError))
                    {
                        failures++;
                        Debug.LogWarning($"[M9-B] Beat analysis failed: {file}\n{analysisError}");
                        continue;
                    }
                }

                AudioClip clip = null;
                string loadError = null;
                yield return LoadAudio(audioCachePath, Path.GetFileNameWithoutExtension(file), value => clip = value, value => loadError = value);
                if (clip == null)
                {
                    failures++;
                    Debug.LogWarning($"[M9-B] Audio load failed: {file}\n{loadError}");
                    continue;
                }

                try
                {
                    SongTimingPoint[] points = BeatGridParser.Parse(File.ReadAllLines(beatsPath));
                    var timing = new SongTimingMap(points);
                    int seed = unchecked((int)uint.Parse(hash.Substring(0, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    ProceduralSongContent content = ProceduralSongChartGenerator.Generate(timing, clip.length, seed);
                    string title = Path.GetFileNameWithoutExtension(file);
                    clip.name = title;
                    _songs.Add(new RuntimeSongDefinition(
                        "custom-" + hash.Substring(0, 16).ToLowerInvariant(),
                        title,
                        "自定义歌曲",
                        clip,
                        points,
                        content));
                    Debug.Log(
                        $"[M9-B] Custom song ready | title={title} | beats={points.Length} | " +
                        $"easy={content.GetNotes(YingYun.Rhythm.Scoring.PlayDifficulty.Easy).Length} | " +
                        $"normal={content.GetNotes(YingYun.Rhythm.Scoring.PlayDifficulty.Normal).Length} | " +
                        $"hard={content.GetNotes(YingYun.Rhythm.Scoring.PlayDifficulty.Hard).Length}");
                    SongsChanged?.Invoke();
                }
                catch (Exception exception)
                {
                    failures++;
                    Destroy(clip);
                    Debug.LogWarning($"[M9-B] Generated chart is invalid: {file}\n{exception}");
                }
            }

            StatusChanged?.Invoke(failures == 0
                ? $"自定义歌曲：{_songs.Count} 首可游玩"
                : $"自定义歌曲：{_songs.Count} 首可游玩，{failures} 首失败（详见日志）");
            _scanRoutine = null;
        }

        private IEnumerator Analyze(string audioPath, string beatsPath, string audioCachePath, Action<string> completed)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string temporaryBeatsPath = beatsPath + ".tmp";
            string temporaryAudioPath = audioCachePath + ".tmp";
            DeleteIfPresent(temporaryBeatsPath);
            DeleteIfPresent(temporaryAudioPath);
            string analyzerDirectory = Path.Combine(Application.streamingAssetsPath, "YingYunBeatAnalyzer", "Windows-x64");
            string executable = Path.Combine(analyzerDirectory, "beat_this_cpp.exe");
            string model = Path.Combine(analyzerDirectory, "beat_this.onnx");
            if (!File.Exists(executable) || !File.Exists(model))
            {
                completed("Bundled beat analyzer files are missing.");
                yield break;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"{Quote(model)} {Quote(audioPath)} --output-beats {Quote(temporaryBeatsPath)} --output-decoded {Quote(temporaryAudioPath)}",
                WorkingDirectory = analyzerDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            try
            {
                _activeProcess = Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                completed(exception.Message);
                yield break;
            }

            while (!_activeProcess.HasExited) yield return null;
            string output = _activeProcess.StandardOutput.ReadToEnd();
            string error = _activeProcess.StandardError.ReadToEnd();
            int exitCode = _activeProcess.ExitCode;
            _activeProcess.Dispose();
            _activeProcess = null;
            if (exitCode != 0 || !File.Exists(temporaryBeatsPath) || !File.Exists(temporaryAudioPath))
            {
                DeleteIfPresent(temporaryBeatsPath);
                DeleteIfPresent(temporaryAudioPath);
                completed(string.IsNullOrWhiteSpace(error) ? output : error);
                yield break;
            }

            try
            {
                DeleteIfPresent(beatsPath);
                DeleteIfPresent(audioCachePath);
                File.Move(temporaryBeatsPath, beatsPath);
                File.Move(temporaryAudioPath, audioCachePath);
            }
            catch (Exception exception)
            {
                DeleteIfPresent(temporaryBeatsPath);
                DeleteIfPresent(temporaryAudioPath);
                completed(exception.Message);
                yield break;
            }
            completed(null);
#else
            completed("Custom song analysis is currently available in Windows builds only.");
            yield break;
#endif
        }

        private static IEnumerator LoadAudio(string path, string clipName, Action<AudioClip> loaded, Action<string> failed)
        {
            Task<DecodedAudioData> task = Task.Run(() => FloatWavReader.Read(path));
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                failed(task.Exception?.GetBaseException().Message ?? "Audio cache load failed.");
                yield break;
            }

            DecodedAudioData data = task.Result;
            AudioClip clip = AudioClip.Create(clipName, data.FrameCount, data.Channels, data.SampleRate, false);
            if (!clip.SetData(data.Samples, 0))
            {
                Destroy(clip);
                failed("Unity rejected decoded audio samples.");
                yield break;
            }
            loaded(clip);
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        public static bool IsSupportedAudioFile(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".flac", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);
        }

        private static string Quote(string path) => "\"" + path.Replace("\"", "\\\"") + "\"";

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private void OnDestroy()
        {
            if (_activeProcess == null) return;
            try
            {
                if (!_activeProcess.HasExited) _activeProcess.Kill();
            }
            catch (InvalidOperationException)
            {
            }
            _activeProcess.Dispose();
            _activeProcess = null;
        }
    }
}
