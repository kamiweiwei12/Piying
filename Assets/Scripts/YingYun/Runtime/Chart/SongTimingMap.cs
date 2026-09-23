using System;
using System.Collections.Generic;

namespace YingYun.Rhythm.Chart
{
    /// <summary>一個速度段的起點。時間以歌曲秒數表示，拍數可包含小數。</summary>
    public readonly struct SongTimingPoint
    {
        public SongTimingPoint(double timeSec, double beat, double bpm, int beatsPerBar = 4)
        {
            if (timeSec < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(timeSec));
            }

            if (beat < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(beat));
            }

            if (bpm <= 0d || double.IsNaN(bpm) || double.IsInfinity(bpm))
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            if (beatsPerBar <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerBar));
            }

            TimeSec = timeSec;
            Beat = beat;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
        }

        public double TimeSec { get; }
        public double Beat { get; }
        public double Bpm { get; }
        public int BeatsPerBar { get; }
    }

    /// <summary>歌曲秒數、拍數與小節位置的唯一換算來源，支援分段變速。</summary>
    public sealed class SongTimingMap
    {
        private const double ContinuityTolerance = 0.000001d;
        private readonly SongTimingPoint[] _points;

        public SongTimingMap(IReadOnlyList<SongTimingPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count == 0)
            {
                throw new ArgumentException("At least one timing point is required.", nameof(points));
            }

            _points = new SongTimingPoint[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                SongTimingPoint point = points[i];
                if (i > 0)
                {
                    SongTimingPoint previous = _points[i - 1];
                    if (point.TimeSec <= previous.TimeSec)
                    {
                        throw new ArgumentException("Timing points must be strictly ordered by time.", nameof(points));
                    }

                    double expectedBeat = previous.Beat + ((point.TimeSec - previous.TimeSec) * previous.Bpm / 60d);
                    if (Math.Abs(point.Beat - expectedBeat) > ContinuityTolerance)
                    {
                        throw new ArgumentException("Timing point beat must be continuous with the previous segment.", nameof(points));
                    }
                }

                _points[i] = point;
            }
        }

        public int Count => _points.Length;

        public SongTimingPoint this[int index] => _points[index];

        public double SecondsToBeat(double songTimeSec)
        {
            int index = FindTimeSegment(songTimeSec);
            SongTimingPoint point = _points[index];
            return point.Beat + ((songTimeSec - point.TimeSec) * point.Bpm / 60d);
        }

        public double BeatToSeconds(double beat)
        {
            int index = FindBeatSegment(beat);
            SongTimingPoint point = _points[index];
            return point.TimeSec + ((beat - point.Beat) * 60d / point.Bpm);
        }

        public SongTimingPoint GetPointAtTime(double songTimeSec)
        {
            return _points[FindTimeSegment(songTimeSec)];
        }

        private int FindTimeSegment(double songTimeSec)
        {
            int low = 0;
            int high = _points.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                if (_points[middle].TimeSec <= songTimeSec)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return Math.Max(0, high);
        }

        private int FindBeatSegment(double beat)
        {
            int low = 0;
            int high = _points.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                if (_points[middle].Beat <= beat)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return Math.Max(0, high);
        }
    }
}
