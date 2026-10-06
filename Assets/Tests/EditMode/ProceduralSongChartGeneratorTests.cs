using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using YingYun.Rhythm.Audio;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Unity.CustomSongs;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class ProceduralSongChartGeneratorTests
    {
        [Test]
        public void BeatGridParser_PreservesVariableMeterAndContinuousTiming()
        {
            SongTimingPoint[] points = BeatGridParser.Parse(new[]
            {
                "0.500\t1", "1.000\t2", "1.500\t3", "2.000\t4",
                "2.500\t1", "3.100\t2", "3.700\t3",
            });

            var timing = new SongTimingMap(points);
            Assert.That(timing.Count, Is.EqualTo(7));
            Assert.That(timing[0].BeatsPerBar, Is.EqualTo(4));
            Assert.That(timing[4].BeatsPerBar, Is.EqualTo(3));
            Assert.That(timing.BeatToSeconds(5d), Is.EqualTo(3.1d).Within(0.000001d));
        }

        [Test]
        public void BeatGridParser_ReportsInvalidSourceRow()
        {
            FormatException exception = Assert.Throws<FormatException>(() =>
                BeatGridParser.Parse(new[] { "0.5\t1", "broken" }));
            Assert.That(exception.Message, Does.Contain("row 2"));
        }

        [Test]
        public void FloatWavReader_ReadsAnalyzerCacheFormat()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
            try
            {
                float[] expected = { -0.5f, 0.25f, 0.75f, -1f };
                using (FileStream stream = File.Create(path))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(new[] { 'R', 'I', 'F', 'F' });
                    writer.Write(36 + (expected.Length * 4));
                    writer.Write(new[] { 'W', 'A', 'V', 'E' });
                    writer.Write(new[] { 'f', 'm', 't', ' ' });
                    writer.Write(16);
                    writer.Write((ushort)3);
                    writer.Write((ushort)2);
                    writer.Write(44100);
                    writer.Write(44100 * 2 * 4);
                    writer.Write((ushort)8);
                    writer.Write((ushort)32);
                    writer.Write(new[] { 'd', 'a', 't', 'a' });
                    writer.Write(expected.Length * 4);
                    foreach (float sample in expected) writer.Write(sample);
                }

                DecodedAudioData result = FloatWavReader.Read(path);
                Assert.That(result.Channels, Is.EqualTo(2));
                Assert.That(result.SampleRate, Is.EqualTo(44100));
                Assert.That(result.FrameCount, Is.EqualTo(2));
                Assert.That(result.Samples, Is.EqualTo(expected));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [TestCase("song.mp3", true)]
        [TestCase("song.MP3", true)]
        [TestCase("song.flac", true)]
        [TestCase("song.FLAC", true)]
        [TestCase("song.wav", true)]
        [TestCase("song.txt", false)]
        public void CustomSongLibrary_RecognizesSupportedAudioExtensions(string path, bool expected)
        {
            Assert.That(CustomSongLibrary.IsSupportedAudioFile(path), Is.EqualTo(expected));
        }

        [Test]
        public void SongMenu_WhenFourthSongIsAdded_MovesToSecondPage()
        {
            Assert.That(DemoFlowPresenter.DetermineSongPage(3, 4, 0), Is.EqualTo(1));
        }

        [Test]
        public void SongMenu_WhenSongCountIsStable_KeepsSelectedSongsPage()
        {
            Assert.That(DemoFlowPresenter.DetermineSongPage(4, 4, 1), Is.EqualTo(0));
            Assert.That(DemoFlowPresenter.DetermineSongPage(4, 4, 3), Is.EqualTo(1));
        }

        [Test]
        public void Generate_IsDeterministicAndKeepsDifficultyContracts()
        {
            SongTimingMap timing = CreateTiming(160);
            double endSec = timing.BeatToSeconds(164);
            ProceduralSongContent first = ProceduralSongChartGenerator.Generate(timing, endSec, 123456789);
            ProceduralSongContent second = ProceduralSongChartGenerator.Generate(timing, endSec, 123456789);

            foreach (PlayDifficulty difficulty in Enum.GetValues(typeof(PlayDifficulty)))
            {
                NoteData[] left = first.GetNotes(difficulty);
                NoteData[] right = second.GetNotes(difficulty);
                Assert.That(left.Length, Is.EqualTo(right.Length));
                for (int i = 0; i < left.Length; i++)
                {
                    Assert.That(left[i].Id, Is.EqualTo(right[i].Id));
                    Assert.That(left[i].Lane, Is.EqualTo(right[i].Lane));
                    Assert.That(left[i].TimeSec, Is.EqualTo(right[i].TimeSec));
                    Assert.That(left[i].DurationSec, Is.EqualTo(right[i].DurationSec));
                    Assert.That(left[i].RequiredLanesMask, Is.EqualTo(right[i].RequiredLanesMask));
                }
                Assert.DoesNotThrow(() => SongChartValidation.ValidateDifficultyFeatures(left, difficulty));
            }

            Assert.That(first.DanceCues.Select(cue => cue.Action),
                Is.EqualTo(second.DanceCues.Select(cue => cue.Action)));
        }

        [Test]
        public void Generate_UsesVariedHoldsAndOnlyErgonomicHardChords()
        {
            SongTimingMap timing = CreateTiming(192);
            ProceduralSongContent content = ProceduralSongChartGenerator.Generate(
                timing,
                timing.BeatToSeconds(196),
                unchecked((int)0xE7FB17E6));

            NoteData[] easy = content.GetNotes(PlayDifficulty.Easy);
            NoteData[] normal = content.GetNotes(PlayDifficulty.Normal);
            NoteData[] hard = content.GetNotes(PlayDifficulty.Hard);
            Assert.That(easy.All(note => note.Kind == NoteKind.Tap && !note.IsChord), Is.True);
            Assert.That(normal.Any(note => note.Kind == NoteKind.Hold), Is.True);
            Assert.That(normal.Any(note => note.IsChord), Is.False);
            Assert.That(hard.Any(note => note.Kind == NoteKind.Hold), Is.True);
            Assert.That(hard.Any(note => note.IsChord), Is.True);
            Assert.That(hard.Where(note => note.IsChord).All(note =>
                KeyboardChordLayout.IsAllowed(note.RequiredLanesMask)), Is.True);

            int[] holdBeats = normal.Where(note => note.Kind == NoteKind.Hold)
                .Select(note => (int)Math.Round(
                    timing.SecondsToBeat(note.EndTimeSec) - timing.SecondsToBeat(note.TimeSec)))
                .Distinct().OrderBy(value => value).ToArray();
            Assert.That(holdBeats, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Generate_HardChartRespectsPhraseDensityAndInputPatterns()
        {
            SongTimingMap timing = CreateTiming(192);
            ProceduralSongContent content = ProceduralSongChartGenerator.Generate(
                timing,
                timing.BeatToSeconds(196),
                20261006);
            NoteData[] hard = content.GetNotes(PlayDifficulty.Hard);

            Assert.DoesNotThrow(() => SongChartValidation.ValidatePlayableLayout(
                hard,
                PlayDifficulty.Hard,
                timing));
            Assert.That(hard.GroupBy(note => note.SegmentId).All(phrase =>
                phrase.Count() <= SongChartValidation.AccessibleHardMaxEventsPerPhrase), Is.True);
            Assert.That(hard.GroupBy(note => note.SegmentId)
                .Where(phrase => (phrase.Key * 8) + 7 < timing.Count)
                .All(phrase => phrase.Count() >= SongChartValidation.AccessibleHardMinEventsPerFullPhrase), Is.True);
            Assert.That(hard.GroupBy(note => note.SegmentId).All(phrase =>
                !phrase.Any(note => note.Kind == NoteKind.Hold) ||
                !phrase.Any(note => note.IsChord)), Is.True);

            double[] chordBeats = hard.Where(note => note.IsChord)
                .Select(note => timing.SecondsToBeat(note.TimeSec))
                .ToArray();
            Assert.That(chordBeats.Zip(chordBeats.Skip(1),
                (left, right) => right - left >= 16d - 0.000001d).All(value => value), Is.True);

            string[] rhythms = hard.GroupBy(note => note.SegmentId)
                .Where(phrase => (phrase.Key * 8) + 7 < timing.Count)
                .OrderBy(phrase => phrase.Key)
                .Select(phrase => string.Join(",", phrase.OrderBy(note => note.TimeSec)
                    .Select(note => (int)Math.Round(
                        (timing.SecondsToBeat(note.TimeSec) - (phrase.Key * 8)) * 2d))))
                .ToArray();
            for (int i = 0; i < rhythms.Length; i++)
            {
                Assert.That(
                    rhythms.Skip(Math.Max(0, i - 4)).Take(Math.Min(4, i)).Contains(rhythms[i]),
                    Is.False);
            }

            int[] singleLanes = hard.Where(note => !note.IsChord).Select(note => note.Lane).ToArray();
            for (int start = 0; start + 18 <= singleLanes.Length; start++)
            {
                int[] window = singleLanes.Skip(start).Take(18).ToArray();
                Assert.That(window.Distinct().Count(), Is.EqualTo(6));
                Assert.That(window.GroupBy(lane => lane).Max(group => group.Count()), Is.LessThanOrEqualTo(6));
            }
        }

        [Test]
        public void Generate_SharesDanceAnchorsAndAvoidsRepeatedPhrases()
        {
            SongTimingMap timing = CreateTiming(128);
            ProceduralSongContent content = ProceduralSongChartGenerator.Generate(
                timing,
                timing.BeatToSeconds(132),
                42);

            foreach (PlayDifficulty difficulty in Enum.GetValues(typeof(PlayDifficulty)))
            {
                NoteData[] notes = content.GetNotes(difficulty);
                foreach (AuthoredDanceCue cue in content.DanceCues)
                {
                    Assert.That(notes.Any(note => note.Id == cue.AnchorNoteId && note.Kind == NoteKind.Tap), Is.True);
                }
            }

            for (int i = 1; i < content.DanceCues.Length - 1; i++)
            {
                Assert.That(content.DanceCues[i].Action, Is.Not.EqualTo(content.DanceCues[i - 1].Action));
            }
        }

        private static SongTimingMap CreateTiming(int count)
        {
            var points = new SongTimingPoint[count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new SongTimingPoint(0.5d + (i * 0.5d), i, 120d, 4);
            }
            return new SongTimingMap(points);
        }
    }
}
