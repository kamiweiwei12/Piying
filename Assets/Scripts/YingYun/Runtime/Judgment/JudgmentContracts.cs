using System;

namespace YingYun.Rhythm.Judgment
{
    public interface ISongClock
    {
        double SongTime { get; }
    }

    public enum NoteKind
    {
        Tap,
        Hold
    }

    public enum InputKind
    {
        Press,
        Release
    }

    public enum JudgmentGrade
    {
        None,
        Perfect,
        Great,
        Good,
        Miss
    }

    public enum JudgmentEventKind
    {
        NoteJudged,
        HoldStarted,
        HoldTick,
        SegmentCompleted,
        SegmentInterrupted
    }

    /// <summary>純資料音符。所有時間欄位均為秒。</summary>
    public readonly struct NoteData
    {
        public NoteData(
            int id,
            string typeId,
            int lane,
            double timeSec,
            double durationSec = 0d,
            int segmentId = 0,
            int requiredLanesMask = 0)
        {
            if (string.IsNullOrWhiteSpace(typeId))
            {
                throw new ArgumentException("Type id is required.", nameof(typeId));
            }

            if (durationSec < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSec));
            }

            if (lane < 0 || lane >= 31)
            {
                throw new ArgumentOutOfRangeException(nameof(lane));
            }

            int resolvedMask = requiredLanesMask == 0 ? 1 << lane : requiredLanesMask;
            if (resolvedMask < 0 || (resolvedMask & (1 << lane)) == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredLanesMask));
            }

            Id = id;
            TypeId = typeId;
            Lane = lane;
            TimeSec = timeSec;
            DurationSec = durationSec;
            SegmentId = segmentId;
            RequiredLanesMask = resolvedMask;
        }

        public int Id { get; }
        public string TypeId { get; }
        public int Lane { get; }
        public double TimeSec { get; }
        public double DurationSec { get; }
        public int SegmentId { get; }
        public int RequiredLanesMask { get; }
        public NoteKind Kind => DurationSec > 0d ? NoteKind.Hold : NoteKind.Tap;
        public bool IsChord => (RequiredLanesMask & (RequiredLanesMask - 1)) != 0;
        public double EndTimeSec => TimeSec + DurationSec;
    }

    /// <summary>與具體裝置無關的輸入事件；時間為校正後 song time 秒數。</summary>
    public readonly struct HitInput
    {
        public HitInput(double inputTimeSec, int lane, InputKind kind, int sourceId = 0)
        {
            InputTimeSec = inputTimeSec;
            Lane = lane;
            Kind = kind;
            SourceId = sourceId;
        }

        public double InputTimeSec { get; }
        public int Lane { get; }
        public InputKind Kind { get; }
        public int SourceId { get; }
    }

    public readonly struct TimingConfig
    {
        public TimingConfig(
            double perfectWindowSec,
            double greatWindowSec,
            double goodWindowSec,
            double holdTickIntervalSec,
            int baseScorePerNote = 1000,
            int holdTickScore = 100,
            int maxComboBonus = 100,
            double chordSpreadWindowSec = 0.070d)
        {
            if (perfectWindowSec < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectWindowSec));
            }

            if (goodWindowSec < perfectWindowSec)
            {
                throw new ArgumentOutOfRangeException(nameof(goodWindowSec));
            }

            if (greatWindowSec < perfectWindowSec || goodWindowSec < greatWindowSec)
            {
                throw new ArgumentOutOfRangeException(nameof(greatWindowSec));
            }

            if (holdTickIntervalSec <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(holdTickIntervalSec));
            }

            if (baseScorePerNote < 0 || holdTickScore < 0 || maxComboBonus <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseScorePerNote));
            }

            if (chordSpreadWindowSec < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(chordSpreadWindowSec));
            }

            PerfectWindowSec = perfectWindowSec;
            GreatWindowSec = greatWindowSec;
            GoodWindowSec = goodWindowSec;
            HoldTickIntervalSec = holdTickIntervalSec;
            BaseScorePerNote = baseScorePerNote;
            HoldTickScore = holdTickScore;
            MaxComboBonus = maxComboBonus;
            ChordSpreadWindowSec = chordSpreadWindowSec;
        }

        public double PerfectWindowSec { get; }
        public double GreatWindowSec { get; }
        public double GoodWindowSec { get; }
        public double HoldTickIntervalSec { get; }
        public int BaseScorePerNote { get; }
        public int HoldTickScore { get; }
        public int MaxComboBonus { get; }
        public double ChordSpreadWindowSec { get; }

        /// <summary>原型手感：中心點前後保留 150 ms 可打範圍，降低視覺抵達後立即失敗的挫折。</summary>
        public static TimingConfig Prototype => new TimingConfig(0.050d, 0.090d, 0.150d, 0.250d);
    }

    public readonly struct JudgmentResult
    {
        public JudgmentResult(
            JudgmentEventKind eventKind,
            int noteId,
            int segmentId,
            JudgmentGrade grade,
            double errorMs,
            int comboAfter,
            int scoreAfter,
            double accuracyAfter,
            int requiredLanesMask = 0)
        {
            EventKind = eventKind;
            NoteId = noteId;
            SegmentId = segmentId;
            Grade = grade;
            ErrorMs = errorMs;
            ComboAfter = comboAfter;
            ScoreAfter = scoreAfter;
            AccuracyAfter = accuracyAfter;
            RequiredLanesMask = requiredLanesMask;
        }

        public JudgmentEventKind EventKind { get; }
        public int NoteId { get; }
        public int SegmentId { get; }
        public JudgmentGrade Grade { get; }
        public double ErrorMs { get; }
        public int ComboAfter { get; }
        public int ScoreAfter { get; }
        public double AccuracyAfter { get; }
        public int RequiredLanesMask { get; }
    }
}
