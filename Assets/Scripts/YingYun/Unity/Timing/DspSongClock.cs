using System;
using UnityEngine;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Timing
{
    /// <summary>以 AudioSettings.dspTime 為唯一基準的歌曲時鐘。</summary>
    public sealed class DspSongClock : ISongClock
    {
        private readonly AudioSource _musicSource;
        private double _dspStart;
        private double _pausedAtDsp;
        private double _pausedTotal;
        private double _audioOffsetSeconds;
        private bool _isScheduled;
        private bool _isPaused;

        public DspSongClock(AudioSource musicSource)
        {
            _musicSource = musicSource != null
                ? musicSource
                : throw new ArgumentNullException(nameof(musicSource));
        }

        public double SongTime => DspToSongTime(AudioSettings.dspTime);
        public double DspStart => _dspStart;
        public double PausedTotal => _pausedTotal;
        public bool IsRunning => _isScheduled && !_isPaused;
        public double AudioOffsetSeconds
        {
            get => _audioOffsetSeconds;
            set => _audioOffsetSeconds = value;
        }

        public void Schedule(AudioClip clip, double leadInSeconds, double audioOffsetSeconds = 0d)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (leadInSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(leadInSeconds));
            }

            _musicSource.Stop();
            _musicSource.clip = clip;
            _audioOffsetSeconds = audioOffsetSeconds;
            _pausedTotal = 0d;
            _isPaused = false;
            _dspStart = AudioSettings.dspTime + leadInSeconds;
            _musicSource.PlayScheduled(_dspStart);
            _isScheduled = true;
        }

        public void Pause()
        {
            if (!_isScheduled || _isPaused)
            {
                return;
            }

            _pausedAtDsp = AudioSettings.dspTime;
            _musicSource.Pause();
            _isPaused = true;
        }

        public void Resume()
        {
            if (!_isScheduled || !_isPaused)
            {
                return;
            }

            _pausedTotal += AudioSettings.dspTime - _pausedAtDsp;
            _musicSource.UnPause();
            _isPaused = false;
        }

        public double DspToSongTime(double dspTime)
        {
            double paused = _pausedTotal;
            if (_isPaused)
            {
                paused += Math.Max(0d, dspTime - _pausedAtDsp);
            }

            return dspTime - _dspStart - paused - _audioOffsetSeconds;
        }
    }
}
