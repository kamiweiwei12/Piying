using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>P7 正式触发层；沿用既有歌曲锚点与秒时间轴，成功才推进十八式。</summary>
    public sealed class PuppetV2Playback
    {
        private readonly DancePhrase[] _timingPhrases;
        private readonly Dictionary<int, int> _byAnchor = new Dictionary<int, int>();
        private int _activeIndex = -1;
        private int _pendingIndex = -1;
        private int _lastPerformedIndex = -1;
        private PuppetV2Pose _entryPose;

        public PuppetV2Playback(DancePhrase[] timingPhrases)
        {
            _timingPhrases = timingPhrases ?? throw new ArgumentNullException(nameof(timingPhrases));
            for (int i = 0; i < timingPhrases.Length; i++)
            {
                if (_byAnchor.ContainsKey(timingPhrases[i].AnchorNoteId))
                    throw new ArgumentException("Duplicate V2 anchor note.", nameof(timingPhrases));
                _byAnchor.Add(timingPhrases[i].AnchorNoteId, i);
            }
            CurrentPose = PuppetV2SequenceChoreography.Evaluate(0d);
        }

        public PuppetV2Pose CurrentPose { get; private set; }
        public event Action<DancePerformanceStatus> StatusChanged;

        public void OnJudged(JudgmentResult result, double songTimeSeconds)
        {
            if (result.EventKind != JudgmentEventKind.NoteJudged ||
                !_byAnchor.TryGetValue(result.NoteId, out int index)) return;

            Evaluate(songTimeSeconds);
            DancePhrase phrase = _timingPhrases[index];
            if (result.Grade == JudgmentGrade.Miss)
            {
                _activeIndex = -1;
                _pendingIndex = -1;
                StatusChanged?.Invoke(Status(DancePerformanceKind.Interrupted,
                    _lastPerformedIndex, index));
                return;
            }

            if (songTimeSeconds < phrase.StartSeconds)
            {
                _pendingIndex = index;
                StatusChanged?.Invoke(Status(DancePerformanceKind.Pending,
                    _lastPerformedIndex, index));
                return;
            }

            Start(index);
        }

        public void Evaluate(double songTimeSeconds)
        {
            if (_pendingIndex >= 0 && songTimeSeconds >= _timingPhrases[_pendingIndex].StartSeconds)
            {
                int ready = _pendingIndex;
                _pendingIndex = -1;
                Start(ready);
            }
            if (_activeIndex < 0) return;

            DancePhrase timing = _timingPhrases[_activeIndex];
            double progress = Math.Max(0d, Math.Min(1d,
                (songTimeSeconds - timing.StartSeconds) / timing.DurationSeconds));
            int moveIndex = _activeIndex % PuppetV2SequenceChoreography.Count;
            double moveBeat = PuppetV2SequenceChoreography.GetStartBeat(moveIndex) +
                (PuppetV2SequenceChoreography.GetBeats(moveIndex) * progress);
            PuppetV2Pose target = PuppetV2SequenceChoreography.Evaluate(moveBeat);
            double recovery = Smooth(Math.Min(1d, progress * timing.DurationBeats));
            CurrentPose = Blend(_entryPose, target, recovery);

            if (progress >= 1d)
            {
                StatusChanged?.Invoke(Status(DancePerformanceKind.Holding, _activeIndex));
                _activeIndex = -1;
            }
        }

        /// <summary>只读预览未来姿势，供操偶装置提前牵引；不会推进播放或发送状态事件。</summary>
        public PuppetV2Pose PreviewPose(double songTimeSeconds)
        {
            if (_activeIndex < 0) return CurrentPose;
            DancePhrase timing = _timingPhrases[_activeIndex];
            double progress = Math.Max(0d, Math.Min(1d,
                (songTimeSeconds - timing.StartSeconds) / timing.DurationSeconds));
            int moveIndex = _activeIndex % PuppetV2SequenceChoreography.Count;
            double moveBeat = PuppetV2SequenceChoreography.GetStartBeat(moveIndex) +
                (PuppetV2SequenceChoreography.GetBeats(moveIndex) * progress);
            PuppetV2Pose target = PuppetV2SequenceChoreography.Evaluate(moveBeat);
            double recovery = Smooth(Math.Min(1d, progress * timing.DurationBeats));
            return Blend(_entryPose, target, recovery);
        }

        private void Start(int index)
        {
            _entryPose = CurrentPose;
            _activeIndex = index;
            _lastPerformedIndex = index;
            StatusChanged?.Invoke(Status(DancePerformanceKind.Performing, index));
        }

        private DancePerformanceStatus Status(DancePerformanceKind kind, int performedIndex,
            int cueIndex = -1)
        {
            string performedName = performedIndex < 0 ? null :
                PuppetV2SequenceChoreography.GetName(performedIndex % PuppetV2SequenceChoreography.Count);
            string cueName = cueIndex < 0 ? null :
                PuppetV2SequenceChoreography.GetName(cueIndex % PuppetV2SequenceChoreography.Count);
            DancePhrase timing = performedIndex < 0 ? null : _timingPhrases[performedIndex];
            return new DancePerformanceStatus(kind, performedName,
                timing?.StartBeat ?? 0, timing?.DurationBeats ?? 0, cueName);
        }

        private static PuppetV2Pose Blend(PuppetV2Pose from, PuppetV2Pose to, double amount)
        {
            return new PuppetV2Pose(
                Lerp(from.RootX, to.RootX, amount), Lerp(from.RootY, to.RootY, amount),
                Lerp(from.Torso, to.Torso, amount), Lerp(from.Head, to.Head, amount),
                Lerp(from.LeftHandX, to.LeftHandX, amount), Lerp(from.LeftHandY, to.LeftHandY, amount),
                Lerp(from.RightHandX, to.RightHandX, amount), Lerp(from.RightHandY, to.RightHandY, amount),
                Lerp(from.LeftWrist, to.LeftWrist, amount), Lerp(from.RightWrist, to.RightWrist, amount),
                Lerp(from.LeftFootX, to.LeftFootX, amount), Lerp(from.LeftFootY, to.LeftFootY, amount),
                Lerp(from.RightFootX, to.RightFootX, amount), Lerp(from.RightFootY, to.RightFootY, amount),
                Lerp(from.LeftShoe, to.LeftShoe, amount), Lerp(from.RightShoe, to.RightShoe, amount),
                Lerp(from.Facing, to.Facing, amount),
                amount < 0.5d ? from.LeftHandShape : to.LeftHandShape,
                amount < 0.5d ? from.RightHandShape : to.RightHandShape,
                from.LeftFootPlanted && to.LeftFootPlanted,
                from.RightFootPlanted && to.RightFootPlanted);
        }

        private static double Smooth(double value) => value * value * (3d - (2d * value));
        private static double Lerp(double from, double to, double amount) => from + ((to - from) * amount);
    }
}
