using System;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Scoring
{
    public enum ResultRating
    {
        Unfinished,
        TianCheng,
        ChuanShen,
        RuYun,
        ChuCheng,
        WeiCheng
    }

    public static class ResultGradeCalculator
    {
        public static ResultRating Calculate(double accuracy, DifficultyConfig difficulty)
        {
            if (difficulty == null)
            {
                throw new ArgumentNullException(nameof(difficulty));
            }

            double clamped = Math.Max(0d, Math.Min(1d, accuracy));
            if (clamped >= difficulty.TianChengMinAccuracy) return ResultRating.TianCheng;
            if (clamped >= difficulty.ChuanShenMinAccuracy) return ResultRating.ChuanShen;
            if (clamped >= difficulty.RuYunMinAccuracy) return ResultRating.RuYun;
            if (clamped >= difficulty.ChuChengMinAccuracy) return ResultRating.ChuCheng;
            return ResultRating.WeiCheng;
        }
    }

    /// <summary>由 JudgmentResult 單向累積的顯示統計；分數、連擊與準確率仍以判定核心輸出為準。</summary>
    public sealed class GameplayStatistics
    {
        public int PerfectCount { get; private set; }
        public int GreatCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }
        public int MaxCombo { get; private set; }
        public int Score { get; private set; }
        public double Accuracy { get; private set; }
        public int JudgedCount => PerfectCount + GreatCount + GoodCount + MissCount;

        public void Reset()
        {
            PerfectCount = 0;
            GreatCount = 0;
            GoodCount = 0;
            MissCount = 0;
            MaxCombo = 0;
            Score = 0;
            Accuracy = 0d;
        }

        public void Apply(JudgmentResult result)
        {
            if (result.EventKind != JudgmentEventKind.NoteJudged)
            {
                return;
            }

            switch (result.Grade)
            {
                case JudgmentGrade.Perfect: PerfectCount++; break;
                case JudgmentGrade.Great: GreatCount++; break;
                case JudgmentGrade.Good: GoodCount++; break;
                case JudgmentGrade.Miss: MissCount++; break;
            }

            MaxCombo = Math.Max(MaxCombo, result.ComboAfter);
            Score = result.ScoreAfter;
            Accuracy = result.AccuracyAfter;
        }
    }
}
