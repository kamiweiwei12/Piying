using System;
using System.Collections.Generic;
using UnityEditor;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Unity.Config;

namespace YingYun.Rhythm.Editor
{
    /// <summary>
    /// 《青玉案·兰芥》候选谱的离线作者数据。运行时只读取序列化结果。
    /// </summary>
    public static class QingYuAnLanJieChartAuthoring
    {
        private const int SharedAnchorBase = 400000;

        private static readonly int[][] LaneMotifs =
        {
            new[] { 3, 4, 5, 2, 1, 0, 2, 4 },
            new[] { 0, 1, 2, 4, 3, 5, 1, 3 },
            new[] { 3, 1, 4, 2, 5, 0, 4, 1 },
            new[] { 2, 0, 1, 3, 5, 4, 2, 5 },
            new[] { 4, 5, 3, 1, 0, 2, 5, 1 },
            new[] { 1, 3, 0, 4, 2, 5, 3, 0 },
            new[] { 5, 2, 4, 1, 3, 0, 2, 4 },
            new[] { 0, 3, 1, 5, 2, 4, 0, 5 },
            new[] { 2, 4, 1, 3, 0, 5, 1, 4 },
            new[] { 4, 2, 5, 0, 3, 1, 5, 2 },
            new[] { 1, 5, 3, 0, 4, 2, 3, 5 },
            new[] { 5, 4, 2, 3, 1, 0, 4, 2 },
        };

        private static readonly int[] PhraseMotifs =
        {
            // 起：8 段
            0, 1, 2, 3, 4, 5, 6, 7,
            // 承：14 段
            2, 8, 4, 9, 1, 10, 5, 11, 3, 6, 0, 7, 8, 2,
            // 转：14 段
            9, 4, 11, 6, 10, 3, 8, 5, 1, 7, 2, 0, 6, 11,
            // 合：12 段
            5, 8, 3, 9, 1, 6, 4, 10, 2, 7, 0, 11,
        };

        private static readonly int[] OrnamentOffsets = { 2, 4, 1, 5, 3, 1, 4, 2 };
        private static readonly int[] HoldLengthsBeats = { 1, 2, 3, 4, 2, 1, 4, 3 };

        // 48 段明确分为起、承、转、合，避免运行时机械轮替动作。
        private static readonly DanceAction[] Routine =
        {
            // 起：0–7
            DanceAction.FinalPose, DanceAction.SingleMountainArm,
            DanceAction.PressPalm, DanceAction.CloudHand,
            DanceAction.SupportPalm, DanceAction.WindFlag,
            DanceAction.DoubleMountainArm, DanceAction.Turn,

            // 承：8–21
            DanceAction.RaiseSleeve, DanceAction.ReverseCloudHand,
            DanceAction.ThreadPalm, DanceAction.TurnWrist,
            DanceAction.SingleFinger, DanceAction.CloudHand,
            DanceAction.PressPalm, DanceAction.SupportPalm,
            DanceAction.WindFlag, DanceAction.DoubleMountainArm,
            DanceAction.FistPalmSalute, DanceAction.SingleMountainArm,
            DanceAction.RaiseSleeve, DanceAction.ReverseCloudHand,

            // 转：22–35
            DanceAction.Turn, DanceAction.ThreadPalm,
            DanceAction.TurnWrist, DanceAction.CloudHand,
            DanceAction.WindFlag, DanceAction.DoubleMountainArm,
            DanceAction.Turn, DanceAction.ReverseCloudHand,
            DanceAction.SingleFinger, DanceAction.PressPalm,
            DanceAction.SupportPalm, DanceAction.RaiseSleeve,
            DanceAction.Turn, DanceAction.SingleMountainArm,

            // 合：36–47
            DanceAction.CloudHand, DanceAction.ThreadPalm,
            DanceAction.TurnWrist, DanceAction.ReverseCloudHand,
            DanceAction.WindFlag, DanceAction.DoubleMountainArm,
            DanceAction.PressPalm, DanceAction.SupportPalm,
            DanceAction.SingleFinger, DanceAction.CloudHand,
            DanceAction.FistPalmSalute, DanceAction.FinalPose,
        };

