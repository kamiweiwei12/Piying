using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Chart
{
    public sealed class ProceduralSongContent
    {
        private readonly NoteData[][] _charts;

        internal ProceduralSongContent(NoteData[][] charts, AuthoredDanceCue[] danceCues)
        {
            _charts = charts;
            DanceCues = danceCues;
        }

        public AuthoredDanceCue[] DanceCues { get; }
        public NoteData[] GetNotes(PlayDifficulty difficulty) => _charts[(int)difficulty];
    }

    /// <summary>Builds deterministic charts and cues from a detected beat grid.</summary>
    public static class ProceduralSongChartGenerator
    {
        private const int SharedAnchorBase = 700000;
        private static readonly int[][] LaneMotifs =
        {
            new[] { 0, 2, 4, 1, 5, 3, 1, 4 },
            new[] { 3, 1, 5, 2, 0, 4, 2, 5 },
            new[] { 1, 4, 0, 3, 5, 2, 4, 0 },
            new[] { 5, 3, 1, 4, 2, 0, 3, 1 },
            new[] { 2, 5, 3, 0, 4, 1, 5, 2 },
            new[] { 4, 0, 2, 5, 1, 3, 0, 4 },
            new[] { 3, 5, 0, 2, 4, 1, 3, 0 },
            new[] { 1, 3, 5, 0, 2, 4, 1, 5 },
            new[] { 5, 2, 0, 4, 1, 3, 5, 0 },
            new[] { 2, 0, 4, 1, 3, 5, 2, 4 },
            new[] { 4, 2, 5, 3, 0, 1, 4, 2 },
            new[] { 0, 4, 1, 5, 3, 2, 0, 5 },
        };

        private static readonly DanceAction[] Actions =
        {
            DanceAction.SingleMountainArm, DanceAction.CloudHand, DanceAction.WindFlag,
            DanceAction.Turn, DanceAction.RaiseSleeve, DanceAction.DoubleMountainArm,
            DanceAction.ReverseCloudHand, DanceAction.FinalPose, DanceAction.PressPalm,
            DanceAction.SupportPalm, DanceAction.ThreadPalm, DanceAction.TurnWrist,
            DanceAction.FistPalmSalute, DanceAction.SingleFinger,
        };

        public static ProceduralSongContent Generate(
            SongTimingMap timing,
            double playableEndSec,
            int seed)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));
            if (timing.Count < 9) throw new ArgumentException("A custom song needs at least nine detected beats.", nameof(timing));
            if (playableEndSec <= timing[0].TimeSec) throw new ArgumentOutOfRangeException(nameof(playableEndSec));

            int phraseCount = ((timing.Count - 1) / 8) + 1;
            var random = new StableRandom(seed);
            var phraseMotifs = new int[phraseCount];
            int previousMotif = -1;
            for (int i = 0; i < phraseCount; i++)
            {
                int motif = random.Next(LaneMotifs.Length);
                if (motif == previousMotif) motif = (motif + 1 + random.Next(LaneMotifs.Length - 1)) % LaneMotifs.Length;
                phraseMotifs[i] = motif;
                previousMotif = motif;
            }

            int[] holdLengths = new int[(timing.Count + 15) / 16];
            for (int block = 0; block < holdLengths.Length; block += 4)
            {
                int[] values = { 1, 2, 3, 4 };
                for (int i = values.Length - 1; i > 0; i--)
                {
                    int swap = random.Next(i + 1);
                    int value = values[i];
                    values[i] = values[swap];
                    values[swap] = value;
                }
                for (int i = 0; i < values.Length && block + i < holdLengths.Length; i++)
                {
                    holdLengths[block + i] = values[i];
                }
            }

            var charts = new NoteData[3][];
            charts[(int)PlayDifficulty.Easy] = CreateChart(timing, PlayDifficulty.Easy, phraseMotifs, holdLengths);
            charts[(int)PlayDifficulty.Normal] = CreateChart(timing, PlayDifficulty.Normal, phraseMotifs, holdLengths);
            charts[(int)PlayDifficulty.Hard] = CreateChart(timing, PlayDifficulty.Hard, phraseMotifs, holdLengths);
            AuthoredDanceCue[] cues = CreateDanceCues(timing, playableEndSec, seed);

            var range = new SongPlayableRange(timing[0].TimeSec, playableEndSec);
            foreach (PlayDifficulty difficulty in Enum.GetValues(typeof(PlayDifficulty)))
            {
                SongChartValidation.ValidateNotes(charts[(int)difficulty], playableEndSec, range);
                SongChartValidation.ValidateDifficultyFeatures(charts[(int)difficulty], difficulty);
                _ = DanceChoreography.CreateAuthored(charts[(int)difficulty], timing, cues);
            }

            return new ProceduralSongContent(charts, cues);
        }

        private static NoteData[] CreateChart(
            SongTimingMap timing,
            PlayDifficulty difficulty,
            int[] phraseMotifs,
            int[] holdLengths)
        {
            var notes = new List<NoteData>(timing.Count * 2);
            var holdEndBeats = new double[KeyboardChordLayout.LaneCount];
            for (int lane = 0; lane < holdEndBeats.Length; lane++) holdEndBeats[lane] = double.NegativeInfinity;

            int interval = difficulty == PlayDifficulty.Easy ? 4 : difficulty == PlayDifficulty.Normal ? 2 : 1;
            for (int beat = 0; beat < timing.Count; beat++)
            {
                if (beat % interval != 0) continue;
                notes.Add(CreateBeatNote(timing, difficulty, beat, phraseMotifs, holdLengths, holdEndBeats));

                if (difficulty == PlayDifficulty.Hard && beat < timing.Count - 1 && beat % 2 == 1)
                {
                    double ornamentBeat = beat + 0.5d;
                    int preferred = (LaneForBeat(beat, phraseMotifs) + 2 + ((beat / 2) % 3)) % 6;
                    int lane = ChooseAvailableLane(preferred, ornamentBeat, holdEndBeats);
                    notes.Add(new NoteData(
                        930000 + (beat * 2) + 1,
                        "tap",
                        lane,
                        timing.BeatToSeconds(ornamentBeat),
                        segmentId: beat / 8));
                }
            }

            notes.Sort((left, right) => left.TimeSec.CompareTo(right.TimeSec));
            return notes.ToArray();
        }

        private static NoteData CreateBeatNote(
            SongTimingMap timing,
            PlayDifficulty difficulty,
            int beat,
            int[] phraseMotifs,
            int[] holdLengths,
            double[] holdEndBeats)
        {
            bool isAnchor = beat % 8 == 0;
            int lane = ChooseAvailableLane(LaneForBeat(beat, phraseMotifs), beat, holdEndBeats);
            int id = isAnchor ? SharedAnchorBase + beat : DifficultyId(difficulty, beat);
            double timeSec = timing.BeatToSeconds(beat);
            if (isAnchor)
            {
                return new NoteData(id, "tap", lane, timeSec, segmentId: beat / 8);
            }

            int holdBeats = Math.Min(
                holdLengths[Math.Min(beat / 16, holdLengths.Length - 1)],
                timing.Count - beat - 1);
            bool useHold = difficulty != PlayDifficulty.Easy && beat % 16 == 6 && holdBeats > 0;
            if (useHold)
            {
                double duration = timing.BeatToSeconds(beat + holdBeats) - timeSec;
                holdEndBeats[lane] = beat + holdBeats;
                return new NoteData(id, "hold", lane, timeSec, duration, beat / 8);
            }

            if (difficulty == PlayDifficulty.Hard && beat % 4 == 2)
            {
                int mask = FindChordMask(lane, beat / 4, beat, holdEndBeats);
                return new NoteData(id, "chord", FirstLane(mask), timeSec, segmentId: beat / 8, requiredLanesMask: mask);
            }

            return new NoteData(id, "tap", lane, timeSec, segmentId: beat / 8);
        }

        private static AuthoredDanceCue[] CreateDanceCues(SongTimingMap timing, double playableEndSec, int seed)
        {
            int phraseCount = ((timing.Count - 1) / 8) + 1;
            var random = new StableRandom(seed ^ unchecked((int)0x5F3759DF));
            var cues = new List<AuthoredDanceCue>(phraseCount);
            DanceAction previous = DanceAction.FinalPose;
            for (int phrase = 0; phrase < phraseCount; phrase++)
            {
                int startBeat = phrase * 8;
                int duration = phrase == phraseCount - 1
                    ? FinalDuration(timing, startBeat, playableEndSec)
                    : 8;
                if (duration < 4) break;

                DanceAction action;
                if (phrase == 0 || phrase == phraseCount - 1)
                {
                    action = DanceAction.FinalPose;
                }
                else
                {
                    action = Actions[random.Next(Actions.Length)];
                    while (action == previous || (previous == DanceAction.Turn && action == DanceAction.Turn))
                    {
                        action = Actions[random.Next(Actions.Length)];
                    }
                }

                cues.Add(new AuthoredDanceCue(
                    action,
                    startBeat,
                    duration,
                    SharedAnchorBase + startBeat,
                    HasClosing(action)));
                previous = action;
            }

            return cues.ToArray();
        }

        private static int FinalDuration(SongTimingMap timing, int startBeat, double playableEndSec)
        {
            int duration = 0;
            while (timing.BeatToSeconds(startBeat + duration + 1) <= playableEndSec + 0.000001d) duration++;
            return duration;
        }

        private static int LaneForBeat(int beat, int[] phraseMotifs)
        {
            int phrase = Math.Min(beat / 8, phraseMotifs.Length - 1);
            return LaneMotifs[phraseMotifs[phrase]][beat % 8];
        }

        private static int ChooseAvailableLane(int preferred, double beat, double[] holdEndBeats)
        {
            int[] offsets = { 0, 2, 4, 1, 3, 5 };
            for (int i = 0; i < offsets.Length; i++)
            {
                int lane = (preferred + offsets[i]) % KeyboardChordLayout.LaneCount;
                if (beat >= holdEndBeats[lane] - 0.000001d) return lane;
            }

            throw new InvalidOperationException("No lane is free while generating the chart.");
        }

        private static int FindChordMask(int preferredLane, int sequence, double beat, double[] holdEndBeats)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int offset = 0; offset < KeyboardChordLayout.AllowedCount; offset++)
                {
                    int mask = KeyboardChordLayout.GetAllowedMask(sequence + offset);
                    if (pass == 0 && !KeyboardChordLayout.ContainsLane(mask, preferredLane)) continue;
                    bool available = true;
                    for (int lane = 0; lane < KeyboardChordLayout.LaneCount; lane++)
                    {
                        if (KeyboardChordLayout.ContainsLane(mask, lane) && beat < holdEndBeats[lane] - 0.000001d)
                        {
                            available = false;
                            break;
                        }
                    }

                    if (available) return mask;
                }
            }

            throw new InvalidOperationException("No allowed chord is free while generating the chart.");
        }

        private static int FirstLane(int mask)
        {
            for (int lane = 0; lane < KeyboardChordLayout.LaneCount; lane++)
            {
                if (KeyboardChordLayout.ContainsLane(mask, lane)) return lane;
            }
            throw new ArgumentException("Chord mask is empty.", nameof(mask));
        }

        private static int DifficultyId(PlayDifficulty difficulty, int beat)
        {
            switch (difficulty)
            {
                case PlayDifficulty.Easy: return 800000 + beat;
                case PlayDifficulty.Normal: return 850000 + beat;
                default: return 900000 + (beat * 2);
            }
        }

        private static bool HasClosing(DanceAction action)
        {
            return action == DanceAction.CloudHand || action == DanceAction.DoubleMountainArm ||
                action == DanceAction.ReverseCloudHand || action == DanceAction.FinalPose ||
                action == DanceAction.SupportPalm || action == DanceAction.TurnWrist ||
                action == DanceAction.FistPalmSalute || action == DanceAction.SingleFinger;
        }

        private struct StableRandom
        {
            private uint _state;

            public StableRandom(int seed)
            {
                _state = unchecked((uint)seed);
                if (_state == 0) _state = 0x6D2B79F5u;
            }

            public int Next(int maximum)
            {
                if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
                uint value = _state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                _state = value;
                return (int)(value % (uint)maximum);
            }
        }
    }
}
