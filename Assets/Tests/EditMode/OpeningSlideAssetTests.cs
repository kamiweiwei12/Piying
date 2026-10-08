using NUnit.Framework;
using UnityEngine;

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
    }
}