        public static void Apply(SongDefinitionAsset song)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));
            SongTimingMap timing = song.CreateTimingMap();
            if (timing.Count != 382)
            {
                throw new InvalidOperationException("《青玉案·兰芥》作者谱要求已留存的 382 个 Beat This 拍点。");
            }

            var charts = new[]
            {
                CreateChart(timing, PlayDifficulty.Easy),
                CreateChart(timing, PlayDifficulty.Normal),
                CreateChart(timing, PlayDifficulty.Hard),
            };
            SongDefinitionAsset.DanceCueData[] cues = CreateDanceCues(timing, song.PlayableEndSec);
            song.ConfigureAuthoredContentEditor(
                charts,
                cues,
                SongDefinitionAsset.TimingStatus.AnalysisCandidate);
            song.ValidateOrThrow();
            EditorUtility.SetDirty(song);
        }

        private static SongDefinitionAsset.DifficultyChart CreateChart(
            SongTimingMap timing,
            PlayDifficulty difficulty)
        {
            var notes = new List<SongDefinitionAsset.NoteRecord>(difficulty == PlayDifficulty.Hard ? 580 : 200);
            var holdEndBeats = new double[KeyboardChordLayout.LaneCount];
            for (int lane = 0; lane < holdEndBeats.Length; lane++)
            {
                holdEndBeats[lane] = double.NegativeInfinity;
            }

            for (int beat = 0; beat < timing.Count; beat++)
            {
                int interval = difficulty == PlayDifficulty.Easy ? 4 : difficulty == PlayDifficulty.Normal ? 2 : 1;
                if (beat % interval != 0)
                {
                    continue;
                }

                notes.Add(CreateBeatNote(timing, difficulty, beat, holdEndBeats));
                if (difficulty == PlayDifficulty.Hard && beat < timing.Count - 1 && beat % 2 == 1)
                {
                    double ornamentBeat = beat + 0.5d;
                    int preferredLane = (LaneForBeat(beat) + OrnamentOffsets[beat % 8]) % 6;
                    notes.Add(new SongDefinitionAsset.NoteRecord
                    {
                        id = 620000 + (beat * 2) + 1,
                        typeId = "tap",
                        lane = ChooseAvailableLane(preferredLane, ornamentBeat, holdEndBeats),
                        timeSec = timing.BeatToSeconds(ornamentBeat),
                        segmentId = beat / 8,
                    });
                }
            }

            return new SongDefinitionAsset.DifficultyChart
            {
                difficulty = difficulty,
                notes = notes.ToArray(),
            };
        }

        private static SongDefinitionAsset.NoteRecord CreateBeatNote(
            SongTimingMap timing,
            PlayDifficulty difficulty,
            int beat,
            double[] holdEndBeats)
        {
            bool isAnchor = beat % 8 == 0;
            int preferredLane = LaneForBeat(beat);
            int lane = ChooseAvailableLane(preferredLane, beat, holdEndBeats);
            var note = new SongDefinitionAsset.NoteRecord
            {
                id = isAnchor ? SharedAnchorBase + beat : DifficultyId(difficulty, beat),
                typeId = "tap",
                lane = lane,
                timeSec = timing.BeatToSeconds(beat),
                segmentId = beat / 8,
            };

            if (isAnchor)
            {
                return note;
            }

            int holdOrdinal = beat / 16;
            int holdBeats = HoldLengthsBeats[holdOrdinal % HoldLengthsBeats.Length];
            bool useHold = difficulty != PlayDifficulty.Easy && beat % 16 == 6 && beat + holdBeats < timing.Count;
            if (useHold)
            {
                note.typeId = "hold";
                note.durationSec = timing.BeatToSeconds(beat + holdBeats) - note.timeSec;
                holdEndBeats[lane] = beat + holdBeats;
                return note;
            }

            if (difficulty == PlayDifficulty.Hard && beat % 4 == 2)
            {
                int chordMask = FindChordMask(lane, beat / 4, beat, holdEndBeats);
                note.typeId = "chord";
                note.lane = FirstLane(chordMask);
                note.requiredLanesMask = chordMask;
            }

            return note;
        }

        private static int LaneForBeat(int beat)
        {
            int phrase = Math.Min(beat / 8, PhraseMotifs.Length - 1);
            return LaneMotifs[PhraseMotifs[phrase]][beat % 8];
        }

        private static int ChooseAvailableLane(int preferred, double beat, double[] holdEndBeats)
        {
            int[] offsets = { 0, 2, 4, 1, 3, 5 };
            for (int i = 0; i < offsets.Length; i++)
            {
                int lane = (preferred + offsets[i]) % KeyboardChordLayout.LaneCount;
                if (beat >= holdEndBeats[lane] - 0.000001d)
                {
                    return lane;
                }
            }

            throw new InvalidOperationException("No free lane remains while authoring a note.");
        }

        private static int FindChordMask(
            int preferredLane,
            int sequenceIndex,
            double beat,
            double[] holdEndBeats)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int offset = 0; offset < KeyboardChordLayout.AllowedCount; offset++)
                {
                    int mask = KeyboardChordLayout.GetAllowedMask(sequenceIndex + offset);
                    if (pass == 0 && !KeyboardChordLayout.ContainsLane(mask, preferredLane))
                    {
                        continue;
                    }

                    bool available = true;
                    for (int lane = 0; lane < KeyboardChordLayout.LaneCount; lane++)
                    {
                        if (KeyboardChordLayout.ContainsLane(mask, lane) && beat < holdEndBeats[lane] - 0.000001d)
                        {
                            available = false;
                            break;
                        }
                    }

                    if (available)
                    {
                        return mask;
                    }
                }
            }

            throw new InvalidOperationException("No allowed chord remains while a hold is active.");
        }

        private static int FirstLane(int mask)
        {
            for (int lane = 0; lane < KeyboardChordLayout.LaneCount; lane++)
            {
                if (KeyboardChordLayout.ContainsLane(mask, lane))
                {
                    return lane;
                }
            }

            throw new ArgumentException("Chord mask must contain a lane.", nameof(mask));
        }

        private static int DifficultyId(PlayDifficulty difficulty, int beat)
        {
            switch (difficulty)
            {
                case PlayDifficulty.Easy: return 410000 + beat;
                case PlayDifficulty.Normal: return 510000 + beat;
                default: return 610000 + (beat * 2);
            }
        }

        private static SongDefinitionAsset.DanceCueData[] CreateDanceCues(
            SongTimingMap timing,
            double playableEndSec)
        {
            int phraseCount = (timing.Count + 7) / 8;
            if (phraseCount != Routine.Length)
            {
                throw new InvalidOperationException("《青玉案·兰芥》套路长度必须覆盖全部八拍舞句。");
            }

            var cues = new SongDefinitionAsset.DanceCueData[phraseCount];
            for (int phrase = 0; phrase < phraseCount; phrase++)
            {
                int startBeat = phrase * 8;
                DanceAction action = Routine[phrase];
                cues[phrase] = new SongDefinitionAsset.DanceCueData
                {
                    action = action,
                    startBeat = startBeat,
                    durationBeats = phrase == phraseCount - 1
                        ? CalculateFinalDuration(timing, startBeat, playableEndSec)
                        : 8,
                    anchorNoteId = SharedAnchorBase + startBeat,
                    hasClosing = HasClosing(action),
                };
            }

            return cues;
        }

        private static int CalculateFinalDuration(
            SongTimingMap timing,
            int startBeat,
            double playableEndSec)
        {
            int duration = 4;
            while (timing.BeatToSeconds(startBeat + duration + 1) <= playableEndSec + 0.000001d)
            {
                duration++;
            }

            if (timing.BeatToSeconds(startBeat + duration) > playableEndSec + 0.000001d)
            {
                throw new InvalidOperationException("《青玉案·兰芥》尾奏不足四拍，无法安排完整收势。");
            }

            return duration;
        }

        private static bool HasClosing(DanceAction action)
        {
            return action == DanceAction.CloudHand || action == DanceAction.DoubleMountainArm ||
                action == DanceAction.ReverseCloudHand || action == DanceAction.FinalPose ||
                action == DanceAction.SupportPalm || action == DanceAction.TurnWrist ||
                action == DanceAction.FistPalmSalute || action == DanceAction.SingleFinger;
        }
    }
}
