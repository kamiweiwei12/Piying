using System.Linq;
using NUnit.Framework;
using YingYun.Rhythm.Puppet;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetPoseEvaluatorTests
    {
        [Test]
        public void Bindings_CoverSixUniqueLanesAndBodyParts()
        {
            ActionBinding[] bindings = PrototypeActionBindings.All;

            Assert.That(bindings.Select(binding => binding.Lane).Distinct().Count(), Is.EqualTo(6));
            Assert.That(bindings.Select(binding => binding.Part).Distinct().Count(), Is.EqualTo(6));
            Assert.That(bindings.Select(binding => binding.KeyLabel), Is.EqualTo(new[] { "Q", "W", "E", "A", "S", "D" }));
        }

        [Test]
        public void Bindings_AreResolvedByLaneRatherThanArrayOrder()
        {
            ActionBinding[] reordered = PrototypeActionBindings.All.Reverse().ToArray();
            var evaluator = new PuppetPoseEvaluator(reordered);
            evaluator.Trigger(1 << 0, 1d);

            PuppetPose pose = evaluator.Evaluate(1d);

            Assert.That(pose.LeftUpperArm, Is.EqualTo(-52d).Within(0.0001d));
            Assert.That(pose.RightUpperArm, Is.Zero);
        }

        [Test]
        public void QPull_RotatesOnlyLeftArmAtTheJoint()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 1d);

            PuppetPose pose = evaluator.Evaluate(1d);

            Assert.That(pose.LeftUpperArm, Is.EqualTo(-52d).Within(0.0001d));
            Assert.That(pose.LeftForearm, Is.EqualTo(-28d).Within(0.0001d));
            Assert.That(pose.RightUpperArm, Is.Zero);
            Assert.That(pose.LeftHandTension, Is.EqualTo(1d));
        }

        [Test]
        public void QPlusE_PullsBothArmsAsOnePose()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger((1 << 0) | (1 << 2), 2d);

            PuppetPose pose = evaluator.Evaluate(2d);

            Assert.That(pose.LeftUpperArm, Is.LessThan(-50d));
            Assert.That(pose.RightUpperArm, Is.GreaterThan(50d));
            Assert.That(pose.LeftHandTension, Is.EqualTo(1d));
            Assert.That(pose.RightHandTension, Is.EqualTo(1d));
        }

        [Test]
        public void DPull_RotatesRightHipAndKneeWithoutSlidingTheTorso()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 5, 3d);

            PuppetPose pose = evaluator.Evaluate(3d);

            Assert.That(pose.RightThigh, Is.EqualTo(24d).Within(0.0001d));
            Assert.That(pose.RightShin, Is.EqualTo(-18d).Within(0.0001d));
            Assert.That(pose.Torso, Is.Zero);
        }

        [Test]
        public void Hold_MaintainsTensionUntilReleaseThenSettles()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.BeginHold(1 << 4, 1d);

            PuppetPose held = evaluator.Evaluate(4d);
            evaluator.Resolve(1 << 4, true, 4d);
            PuppetPose releaseStart = evaluator.Evaluate(4d);
            PuppetPose settled = evaluator.Evaluate(4.8d);

            Assert.That(held.TorsoTension, Is.GreaterThan(0.95d));
            Assert.That(releaseStart.TorsoTension, Is.EqualTo(1d));
            Assert.That(settled.Torso, Is.Zero.Within(0.0001d));
            Assert.That(settled.TorsoTension, Is.Zero);
        }

        [Test]
        public void Release_ProducesASmallOppositeDirectionRebound()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 0d);

            PuppetPose initial = evaluator.Evaluate(0d);
            PuppetPose rebound = evaluator.Evaluate(0.25d);
            PuppetPose settled = evaluator.Evaluate(0.8d);

            Assert.That(initial.LeftUpperArm, Is.LessThan(0d));
            Assert.That(rebound.LeftUpperArm, Is.GreaterThan(0d));
            Assert.That(settled.LeftUpperArm, Is.Zero.Within(0.0001d));
        }

        [Test]
        public void SameEventSequenceAndSongTime_ProducesSamePose()
        {
            var first = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            var second = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            first.Trigger(1 << 0, 1d);
            first.Trigger((1 << 0) | (1 << 2), 1.4d);
            first.Trigger(1 << 5, 1.8d);
            second.Trigger(1 << 0, 1d);
            second.Trigger((1 << 0) | (1 << 2), 1.4d);
            second.Trigger(1 << 5, 1.8d);

            PuppetPose a = first.Evaluate(1.9d);
            PuppetPose b = second.Evaluate(1.9d);

            Assert.That(a.LeftUpperArm, Is.EqualTo(b.LeftUpperArm));
            Assert.That(a.RightUpperArm, Is.EqualTo(b.RightUpperArm));
            Assert.That(a.RightThigh, Is.EqualTo(b.RightThigh));
        }
    }
}
