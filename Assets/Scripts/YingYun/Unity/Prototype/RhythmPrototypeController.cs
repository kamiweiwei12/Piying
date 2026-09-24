using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Input;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Timing;
using YingYun.Rhythm.Unity.Config;
using YingYun.Rhythm.Unity.CustomSongs;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Prototype
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RhythmPrototypeController : MonoBehaviour
    {
        private const double PrototypeDurationSeconds = 180d;
        private const double CountdownLeadInSeconds = 3d;
        private const string TrialLightSongId = "trial-light";
        private const string XiangWangXingSongId = "xiang-wang-xing-special";
        private const string QingYuAnLanJieSongId = "qing-yu-an-lan-jie";
        private const string AudioOffsetPreference = "YingYun.AudioOffsetMs";
        private const string InputOffsetPreference = "YingYun.InputOffsetMs";

        [SerializeField] private AudioClip music;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private SongCatalogAsset songCatalog;
        [SerializeField] private double bpm = 120d;
        [SerializeField] private double audioOffsetSeconds;
        [SerializeField] private double inputOffsetSeconds;

        private readonly List<JudgmentResult> _frameResults = new List<JudgmentResult>(16);
        private readonly List<double> _hitErrorsMs = new List<double>(128);
        private readonly Dictionary<string, IPlayableSongDefinition> _availableSongs =
            new Dictionary<string, IPlayableSongDefinition>(StringComparer.Ordinal);
        private DspSongClock _clock;
        private ClockBridge _bridge;
        private InputSystemNoteInputSource _input;
        private JudgmentEngine _judgment;
        private RadialNotePresenter _presenter;
        private ShadowPuppetPresenter _puppet;
        private GameplayHudPresenter _hud;
        private DemoFlowPresenter _flow;
        private AudioSource _musicSource;
        private CalibrationSettings _calibration;
        private PlayDifficulty _difficulty = PlayDifficulty.Normal;
        private IPlayableSongDefinition _selectedSong;
        private CustomSongLibrary _customSongs;
        private SongTimingMap _timingMap;
        private double _resultTimeSec;
        private int _lastBeat = int.MinValue;
        private int _noteCount;
        private bool _isComplete;
        private bool _allNotesJudged;
        private bool _isPaused;

        public void Configure(AudioClip clip, InputActionAsset actions)
        {
            music = clip;
            inputActions = actions;
        }

        private void Awake()
        {
            if (songCatalog == null)
            {
                songCatalog = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog");
            }

            if (music == null || inputActions == null)
            {
                Debug.LogError("[M2] RhythmPrototypeController requires Music and Input Actions.", this);
                enabled = false;
                return;
            }

            _musicSource = GetComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = false;
            _musicSource.spatialBlend = 0f;

            _calibration = new CalibrationSettings(
                PlayerPrefs.GetFloat(AudioOffsetPreference, (float)(audioOffsetSeconds * 1000d)),
                PlayerPrefs.GetFloat(InputOffsetPreference, (float)(inputOffsetSeconds * 1000d)));
            _clock = new DspSongClock(_musicSource);
            _bridge = new ClockBridge();
            _bridge.Capture(AudioSettings.dspTime, Time.realtimeSinceStartupAsDouble);
            _input = new InputSystemNoteInputSource(inputActions, _bridge, _clock, _calibration.InputOffsetMs / 1000d);
            _presenter = GetComponent<RadialNotePresenter>();
            if (_presenter == null)
            {
                _presenter = gameObject.AddComponent<RadialNotePresenter>();
            }

            _puppet = GetComponent<ShadowPuppetPresenter>();
            if (_puppet == null)
            {
                _puppet = gameObject.AddComponent<ShadowPuppetPresenter>();
            }

            Debug.Log(string.Format(
                "[M6.5] shadow-play-ready | joints={0} | bambooRods={1} | motion=rod-driven-spring-joint",
                _puppet.JointCount,
                _puppet.RodCount));

            _hud = GetComponent<GameplayHudPresenter>();
            if (_hud == null)
            {
                _hud = gameObject.AddComponent<GameplayHudPresenter>();
            }
            _puppet.DanceStatusChanged += _hud.ShowDanceStatus;

            _flow = GetComponent<DemoFlowPresenter>();
            if (_flow == null)
            {
                _flow = gameObject.AddComponent<DemoFlowPresenter>();
            }

            _flow.PlayRequested += StartPerformance;
            _flow.CalibrationAdjusted += AdjustCalibration;
            _flow.ResumeRequested += ResumePerformance;
            _flow.RestartRequested += RestartFromPause;
            _flow.ReturnRequested += ShowSongSelection;
            _flow.CustomSongsRefreshRequested += RefreshCustomSongs;
            _flow.CustomSongsFolderRequested += OpenCustomSongsFolder;
            ConfigureSongSelection();
            _customSongs = GetComponent<CustomSongLibrary>();
            if (_customSongs == null) _customSongs = gameObject.AddComponent<CustomSongLibrary>();
            _customSongs.SongsChanged += RefreshSongSelection;
            _customSongs.StatusChanged += _flow.SetSongImportStatus;
            _flow.SetSongImportStatus($"自定义歌曲文件夹：{_customSongs.UserSongsPath}");
            _customSongs.BeginScan();
            ShowSongSelection();
        }

        private void Update()
        {
            if (_clock == null)
            {
                return;
            }

            HandleTransportControls();
            if (_flow.IsMenuVisible)
            {
                return;
            }

            if (_isPaused)
            {
                return;
            }

            if (_isComplete)
            {
                return;
            }

            _bridge.Capture(AudioSettings.dspTime, Time.realtimeSinceStartupAsDouble);
            _hud.TickSongTime(_clock.SongTime);
            while (_input.TryDequeue(out HitInput input))
            {
                _judgment.EnqueueInput(input);
            }

            _presenter.Tick(_clock.SongTime);
            _puppet.Tick(_clock.SongTime);
            _judgment.Advance(_frameResults);
            for (int i = 0; i < _frameResults.Count; i++)
            {
                JudgmentResult result = _frameResults[i];
                _presenter.OnJudged(result);
                _puppet.OnJudged(result);
                _hud.OnJudged(result);
                if (result.EventKind == JudgmentEventKind.HoldStarted)
                {
                    Debug.Log(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[M5] hold-started | note={0} | mask={1} | errorMs={2:F3}",
                        result.NoteId,
                        result.RequiredLanesMask,
                        result.ErrorMs));
                    continue;
                }

                if (result.EventKind == JudgmentEventKind.SegmentCompleted ||
                    result.EventKind == JudgmentEventKind.SegmentInterrupted)
                {
                    Debug.Log(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[M5] segment | id={0} | state={1}",
                        result.SegmentId,
                        result.EventKind));
                    continue;
                }

                if (result.EventKind != JudgmentEventKind.NoteJudged)
                {
                    continue;
                }

                if (result.Grade != JudgmentGrade.Miss)
                {
                    _hitErrorsMs.Add(result.ErrorMs);
                }

                Debug.Log(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "[M2] judgment | note={0} | grade={1} | errorMs={2:F3} | combo={3} | score={4} | accuracy={5:P2} | mask={6}",
                    result.NoteId,
                    result.Grade,
                    result.ErrorMs,
                    result.ComboAfter,
                    result.ScoreAfter,
                    result.AccuracyAfter,
                    result.RequiredLanesMask));
            }

            if (!_allNotesJudged && _judgment.JudgedNoteCount >= _noteCount)
            {
                _allNotesJudged = true;
            }

            if (_allNotesJudged && _clock.SongTime >= _resultTimeSec)
            {
                _isComplete = true;
                _clock.Pause();
                _hud.ShowResult();
                Debug.Log(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "[M4] result | score={0} | accuracy={1:P2} | maxCombo={2}",
                    _judgment.Score,
                    _judgment.Accuracy,
                    _judgment.MaxCombo));
            }

            LogMetronome();
        }

        public void TogglePause()
        {
            if (_clock == null)
            {
                return;
            }

            if (_clock.IsRunning)
            {
                _clock.Pause();
                _isPaused = true;
                _flow.ShowPause();
                Debug.Log(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "[M2] paused | songTime={0:F6} | dsp={1:F6}",
                    _clock.SongTime,
                    AudioSettings.dspTime));
            }
            else if (_isPaused)
            {
                _clock.Resume();
                _isPaused = false;
                _flow.HidePause();
                Debug.Log(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "[M2] resumed | songTime={0:F6} | dsp={1:F6} | pausedTotal={2:F6}",
                    _clock.SongTime,
                    AudioSettings.dspTime,
                    _clock.PausedTotal));
            }
        }

        public void Restart()
        {
            if (_clock == null)
            {
                return;
            }

            _input?.Clear();
            _frameResults.Clear();
            _hitErrorsMs.Clear();
            _lastBeat = int.MinValue;
            _isComplete = false;
            _allNotesJudged = false;
            _isPaused = false;
            _flow.HidePause();
            NoteData[] notes;
            DancePhrase[] dance;
            AudioClip activeMusic;
            if (_selectedSong != null && _selectedSong.HasAuthoredCharts)
            {
                _selectedSong.ValidateOrThrow();
                _timingMap = _selectedSong.CreateTimingMap();
                notes = _selectedSong.GetNotes(_difficulty);
                dance = DanceChoreography.CreateAuthored(notes, _timingMap, _selectedSong.GetDanceCues());
                activeMusic = _selectedSong.Music;
                _resultTimeSec = _selectedSong.PlayableEndSec;
            }
            else
            {
                _timingMap = null;
                notes = PrototypeDanceChart.Create(bpm, PrototypeDurationSeconds, _difficulty);
                dance = DanceChoreography.Create(notes, bpm, PrototypeDurationSeconds);
                activeMusic = music;
                _resultTimeSec = PrototypeDurationSeconds;
            }

            _noteCount = notes.Length;
            _judgment = new JudgmentEngine(notes, TimingConfig.Prototype, _clock);
            _presenter.Begin(notes);
            _puppet.Begin(dance);
            _hud.Begin(notes.Length, DifficultyConfig.Prototype);
            _clock.Schedule(activeMusic, CountdownLeadInSeconds, _calibration.AudioOffsetMs / 1000d);

            for (int i = 0; i < dance.Length; i++)
            {
                Debug.Log($"[M8] {dance[i].Display}");
            }

            Debug.Log(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "[M9] scheduled | song={0} | difficulty={1} | dspStart={2:F6} | leadIn={3:F3}s | clip={4} | notes={5} | phrases={6} | audioOffsetMs={7:F1} | inputOffsetMs={8:F1}",
                _selectedSong != null ? _selectedSong.SongId : TrialLightSongId,
                _difficulty,
                _clock.DspStart,
                CountdownLeadInSeconds,
                activeMusic.name,
                notes.Length,
                dance.Length,
                _calibration.AudioOffsetMs,
                _calibration.InputOffsetMs));
        }

        public void StartPerformance(string songId, PlayDifficulty difficulty)
        {
            _availableSongs.TryGetValue(songId, out IPlayableSongDefinition requested);
            if (requested == null || (songId != TrialLightSongId && !requested.HasAuthoredCharts))
            {
                Debug.LogError($"[M9] Song is not playable: {songId}", this);
                return;
            }

            _selectedSong = requested;
            _difficulty = difficulty;
            _flow.HideMenu();
            _flow.HidePause();
            _hud.SetVisible(true);
            Restart();
        }

        public void ResumePerformance()
        {
            if (_isPaused)
            {
                TogglePause();
            }
        }

        public void RestartFromPause()
        {
            _flow.HidePause();
            Restart();
        }

        public void ShowSongSelection()
        {
            _musicSource?.Stop();
            _input?.Clear();
            _isComplete = true;
            _isPaused = false;
            _flow.HidePause();
            _presenter.Begin(Array.Empty<NoteData>());
            _puppet.Begin();
            _hud.SetVisible(false);
            _flow.ShowMenu(_calibration.AudioOffsetMs, _calibration.InputOffsetMs);
        }

        public void AdjustCalibration(double audioDeltaMs, double inputDeltaMs)
        {
            _calibration.AdjustAudio(audioDeltaMs);
            _calibration.AdjustInput(inputDeltaMs);
            PlayerPrefs.SetFloat(AudioOffsetPreference, (float)_calibration.AudioOffsetMs);
            PlayerPrefs.SetFloat(InputOffsetPreference, (float)_calibration.InputOffsetMs);
            PlayerPrefs.Save();
            _clock.AudioOffsetSeconds = _calibration.AudioOffsetMs / 1000d;
            _input.InputOffsetSeconds = _calibration.InputOffsetMs / 1000d;
            _flow.RefreshCalibration(_calibration.AudioOffsetMs, _calibration.InputOffsetMs);
        }

        private void OnDestroy()
        {
            if (_puppet != null && _hud != null)
                _puppet.DanceStatusChanged -= _hud.ShowDanceStatus;
            if (_flow != null)
            {
                _flow.PlayRequested -= StartPerformance;
                _flow.CalibrationAdjusted -= AdjustCalibration;
                _flow.ResumeRequested -= ResumePerformance;
                _flow.RestartRequested -= RestartFromPause;
                _flow.ReturnRequested -= ShowSongSelection;
                _flow.CustomSongsRefreshRequested -= RefreshCustomSongs;
                _flow.CustomSongsFolderRequested -= OpenCustomSongsFolder;
            }
            if (_customSongs != null)
            {
                _customSongs.SongsChanged -= RefreshSongSelection;
                _customSongs.StatusChanged -= _flow.SetSongImportStatus;
            }
            _input?.Dispose();
        }

        private void HandleTransportControls()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (_flow.IsMenuVisible)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) StartPerformance(_flow.SelectedSongId, PlayDifficulty.Easy);
                else if (keyboard.digit2Key.wasPressedThisFrame) StartPerformance(_flow.SelectedSongId, PlayDifficulty.Normal);
                else if (keyboard.digit3Key.wasPressedThisFrame) StartPerformance(_flow.SelectedSongId, PlayDifficulty.Hard);
                else if (keyboard.leftBracketKey.wasPressedThisFrame) AdjustCalibration(-5d, 0d);
                else if (keyboard.rightBracketKey.wasPressedThisFrame) AdjustCalibration(5d, 0d);
                else if (keyboard.minusKey.wasPressedThisFrame) AdjustCalibration(0d, -5d);
                else if (keyboard.equalsKey.wasPressedThisFrame) AdjustCalibration(0d, 5d);
                return;
            }

            if (_flow.IsPauseVisible)
            {
                if (keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                {
                    ResumePerformance();
                }
                return;
            }

            if (_isComplete && keyboard.enterKey.wasPressedThisFrame)
            {
                ShowSongSelection();
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }

            if (!_isComplete && (keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                TogglePause();
            }
        }

        private void LogMetronome()
        {
            double songTime = _clock.SongTime;
            if (songTime < 0d)
            {
                return;
            }

            int beat = (int)Math.Floor(_timingMap != null
                ? _timingMap.SecondsToBeat(songTime)
                : songTime * bpm / 60d);
            if (beat == _lastBeat)
            {
                return;
            }

            _lastBeat = beat;
            Debug.Log(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "[M2] beat={0} | songTime={1:F6} | dsp={2:F6} | bridgeMs={3:F3} | inputMedianMs={4}",
                beat,
                songTime,
                AudioSettings.dspTime,
                _bridge.DspMinusRealtime * 1000d,
                MedianText()));
        }

        private string MedianText()
        {
            if (_hitErrorsMs.Count == 0)
            {
                return "n/a";
            }

            var copy = _hitErrorsMs.ToArray();
            Array.Sort(copy);
            int middle = copy.Length / 2;
            double median = copy.Length % 2 == 0
                ? (copy[middle - 1] + copy[middle]) * 0.5d
                : copy[middle];
            return median.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void ConfigureSongSelection()
        {
            if (songCatalog == null)
            {
                throw new InvalidOperationException("Song catalog is required.");
            }

            SongDefinitionAsset trialLight = songCatalog.Find(TrialLightSongId);
            SongDefinitionAsset xiangWangXing = songCatalog.Find(XiangWangXingSongId);
            SongDefinitionAsset qingYuAnLanJie = songCatalog.Find(QingYuAnLanJieSongId);
            if (trialLight == null || xiangWangXing == null || qingYuAnLanJie == null ||
                !xiangWangXing.HasAuthoredCharts || !qingYuAnLanJie.HasAuthoredCharts)
            {
                throw new InvalidOperationException("三首歌曲的可玩资料必须存在。");
            }

            _selectedSong = trialLight;
            RefreshSongSelection();
        }

        private void RefreshSongSelection()
        {
            if (_flow == null || songCatalog == null) return;

            _availableSongs.Clear();
            var entries = new List<SongMenuEntry>();
            for (int i = 0; i < songCatalog.Songs.Count; i++)
            {
                SongDefinitionAsset song = songCatalog.Songs[i];
                if (song == null || (song.SongId != TrialLightSongId && !song.HasAuthoredCharts)) continue;
                _availableSongs.Add(song.SongId, song);
                entries.Add(new SongMenuEntry(song.SongId, song.Title, song.Artist));
            }

            if (_customSongs != null)
            {
                for (int i = 0; i < _customSongs.Songs.Count; i++)
                {
                    RuntimeSongDefinition song = _customSongs.Songs[i];
                    _availableSongs[song.SongId] = song;
                    entries.Add(new SongMenuEntry(song.SongId, song.Title, song.Artist));
                }
            }

            string selectedId = _selectedSong != null && _availableSongs.ContainsKey(_selectedSong.SongId)
                ? _selectedSong.SongId
                : TrialLightSongId;
            _selectedSong = _availableSongs[selectedId];
            _flow.ConfigureSongs(entries, selectedId);
        }

        private void RefreshCustomSongs()
        {
            _customSongs?.BeginScan();
        }

        private void OpenCustomSongsFolder()
        {
            _customSongs?.OpenSongsFolder();
        }
    }
}
