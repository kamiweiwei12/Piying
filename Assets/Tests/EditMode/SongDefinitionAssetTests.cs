using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Chart;
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
    }
}
