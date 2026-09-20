using System.Collections.Generic;
using NUnit.Framework;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Tests
{
    public sealed class GameplayStatisticsTests
    {
        private sealed class FakeSongClock : ISongClock
        {
            public double SongTime { get; set; }
        }

        [TestCase(0.95d, ResultRating.TianCheng)]
        [TestCase(0.949999d, ResultRating.ChuanShen)]
        [TestCase(0.90d, ResultRating.ChuanShen)]
        [TestCase(0.80d, ResultRating.RuYun)]
        [TestCase(0.70d, ResultRating.ChuCheng)]
        [TestCase(0.699999d, ResultRating.WeiCheng)]
        public void ResultRating_UsesInclusiveChineseGradeThresholds(double accuracy, ResultRating expected)
        {
            Assert.That(
                ResultGradeCalculator.Calculate(accuracy, DifficultyConfig.Prototype),
                Is.EqualTo(expected));
        }

        [Test]
        public void ResultRating_UsesThresholdsFromDifficultyConfig()
        {
            var difficulty = new DifficultyConfig(0.99d, 0.94d, 0.84d, 0.74d);

            Assert.That(
                ResultGradeCalculator.Calculate(0.95d, difficulty),
                Is.EqualTo(ResultRating.ChuanShen));
            Assert.That(
                ResultGradeCalculator.Calculate(0.74d, difficulty),
                Is.EqualTo(ResultRating.ChuCheng));
            Assert.That(
                ResultGradeCalculator.Calculate(0.739999d, difficulty),
                Is.EqualTo(ResultRating.WeiCheng));
        }

        [Test]
        public void Statistics_CountsFourGradesAndKeepsAuthoritativeTotals()
        {
            var statistics = new GameplayStatistics();
            statistics.Apply(Result(JudgmentGrade.Perfect, 1, 1010, 1d));
            statistics.Apply(Result(JudgmentGrade.Great, 2, 1775, 0.875d));
            statistics.Apply(Result(JudgmentGrade.Good, 3, 2290, 0.75d));
            statistics.Apply(Result(JudgmentGrade.Miss, 0, 2290, 0.5625d));

            Assert.That(statistics.PerfectCount, Is.EqualTo(1));
            Assert.That(statistics.GreatCount, Is.EqualTo(1));
            Assert.That(statistics.GoodCount, Is.EqualTo(1));
            Assert.That(statistics.MissCount, Is.EqualTo(1));
            Assert.That(statistics.MaxCombo, Is.EqualTo(3));
            Assert.That(statistics.Score, Is.EqualTo(2290));
            Assert.That(statistics.Accuracy, Is.EqualTo(0.5625d));
        }

        [Test]
        public void Reset_ClearsAllVisibleStatistics()
        {
            var statistics = new GameplayStatistics();
            statistics.Apply(Result(JudgmentGrade.Perfect, 1, 1010, 1d));

            statistics.Reset();

            Assert.That(statistics.JudgedCount, Is.Zero);
            Assert.That(statistics.MaxCombo, Is.Zero);
            Assert.That(statistics.Score, Is.Zero);
            Assert.That(statistics.Accuracy, Is.Zero);
        }

        [Test]
        public void Statistics_MatchJudgmentEngineForMixedJudgments()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 1d),
                new NoteData(2, "tap", 1, 2d),
                new NoteData(3, "tap", 2, 3d),
                new NoteData(4, "tap", 3, 4d)
            };
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(notes, TimingConfig.Prototype, clock);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(2.060d, 1, InputKind.Press));
            engine.EnqueueInput(new HitInput(3.090d, 2, InputKind.Press));
            var statistics = new GameplayStatistics();
            var results = new List<JudgmentResult>();

            foreach (double songTime in new[] { 1.05d, 2.1d, 3.1d, 4.2d })
            {
                clock.SongTime = songTime;
                engine.Advance(results);
                foreach (JudgmentResult result in results)
                {
                    statistics.Apply(result);
                }
            }

            Assert.That(statistics.PerfectCount, Is.EqualTo(1));
            Assert.That(statistics.GreatCount, Is.EqualTo(1));
            Assert.That(statistics.GoodCount, Is.EqualTo(1));
            Assert.That(statistics.MissCount, Is.EqualTo(1));
            Assert.That(statistics.JudgedCount, Is.EqualTo(engine.JudgedNoteCount));
            Assert.That(statistics.MaxCombo, Is.EqualTo(engine.MaxCombo));
            Assert.That(statistics.Score, Is.EqualTo(engine.Score));
            Assert.That(statistics.Accuracy, Is.EqualTo(engine.Accuracy).Within(0.000001d));
        }

        private static JudgmentResult Result(JudgmentGrade grade, int combo, int score, double accuracy)
        {
            return new JudgmentResult(JudgmentEventKind.NoteJudged, 1, 0, grade, 0d, combo, score, accuracy);
        }
    }
}
