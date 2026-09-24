using System;
using NUnit.Framework;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Tests
{
    public sealed class SongTimingMapTests
    {
        [Test]
        public void FixedTempo_ConvertsSecondsAndBeatsBothWays()
        {
            var map = new SongTimingMap(new[] { new SongTimingPoint(1.25d, 0d, 120d) });

            Assert.That(map.SecondsToBeat(2.25d), Is.EqualTo(2d).Within(1e-9d));
            Assert.That(map.BeatToSeconds(2d), Is.EqualTo(2.25d).Within(1e-9d));
            Assert.That(map.SecondsToBeat(0.25d), Is.EqualTo(-2d).Within(1e-9d));
        }

        [Test]
        public void TempoChange_PreservesContinuityAndRoundTrips()
        {
            var map = new SongTimingMap(new[]
            {
                new SongTimingPoint(0.5d, 0d, 120d),
                new SongTimingPoint(4.5d, 8d, 90d),
            });

            Assert.That(map.SecondsToBeat(4.5d), Is.EqualTo(8d).Within(1e-9d));
            Assert.That(map.SecondsToBeat(6.5d), Is.EqualTo(11d).Within(1e-9d));
            Assert.That(map.BeatToSeconds(11d), Is.EqualTo(6.5d).Within(1e-9d));
        }

        [Test]
        public void DiscontinuousOrUnsortedPoints_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new SongTimingMap(new[]
            {
                new SongTimingPoint(0d, 0d, 120d),
                new SongTimingPoint(2d, 5d, 120d),
            }));
            Assert.Throws<ArgumentException>(() => new SongTimingMap(new[]
            {
                new SongTimingPoint(2d, 0d, 120d),
                new SongTimingPoint(1d, 2d, 120d),
            }));
        }

        [Test]
        public void NotesOutsidePlayableRange_AreRejected()
        {
            var notes = new[] { new NoteData(1, "tap", 0, 1d) };

            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateNotes(
                notes, 10d, new SongPlayableRange(2d, 9d)));
        }

        [Test]
        public void SortedNotesInsidePlayableRange_AreAccepted()
        {
            var notes = new[]
            {
                new NoteData(1, "tap", 0, 2d),
                new NoteData(2, "hold", 1, 4d, 1d),
            };

            Assert.DoesNotThrow(() => SongChartValidation.ValidateNotes(
                notes, 10d, new SongPlayableRange(2d, 9d)));
        }

        [Test]
        public void HoldEndingAfterPlayableRange_IsRejected()
        {
            var notes = new[] { new NoteData(1, "hold", 0, 8.5d, 1d) };

            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateNotes(
                notes, 10d, new SongPlayableRange(2d, 9d)));
        }

        [Test]
        public void DifficultyFeatures_AcceptOnlyTheApprovedMechanicLadder()
        {
            var tap = new NoteData(1, "tap", 0, 1d);
            var hold = new NoteData(2, "hold", 1, 2d, 1d);
            var chord = new NoteData(3, "chord", 0, 4d, requiredLanesMask: 5);

            Assert.DoesNotThrow(() => SongChartValidation.ValidateDifficultyFeatures(
                new[] { tap }, PlayDifficulty.Easy));
            Assert.DoesNotThrow(() => SongChartValidation.ValidateDifficultyFeatures(
                new[] { tap, hold }, PlayDifficulty.Normal));
            Assert.DoesNotThrow(() => SongChartValidation.ValidateDifficultyFeatures(
                new[] { tap, hold, chord }, PlayDifficulty.Hard));
        }

        [Test]
        public void DifficultyFeatures_RejectMechanicsAssignedToTheWrongLevel()
        {
            var tap = new NoteData(1, "tap", 0, 1d);
            var hold = new NoteData(2, "hold", 1, 2d, 1d);
            var chord = new NoteData(3, "chord", 0, 4d, requiredLanesMask: 5);

            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateDifficultyFeatures(
                new[] { tap, hold }, PlayDifficulty.Easy));
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateDifficultyFeatures(
                new[] { tap, hold, chord }, PlayDifficulty.Normal));
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateDifficultyFeatures(
                new[] { tap, hold }, PlayDifficulty.Hard));
        }

        [Test]
        public void DifficultyFeatures_AllowsTwelvePairsAndRejectsOnlyThreePairs()
        {
            var tap = new NoteData(1, "tap", 0, 1d);
            var hold = new NoteData(2, "hold", 1, 2d, 1d);
            for (int i = 0; i < KeyboardChordLayout.AllowedCount; i++)
            {
                int mask = KeyboardChordLayout.GetAllowedMask(i);
                var chord = new NoteData(10 + i, "chord", FirstLane(mask), 4d + i, requiredLanesMask: mask);
                Assert.DoesNotThrow(() => SongChartValidation.ValidateDifficultyFeatures(
                    new[] { tap, hold, chord }, PlayDifficulty.Hard), $"allowed mask {mask}");
            }

            int[] forbiddenMasks =
            {
                (1 << 1) | (1 << 4), // W+S
                (1 << 0) | (1 << 5), // Q+D
                (1 << 2) | (1 << 3), // E+A
            };
            for (int i = 0; i < forbiddenMasks.Length; i++)
            {
                int mask = forbiddenMasks[i];
                var chord = new NoteData(30 + i, "chord", FirstLane(mask), 20d + i, requiredLanesMask: mask);
                Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateDifficultyFeatures(
                    new[] { tap, hold, chord }, PlayDifficulty.Hard), $"forbidden mask {mask}");
            }
        }

        private static int FirstLane(int mask)
        {
            for (int lane = 0; lane < KeyboardChordLayout.LaneCount; lane++)
            {
                if ((mask & (1 << lane)) != 0)
                {
                    return lane;
                }
            }

            throw new ArgumentException("Mask needs a lane.", nameof(mask));
        }
    }
}
