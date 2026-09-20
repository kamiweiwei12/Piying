using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Tests
{
    public sealed class JudgmentEngineTests
    {
        private sealed class FakeSongClock : ISongClock
        {
            public double SongTime { get; set; }
        }

        private static readonly TimingConfig Config = new TimingConfig(0.040d, 0.100d, 0.250d);

        [TestCase(-0.040d, JudgmentGrade.Perfect)]
        [TestCase(0.040d, JudgmentGrade.Perfect)]
        [TestCase(-0.100d, JudgmentGrade.Good)]
        [TestCase(0.100d, JudgmentGrade.Good)]
        public void TimingWindow_InclusiveAndSymmetric(double offset, JudgmentGrade expected)
        {
            JudgmentEngine engine = CreateTapEngine(out FakeSongClock clock);
            engine.EnqueueInput(new HitInput(1d + offset, 0, InputKind.Press));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.2d)).Single();

            Assert.That(result.Grade, Is.EqualTo(expected));
            Assert.That(result.ErrorMs, Is.EqualTo(offset * 1000d).Within(0.0001d));
        }

        [Test]
        public void InputOutsideWindow_IsPreservedAndNoteEventuallyMisses()
        {
            JudgmentEngine engine = CreateTapEngine(out FakeSongClock clock);
            engine.EnqueueInput(new HitInput(0.899d, 0, InputKind.Press));

            IReadOnlyList<JudgmentResult> results = AdvanceTo(engine, clock, 1.101d);

            Assert.That(NoteResults(results).Single().Grade, Is.EqualTo(JudgmentGrade.Miss));
            Assert.That(engine.PendingInputCount, Is.EqualTo(1));
        }

        [Test]
        public void MultipleInputs_SelectClosestAndKeepUnusedInput()
        {
            JudgmentEngine engine = CreateTapEngine(out FakeSongClock clock);
            engine.EnqueueInput(new HitInput(0.930d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(1.010d, 0, InputKind.Press));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.1d)).Single();

            Assert.That(result.ErrorMs, Is.EqualTo(10d).Within(0.0001d));
            Assert.That(engine.PendingInputCount, Is.EqualTo(1));
        }

        [Test]
        public void AdvancePastLateWindow_AutomaticallyMissesAndBreaksCombo()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 1d),
                new NoteData(2, "tap", 0, 2d)
            };
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(notes, Config, clock);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));
            AdvanceTo(engine, clock, 1d);

            IReadOnlyList<JudgmentResult> results = AdvanceTo(engine, clock, 2.101d);

            Assert.That(NoteResults(results).Single().Grade, Is.EqualTo(JudgmentGrade.Miss));
            Assert.That(engine.Combo, Is.Zero);
        }

        [Test]
        public void Hold_EmitsTicksAndCompletesOnValidRelease()
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "hold", 2, 1d, 1d) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(1d, 2, InputKind.Press));
            AdvanceTo(engine, clock, 1d);

            IReadOnlyList<JudgmentResult> tickResults = AdvanceTo(engine, clock, 1.75d);
            engine.EnqueueInput(new HitInput(2d, 2, InputKind.Release));
            IReadOnlyList<JudgmentResult> releaseResults = AdvanceTo(engine, clock, 2d);

            Assert.That(tickResults.Count(x => x.EventKind == JudgmentEventKind.HoldTick), Is.EqualTo(3));
            Assert.That(releaseResults.Count(x => x.EventKind == JudgmentEventKind.HoldTick), Is.EqualTo(1));
            Assert.That(NoteResults(releaseResults).Single().Grade, Is.EqualTo(JudgmentGrade.Perfect));
        }

        [Test]
        public void Hold_EarlyReleaseProducesMiss()
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "hold", 1, 1d, 1d) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(1d, 1, InputKind.Press));
            AdvanceTo(engine, clock, 1d);
            engine.EnqueueInput(new HitInput(1.5d, 1, InputKind.Release));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.5d)).Single();

            Assert.That(result.Grade, Is.EqualTo(JudgmentGrade.Miss));
        }

        [Test]
        public void ScoreComboAndAccuracy_HaveSingleDeterministicSource()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 1d),
                new NoteData(2, "tap", 0, 2d),
                new NoteData(3, "tap", 0, 3d)
            };
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(notes, Config, clock);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(2.080d, 0, InputKind.Press));
            AdvanceTo(engine, clock, 2.1d);
            AdvanceTo(engine, clock, 3.101d);

            Assert.That(engine.Combo, Is.Zero);
            Assert.That(engine.MaxCombo, Is.EqualTo(2));
            Assert.That(engine.Score, Is.EqualTo(1520));
            Assert.That(engine.Accuracy, Is.EqualTo(0.5d).Within(0.000001d));
        }

        [Test]
        public void Segment_ReportsCompletedOrInterruptedAfterAllNotesResolve()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 1d, segmentId: 10),
                new NoteData(2, "tap", 1, 1d, segmentId: 10),
                new NoteData(3, "tap", 0, 2d, segmentId: 20)
            };
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(notes, Config, clock);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(1d, 1, InputKind.Press));

            IReadOnlyList<JudgmentResult> completed = AdvanceTo(engine, clock, 1d);
            IReadOnlyList<JudgmentResult> interrupted = AdvanceTo(engine, clock, 2.101d);

            Assert.That(completed.Any(x => x.EventKind == JudgmentEventKind.SegmentCompleted && x.SegmentId == 10), Is.True);
            Assert.That(interrupted.Any(x => x.EventKind == JudgmentEventKind.SegmentInterrupted && x.SegmentId == 20), Is.True);
        }

        [Test]
        public void SameChartAndReplay_ProduceIdenticalResults()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 1d),
                new NoteData(2, "tap", 1, 2d),
                new NoteData(3, "hold", 2, 3d, 1d)
            };
            var replay = new[]
            {
                new HitInput(1.01d, 0, InputKind.Press),
                new HitInput(1.94d, 1, InputKind.Press),
                new HitInput(3d, 2, InputKind.Press),
                new HitInput(4d, 2, InputKind.Release)
            };

            string first = RunReplay(notes, replay);
            string second = RunReplay(notes, replay);

            Assert.That(second, Is.EqualTo(first));
        }

        private static JudgmentEngine CreateTapEngine(out FakeSongClock clock)
        {
            clock = new FakeSongClock();
            return new JudgmentEngine(new[] { new NoteData(1, "tap", 0, 1d) }, Config, clock);
        }

        private static IReadOnlyList<JudgmentResult> AdvanceTo(
            JudgmentEngine engine,
            FakeSongClock clock,
            double songTime)
        {
            clock.SongTime = songTime;
            return engine.Advance(new List<JudgmentResult>());
        }

        private static IEnumerable<JudgmentResult> NoteResults(IEnumerable<JudgmentResult> results)
        {
            return results.Where(x => x.EventKind == JudgmentEventKind.NoteJudged);
        }

        private static string RunReplay(IEnumerable<NoteData> notes, IEnumerable<HitInput> replay)
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(notes, Config, clock);
            foreach (HitInput input in replay)
            {
                engine.EnqueueInput(input);
            }

            var signature = new List<string>();
            foreach (double time in new[] { 1.2d, 2.2d, 3.2d, 4.2d })
            {
                signature.AddRange(AdvanceTo(engine, clock, time).Select(x =>
                    $"{x.EventKind}:{x.NoteId}:{x.Grade}:{x.ErrorMs:F3}:{x.ComboAfter}:{x.ScoreAfter}:{x.AccuracyAfter:F6}"));
            }

            return string.Join("|", signature);
        }
    }
}
