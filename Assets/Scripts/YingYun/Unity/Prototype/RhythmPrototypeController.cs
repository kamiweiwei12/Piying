using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Input;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Timing;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Prototype
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RhythmPrototypeController : MonoBehaviour
    {
        private const double PrototypeDurationSeconds = 180d;

        [SerializeField] private AudioClip music;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private double bpm = 120d;
        [SerializeField] private double leadInSeconds = 1d;
        [SerializeField] private double audioOffsetSeconds;
        [SerializeField] private double inputOffsetSeconds;

        private readonly List<JudgmentResult> _frameResults = new List<JudgmentResult>(16);
        private readonly List<double> _hitErrorsMs = new List<double>(128);
        private DspSongClock _clock;
        private ClockBridge _bridge;
        private InputSystemNoteInputSource _input;
        private JudgmentEngine _judgment;
        private RadialNotePresenter _presenter;
        private ShadowPuppetPresenter _puppet;
        private GameplayHudPresenter _hud;
        private int _lastBeat = int.MinValue;
        private int _noteCount;
        private bool _isComplete;

        public void Configure(AudioClip clip, InputActionAsset actions)
        {
            music = clip;
            inputActions = actions;
        }

        private void Awake()
        {
            if (music == null || inputActions == null)
            {
                Debug.LogError("[M2] RhythmPrototypeController requires Music and Input Actions.", this);
                enabled = false;
                return;
            }

            var source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;

            _clock = new DspSongClock(source);
            _bridge = new ClockBridge();
            _bridge.Capture(AudioSettings.dspTime, Time.realtimeSinceStartupAsDouble);
            _input = new InputSystemNoteInputSource(inputActions, _bridge, _clock, inputOffsetSeconds);
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

            Restart();
        }

        private void Update()
        {
            if (_clock == null)
            {
                return;
            }

            HandleTransportControls();
            if (_isComplete)
            {
                return;
            }

            _bridge.Capture(AudioSettings.dspTime, Time.realtimeSinceStartupAsDouble);
            while (_input.TryDequeue(out HitInput input))
            {
                _puppet.OnInput(input);
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

            if (!_isComplete && _judgment.JudgedNoteCount >= _noteCount)
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
                Debug.Log(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "[M2] paused | songTime={0:F6} | dsp={1:F6}",
                    _clock.SongTime,
                    AudioSettings.dspTime));
            }
            else
            {
                _clock.Resume();
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
            NoteData[] notes = PrototypeDanceChart.Create(bpm, PrototypeDurationSeconds);
            _noteCount = notes.Length;
            _judgment = new JudgmentEngine(notes, TimingConfig.Prototype, _clock);
            _presenter.Begin(notes);
            _puppet.Begin();
            _hud.Begin(notes.Length, DifficultyConfig.Prototype);
            _clock.Schedule(music, leadInSeconds, audioOffsetSeconds);

            Debug.Log(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "[M2] scheduled | dspStart={0:F6} | leadIn={1:F3}s | bpm={2:F3} | clip={3} | loop=true | controls=P pause/resume,R restart",
                _clock.DspStart,
                leadInSeconds,
                bpm,
                music.name));
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }

        private void HandleTransportControls()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }

            if (!_isComplete && keyboard.pKey.wasPressedThisFrame)
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

            int beat = (int)Math.Floor(songTime * bpm / 60d);
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
    }
}
