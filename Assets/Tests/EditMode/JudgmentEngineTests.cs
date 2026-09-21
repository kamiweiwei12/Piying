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

        private static readonly TimingConfig Config = TimingConfig.Prototype;

        [TestCase(-0.050d, JudgmentGrade.Perfect)]
        [TestCase(0.050d, JudgmentGrade.Perfect)]
        [TestCase(-0.090d, JudgmentGrade.Great)]
        [TestCase(0.090d, JudgmentGrade.Great)]
        [TestCase(-0.150d, JudgmentGrade.Good)]
        [TestCase(0.150d, JudgmentGrade.Good)]
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
            engine.EnqueueInput(new HitInput(0.849d, 0, InputKind.Press));

            IReadOnlyList<JudgmentResult> results = AdvanceTo(engine, clock, 1.151d);

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

            IReadOnlyList<JudgmentResult> results = AdvanceTo(engine, clock, 2.151d);

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

        [TestCase(1.850d, JudgmentGrade.Good)]
        [TestCase(1.849d, JudgmentGrade.Miss)]
        public void Hold_EarlyReleaseWindow_IsInclusive(double releaseTime, JudgmentGrade expected)
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "hold", 1, 1d, 1d) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(1d, 1, InputKind.Press));
            AdvanceTo(engine, clock, 1d);
            engine.EnqueueInput(new HitInput(releaseTime, 1, InputKind.Release));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, releaseTime)).Single();

            Assert.That(result.Grade, Is.EqualTo(expected));
        }

        [Test]
        public void Hold_HeldThroughTailCompletesWithoutTimedRelease()
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "hold", 1, 1d, 1d) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(1d, 1, InputKind.Press));
            AdvanceTo(engine, clock, 1d);

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 2d)).Single();

            Assert.That(result.Grade, Is.EqualTo(JudgmentGrade.Perfect));
            Assert.That(engine.Combo, Is.EqualTo(1));
        }

        [Test]
        public void Hold_IgnoresReleaseThatPredatesItsPressInSameFrame()
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "hold", 0, 1d, 1d) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(0.9d, 0, InputKind.Release));
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));

            IReadOnlyList<JudgmentResult> start = AdvanceTo(engine, clock, 1d);

            Assert.That(start.Any(x => x.EventKind == JudgmentEventKind.HoldStarted), Is.True);
            Assert.That(NoteResults(start), Is.Empty);
            Assert.That(engine.PendingInputCount, Is.Zero);
            Assert.That(NoteResults(AdvanceTo(engine, clock, 2d)).Single().Grade, Is.EqualTo(JudgmentGrade.Perfect));
        }

        [Test]
        public void TapRelease_DoesNotPoisonLaterHoldOnSameLane()
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[]
                {
                    new NoteData(1, "tap", 0, 0d),
                    new NoteData(2, "hold", 0, 1d, 1d)
                },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(0d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(0.05d, 0, InputKind.Release));
            AdvanceTo(engine, clock, 0.05d);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));

            IReadOnlyList<JudgmentResult> start = AdvanceTo(engine, clock, 1d);

            Assert.That(start.Any(x => x.EventKind == JudgmentEventKind.HoldStarted), Is.True);
            Assert.That(NoteResults(AdvanceTo(engine, clock, 2d)).Single().Grade, Is.EqualTo(JudgmentGrade.Perfect));
        }

        [Test]
        public void Hold_ReleaseAfterTailDoesNotPoisonNextHold()
        {
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[]
                {
                    new NoteData(1, "hold", 0, 1d, 1d),
                    new NoteData(2, "hold", 0, 3d, 1d)
                },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));
            AdvanceTo(engine, clock, 1d);
            Assert.That(NoteResults(AdvanceTo(engine, clock, 2d)).Single().Grade, Is.EqualTo(JudgmentGrade.Perfect));
            engine.EnqueueInput(new HitInput(2.1d, 0, InputKind.Release));
            AdvanceTo(engine, clock, 2.1d);
            engine.EnqueueInput(new HitInput(3d, 0, InputKind.Press));
            AdvanceTo(engine, clock, 3d);

            Assert.That(NoteResults(AdvanceTo(engine, clock, 4d)).Single().Grade, Is.EqualTo(JudgmentGrade.Perfect));
        }

        [Test]
        public void Chord_ConsumesRequiredLanesAtomicallyAndAddsOneCombo()
        {
            int bothHands = (1 << 0) | (1 << 2);
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "chord", 0, 1d, requiredLanesMask: bothHands) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(0.980d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(1.030d, 2, InputKind.Press));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.1d)).Single();

            Assert.That(result.Grade, Is.EqualTo(JudgmentGrade.Perfect));
            Assert.That(result.ErrorMs, Is.EqualTo(30d).Within(0.0001d));
            Assert.That(result.RequiredLanesMask, Is.EqualTo(bothHands));
            Assert.That(engine.Combo, Is.EqualTo(1));
            Assert.That(engine.JudgedNoteCount, Is.EqualTo(1));
            Assert.That(engine.PendingInputCount, Is.Zero);
        }

        [Test]
        public void Chord_InputOrderDoesNotChangeResult()
        {
            int bothHands = (1 << 0) | (1 << 2);
            var note = new NoteData(1, "chord", 0, 1d, requiredLanesMask: bothHands);

            string leftFirst = RunReplay(
                new[] { note },
                new[]
                {
                    new HitInput(0.980d, 0, InputKind.Press),
                    new HitInput(1.030d, 2, InputKind.Press)
                });
            string rightFirst = RunReplay(
                new[] { note },
                new[]
                {
                    new HitInput(1.030d, 2, InputKind.Press),
                    new HitInput(0.980d, 0, InputKind.Press)
                });

            Assert.That(rightFirst, Is.EqualTo(leftFirst));
        }

        [TestCase(0.965d, 1.035d, JudgmentGrade.Perfect)]
        [TestCase(0.964d, 1.036d, JudgmentGrade.Miss)]
        public void Chord_SpreadWindow_IsInclusive(double firstTime, double secondTime, JudgmentGrade expected)
        {
            int bothHands = (1 << 0) | (1 << 2);
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "chord", 0, 1d, requiredLanesMask: bothHands) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(firstTime, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(secondTime, 2, InputKind.Press));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.151d)).Single();

            Assert.That(result.Grade, Is.EqualTo(expected));
            Assert.That(engine.PendingInputCount, Is.EqualTo(expected == JudgmentGrade.Miss ? 2 : 0));
        }

        [Test]
        public void Chord_MissingLaneMissesWithoutConsumingPartialInput()
        {
            int bothHands = (1 << 0) | (1 << 2);
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "chord", 0, 1d, requiredLanesMask: bothHands) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(1d, 0, InputKind.Press));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.151d)).Single();

            Assert.That(result.Grade, Is.EqualTo(JudgmentGrade.Miss));
            Assert.That(engine.PendingInputCount, Is.EqualTo(1));
        }

        [Test]
        public void Chord_ChoosesACompleteSetWhenPerLaneClosestInputsAreTooFarApart()
        {
            int bothHands = (1 << 0) | (1 << 2);
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(
                new[] { new NoteData(1, "chord", 0, 1d, requiredLanesMask: bothHands) },
                Config,
                clock);
            engine.EnqueueInput(new HitInput(0.930d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(1.060d, 0, InputKind.Press));
            engine.EnqueueInput(new HitInput(0.940d, 2, InputKind.Press));

            JudgmentResult result = NoteResults(AdvanceTo(engine, clock, 1.1d)).Single();

            Assert.That(result.Grade, Is.EqualTo(JudgmentGrade.Great));
            Assert.That(result.ErrorMs, Is.EqualTo(-70d).Within(0.0001d));
            Assert.That(engine.PendingInputCount, Is.EqualTo(1));
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
            engine.EnqueueInput(new HitInput(2.060d, 0, InputKind.Press));
            AdvanceTo(engine, clock, 2.1d);
            AdvanceTo(engine, clock, 3.151d);

            Assert.That(engine.Combo, Is.Zero);
            Assert.That(engine.MaxCombo, Is.EqualTo(2));
            Assert.That(engine.Score, Is.EqualTo(1775));
            Assert.That(engine.Accuracy, Is.EqualTo(7d / 12d).Within(0.000001d));
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
            IReadOnlyList<JudgmentResult> interrupted = AdvanceTo(engine, clock, 2.151d);

            Assert.That(completed.Any(x => x.EventKind == JudgmentEventKind.SegmentCompleted && x.SegmentId == 10), Is.True);
            Assert.That(interrupted.Any(x => x.EventKind == JudgmentEventKind.SegmentInterrupted && x.SegmentId == 20), Is.True);
        }

        [Test]
        public void Segment_FirstMissInterruptsImmediatelyAndOnlyOnce()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 1d, segmentId: 10),
                new NoteData(2, "tap", 1, 2d, segmentId: 10)
            };
            var clock = new FakeSongClock();
            var engine = new JudgmentEngine(notes, Config, clock);

            IReadOnlyList<JudgmentResult> firstMiss = AdvanceTo(engine, clock, 1.151d);
            IReadOnlyList<JudgmentResult> secondMiss = AdvanceTo(engine, clock, 2.151d);

            Assert.That(firstMiss.Count(x => x.EventKind == JudgmentEventKind.SegmentInterrupted), Is.EqualTo(1));
            Assert.That(secondMiss.Any(x => x.EventKind == JudgmentEventKind.SegmentInterrupted), Is.False);
            Assert.That(secondMiss.Any(x => x.EventKind == JudgmentEventKind.SegmentCompleted), Is.False);
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
