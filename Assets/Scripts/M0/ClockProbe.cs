// ============================================================================
// 《影韵》M0-A 音訊時鐘基準 + Input Clock 基礎量測（拋棄式量測工具）
// ----------------------------------------------------------------------------
// 用途：以實測數據回答 M0-A 的問題（dspTime 步進、暫停行為、Input 時鐘 offset）。
//       本檔不是正式遊戲程式；M0-A 驗收完成後將另行提案移除。
// 限制：只使用 Unity 既有 API，不修改任何專案設定、資產、Scene、Prefab、Input Actions。
// 產出：<project>/Logs/M0_clock_*.csv（逐幀）、M0_input_events_*.csv（逐輸入事件）
//       （Logs/ 已在 .gitignore 中，不進版控）
//
// 注意：本專案 EditorSettings 已開啟 Disable Domain Reload，
//       所有 static 狀態必須在 SubsystemRegistration 顯式重置，
//       且事件訂閱必須使用 static handler 才能可靠解除，避免跨 Play session 洩漏。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace YingYun.M0
{
    /// <summary>
    /// M0-A 量測探針：由 RuntimeInitializeOnLoadMethod 自動安裝，不需任何 Scene / Prefab。
    /// </summary>
    public sealed class ClockProbe : MonoBehaviour
    {
        private const double LeadInSeconds = 1.0;      // PlayScheduled 前置時間
        private const int ClipLengthSeconds = 2;       // 循環測試音長度（秒）
        private const int ClickIntervalMs = 500;       // 每 500 ms 一個 click
        private const int ClickLengthMs = 10;          // click 長度 10 ms
        private const int FlushEveryFrames = 60;
        private const int MaxStatSamples = 200000;     // 統計樣本上限

        // ------------------------------------------------------------------
        // Static 狀態（Disable Domain Reload → 必須在 SubsystemRegistration 重置）
        // ------------------------------------------------------------------
        private static ClockProbe s_Instance;
        private static bool s_Installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // 顯式解除訂閱：domain reload 被關閉時靜態事件清單不會自動清空
            InputSystem.onEvent -= OnRawInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
            Application.focusChanged -= OnFocusChanged;

            s_Instance = null;
            s_Installed = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (s_Installed)
            {
                return;
            }

            s_Installed = true;

            var go = new GameObject("[M0ClockProbe]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<ClockProbe>();
        }

        /// <summary>
        /// 場景載入完成後才決定要使用哪一個 AudioListener。
        /// 理由：BeforeSceneLoad（Install → Awake → SetupAudio）時場景物件（例如 Main Camera）
        /// 尚未載入，在該時點檢查會誤判為「沒有 listener」而多建立一個，導致 Console 持續出現
        /// 「There are N audio listeners in the scene」。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ResolveListenerAfterSceneLoad()
        {
            if (s_Instance != null)
            {
                s_Instance.ResolveListener("AfterSceneLoad");
            }
        }

        // ------------------------------------------------------------------
        // Instance 狀態
        // ------------------------------------------------------------------
        private AudioSource _source;
        private AudioClip _clip;
        private AudioListener _addedListener;
        private SampleCounterFilter _filter;   // 取樣計數器（掛在 AudioSource 所在的子物件上）
        private bool _listenerResolved;        // AudioListener 決策是否已完成（避免重複判斷）
        private string _listenerSourceName = ""; // 實際使用的 listener 來源物件名稱（供 log 記錄）

        /// <summary>累計消耗的 sample frames（由 SampleCounterFilter 提供；未就緒時回 0）。</summary>
        private long ConsumedFrames
        {
            get { return _filter != null ? _filter.ConsumedFrames : 0L; }
        }

        /// <summary>最後看到的聲道數（由 SampleCounterFilter 提供）。</summary>
        private int FilterChannels
        {
            get { return _filter != null ? _filter.LastChannels : 0; }
        }

        private double _dspStart;
        private int _outputSampleRate;
        private int _dspBufferLength;
        private int _dspNumBuffers;
        private int _clipSampleRate;

        private int _frame;
        private long _eventCount;
        private long _focusEventCount;
        private long _deviceEventCount;
        private double _lastInputEventTime;
        private bool _hasInputEvent;
        private string _lastInputDevice = "";
        private string _lastInputLayout = "";

        private string _scenario = "Normal";
        private string _marker = "";

        private readonly List<double> _dspSteps = new List<double>(4096);
        private readonly List<double> _inputOffsets = new List<double>(4096);
        private int _zeroDspDeltaFrames;
        private double _prevDsp = double.NaN;

        private readonly Dictionary<string, Segment> _segments = new Dictionary<string, Segment>(StringComparer.Ordinal);
        private string _segName;
        private double _segStartRealtime;
        private double _segStartDsp;
        private long _segStartConsumed;

        private string _clockCsvPath;
        private string _eventCsvPath;
        private StreamWriter _clockWriter;
        private StreamWriter _eventWriter;
        private readonly StringBuilder _sb = new StringBuilder(768);
        private bool _summaryPrinted;
        private bool _closed;

        /// <summary>單一情境區段的累計量（Normal / Pause / ...）。</summary>
        private sealed class Segment
        {
            public string Name;
            public double RealtimeSeconds;
            public double DspSeconds;
            public double ConsumedSeconds;
            public int Frames;
            public bool PausesAudioSource;
            public bool PausesListener;

            public double DspRatio
            {
                get { return RealtimeSeconds > 0.0 ? DspSeconds / RealtimeSeconds : 0.0; }
            }

            public double ConsumedRatio
            {
                get { return RealtimeSeconds > 0.0 ? ConsumedSeconds / RealtimeSeconds : 0.0; }
            }
        }

        // ------------------------------------------------------------------
        // 生命週期
        // ------------------------------------------------------------------
        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            SetupAudio();
            OpenCsvFiles();
            Subscribe();
            WriteCsvHeaders();
            BeginSegment(_scenario);

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[M0] probe started | outputSampleRate={0} | dspBuffer={1} samples x {2} buffers | clip={3} Hz | leadIn={4}s | dspStart={5:F6} | csv={6}",
                _outputSampleRate, _dspBufferLength, _dspNumBuffers, _clipSampleRate, LeadInSeconds, _dspStart, _clockCsvPath));
            Debug.Log("[M0] 按鍵：1=Normal 2=AudioSource.Pause 3=AudioSource.UnPause 4=AudioListener.pause 5=AudioListener.resume 0=摘要；空白鍵=輸入事件標記");
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            InputSystem.onEvent -= OnRawInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
            Application.focusChanged -= OnFocusChanged;

            EndSegment();

            if (!_summaryPrinted)
            {
                PrintSummary();
            }

            FlushAndClose();
            s_Instance = null;
        }

        // ------------------------------------------------------------------
        // AudioListener 決策（必須在場景載入完成後才執行）
        // ------------------------------------------------------------------
        /// <summary>
        /// 決定使用哪一個 AudioListener：
        /// 場景已有 listener（包含 inactive）→ 沿用它並記錄其 GameObject 名稱，addedListener=false；
        /// 場景完全沒有 listener → 才在 probe 物件上建立 fallback，addedListener=true。
        /// </summary>
        private void ResolveListener(string tag)
        {
            if (_listenerResolved)
            {
                return;
            }

            _listenerResolved = true;

            var existing = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);

            if (existing != null && existing.Length > 0 && existing[0] != null)
            {
                var host = existing[0].gameObject;
                _listenerSourceName = host != null ? host.name : "(unknown)";
                _addedListener = null;   // 使用場景既有 listener，probe 不再新增
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[M0] listener({0}) using existing AudioListener on '{1}' | found={2} | addedListener=false | rt={3:F3}",
                    tag, _listenerSourceName, existing.Length, Time.realtimeSinceStartupAsDouble));
            }
            else
            {
                _addedListener = gameObject.AddComponent<AudioListener>();
                _listenerSourceName = gameObject.name + " (probe fallback)";
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[M0] listener({0}) no AudioListener in scene -> created fallback on '{1}' | addedListener=true | rt={2:F3}",
                    tag, gameObject.name, Time.realtimeSinceStartupAsDouble));
            }

        }

        // ------------------------------------------------------------------
        // 音訊：程序化測試音（不依賴任何既有 AudioClip 資產）
        // ------------------------------------------------------------------
        private void SetupAudio()
        {
            _outputSampleRate = AudioSettings.outputSampleRate;
            AudioSettings.GetDSPBufferSize(out _dspBufferLength, out _dspNumBuffers);

            // 註：AudioListener 的決策刻意延後到場景載入完成後才做（見 ResolveListener），
            //     因為 BeforeSceneLoad 時場景物件（例如 Main Camera）尚未載入，
            //     在此時檢查會誤判為「沒有 listener」而多建立一個。

            // AudioSource 與取樣計數器放在子物件上；AudioListener 留在根物件。
            // 理由：OnAudioFilterRead 所在的 GameObject 只能有 AudioSource 或 AudioListener 其一，
            //       否則 Unity 會把回呼綁到 AudioListener（收到最終混音），使 consumedSamples 在
            //       AudioSource 被暫停時仍持續增加，導致 M0-A 的暫停語意判定失真。
            var sourceObject = new GameObject("M0ClockProbe_Source");
            sourceObject.transform.SetParent(transform, false);
            _source = sourceObject.AddComponent<AudioSource>();
            _filter = sourceObject.AddComponent<SampleCounterFilter>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;   // 2D，避免距離衰減
            _source.volume = 0.15f;
            _source.mute = false;

            _clipSampleRate = _outputSampleRate > 0 ? _outputSampleRate : 48000;
            _clip = BuildClickClip(_clipSampleRate);
            _source.clip = _clip;

            double dspNow = AudioSettings.dspTime;
            _dspStart = dspNow + LeadInSeconds;
            _source.PlayScheduled(_dspStart);
        }

        private static AudioClip BuildClickClip(int sampleRate)
        {
            int length = sampleRate * ClipLengthSeconds;
            var data = new float[length];

            int clickSamples = Mathf.Max(1, sampleRate * ClickLengthMs / 1000);
            int interval = Mathf.Max(clickSamples, sampleRate * ClickIntervalMs / 1000);

            for (int start = 0; start < length; start += interval)
            {
                for (int i = 0; i < clickSamples; i++)
                {
                    int index = start + i;
                    if (index >= length)
                    {
                        break;
                    }

                    float phase = 2f * Mathf.PI * 1000f * (i / (float)sampleRate);
                    float envelope = 1f - (i / (float)clickSamples);
                    data[index] = 0.8f * envelope * Mathf.Sin(phase);
                }
            }

            var clip = AudioClip.Create("M0ClockProbe_Click", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 註：OnAudioFilterRead 已抽出至 SampleCounterFilter（掛在 AudioSource 所在的子物件上），
        //     以避免與 AudioListener 爭用同一個 AudioFilter 回呼。

        // ------------------------------------------------------------------
        // CSV
        // ------------------------------------------------------------------
        private static string ProjectLogFolder
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs")); }
        }

        private void OpenCsvFiles()
        {
            string folder = ProjectLogFolder;
            Directory.CreateDirectory(folder);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            _clockCsvPath = Path.Combine(folder, "M0_clock_" + stamp + ".csv");
            _eventCsvPath = Path.Combine(folder, "M0_input_events_" + stamp + ".csv");

            _clockWriter = new StreamWriter(_clockCsvPath, false, new UTF8Encoding(false));
            _eventWriter = new StreamWriter(_eventCsvPath, false, new UTF8Encoding(false));
            _clockWriter.AutoFlush = false;
            _eventWriter.AutoFlush = false;
        }

        private void WriteCsvHeaders()
        {
            _clockWriter.WriteLine(
                "frame,scenario,marker,dspTime,realtimeSinceStartup,unscaledTime,unityTime,deltaTime,timeScale," +
                "inputClockOffset,lastInputEventTime,inputEventCount," +
                "audioSourceTime,audioSourceTimeSamples,audioSourceIsPlaying,consumedSamples,consumedSeconds," +
                "dspElapsedSeconds,dspStepMs,outputSampleRate,dspBufferLength,dspNumBuffers," +
                "audioListenerPaused,audioSourcePausedByProbe,appFocused,focusEventCount,deviceEventCount," +
                "keyboardEnabled,keyboardDeviceCount,filterChannels,dspStart");

            _eventWriter.WriteLine(
                "eventIndex,frame,eventTime,realtimeAtCallback,inputClockOffset,maxEventTime,deviceName,deviceLayout,deviceId");

            _clockWriter.Flush();
            _eventWriter.Flush();
        }

        private void FlushAndClose()
        {
            try
            {
                if (_clockWriter != null)
                {
                    _clockWriter.Flush();
                    _clockWriter.Dispose();
                    _clockWriter = null;
                }

                if (_eventWriter != null)
                {
                    _eventWriter.Flush();
                    _eventWriter.Dispose();
                    _eventWriter = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[M0] flush/close failed: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        // 訂閱 / 解除訂閱（使用 static handler，確保 Disable Domain Reload 下可可靠解除）
        // ------------------------------------------------------------------
        private readonly List<double> _focusTimestamps = new List<double>(64);

        private void Subscribe()
        {
            InputSystem.onEvent += OnRawInputEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
            Application.focusChanged += OnFocusChanged;

            var settings = InputSystem.settings;
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[M0] input settings | updateMode={0} backgroundBehavior={1} maxQueuedEventsPerUpdate={2} | appFocused={3}",
                settings.updateMode, settings.backgroundBehavior, settings.maxQueuedEventsPerUpdate, Application.isFocused));
            LogDeviceSnapshot("startup");
        }

        private void LogDeviceSnapshot(string tag)
        {
            var devices = InputSystem.devices;
            int enabled = 0;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] != null && devices[i].enabled)
                {
                    enabled++;
                }
            }

            var kb = Keyboard.current;
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[M0] deviceSnapshot({0}) count={1} enabled={2} keyboardPresent={3} keyboardEnabled={4} time={5:F6} dsp={6:F6}",
                tag, devices.Count, enabled, kb != null, kb != null && kb.enabled,
                Time.realtimeSinceStartupAsDouble, AudioSettings.dspTime));
        }

        private static void OnRawInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            var probe = s_Instance;
            if (probe != null)
            {
                probe.HandleRawInputEvent(eventPtr, device);
            }
        }

        private void HandleRawInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            double eventTime = eventPtr.time;
            double realtimeNow = Time.realtimeSinceStartupAsDouble;
            double offset = eventTime - realtimeNow;

            _eventCount++;
            _lastInputEventTime = eventTime;
            _hasInputEvent = true;
            _lastInputDevice = device != null ? device.name : "(null)";
            _lastInputLayout = device != null ? device.layout : "";

            if (_inputOffsets.Count < MaxStatSamples)
            {
                _inputOffsets.Add(offset);
            }

            if (_eventWriter == null)
            {
                return;
            }

            _sb.Clear();
            _sb.Append(_eventCount.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(_frame.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(eventTime.ToString("F6", CultureInfo.InvariantCulture)).Append(',')
               .Append(realtimeNow.ToString("F6", CultureInfo.InvariantCulture)).Append(',')
               .Append((offset * 1000.0).ToString("F4", CultureInfo.InvariantCulture)).Append(',')
               .Append(_lastInputEventTime.ToString("F6", CultureInfo.InvariantCulture)).Append(',')
               .Append(_lastInputDevice).Append(',')
               .Append(_lastInputLayout).Append(',')
               .Append(device != null ? device.deviceId.ToString(CultureInfo.InvariantCulture) : "-1");
            _eventWriter.WriteLine(_sb.ToString());

            if ((_eventCount % FlushEveryFrames) == 0)
            {
                _eventWriter.Flush();
            }
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            var probe = s_Instance;
            if (probe == null)
            {
                return;
            }

            probe._deviceEventCount++;
            probe._marker = "MARKER:device:" + change;

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[M0] deviceChange {0} | name={1} layout={2} id={3} enabled={4} time={5:F6}",
                change,
                device != null ? device.name : "(null)",
                device != null ? device.layout : "",
                device != null ? device.deviceId : -1,
                device != null && device.enabled,
                Time.realtimeSinceStartupAsDouble));
        }

        private static void OnFocusChanged(bool focused)
        {
            var probe = s_Instance;
            if (probe != null)
            {
                probe.HandleFocusChanged(focused);
            }
        }

        private void HandleFocusChanged(bool focused)
        {
            _focusEventCount++;
            _marker = focused ? "MARKER:focus:true" : "MARKER:focus:false";

            double realtime = Time.realtimeSinceStartupAsDouble;
            if (_focusTimestamps.Count < MaxStatSamples)
            {
                _focusTimestamps.Add(realtime);
            }

            var kb = Keyboard.current;
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[M0] focusChanged focused={0} realtime={1:F6} dspTime={2:F6} isFocused={3} keyboardPresent={4} keyboardEnabled={5} devices={6}",
                focused, realtime, AudioSettings.dspTime, Application.isFocused, kb != null, kb != null && kb.enabled,
                InputSystem.devices.Count));

            LogDeviceSnapshot(focused ? "focus:true" : "focus:false");
        }

        // ------------------------------------------------------------------
        // 每幀：情境控制 + 寫入量測列
        // ------------------------------------------------------------------
        private bool _sourcePaused;
        private bool _listenerPausedByProbe;
        private int _segFrames;

        private void Update()
        {
            if (!_listenerResolved)
            {
                // AfterSceneLoad 鉤子若因故未執行，於第一幀補做一次（idempotent）
                ResolveListener("FirstUpdate");
            }

            HandleScenarioKeys();
            WriteFrameRow();
            _marker = "";
        }

        private void HandleScenarioKeys()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.digit1Key.wasPressedThisFrame)
            {
                SetScenario("Normal", false, false, "MARKER:Normal");
            }

            if (kb.digit2Key.wasPressedThisFrame)
            {
                _source.Pause();
                SetScenario("AudioSource.Pause", true, false, "MARKER:AudioSource.Pause");
            }

            if (kb.digit3Key.wasPressedThisFrame)
            {
                _source.UnPause();
                SetScenario("AudioSource.UnPause", false, false, "MARKER:AudioSource.UnPause");
            }

            if (kb.digit4Key.wasPressedThisFrame)
            {
                AudioListener.pause = true;
                SetScenario("AudioListener.pause", false, true, "MARKER:AudioListener.pause");
            }

            if (kb.digit5Key.wasPressedThisFrame)
            {
                AudioListener.pause = false;
                SetScenario("AudioListener.resume", false, true, "MARKER:AudioListener.resume");
            }

            if (kb.digit0Key.wasPressedThisFrame)
            {
                PrintSummary();
            }

            if (kb.spaceKey.wasPressedThisFrame)
            {
                _marker = "MARKER:space";
            }
        }

        private void SetScenario(string name, bool sourcePaused, bool listenerPaused, string marker)
        {
            EndSegment();

            _scenario = name;
            _marker = marker;
            _sourcePaused = sourcePaused;
            _listenerPausedByProbe = listenerPaused;

            Segment seg;
            if (!_segments.TryGetValue(name, out seg))
            {
                seg = new Segment();
                seg.Name = name;
                _segments[name] = seg;
            }

            seg.PausesAudioSource = sourcePaused;
            seg.PausesListener = listenerPaused;

            BeginSegment(name);
            _segFrames = 0;

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[M0] scenario -> {0} ({1}) | realtime={2:F6} dsp={3:F6} consumed={4}",
                name, marker, Time.realtimeSinceStartupAsDouble, AudioSettings.dspTime,
                ConsumedFrames));
        }

        private void WriteFrameRow()
        {
            if (_clockWriter == null)
            {
                return;
            }

            _frame++;
            _segFrames++;

            double dsp = AudioSettings.dspTime;
            double realtime = Time.realtimeSinceStartupAsDouble;
            double unscaled = Time.unscaledTimeAsDouble;
            long consumed = ConsumedFrames;

            double dspStepMs;
            if (double.IsNaN(_prevDsp))
            {
                dspStepMs = double.NaN;
            }
            else
            {
                double step = dsp - _prevDsp;
                if (step > 0.0)
                {
                    dspStepMs = step * 1000.0;
                    if (_dspSteps.Count < MaxStatSamples)
                    {
                        _dspSteps.Add(step);
                    }
                }
                else
                {
                    _zeroDspDeltaFrames++;
                    dspStepMs = 0.0;
                }
            }

            _prevDsp = dsp;

            double consumedSeconds = _outputSampleRate > 0 ? consumed / (double)_outputSampleRate : 0.0;
            double dspElapsed = dsp - _dspStart;
            double inputOffsetMs = _hasInputEvent ? (_lastInputEventTime - realtime) * 1000.0 : double.NaN;
            var kb = Keyboard.current;

            _sb.Clear();
            _sb.Append(_frame.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append(_scenario).Append(',');
            _sb.Append(_marker).Append(',');
            AppendNum(dsp, 6);
            AppendNum(realtime, 6);
            AppendNum(unscaled, 6);
            AppendNum(Time.timeAsDouble, 6);
            AppendNum(Time.deltaTime, 6);
            AppendNum(Time.timeScale, 4);
            AppendNum(inputOffsetMs, 4);
            AppendNum(_hasInputEvent ? _lastInputEventTime : double.NaN, 6);
            _sb.Append(_eventCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendNum(_source != null ? _source.time : 0f, 6);
            _sb.Append((_source != null ? _source.timeSamples : 0).ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append((_source != null && _source.isPlaying) ? "1" : "0").Append(',');
            _sb.Append(consumed.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendNum(consumedSeconds, 6);
            AppendNum(dspElapsed, 6);
            AppendNum(dspStepMs, 4);
            _sb.Append(_outputSampleRate.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append(_dspBufferLength.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append(_dspNumBuffers.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append(AudioListener.pause ? "1" : "0").Append(',');
            _sb.Append(_sourcePaused ? "1" : "0").Append(',');
            _sb.Append(Application.isFocused ? "1" : "0").Append(',');
            _sb.Append(_focusEventCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append(_deviceEventCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append((kb != null && kb.enabled) ? "1" : "0").Append(',');
            _sb.Append(InputSystem.devices.Count.ToString(CultureInfo.InvariantCulture)).Append(',');
            _sb.Append(FilterChannels.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendNum(_dspStart, 6);

            _clockWriter.WriteLine(_sb.ToString());

            if ((_frame % FlushEveryFrames) == 0)
            {
                _clockWriter.Flush();
            }

            if (_hasInputEvent && _inputOffsets.Count < MaxStatSamples)
            {
                // 每幀的 offset 樣本（與逐事件樣本分開，用於比較「事件當下」與「幀取樣」兩種 offset）
                _frameOffsets.Add((_lastInputEventTime - realtime) * 1000.0);
            }
        }

        private readonly List<double> _frameOffsets = new List<double>(4096);

        private void AppendNum(double value, int digits)
        {
            _sb.Append(value.ToString("F" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)).Append(',');
        }

        // ------------------------------------------------------------------
        // 情境區段累計
        // ------------------------------------------------------------------
        private void BeginSegment(string name)
        {
            _segName = name;
            _segStartRealtime = Time.realtimeSinceStartupAsDouble;
            _segStartDsp = AudioSettings.dspTime;
            _segStartConsumed = ConsumedFrames;
        }

        private void EndSegment()
        {
            if (_segName == null)
            {
                return;
            }

            Segment seg;
            if (_segments.TryGetValue(_segName, out seg))
            {
                int rate = _outputSampleRate > 0 ? _outputSampleRate : 48000;
                seg.RealtimeSeconds += Time.realtimeSinceStartupAsDouble - _segStartRealtime;
                seg.DspSeconds += AudioSettings.dspTime - _segStartDsp;
                seg.ConsumedSeconds += (ConsumedFrames - _segStartConsumed) / (double)rate;
                seg.Frames += _segFrames;
            }

            _segName = null;
            _segFrames = 0;
        }

        // ------------------------------------------------------------------
        // 摘要
        // ------------------------------------------------------------------
        private void PrintSummary()
        {
            if (_summaryPrinted)
            {
                return;
            }

            _summaryPrinted = true;

            long consumed = ConsumedFrames;
            double consumedSeconds = _outputSampleRate > 0 ? consumed / (double)_outputSampleRate : 0.0;
            double dspElapsed = AudioSettings.dspTime - _dspStart;
            double nominalBufferMs = _outputSampleRate > 0 ? (_dspBufferLength * 1000.0 / _outputSampleRate) : 0.0;
            var kb = Keyboard.current;

            Debug.Log("================ M0-A SUMMARY (ClockProbe) ================");
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[M0] session frames={0} | realtime={1:F3}s | dspTime={2:F6} | dspElapsed={3:F6}s",
                _frame, Time.realtimeSinceStartupAsDouble, AudioSettings.dspTime, dspElapsed));
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[M0] audio cfg | outputSampleRate={0} | dspBuffer={1} samples x {2} buffers | nominalBufferPeriod={3:F3} ms | filterChannels={4} | addedListener={5}",
                _outputSampleRate, _dspBufferLength, _dspNumBuffers, nominalBufferMs, FilterChannels, _addedListener != null));
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[M0] consumedSamples={0} => consumedSeconds={1:F6} | dspElapsed={2:F6} | diff(consumed-dsp)={3:F3} ms",
                consumed, consumedSeconds, dspElapsed, (consumedSeconds - dspElapsed) * 1000.0));
            LogStats("dspStep(ms)", _dspSteps, 1000.0);
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[M0] zeroDspDeltaFrames={0} / totalFrames={1}", _zeroDspDeltaFrames, _frame));
            LogStats("inputClockOffsetFromEvents(ms)", _inputOffsets, 1000.0);
            LogStats("inputClockOffsetAtFrame(ms)", _frameOffsets, 1.0);
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[M0] input events={0} | focusEvents={1} | deviceEvents={2} | focused={3} | keyboardEnabled={4} | keyboardPresent={5}",
                _eventCount, _focusEventCount, _deviceEventCount, Application.isFocused,
                kb != null && kb.enabled, kb != null));

            foreach (var kv in _segments)
            {
                var s = kv.Value;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[M0] segment(finalized) {0,-22} frames={1,-6} realtime={2,8:F3}s dsp={3,8:F3}s consumed={4,8:F3}s | dsp/rt={5:F4} consumed/rt={6:F4} | srcPaused={7} listenerPaused={8}",
                    s.Name, s.Frames, s.RealtimeSeconds, s.DspSeconds, s.ConsumedSeconds,
                    s.DspRatio, s.ConsumedRatio, s.PausesAudioSource ? 1 : 0, s.PausesListener ? 1 : 0));
            }

            if (_segName != null)
            {
                int rate = _outputSampleRate > 0 ? _outputSampleRate : 48000;
                double liveRealtime = Time.realtimeSinceStartupAsDouble - _segStartRealtime;
                double liveDsp = AudioSettings.dspTime - _segStartDsp;
                double liveConsumed = (ConsumedFrames - _segStartConsumed) / (double)rate;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[M0] segment(live)      {0,-22} frames={1,-6} realtime={2,8:F3}s dsp={3,8:F3}s consumed={4,8:F3}s | dsp/rt={5:F4} consumed/rt={6:F4}",
                    _segName, _segFrames, liveRealtime, liveDsp, liveConsumed,
                    liveRealtime > 0.0 ? liveDsp / liveRealtime : 0.0,
                    liveRealtime > 0.0 ? liveConsumed / liveRealtime : 0.0));
            }

            Debug.Log("[M0] csv(frame): " + _clockCsvPath);
            Debug.Log("[M0] csv(events): " + _eventCsvPath);
            Debug.Log("==========================================================");
        }

        /// <summary>計算 mean / median / std / p95 / min / max（unitScale：秒→毫秒用 1000）。</summary>
        private static void LogStats(string label, List<double> values, double unitScale)
        {
            if (values == null || values.Count == 0)
            {
                Debug.Log("[M0] " + label + ": no samples");
                return;
            }

            var sorted = new List<double>(values);
            sorted.Sort();

            double sum = 0.0;
            for (int i = 0; i < sorted.Count; i++)
            {
                sum += sorted[i];
            }

            double mean = sum / sorted.Count;
            double median = sorted[sorted.Count / 2];
            double p95 = sorted[(int)Math.Min(sorted.Count - 1, Math.Floor(sorted.Count * 0.95))];

            double variance = 0.0;
            for (int i = 0; i < sorted.Count; i++)
            {
                double d = sorted[i] - mean;
                variance += d * d;
            }

            double std = Math.Sqrt(variance / sorted.Count);

            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[M0] {0} | n={1} mean={2:F4} median={3:F4} std={4:F4} p95={5:F4} min={6:F4} max={7:F4}",
                label, sorted.Count, mean * unitScale, median * unitScale, std * unitScale,
                p95 * unitScale, sorted[0] * unitScale, sorted[sorted.Count - 1] * unitScale));
        }
    }
}
