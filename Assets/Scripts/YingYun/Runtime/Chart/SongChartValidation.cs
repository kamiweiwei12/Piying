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
        public const int AccessibleHardMinEventsPerFullPhrase = 4;
        public const int AccessibleHardMaxEventsPerPhrase = 6;

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

            var singleTapsPerBeat = new Dictionary<int, int>();
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].Kind != NoteKind.Tap || notes[i].IsChord) continue;
                double beat = timing.SecondsToBeat(notes[i].TimeSec);
                int beatIndex = (int)Math.Floor(beat + tolerance);
                singleTapsPerBeat.TryGetValue(beatIndex, out int count);
                count++;
                if (count > 2)
                {
                    throw new ArgumentException("A beat may contain at most two single tap notes.", nameof(notes));
                }
                singleTapsPerBeat[beatIndex] = count;
            }

            foreach (IGrouping<int, NoteData> phrase in notes.GroupBy(note => note.SegmentId))
            {
                bool containsHold = phrase.Any(note => note.Kind == NoteKind.Hold);
                bool containsChord = phrase.Any(note => note.IsChord);
                if (containsHold && containsChord)
                {
                    throw new ArgumentException("A phrase may contain holds or chords, but not both.", nameof(notes));
                }

                if (difficulty == PlayDifficulty.Hard && phrase.Count() > AccessibleHardMaxEventsPerPhrase)
                {
                    throw new ArgumentException("Hard charts may contain at most six judgment points per eight-beat phrase.", nameof(notes));
                }
            }

            double previousChordBeat = double.NegativeInfinity;
            double minimumChordGap = difficulty == PlayDifficulty.Hard && timing.Count >= 9 ? 16d : 2d;
            for (int i = 0; i < notes.Count; i++)
            {
                if (!notes[i].IsChord) continue;
                double chordBeat = timing.SecondsToBeat(notes[i].TimeSec);
                if (chordBeat - previousChordBeat < minimumChordGap - tolerance)
                {
                    throw new ArgumentException(
                        $"Chord starts must be separated by at least {minimumChordGap:0} beats.",
                        nameof(notes));
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

            if (timing.Count >= 9)
            {
                ValidateAccessibleHardChart(notes, timing);
            }
        }

        private static void ValidateAccessibleHardChart(
            IReadOnlyList<NoteData> notes,
            SongTimingMap timing)
        {
            const double tolerance = 0.000001d;
            var recentRhythms = new Queue<string>(4);
            foreach (IGrouping<int, NoteData> phrase in notes
                .GroupBy(note => note.SegmentId)
                .OrderBy(group => group.Key))
            {
                int phraseStart = phrase.Key * 8;
                bool fullPhrase = phraseStart + 7 < timing.Count;
                NoteData[] phraseNotes = phrase.OrderBy(note => note.TimeSec).ToArray();
                if (fullPhrase && phraseNotes.Length < AccessibleHardMinEventsPerFullPhrase)
                {
                    throw new ArgumentException("A full Hard phrase needs at least four judgment points.", nameof(notes));
                }

                int chordCount = phraseNotes.Count(note => note.IsChord);
                if (chordCount > 1)
                {
                    throw new ArgumentException("A Hard phrase may contain at most one chord.", nameof(notes));
                }

                if (!fullPhrase) continue;

                var occupiedWholeBeats = new HashSet<int>();
                var rhythmUnits = new int[phraseNotes.Length];
                int halfBeatCount = 0;
                for (int i = 0; i < phraseNotes.Length; i++)
                {
                    double localBeat = timing.SecondsToBeat(phraseNotes[i].TimeSec) - phraseStart;
                    int halfUnits = (int)Math.Round(localBeat * 2d);
                    if (Math.Abs(localBeat * 2d - halfUnits) > tolerance)
                    {
                        throw new ArgumentException("Hard notes must stay on whole-beat or half-beat snaps.", nameof(notes));
                    }

                    rhythmUnits[i] = halfUnits;
                    if ((halfUnits & 1) == 0)
                    {
                        occupiedWholeBeats.Add(halfUnits / 2);
                    }
                    else
                    {
                        halfBeatCount++;
                    }
                }

                if (8 - occupiedWholeBeats.Count < 2)
                {
                    throw new ArgumentException("A full Hard phrase must leave at least two whole beats empty.", nameof(notes));
                }
                if (halfBeatCount > 1)
                {
                    throw new ArgumentException("A Hard phrase may contain at most one half-beat note.", nameof(notes));
                }

                string rhythm = string.Join(",", rhythmUnits);
                if (recentRhythms.Contains(rhythm))
                {
                    throw new ArgumentException("A Hard rhythm skeleton may not repeat within four phrases.", nameof(notes));
                }
                Remember(recentRhythms, rhythm, 4);
            }

            int uninterruptedEvents = 1;
            for (int i = 1; i < notes.Count; i++)
            {
                double previousBeat = timing.SecondsToBeat(notes[i - 1].TimeSec);
                double currentBeat = timing.SecondsToBeat(notes[i].TimeSec);
                uninterruptedEvents = currentBeat - previousBeat <= 1d + tolerance
                    ? uninterruptedEvents + 1
                    : 1;
                if (uninterruptedEvents > 3)
                {
                    throw new ArgumentException("Hard charts may not require more than three uninterrupted events.", nameof(notes));
                }
            }

            var recentChordMasks = new Queue<int>(4);
            for (int i = 0; i < notes.Count; i++)
            {
                NoteData note = notes[i];
                double beat = timing.SecondsToBeat(note.TimeSec);
                if (note.IsChord)
                {
                    double localBeat = beat - (note.SegmentId * 8);
                    if (Math.Abs(localBeat - 4d) > tolerance)
                    {
                        throw new ArgumentException("Hard chords may appear only on the middle strong beat of a phrase.", nameof(notes));
                    }
                    if (recentChordMasks.Contains(note.RequiredLanesMask))
                    {
                        throw new ArgumentException("A chord pair may not repeat within four chord accents.", nameof(notes));
                    }
                    if (i > 0 && beat - timing.SecondsToBeat(notes[i - 1].TimeSec) < 1d - tolerance)
                    {
                        throw new ArgumentException("A Hard chord needs at least one beat from the preceding event.", nameof(notes));
                    }
                    if (i + 1 < notes.Count && timing.SecondsToBeat(notes[i + 1].TimeSec) - beat < 1d - tolerance)
                    {
                        throw new ArgumentException("A Hard chord needs at least one beat from the following event.", nameof(notes));
                    }
                    Remember(recentChordMasks, note.RequiredLanesMask, 4);
                }

                if (note.Kind != NoteKind.Hold) continue;
                double holdBeats = timing.SecondsToBeat(note.EndTimeSec) - beat;
                if (holdBeats < 2d - tolerance || holdBeats > 3d + tolerance)
                {
                    throw new ArgumentException("Hard holds must last between two and three beats.", nameof(notes));
                }

                for (int otherIndex = 0; otherIndex < notes.Count; otherIndex++)
                {
                    if (otherIndex == i) continue;
                    double otherBeat = timing.SecondsToBeat(notes[otherIndex].TimeSec);
                    if (otherBeat > beat + tolerance && otherBeat < beat + holdBeats - tolerance)
                    {
                        throw new ArgumentException("Hard hold interiors must remain free of other notes.", nameof(notes));
                    }
                }
            }

            int[] singleLanes = notes.Where(note => !note.IsChord).Select(note => note.Lane).ToArray();
            for (int i = 1; i < singleLanes.Length; i++)
            {
                if (singleLanes[i] == singleLanes[i - 1])
                {
                    throw new ArgumentException("Hard charts may not repeat the same lane on adjacent single notes.", nameof(notes));
                }
                if (i >= 3 && singleLanes[i] == singleLanes[i - 2] && singleLanes[i - 1] == singleLanes[i - 3])
                {
                    throw new ArgumentException("Hard charts may not use four-note A-B-A-B loops.", nameof(notes));
                }
                if (i >= 2)
                {
                    int first = singleLanes[i - 2];
                    int second = singleLanes[i - 1];
                    int third = singleLanes[i];
                    int firstPreviousStart = Math.Max(0, i - 16);
                    for (int start = firstPreviousStart; start <= i - 3; start++)
                    {
                        if (singleLanes[start] == first && singleLanes[start + 1] == second && singleLanes[start + 2] == third)
                        {
                            throw new ArgumentException("A three-lane sequence may not repeat within sixteen recent notes.", nameof(notes));
                        }
                    }
                }
            }

            for (int start = 0; start + 18 <= singleLanes.Length; start++)
            {
                var counts = new int[KeyboardChordLayout.LaneCount];
                for (int i = start; i < start + 18; i++) counts[singleLanes[i]]++;
                if (counts.Any(count => count == 0 || count > 6))
                {
                    throw new ArgumentException("Every eighteen Hard single notes must cover all lanes without lane concentration.", nameof(notes));
                }
            }
        }

        private static void Remember<T>(Queue<T> values, T value, int capacity)
        {
            if (values.Count == capacity) values.Dequeue();
            values.Enqueue(value);
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
