using System;
using System.Collections.Generic;
using UnityEditor;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Unity.Config;

namespace YingYun.Rhythm.Editor
{
    /// <summary>
    /// 《象王行》候選譜的離線作者資料。執行時只讀取序列化結果，不分析音訊或生成音符。
    /// </summary>
    public static class XiangWangXingChartAuthoring
    {
        private const int SharedAnchorBase = 100000;

        private static readonly int[][] LaneMotifs =
        {
            new[] { 0, 2, 4, 1, 5, 3, 1, 4 },
            new[] { 3, 1, 5, 2, 0, 4, 2, 5 },
            new[] { 1, 4, 0, 3, 5, 2, 4, 0 },
            new[] { 4, 3, 1, 5, 2, 0, 3, 1 },
        };

        // 明確排列起、承、轉、合，避免在執行時輪替十四式。
        private static readonly DanceAction[] Routine =
        {
            // 起：0–7
            DanceAction.FinalPose, DanceAction.SingleMountainArm,
            DanceAction.CloudHand, DanceAction.PressPalm,
            DanceAction.SupportPalm, DanceAction.WindFlag,
            DanceAction.DoubleMountainArm, DanceAction.Turn,

            // 承一：8–17
            DanceAction.RaiseSleeve, DanceAction.ReverseCloudHand,
            DanceAction.ThreadPalm, DanceAction.TurnWrist,
            DanceAction.SingleFinger, DanceAction.CloudHand,
            DanceAction.PressPalm, DanceAction.SupportPalm,
            DanceAction.WindFlag, DanceAction.DoubleMountainArm,

            // 轉一：18–29
            DanceAction.Turn, DanceAction.RaiseSleeve,
            DanceAction.ReverseCloudHand, DanceAction.ThreadPalm,
            DanceAction.TurnWrist, DanceAction.FistPalmSalute,
            DanceAction.SingleMountainArm, DanceAction.CloudHand,
            DanceAction.WindFlag, DanceAction.DoubleMountainArm,
            DanceAction.ReverseCloudHand, DanceAction.FinalPose,

            // 承二：30–39
            DanceAction.Turn, DanceAction.SingleMountainArm,
            DanceAction.PressPalm, DanceAction.SupportPalm,
            DanceAction.ThreadPalm, DanceAction.TurnWrist,
            DanceAction.RaiseSleeve, DanceAction.CloudHand,
            DanceAction.WindFlag, DanceAction.DoubleMountainArm,

            // 合：40–49
            DanceAction.Turn, DanceAction.ReverseCloudHand,
            DanceAction.SingleFinger, DanceAction.PressPalm,
            DanceAction.SupportPalm, DanceAction.ThreadPalm,
            DanceAction.CloudHand, DanceAction.DoubleMountainArm,
            DanceAction.FistPalmSalute, DanceAction.FinalPose,
        };

        public static void Apply(SongDefinitionAsset song)
        {
            if (song == null) throw new ArgumentNullException(nameof(song));
            SongTimingMap timing = song.CreateTimingMap();
            if (timing.Count != 399)
            {
                throw new InvalidOperationException("《象王行》作者譜要求已留存的 399 個 Beat This 拍點。");
            }

            var charts = new[]
            {
                CreateChart(timing, PlayDifficulty.Easy),
                CreateChart(timing, PlayDifficulty.Normal),
                CreateChart(timing, PlayDifficulty.Hard),
            };
            SongDefinitionAsset.DanceCueData[] cues = CreateDanceCues(timing);
            song.ConfigureAuthoredContentEditor(
                charts,
                cues,
                SongDefinitionAsset.TimingStatus.Verified);
            song.ValidateOrThrow();
            EditorUtility.SetDirty(song);
        }

