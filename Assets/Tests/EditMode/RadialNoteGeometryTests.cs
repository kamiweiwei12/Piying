using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class RadialNoteGeometryTests
    {
        private static readonly Vector2 Spawn = new Vector2(-6f, 4f);
        private static readonly Vector2 Receptor = new Vector2(-2f, 2f);

        [Test]
        public void Position_AtVisibleLead_IsSpawnPosition()
        {
            Vector2 actual = RadialNoteGeometry.Position(Spawn, Receptor, 8d, 10d, 2d);
            Assert.That(actual, Is.EqualTo(Spawn));
        }

        [Test]
        public void Position_AtNoteTime_IsReceptorPosition()
        {
            Vector2 actual = RadialNoteGeometry.Position(Spawn, Receptor, 10d, 10d, 2d);
            Assert.That(actual, Is.EqualTo(Receptor));
        }

        [Test]
        public void Position_Halfway_InterpolatesFromAbsoluteSongTime()
        {
            Vector2 actual = RadialNoteGeometry.Position(Spawn, Receptor, 9d, 10d, 2d);
            Assert.That(actual.x, Is.EqualTo(-4f).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(3f).Within(0.0001f));
        }

        [TestCase(7d, 0f)]
        [TestCase(11d, 1f)]
        public void Progress_ClampsOutsideVisibleWindow(double songTime, float expected)
        {
            Assert.That(RadialNoteGeometry.Progress(songTime, 10d, 2d), Is.EqualTo(expected));
        }

        [Test]
        public void BodyReceptor_SharesBottomRowWithBothFeet()
        {
            Vector2 leftFoot = RadialNotePresenter.ReceptorPositionForLane(3);
            Vector2 body = RadialNotePresenter.ReceptorPositionForLane(4);
            Vector2 rightFoot = RadialNotePresenter.ReceptorPositionForLane(5);

            Assert.That(body.x, Is.Zero.Within(0.0001f));
            Assert.That(body.y, Is.EqualTo(leftFoot.y).Within(0.0001f));
            Assert.That(body.y, Is.EqualTo(rightFoot.y).Within(0.0001f));
        }
    }
}
