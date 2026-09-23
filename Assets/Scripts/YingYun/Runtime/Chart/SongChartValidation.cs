using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Chart
{
    public readonly struct SongPlayableRange
    {
        public SongPlayableRange(double startSec, double endSec)
        {
            if (startSec < 0d || endSec <= startSec)
            {
                throw new ArgumentOutOfRangeException(nameof(endSec));
            }

            StartSec = startSec;
            EndSec = endSec;
        }

        public double StartSec { get; }
        public double EndSec { get; }
    }

    /// <summary>譜面載入前的資料驗證；不依賴 Unity 或執行期音訊狀態。</summary>
    public static class SongChartValidation
    {
        public static void ValidateNotes(
            IReadOnlyList<NoteData> notes,
            double clipDurationSec,
            SongPlayableRange playableRange)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            if (clipDurationSec <= 0d || playableRange.EndSec > clipDurationSec)
            {
                throw new ArgumentOutOfRangeException(nameof(clipDurationSec));
            }

            double previousTime = double.NegativeInfinity;
            var noteIds = new HashSet<int>();
            for (int i = 0; i < notes.Count; i++)
            {
                NoteData note = notes[i];
                if (!noteIds.Add(note.Id))
                {
                    throw new ArgumentException("Note ids must be unique.", nameof(notes));
                }

                if (note.TimeSec < previousTime)
                {
                    throw new ArgumentException("Notes must be ordered by time.", nameof(notes));
                }

                if (note.TimeSec < playableRange.StartSec || note.EndTimeSec > playableRange.EndSec)
                {
                    throw new ArgumentException("Notes must remain inside the playable range.", nameof(notes));
                }

                previousTime = note.TimeSec;
            }
        }
    }
}