        private static SongDefinitionAsset.DifficultyChart CreateChart(
            SongTimingMap timing,
            PlayDifficulty difficulty)
        {
            if (difficulty == PlayDifficulty.Hard)
            {
                return CreateAccessibleHardChart(timing);
            }

            var notes = new List<SongDefinitionAsset.NoteRecord>(difficulty == PlayDifficulty.Hard ? 600 : 220);
            for (int beat = 0; beat < timing.Count; beat++)
            {
                int interval = difficulty == PlayDifficulty.Easy ? 4 : difficulty == PlayDifficulty.Normal ? 2 : 1;
                if (beat % interval != 0)
                {
                    continue;
                }

                notes.Add(CreateBeatNote(timing, difficulty, beat));
                if (difficulty == PlayDifficulty.Hard && beat < timing.Count - 1 && beat % 4 == 1)
                {
                    notes.Add(new SongDefinitionAsset.NoteRecord
                    {
                        id = 320000 + (beat * 2) + 1,
                        typeId = "tap",
                        lane = (LaneForBeat(beat) + 2 + ((beat / 4) % 3)) % 6,
                        timeSec = timing.BeatToSeconds(beat + 0.5d),
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

        private static SongDefinitionAsset.DifficultyChart CreateAccessibleHardChart(SongTimingMap timing)
        {
            NoteData[] generated = ProceduralSongChartGenerator.GenerateAccessibleHardNotes(
                timing,
                unchecked((int)0x58495758),
                SharedAnchorBase,
                310000);
            var records = new SongDefinitionAsset.NoteRecord[generated.Length];
            for (int i = 0; i < generated.Length; i++)
            {
                NoteData note = generated[i];
                records[i] = new SongDefinitionAsset.NoteRecord
                {
                    id = note.Id,
                    typeId = note.TypeId,
                    lane = note.Lane,
                    timeSec = note.TimeSec,
                    durationSec = note.DurationSec,
                    segmentId = note.SegmentId,
                    requiredLanesMask = note.RequiredLanesMask,
                };
            }

            return new SongDefinitionAsset.DifficultyChart
            {
                difficulty = PlayDifficulty.Hard,
                notes = records,
            };
        }

        private static SongDefinitionAsset.NoteRecord CreateBeatNote(
            SongTimingMap timing,
            PlayDifficulty difficulty,
            int beat)
        {
            bool isAnchor = beat % 8 == 0;
            int laneInterval = difficulty == PlayDifficulty.Easy ? 4 : difficulty == PlayDifficulty.Normal ? 2 : 1;
            int lane = difficulty == PlayDifficulty.Hard ? LaneForBeat(beat) : (beat / laneInterval) % 6;
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

            bool useHold = difficulty != PlayDifficulty.Easy && beat % 16 == 6 && beat + 2 < timing.Count;
            if (useHold)
            {
                note.typeId = "hold";
                note.durationSec = timing.BeatToSeconds(beat + 2) - note.timeSec;
                return note;
            }

            bool chordPhrase = ((beat / 8) & 1) == 1;
            bool useChord = difficulty == PlayDifficulty.Hard && chordPhrase && beat % 4 == 2;
            if (useChord)
            {
                int pairedLane = (lane + 3) % 6;
                int chordMask = (1 << lane) | (1 << pairedLane);
                if (!KeyboardChordLayout.IsAllowed(chordMask))
                {
                    pairedLane = (lane + 2) % 6;
                    chordMask = (1 << lane) | (1 << pairedLane);
                }

                note.typeId = "chord";
                note.requiredLanesMask = chordMask;
            }

            return note;
        }

        private static int LaneForBeat(int beat)
        {
            int phrase = beat / 8;
            return LaneMotifs[phrase % LaneMotifs.Length][beat % 8];
        }

        private static int DifficultyId(PlayDifficulty difficulty, int beat)
        {
            switch (difficulty)
            {
                case PlayDifficulty.Easy: return 110000 + beat;
                case PlayDifficulty.Normal: return 210000 + beat;
                default: return 310000 + (beat * 2);
            }
        }

        private static SongDefinitionAsset.DanceCueData[] CreateDanceCues(SongTimingMap timing)
        {
            int phraseCount = (timing.Count + 7) / 8;
            if (phraseCount != Routine.Length)
            {
                throw new InvalidOperationException("《象王行》套路长度必须覆盖全部八拍舞句。");
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
                    durationBeats = phrase == phraseCount - 1 ? 13 : 8,
                    anchorNoteId = SharedAnchorBase + startBeat,
                    hasClosing = HasClosing(action),
                };
            }

            return cues;
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
