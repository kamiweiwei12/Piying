using System;
using System.Collections.Generic;
using System.Linq;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Scoring;

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

        /// <summary>三難度逐級增加操作類型：Easy 只點按，Normal 加長按，Hard 再加合奏。</summary>
        public static void ValidateDifficultyFeatures(
            IReadOnlyList<NoteData> notes,
            PlayDifficulty difficulty)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            if (notes.Count == 0)
            {
                throw new ArgumentException("Difficulty charts must contain at least one note.", nameof(notes));
            }

            bool hasHold = notes.Any(note => note.Kind == NoteKind.Hold);
            bool hasChord = notes.Any(note => note.IsChord);
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].IsChord && !KeyboardChordLayout.IsAllowed(notes[i].RequiredLanesMask))
                {
                    throw new ArgumentException(
                        "Chord notes may not use Q+D, W+S, or E+A.",
                        nameof(notes));
                }
            }

            switch (difficulty)
            {
                case PlayDifficulty.Easy:
                    if (hasHold || hasChord)
                    {
                        throw new ArgumentException("Easy charts may contain only single-lane tap notes.", nameof(notes));
                    }
                    break;
                case PlayDifficulty.Normal:
                    if (!hasHold || hasChord)
                    {
                        throw new ArgumentException("Normal charts require holds and may not contain chords.", nameof(notes));
                    }
                    break;
                case PlayDifficulty.Hard:
                    if (!hasHold || !hasChord)
                    {
                        throw new ArgumentException("Hard charts require both holds and chords.", nameof(notes));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty));
            }
        }
    }
}
