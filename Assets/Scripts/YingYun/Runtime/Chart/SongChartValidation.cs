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
            PlayDifficulty difficulty,
            SongTimingMap timing = null)
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
                        "Chord notes must use an allowed two-key T/Y/U/G/H/J combination.",
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

            if (timing != null)
            {
                ValidatePlayableLayout(notes, difficulty, timing);
            }
        }

        /// <summary>验证六键空间布局、名角密度及八拍乐句的长按／组合互斥规则。</summary>
        public static void ValidatePlayableLayout(
            IReadOnlyList<NoteData> notes,
            PlayDifficulty difficulty,
            SongTimingMap timing)
        {
            if (notes == null) throw new ArgumentNullException(nameof(notes));
            if (timing == null) throw new ArgumentNullException(nameof(timing));

            const double tolerance = 0.000001d;
            int index = 0;
            while (index < notes.Count)
            {
                double timeSec = notes[index].TimeSec;
                int simultaneousNotes = 0;
                int simultaneousLanes = 0;
                while (index < notes.Count && Math.Abs(notes[index].TimeSec - timeSec) <= tolerance)
                {
                    simultaneousNotes++;
                    simultaneousLanes |= notes[index].RequiredLanesMask;
                    index++;
                }

                if (simultaneousNotes > 2 || CountBits(simultaneousLanes) > 2)
                {
                    throw new ArgumentException("A beat position may require at most two simultaneous inputs.", nameof(notes));
                }
            }

            foreach (IGrouping<int, NoteData> phrase in notes.GroupBy(note => note.SegmentId))
            {
                bool containsHold = phrase.Any(note => note.Kind == NoteKind.Hold);
                bool containsChord = phrase.Any(note => note.IsChord);
                if (containsHold && containsChord)
                {
                    throw new ArgumentException("A phrase may contain holds or chords, but not both.", nameof(notes));
                }

                if (difficulty == PlayDifficulty.Hard && phrase.Count() > 10)
                {
                    throw new ArgumentException("Hard charts may contain at most ten judgment points per eight-beat phrase.", nameof(notes));
                }
            }

            double previousChordBeat = double.NegativeInfinity;
            for (int i = 0; i < notes.Count; i++)
            {
                if (!notes[i].IsChord) continue;
                double chordBeat = timing.SecondsToBeat(notes[i].TimeSec);
                if (chordBeat - previousChordBeat < 2d - tolerance)
                {
                    throw new ArgumentException("Chord starts must be separated by at least two beats.", nameof(notes));
                }
                previousChordBeat = chordBeat;
            }

            if (difficulty != PlayDifficulty.Hard)
            {
                return;
            }

            int previousLane = -1;
            int direction = 0;
            int directionalSteps = 0;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].IsChord)
                {
                    previousLane = -1;
                    direction = 0;
                    directionalSteps = 0;
                    continue;
                }

                int lane = notes[i].Lane;
                if (previousLane >= 0)
                {
                    int step = lane == (previousLane + 1) % KeyboardChordLayout.LaneCount ? 1 :
                        lane == (previousLane + KeyboardChordLayout.LaneCount - 1) % KeyboardChordLayout.LaneCount ? -1 : 0;
                    if (step != 0 && step == direction)
                    {
                        directionalSteps++;
                    }
                    else
                    {
                        direction = step;
                        directionalSteps = step == 0 ? 0 : 1;
                    }

                    if (directionalSteps > 3)
                    {
                        throw new ArgumentException("Charts may not use a mechanical one-way six-lane sweep.", nameof(notes));
                    }
                }

                previousLane = lane;
            }
        }

        private static int CountBits(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }
            return count;
        }
    }
}
