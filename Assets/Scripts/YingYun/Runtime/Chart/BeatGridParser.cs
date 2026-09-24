using System;
using System.Collections.Generic;
using System.Globalization;

namespace YingYun.Rhythm.Chart
{
    /// <summary>Parses Beat This TSV rows into the project's variable-tempo timing map.</summary>
    public static class BeatGridParser
    {
        public static SongTimingPoint[] Parse(IEnumerable<string> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var times = new List<double>();
            var barBeats = new List<int>();
            int lineNumber = 0;
            foreach (string line in lines)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] fields = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length != 2 ||
                    !double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double timeSec) ||
                    !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int barBeat) ||
                    timeSec < 0d || barBeat < 1 || barBeat > 16)
                {
                    throw new FormatException($"Invalid Beat This row {lineNumber}: {line}");
                }

                if (times.Count > 0 && timeSec <= times[times.Count - 1])
                {
                    throw new FormatException($"Beat This timestamps must increase at row {lineNumber}.");
                }

                if (barBeats.Count > 0 && barBeat != 1 && barBeat != barBeats[barBeats.Count - 1] + 1)
                {
                    throw new FormatException($"Beat-in-bar must increment or restart at row {lineNumber}.");
                }

                times.Add(timeSec);
                barBeats.Add(barBeat);
            }

            if (times.Count < 2 || barBeats[0] != 1)
            {
                throw new FormatException("Beat This data needs at least two beats and must begin at a downbeat.");
            }

            var points = new SongTimingPoint[times.Count];
            int barStart = 0;
            int previousCompleteMeter = 4;
            while (barStart < barBeats.Count)
            {
                int nextBar = barStart + 1;
                while (nextBar < barBeats.Count && barBeats[nextBar] != 1) nextBar++;

                int detectedMeter = barBeats[nextBar - 1];
                if (nextBar == barBeats.Count && detectedMeter < 2)
                {
                    detectedMeter = previousCompleteMeter;
                }
                else
                {
                    previousCompleteMeter = detectedMeter;
                }

                for (int i = barStart; i < nextBar; i++)
                {
                    double interval = i + 1 < times.Count
                        ? times[i + 1] - times[i]
                        : times[i] - times[i - 1];
                    points[i] = new SongTimingPoint(times[i], i, 60d / interval, detectedMeter);
                }

                barStart = nextBar;
            }

            return points;
        }
    }
}
