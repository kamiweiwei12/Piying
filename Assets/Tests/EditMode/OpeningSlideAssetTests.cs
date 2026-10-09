using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class OpeningSlideAssetTests
    {
        [Test]
        public void OpeningSlides_AreBundledWithMatchingDimensions()
        {
            Texture2D laugh = Resources.Load<Texture2D>("YingYun/UI/Opening/OpeningLaugh");
            Texture2D finalPose = Resources.Load<Texture2D>("YingYun/UI/Opening/OpeningFinal");

            Assert.That(laugh, Is.Not.Null);
            Assert.That(finalPose, Is.Not.Null);
            Assert.That(finalPose.width, Is.EqualTo(laugh.width));
            Assert.That(finalPose.height, Is.EqualTo(laugh.height));
            Assert.That((float)laugh.width / laugh.height, Is.EqualTo(16f / 9f).Within(0.02f));
        }

        /// <summary>開場循環動畫走 StreamingAssets（Web 平台不支援內嵌 VideoClip），必須隨建置一起出貨。</summary>
        [Test]
        public void OpeningLoopVideo_IsBundledInStreamingAssets()
        {
            string path = Path.Combine(Application.streamingAssetsPath, DemoFlowPresenter.OpeningVideoRelativePath);

            Assert.That(File.Exists(path), Is.True, $"Missing opening loop video: {path}");
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), "Opening loop video is empty.");
        }
    }
}
