using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Unity.Config;

namespace YingYun.Rhythm.Tests
{
    public sealed class SongDefinitionAssetTests
    {
        [Test]
        public void Catalog_ContainsThreeValidUniqueSongs()
        {
            SongCatalogAsset catalog = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Songs.Count, Is.EqualTo(3));
            Assert.That(catalog.Songs.All(song => song != null && song.Music != null), Is.True);
            Assert.That(catalog.Songs.Select(song => song.SongId).Distinct().Count(), Is.EqualTo(3));
            foreach (SongDefinitionAsset song in catalog.Songs)
            {
                Assert.DoesNotThrow(song.ValidateOrThrow, song.Title);
            }
        }

        [Test]
        public void ImportedSongs_RemainAnalysisCandidatesUntilChartsAreAudited()
        {
            SongCatalogAsset catalog = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog");

            Assert.That(
                catalog.Find("xiang-wang-xing-special").CurrentTimingStatus,
                Is.EqualTo(SongDefinitionAsset.TimingStatus.AnalysisCandidate));
            Assert.That(
                catalog.Find("qing-yu-an-lan-jie").CurrentTimingStatus,
                Is.EqualTo(SongDefinitionAsset.TimingStatus.AnalysisCandidate));
            Assert.That(
                catalog.Find("trial-light").CurrentTimingStatus,
                Is.EqualTo(SongDefinitionAsset.TimingStatus.Verified));
        }

        [Test]
        public void ImportedBeatThisTiming_PreservesSourceBeatCountsAndCompoundBars()
        {
            SongCatalogAsset catalog = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog");
            SongDefinitionAsset xiang = catalog.Find("xiang-wang-xing-special");
            SongDefinitionAsset qing = catalog.Find("qing-yu-an-lan-jie");
            SongTimingMap xiangTiming = xiang.CreateTimingMap();
            SongTimingMap qingTiming = qing.CreateTimingMap();

            Assert.That(xiangTiming.Count, Is.EqualTo(399));
            Assert.That(xiang.FirstPlayableSec, Is.EqualTo(0.86d).Within(0.000001d));
            Assert.That(
                Enumerable.Range(0, xiangTiming.Count)
                    .Any(index => xiangTiming[index].BeatsPerBar == 6),
                Is.True);

            Assert.That(qingTiming.Count, Is.EqualTo(382));
            Assert.That(qing.FirstPlayableSec, Is.EqualTo(0.52d).Within(0.000001d));
            Assert.That(
                Enumerable.Range(0, qingTiming.Count)
                    .Any(index => qingTiming[index].BeatsPerBar == 8),
                Is.True);
        }

        [Test]
        public void XiangWangXing_ContainsThreeOrderedDifficultyCharts()
        {
            SongDefinitionAsset song = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog")
                .Find("xiang-wang-xing-special");

            var easy = song.GetNotes(PlayDifficulty.Easy);
            var normal = song.GetNotes(PlayDifficulty.Normal);
            var hard = song.GetNotes(PlayDifficulty.Hard);

            Assert.That(easy.Length, Is.EqualTo(100));
            Assert.That(normal.Length, Is.EqualTo(200));
            Assert.That(hard.Length, Is.EqualTo(598));
            Assert.That(easy.Select(note => note.Lane).Distinct().Count(), Is.EqualTo(6));
            Assert.That(normal.Select(note => note.Lane).Distinct().Count(), Is.EqualTo(6));
            Assert.That(hard.Select(note => note.Lane).Distinct().Count(), Is.EqualTo(6));
            Assert.That(normal.Any(note => note.Kind == NoteKind.Hold), Is.True);
            Assert.That(normal.Any(note => note.IsChord), Is.True);
            Assert.That(hard.Any(note => note.Kind == NoteKind.Hold), Is.True);
            Assert.That(hard.Any(note => note.IsChord), Is.True);
            Assert.That(IsOrdered(easy), Is.True);
            Assert.That(IsOrdered(normal), Is.True);
            Assert.That(IsOrdered(hard), Is.True);
            Assert.DoesNotThrow(song.ValidateOrThrow);
        }

        [Test]
        public void XiangWangXing_DanceAnchorsAreSharedAndCompileToFiftyPhrases()
        {
            SongDefinitionAsset song = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog")
                .Find("xiang-wang-xing-special");
            AuthoredDanceCue[] cues = song.GetDanceCues();

            Assert.That(cues.Length, Is.EqualTo(50));
            Assert.That(cues[0].Action, Is.EqualTo(DanceAction.FinalPose));
            Assert.That(cues[cues.Length - 1].Action, Is.EqualTo(DanceAction.FinalPose));
            Assert.That(cues[cues.Length - 1].DurationBeats, Is.EqualTo(13));
            foreach (PlayDifficulty difficulty in Enum.GetValues(typeof(PlayDifficulty)))
            {
                var notes = song.GetNotes(difficulty);
                var ids = notes.Select(note => note.Id).ToHashSet();
                Assert.That(cues.All(cue => ids.Contains(cue.AnchorNoteId)), Is.True, difficulty.ToString());

                DancePhrase[] phrases = DanceChoreography.CreateAuthored(
                    notes,
                    song.CreateTimingMap(),
                    cues);
                Assert.That(phrases.Length, Is.EqualTo(50), difficulty.ToString());
                Assert.That(phrases.All(phrase => phrase.DurationBeats >= 4), Is.True);
                Assert.That(phrases.All(phrase => phrase.ActiveRodCount <= 2), Is.True);
                Assert.That(
                    phrases[phrases.Length - 1].StartSeconds + phrases[phrases.Length - 1].DurationSeconds,
                    Is.LessThanOrEqualTo(song.PlayableEndSec + 0.001d));
            }
        }

        private static bool IsOrdered(NoteData[] notes)
        {
            for (int i = 1; i < notes.Length; i++)
            {
                if (notes[i].TimeSec < notes[i - 1].TimeSec)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
