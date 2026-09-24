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
            for (int beat = 0; beat < timing.Count; beat++)
            {
                int interval = difficulty == PlayDifficulty.Easy ? 4 : difficulty == PlayDifficulty.Normal ? 2 : 1;
                if (beat % interval != 0)
                {
                    continue;
                }

                notes.Add(CreateBeatNote(timing, difficulty, beat));
                if (difficulty == PlayDifficulty.Hard && beat < timing.Count - 1 && beat % 2 == 1)
                {
                    notes.Add(new SongDefinitionAsset.NoteRecord
                    {
                        id = 620000 + (beat * 2) + 1,
                        typeId = "tap",
                        lane = (beat + 2) % 6,
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

        private static SongDefinitionAsset.NoteRecord CreateBeatNote(
            SongTimingMap timing,
            PlayDifficulty difficulty,
            int beat)
        {
            bool isAnchor = beat % 8 == 0;
            int laneInterval = difficulty == PlayDifficulty.Easy ? 4 : difficulty == PlayDifficulty.Normal ? 2 : 1;
            int lane = (beat / laneInterval) % 6;
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

            if (difficulty == PlayDifficulty.Hard && beat % 4 == 2)
            {
                int pairedLane = (lane + 3) % 6;
                note.typeId = "chord";
                note.requiredLanesMask = (1 << lane) | (1 << pairedLane);
            }

            return note;
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
