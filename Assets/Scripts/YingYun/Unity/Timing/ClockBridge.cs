using System;

namespace YingYun.Rhythm.Timing
{
    /// <summary>將 Input System 的 realtime 時間戳映射至 DSP 與 song time。</summary>
    public sealed class ClockBridge
    {
        private readonly double _smoothing;
        private double _dspMinusRealtime;
        private bool _hasSample;

        public ClockBridge(double smoothing = 0.1d)
        {
            if (smoothing <= 0d || smoothing > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(smoothing));
            }

            _smoothing = smoothing;
        }

        public double DspMinusRealtime => _dspMinusRealtime;
        public bool HasSample => _hasSample;

        public void Capture(double dspTime, double realtimeSinceStartup)
        {
            double sample = dspTime - realtimeSinceStartup;
            if (!_hasSample)
            {
                _dspMinusRealtime = sample;
                _hasSample = true;
                return;
            }

            _dspMinusRealtime += (sample - _dspMinusRealtime) * _smoothing;
        }

        public double InputTimeToDsp(double inputEventTime)
        {
            if (!_hasSample)
            {
                throw new InvalidOperationException("ClockBridge requires at least one clock sample.");
            }

            return inputEventTime + _dspMinusRealtime;
        }

        public double InputTimeToSong(
            double inputEventTime,
            DspSongClock songClock,
            double inputOffsetSeconds)
        {
            if (songClock == null)
            {
                throw new ArgumentNullException(nameof(songClock));
            }

            return songClock.DspToSongTime(InputTimeToDsp(inputEventTime)) - inputOffsetSeconds;
        }
    }
}
