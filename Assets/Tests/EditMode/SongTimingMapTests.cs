using System;
using System.Linq;
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
                (1 << 1) | (1 << 4), // Y+H
                (1 << 0) | (1 << 5), // T+J
                (1 << 2) | (1 << 3), // U+G
            };
            for (int i = 0; i < forbiddenMasks.Length; i++)
            {
                int mask = forbiddenMasks[i];
                var chord = new NoteData(30 + i, "chord", FirstLane(mask), 20d + i, requiredLanesMask: mask);
                Assert.Throws<ArgumentException>(() => SongChartValidation.ValidateDifficultyFeatures(
                    new[] { tap, hold, chord }, PlayDifficulty.Hard), $"forbidden mask {mask}");
            }
        }

        [Test]
        public void LayoutValidation_RejectsDenseMixedAndMechanicalHardPatterns()
        {
            var timing = new SongTimingMap(new[] { new SongTimingPoint(0d, 0d, 120d) });
            int chordMask = (1 << 0) | (1 << 1);
            var mixed = new[]
            {
                new NoteData(1, "tap", 0, 0d, segmentId: 0),
                new NoteData(2, "hold", 2, 0.5d, 0.5d, segmentId: 0),
                new NoteData(3, "chord", 0, 1.5d, segmentId: 0, requiredLanesMask: chordMask),
            };
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                mixed, PlayDifficulty.Hard, timing));

            var sweep = Enumerable.Range(0, 5)
                .Select(index => new NoteData(10 + index, "tap", index, index * 0.5d, segmentId: 0))
                .ToArray();
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                sweep, PlayDifficulty.Hard, timing));
        }

        [Test]
        public void LayoutValidation_RejectsOverloadedBeatCloseChordsAndDensePhrase()
        {
            var timing = new SongTimingMap(new[] { new SongTimingPoint(0d, 0d, 120d) });
            var overloaded = new[]
            {
                new NoteData(1, "tap", 0, 0d, segmentId: 0),
                new NoteData(2, "tap", 2, 0.1d, segmentId: 0),
                new NoteData(3, "tap", 4, 0.2d, segmentId: 0),
            };
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                overloaded, PlayDifficulty.Hard, timing));

            int firstMask = (1 << 0) | (1 << 1);
            int secondMask = (1 << 2) | (1 << 4);
            var closeChords = new[]
            {
                new NoteData(10, "chord", 0, 0d, segmentId: 0, requiredLanesMask: firstMask),
                new NoteData(11, "chord", 2, 0.5d, segmentId: 1, requiredLanesMask: secondMask),
            };
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                closeChords, PlayDifficulty.Hard, timing));

            int[] motif = { 0, 2, 4, 1, 5, 3, 1, 4, 0, 5, 2 };
            NoteData[] densePhrase = motif.Select((lane, index) =>
                new NoteData(20 + index, "tap", lane, index * 0.5d, segmentId: 0)).ToArray();
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                densePhrase, PlayDifficulty.Hard, timing));
        }

        [Test]
        public void AccessibleHardValidation_RejectsTwoLaneLoopsAndRecentRhythmRepeats()
        {
            SongTimingMap timing = CreateTiming(24);
            int[] loopLanes = { 0, 1, 0, 1 };
            NoteData[] twoLaneLoop = loopLanes.Select((lane, index) =>
                new NoteData(100 + index, "tap", lane, timing.BeatToSeconds(index * 2), segmentId: 0))
                .ToArray();
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                twoLaneLoop, PlayDifficulty.Hard, timing));

            int[] lanes = { 0, 2, 4, 1, 3, 5, 0, 2 };
            NoteData[] repeatedRhythm = lanes.Select((lane, index) =>
            {
                int phrase = index / 4;
                int localEvent = index % 4;
                int beat = (phrase * 8) + (localEvent * 2);
                return new NoteData(200 + index, "tap", lane, timing.BeatToSeconds(beat), segmentId: phrase);
            }).ToArray();
            Assert.Throws<ArgumentException>(() => SongChartValidation.ValidatePlayableLayout(
                repeatedRhythm, PlayDifficulty.Hard, timing));
        }

        private static SongTimingMap CreateTiming(int count)
        {
            var points = new SongTimingPoint[count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new SongTimingPoint(i * 0.5d, i, 120d, 4);
            }
            return new SongTimingMap(points);
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
