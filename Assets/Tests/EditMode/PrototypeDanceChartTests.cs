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

            Assert.That(notes.Length, Is.EqualTo(271));
            Assert.That(notes.Select(x => x.Id).Distinct().Count(), Is.EqualTo(notes.Length));
            Assert.That(notes.Zip(notes.Skip(1), (left, right) => left.TimeSec <= right.TimeSec).All(x => x), Is.True);
            Assert.That(notes[notes.Length - 1].TimeSec, Is.EqualTo(180d).Within(0.0000001d));
        }

        [Test]
        public void Create_NormalContainsTapAndHoldWithoutChord()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 8d);

            NoteData[] phrases = notes.Where(x => x.SegmentId < 2).ToArray();
            Assert.That(phrases.Any(x => x.Kind == NoteKind.Tap && !x.IsChord), Is.True);
            Assert.That(phrases.Any(x => x.Kind == NoteKind.Hold), Is.True);
            Assert.That(phrases.Any(x => x.IsChord), Is.False);
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
        public void Create_NormalRequiresOnlyOneLanePerNote()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 4d);

            Assert.That(notes.All(x => !x.IsChord), Is.True);
            Assert.That(notes.Any(x => x.Kind == NoteKind.Hold), Is.True);
        }

        [Test]
        public void Create_DifficultiesIncreaseDensityAndRemainSorted()
        {
            NoteData[] easy = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Easy);
            NoteData[] normal = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Normal);
            NoteData[] hard = PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Hard);

            Assert.That(easy.Length, Is.LessThan(normal.Length));
            Assert.That(normal.Length, Is.LessThan(hard.Length));
            foreach (NoteData[] chart in new[] { easy, normal, hard })
            {
                Assert.That(chart.Zip(chart.Skip(1), (left, right) => left.TimeSec <= right.TimeSec).All(x => x), Is.True);
                Assert.That(chart.Any(x => x.Kind == NoteKind.Tap && !x.IsChord), Is.True);
            }

            Assert.That(easy.All(x => x.Kind == NoteKind.Tap), Is.True);
            Assert.That(easy.Any(x => x.IsChord), Is.False);
            Assert.That(normal.Any(x => x.Kind == NoteKind.Hold), Is.True);
            Assert.That(normal.Any(x => x.IsChord), Is.False);
            Assert.That(hard.Any(x => x.Kind == NoteKind.Hold), Is.True);
            Assert.That(hard.Any(x => x.IsChord), Is.True);
        }

        [Test]
        public void Create_NormalDensityFallsStrictlyBetweenEasyAndHard()
        {
            const double duration = 180d;
            NoteData[] easy = PrototypeDanceChart.Create(120d, duration, PlayDifficulty.Easy);
            NoteData[] normal = PrototypeDanceChart.Create(120d, duration, PlayDifficulty.Normal);
            NoteData[] hard = PrototypeDanceChart.Create(120d, duration, PlayDifficulty.Hard);

            Assert.That(easy.Length / duration, Is.EqualTo(1d).Within(0.0001d));
            Assert.That(normal.Length / duration, Is.GreaterThan(easy.Length / duration));
            Assert.That(normal.Length / duration, Is.LessThan(hard.Length / duration));
        }

        [Test]
        public void Create_HardCoversAllTwelveAllowedTwoLaneChordCombinations()
        {
            NoteData[] hard = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Hard);
            int[] chordMasks = hard.Where(x => x.IsChord).Select(x => x.RequiredLanesMask).Distinct().ToArray();

            Assert.That(chordMasks.Length, Is.EqualTo(KeyboardChordLayout.AllowedCount));
            Assert.That(chordMasks.All(mask => CountBits(mask) == 2), Is.True);
            Assert.That(chordMasks.All(KeyboardChordLayout.IsAllowed), Is.True);
            Assert.That(chordMasks.Contains((1 << 1) | (1 << 4)), Is.False, "W+S");
            Assert.That(chordMasks.Contains((1 << 0) | (1 << 5)), Is.False, "Q+D");
            Assert.That(chordMasks.Contains((1 << 2) | (1 << 3)), Is.False, "E+A");
        }

        [Test]
        public void Create_EasyRequiresOnlyOneKeyAndAtMostOneNotePerSecond()
        {
            NoteData[] easy = PrototypeDanceChart.Create(120d, 40d, PlayDifficulty.Easy);

            Assert.That(easy.All(x => x.Kind == NoteKind.Tap), Is.True);
            Assert.That(easy.All(x => (x.RequiredLanesMask & (x.RequiredLanesMask - 1)) == 0), Is.True);
            Assert.That(
                easy.Zip(easy.Skip(1), (left, right) => right.TimeSec - left.TimeSec >= 1d - 1e-9d).All(x => x),
                Is.True);
        }

        [TestCase(PlayDifficulty.Easy)]
        [TestCase(PlayDifficulty.Normal)]
        [TestCase(PlayDifficulty.Hard)]
        public void Create_AllDifficultiesAvoidSameLaneOverlap(PlayDifficulty difficulty)
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 40d, difficulty);

            foreach (IGrouping<int, NoteData> lane in notes.GroupBy(x => x.Lane))
            {
                NoteData[] ordered = lane.OrderBy(x => x.TimeSec).ToArray();
                for (int i = 1; i < ordered.Length; i++)
                {
                    Assert.That(ordered[i - 1].TimeSec + ordered[i - 1].DurationSec,
                        Is.LessThanOrEqualTo(ordered[i].TimeSec + 1e-9d));
                }
            }
        }

        [TestCase(0d)]
        [TestCase(-120d)]
        public void Create_InvalidBpmThrows(double bpm)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PrototypeDanceChart.Create(bpm, 10d));
        }

        private static int CountBits(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }

            return count;
        }
    }
}
