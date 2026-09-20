using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using YingYun.Rhythm.Input;
using YingYun.Rhythm.Judgment;
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
        private int _lastBeat = int.MinValue;

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

            Restart();
        }

        private void Update()
        {
            if (_clock == null)
            {
                return;
            }

            HandleTransportControls();

            _bridge.Capture(AudioSettings.dspTime, Time.realtimeSinceStartupAsDouble);
            while (_input.TryDequeue(out HitInput input))
            {
                _judgment.EnqueueInput(input);
            }

            _presenter.Tick(_clock.SongTime);
            _judgment.Advance(_frameResults);
            for (int i = 0; i < _frameResults.Count; i++)
            {
                JudgmentResult result = _frameResults[i];
                _presenter.OnJudged(result);
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
                    "[M2] judgment | note={0} | grade={1} | errorMs={2:F3} | combo={3} | score={4} | accuracy={5:P2}",
                    result.NoteId,
                    result.Grade,
                    result.ErrorMs,
                    result.ComboAfter,
                    result.ScoreAfter,
                    result.AccuracyAfter));
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
            NoteData[] notes = CreatePrototypeChart();
            _judgment = new JudgmentEngine(notes, TimingConfig.Prototype, _clock);
            _presenter.Begin(notes);
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

            if (keyboard.pKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        private NoteData[] CreatePrototypeChart()
        {
            double beatDuration = 60d / bpm;
            int noteCount = (int)Math.Floor(PrototypeDurationSeconds / beatDuration);
            var notes = new NoteData[noteCount];
            for (int i = 0; i < noteCount; i++)
            {
                notes[i] = new NoteData(i + 1, "tap", i % 6, i * beatDuration, segmentId: i / 16);
            }

            return notes;
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
