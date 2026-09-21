using System;
using System.Linq;
using NUnit.Framework;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Tests
{
    public sealed class PrototypeDanceChartTests
    {
        [Test]
        public void Create_ProducesSortedUniqueNotesAtThreeMinuteEndpoint()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 180d);

            Assert.That(notes.Length, Is.EqualTo(406));
            Assert.That(notes.Select(x => x.Id).Distinct().Count(), Is.EqualTo(notes.Length));
            Assert.That(notes.Zip(notes.Skip(1), (left, right) => left.TimeSec <= right.TimeSec).All(x => x), Is.True);
            Assert.That(notes[notes.Length - 1].TimeSec, Is.EqualTo(180d).Within(0.0000001d));
        }

        [Test]
        public void Create_EachFullPhraseContainsTapHoldAndChord()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 8d);

            foreach (IGrouping<int, NoteData> segment in notes.Where(x => x.SegmentId < 2).GroupBy(x => x.SegmentId))
            {
                Assert.That(segment.Any(x => x.Kind == NoteKind.Tap && !x.IsChord), Is.True);
                Assert.That(segment.Any(x => x.Kind == NoteKind.Hold), Is.True);
                Assert.That(segment.Any(x => x.IsChord), Is.True);
            }
        }

        [Test]
        public void Create_AllSixLanesFeatureBothTapAndHold()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 24d);

            for (int lane = 0; lane < 6; lane++)
            {
                int captured = lane;
                Assert.That(
                    notes.Any(x => x.Lane == captured && x.Kind == NoteKind.Tap),
                    Is.True,
                    $"lane {captured} needs a tap note");
                Assert.That(
                    notes.Any(x => x.Lane == captured && x.Kind == NoteKind.Hold),
                    Is.True,
                    $"lane {captured} needs a hold note");
            }
        }

        [Test]
        public void Create_HoldNotesAreTwoBeatsLong()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 36d);
            NoteData[] holds = notes.Where(x => x.Kind == NoteKind.Hold).ToArray();

            Assert.That(holds.Length, Is.GreaterThan(0));
            Assert.That(holds.All(x => Math.Abs(x.DurationSec - 1d) < 0.001d), Is.True);
        }

        [Test]
        public void Create_NoLaneHasOverlappingNotes()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 180d);

            foreach (IGrouping<int, NoteData> lane in notes.GroupBy(x => x.Lane))
            {
                NoteData[] ordered = lane.OrderBy(x => x.TimeSec).ToArray();
                for (int i = 1; i < ordered.Length; i++)
                {
                    double previousEnd = ordered[i - 1].TimeSec + ordered[i - 1].DurationSec;
                    Assert.That(
                        previousEnd <= ordered[i].TimeSec + 1e-9d,
                        Is.True,
                        $"lane {lane.Key} overlaps at {previousEnd:F3}s -> {ordered[i].TimeSec:F3}s");
                }
            }
        }

        [Test]
        public void Create_UsesApprovedSpatialBodyMapping()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 4d);
            NoteData openingChord = notes.Single(x => x.IsChord && x.TimeSec < 4d);

            int bothHands = (1 << PrototypeDanceChart.LeftHandLane) |
                            (1 << PrototypeDanceChart.RightHandLane);
            Assert.That(openingChord.RequiredLanesMask, Is.EqualTo(bothHands));
            Assert.That(notes.Any(x => x.Lane == PrototypeDanceChart.BodyLane && x.Kind == NoteKind.Hold), Is.True);
        }

        [Test]
        public void Create_DifficultiesIncreaseDensityAndKeepAllNoteKinds()
        {
            NoteData[] easy = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Easy);
            NoteData[] normal = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Normal);
            NoteData[] hard = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Hard);

            Assert.That(easy.Length, Is.LessThan(normal.Length));
            Assert.That(normal.Length, Is.LessThan(hard.Length));
            foreach (NoteData[] chart in new[] { easy, normal, hard })
            {
                Assert.That(chart.Zip(chart.Skip(1), (left, right) => left.TimeSec <= right.TimeSec).All(x => x), Is.True);
                Assert.That(chart.Any(x => x.Kind == NoteKind.Tap && !x.IsChord), Is.True);
                Assert.That(chart.Any(x => x.Kind == NoteKind.Hold), Is.True);
                Assert.That(chart.Any(x => x.IsChord), Is.True);
            }
        }

        [TestCase(0d)]
        [TestCase(-120d)]
        public void Create_InvalidBpmThrows(double bpm)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PrototypeDanceChart.Create(bpm, 10d));
        }
    }
}
