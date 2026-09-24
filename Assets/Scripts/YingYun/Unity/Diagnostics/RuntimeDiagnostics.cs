using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace YingYun.Rhythm.Unity.Diagnostics
{
    /// <summary>保存可由试玩同伴直接回传的运行环境、普通日志与异常堆栈。</summary>
    public sealed class RuntimeDiagnostics : MonoBehaviour
    {
        private const int VisibleEntryCapacity = 120;
        private const int VisibleEntryCount = 24;
        private const string LogFileName = "YingYun-latest.log";
        private readonly object _sync = new object();
        private readonly Queue<string> _visibleEntries = new Queue<string>(VisibleEntryCapacity);
        private StreamWriter _writer;
        private volatile bool _needsFlush;
        private float _nextFlushTime;
        private bool _initialized;

        public string LogsDirectory { get; private set; }
        public string SessionLogPath { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            LogsDirectory = Path.Combine(Application.persistentDataPath, "Diagnostics");
            SessionLogPath = Path.Combine(LogsDirectory, LogFileName);

            try
            {
                Directory.CreateDirectory(LogsDirectory);
                var stream = new FileStream(
                    SessionLogPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.ReadWrite);
                _writer = new StreamWriter(stream, new UTF8Encoding(false));
                WriteSessionHeader();
            }
            catch (Exception exception)
            {
                _writer?.Dispose();
                _writer = null;
                Debug.LogError($"[M10] Cannot create diagnostic log: {exception.Message}", this);
            }

            Application.logMessageReceivedThreaded += CaptureLog;
            Debug.Log($"[M10] diagnostics-ready | path={SessionLogPath}", this);
        }

        public string GetVisibleText()
        {
            lock (_sync)
            {
                var builder = new StringBuilder(8192);
                string[] entries = _visibleEntries.ToArray();
                int first = Math.Max(0, entries.Length - VisibleEntryCount);
                for (int i = first; i < entries.Length; i++)
                {
                    builder.AppendLine(entries[i]);
                }

                return builder.Length == 0 ? "目前尚无运行日志。" : builder.ToString();
            }
        }

        public string GetShareableText()
        {
            lock (_sync)
            {
                _writer?.Flush();
                try
                {
                    return File.Exists(SessionLogPath)
                        ? ReadLogWhileWriting()
                        : GetVisibleText();
                }
                catch (Exception exception)
                {
                    return $"无法读取日志文件：{exception.Message}\n\n{GetVisibleText()}";
                }
            }
        }

        public void OpenLogsDirectory()
        {
            try
            {
                Directory.CreateDirectory(LogsDirectory);
                Application.OpenURL(new Uri(LogsDirectory).AbsoluteUri);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[M10] Cannot open diagnostic folder: {exception.Message}", this);
            }
        }

        private string ReadLogWhileWriting()
        {
            using var stream = new FileStream(
                SessionLogPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string entry = $"[{timestamp}] [{type}] {condition}";
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
                !string.IsNullOrWhiteSpace(stackTrace))
            {
                entry += Environment.NewLine + stackTrace.TrimEnd();
            }

            lock (_sync)
            {
                while (_visibleEntries.Count >= VisibleEntryCapacity)
                {
                    _visibleEntries.Dequeue();
                }

                _visibleEntries.Enqueue(entry);
                if (_writer != null)
                {
                    _writer.WriteLine(entry);
                    _needsFlush = true;
                    if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    {
                        _writer.Flush();
                        _needsFlush = false;
                    }
                }
            }
        }

        private void WriteSessionHeader()
        {
            _writer.WriteLine("=== 影韵 Demo 运行诊断 ===");
            _writer.WriteLine($"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            _writer.WriteLine($"游戏版本: {Application.version}");
            _writer.WriteLine($"Unity版本: {Application.unityVersion}");
            _writer.WriteLine($"平台: {Application.platform}");
            _writer.WriteLine($"操作系统: {SystemInfo.operatingSystem}");
            _writer.WriteLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} threads)");
            _writer.WriteLine($"内存: {SystemInfo.systemMemorySize} MB");
            _writer.WriteLine($"GPU: {SystemInfo.graphicsDeviceName}");
            _writer.WriteLine($"显存: {SystemInfo.graphicsMemorySize} MB");
            _writer.WriteLine($"图形API: {SystemInfo.graphicsDeviceType}");
            _writer.WriteLine($"分辨率: {Screen.width}x{Screen.height} @{Screen.currentResolution.refreshRateRatio.value:F2} Hz");
            _writer.WriteLine($"数据目录: {Application.persistentDataPath}");
            _writer.WriteLine("================================");
            _writer.Flush();
        }

        private void Update()
        {
            if (!_needsFlush || Time.unscaledTime < _nextFlushTime) return;
            lock (_sync)
            {
                _writer?.Flush();
                _needsFlush = false;
                _nextFlushTime = Time.unscaledTime + 1f;
            }
        }

        private void OnApplicationQuit()
        {
            lock (_sync)
            {
                _writer?.Flush();
            }
        }

        private void OnDestroy()
        {
            if (_initialized) Application.logMessageReceivedThreaded -= CaptureLog;
            lock (_sync)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
