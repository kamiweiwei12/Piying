using System;
using System.Linq;
using NUnit.Framework;
using YingYun.Rhythm.Puppet;

namespace YingYun.Rhythm.Tests
{
    /// <summary>
    /// M6.1 連續操偶：關節是帶慣性的彈簧－阻尼系統，
    /// 所以「按下瞬間尚未位移、數十毫秒內明顯被拉動、繩索回鬆後仍繼續滑行」都是規格。
    /// </summary>
    public sealed class PuppetPoseEvaluatorTests
    {
        private const double FrameSeconds = 1d / 60d;
        private const double StepSeconds = PuppetPoseEvaluator.FixedStepSeconds;

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

            double leftExtreme = Extreme(evaluator, pose => pose.LeftUpperArm, 1d, 1.6d);
            double rightExtreme = Extreme(evaluator, pose => pose.RightUpperArm, 1d, 1.6d);

            Assert.That(leftExtreme, Is.LessThan(-40d));
            Assert.That(rightExtreme, Is.Zero.Within(0.0001d));
        }

        [Test]
        public void Tap_PullsTheJointWithinAFewFramesAndPeaksNearTheBindingAngle()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 1d);

            PuppetPose atPress = evaluator.Evaluate(1d);
            Assert.That(atPress.LeftUpperArm, Is.Zero.Within(0.0001d));
            Assert.That(atPress.LeftHandTension, Is.EqualTo(1d));

            // 三幀（約 50 ms）內就必須看得出來被拉動。
            PuppetPose afterThreeFrames = evaluator.Evaluate(1d + (3d * FrameSeconds));
            Assert.That(afterThreeFrames.LeftUpperArm, Is.LessThan(-3d));

            double peak = Extreme(evaluator, pose => pose.LeftUpperArm, 1d + (3d * FrameSeconds), 1.6d);
            Assert.That(peak, Is.EqualTo(-52d).Within(8d));
        }

        [Test]
        public void QPull_RotatesTheLeftArmChainOnly()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 1d);

            double shoulder = Extreme(evaluator, pose => pose.LeftUpperArm, 1d, 1.7d);
            double elbow = Extreme(evaluator, pose => pose.LeftForearm, 1d, 1.7d);
            double rightArm = Extreme(evaluator, pose => pose.RightUpperArm, 1d, 1.7d);

            Assert.That(shoulder, Is.EqualTo(-52d).Within(8d));
            Assert.That(elbow, Is.LessThan(-15d));
            Assert.That(elbow, Is.GreaterThan(-45d));
            Assert.That(rightArm, Is.Zero.Within(0.0001d));
        }

        [Test]
        public void QPlusE_PullsBothArmsAsOnePose()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger((1 << 0) | (1 << 2), 2d);

            PuppetPose atPress = evaluator.Evaluate(2d);
            Assert.That(atPress.LeftHandTension, Is.EqualTo(1d));
            Assert.That(atPress.RightHandTension, Is.EqualTo(1d));

            double left = Extreme(evaluator, pose => pose.LeftUpperArm, 2d, 2.6d);
            double right = Extreme(evaluator, pose => pose.RightUpperArm, 2d, 2.6d);

            Assert.That(left, Is.LessThan(-40d));
            Assert.That(right, Is.GreaterThan(40d));
        }

        [Test]
        public void DPull_RotatesRightHipAndKneeWithoutSlidingTheTorso()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 5, 3d);

            double thigh = Extreme(evaluator, pose => pose.RightThigh, 3d, 3.6d);
            double shin = Extreme(evaluator, pose => pose.RightShin, 3d, 3.6d);
            double torso = Extreme(evaluator, pose => pose.Torso, 3d, 3.6d);

            Assert.That(thigh, Is.EqualTo(24d).Within(8d));
            Assert.That(shin, Is.LessThan(-8d));
            Assert.That(Math.Abs(torso), Is.LessThan(5d));
        }

        [Test]
        public void Hold_MaintainsTensionUntilReleaseThenSettles()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.BeginHold(1 << 4, 1d);

            PuppetPose held = evaluator.Evaluate(3d);
            Assert.That(held.TorsoTension, Is.GreaterThan(0.95d));
            Assert.That(held.Torso, Is.GreaterThan(6d));

            evaluator.Resolve(1 << 4, true, 3d);
            PuppetPose atRelease = evaluator.Evaluate(3d);
            Assert.That(atRelease.TorsoTension, Is.EqualTo(1d).Within(0.05d));

            PuppetPose settled = evaluator.Evaluate(5d);
            Assert.That(settled.Torso, Is.Zero.Within(1d));
            Assert.That(settled.TorsoTension, Is.Zero);
        }

        [Test]
        public void Pull_KeepsMovingAfterTheStringGoesSlackThenRebounds()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 0d);

            // 繩索已回鬆（驅動 0.71 秒結束），關節仍靠慣性滑行，不會停在半空也不會瞬間歸零。
            PuppetPose coasting = evaluator.Evaluate(0.4d);
            Assert.That(coasting.LeftUpperArm, Is.LessThan(-15d));

            double rebound = Maximum(evaluator, pose => pose.LeftUpperArm, 0.4d, 1.6d);
            Assert.That(rebound, Is.GreaterThan(0.5d));
            Assert.That(rebound, Is.LessThan(20d));

            PuppetPose settled = evaluator.Evaluate(1.8d);
            Assert.That(settled.LeftUpperArm, Is.Zero.Within(1d));
        }

        [Test]
        public void RepeatedPulls_KeepTheLimbMovingInsteadOfReturningToRest()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 0d);
            evaluator.Trigger(1 << 0, 0.5d);
            evaluator.Trigger(1 << 0, 1d);
            evaluator.Evaluate(0d);

            Assert.That(evaluator.Evaluate(0.45d).LeftUpperArm, Is.LessThan(-15d));
            Assert.That(evaluator.Evaluate(0.95d).LeftUpperArm, Is.LessThan(-15d));
            Assert.That(evaluator.Evaluate(1.45d).LeftUpperArm, Is.LessThan(-15d));

            double previous = evaluator.Evaluate(1.45d).LeftUpperArm;
            double maxDelta = 0d;
            for (double t = 1.45d + FrameSeconds; t <= 2.4d + 1e-9d; t += FrameSeconds)
            {
                double current = evaluator.Evaluate(t).LeftUpperArm;
                maxDelta = Math.Max(maxDelta, Math.Abs(current - previous));
                previous = current;
            }

            Assert.That(maxDelta, Is.LessThan(10d));
        }

        [Test]
        public void DifferentSampleRates_ProduceTheSamePose()
        {
            var slow = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            var fast = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            slow.Trigger(1 << 0, 0d);
            fast.Trigger(1 << 0, 0d);
            slow.Evaluate(0d);
            fast.Evaluate(0d);

            for (double t = 1d / 30d; t <= 1.5d + 1e-9d; t += 1d / 30d)
            {
                slow.Evaluate(t);
            }

            for (double t = StepSeconds; t <= 1.5d + 1e-9d; t += StepSeconds)
            {
                fast.Evaluate(t);
            }

            Assert.That(slow.Evaluate(1.5d).LeftUpperArm, Is.EqualTo(fast.Evaluate(1.5d).LeftUpperArm).Within(0.001d));
            Assert.That(slow.Evaluate(1.5d).LeftForearm, Is.EqualTo(fast.Evaluate(1.5d).LeftForearm).Within(0.001d));
        }

        [Test]
        public void RepeatedSameKey_ChangesTheGesture()
        {
            // 兩邊第二次按鍵都落在 0.9 秒，差別只有前一顆是否在 1.2 秒的變化窗內。
            var repeated = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            repeated.Trigger(1 << 0, 0d);
            repeated.Trigger(1 << 0, 0.9d);
            repeated.Evaluate(0d);

            var spaced = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            spaced.Trigger(1 << 0, -0.5d);
            spaced.Trigger(1 << 0, 0.9d);
            spaced.Evaluate(-0.5d);

            double repeatedPeak = Minimum(repeated, pose => pose.LeftUpperArm, 0.9d, 1.6d);
            double spacedPeak = Minimum(spaced, pose => pose.LeftUpperArm, 0.9d, 1.6d);
            Assert.That(spacedPeak, Is.LessThan(repeatedPeak - 5d));

            // 手勢不同也反映在繩索張緊的時間長度上。
            Assert.That(repeated.Evaluate(1.15d).LeftHandTension, Is.LessThan(spaced.Evaluate(1.15d).LeftHandTension - 0.15d));
        }

        [Test]
        public void Cascade_DistalJointTrailsTheProximalJoint()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 0d);

            double shoulderPeakTime = ExtremeTime(evaluator, pose => pose.LeftUpperArm, 0.05d, 0.8d);
            double elbowPeakTime = ExtremeTime(evaluator, pose => pose.LeftForearm, 0.05d, 0.8d);

            Assert.That(elbowPeakTime, Is.GreaterThan(shoulderPeakTime + 0.02d));
        }

        [Test]
        public void NoInput_KeepsThePuppetStill()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);

            foreach (double songTime in new[] { 0d, 0.5d, 1d, 2d, 5d })
            {
                PuppetPose pose = evaluator.Evaluate(songTime);
                Assert.That(pose.Head, Is.Zero.Within(0.0001d));
                Assert.That(pose.Torso, Is.Zero.Within(0.0001d));
                Assert.That(pose.LeftUpperArm, Is.Zero.Within(0.0001d));
                Assert.That(pose.LeftHandTension, Is.Zero);
                Assert.That(pose.TorsoTension, Is.Zero);
            }
        }

        [Test]
        public void Fail_DroopsHeadAndTorsoThenSettles()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Evaluate(0d);
            evaluator.Fail(1d);

            double head = Extreme(evaluator, pose => pose.Head, 1d, 1.6d);
            double torso = Extreme(evaluator, pose => pose.Torso, 1d, 1.6d);

            Assert.That(head, Is.GreaterThan(5d));
            Assert.That(torso, Is.GreaterThan(8d));
            Assert.That(evaluator.Evaluate(3d).Head, Is.Zero.Within(0.5d));
        }

        [Test]
        public void Sequence_KeepsThePuppetMovingWithoutPoseJumps()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.Trigger(1 << 0, 0d);
            evaluator.Trigger(1 << 2, 0.5d);
            evaluator.Trigger((1 << 0) | (1 << 2), 1d);
            evaluator.Trigger(1 << 5, 1.5d);
            evaluator.Evaluate(0d);

            double previous = evaluator.Evaluate(0d).LeftUpperArm;
            double maxDelta = 0d;
            for (double t = FrameSeconds; t <= 2d + 1e-9d; t += FrameSeconds)
            {
                double current = evaluator.Evaluate(t).LeftUpperArm;
                maxDelta = Math.Max(maxDelta, Math.Abs(current - previous));
                previous = current;
            }

            Assert.That(maxDelta, Is.LessThan(10d));
            Assert.That(evaluator.Evaluate(0.45d).LeftUpperArm, Is.LessThan(-30d));
            Assert.That(evaluator.Evaluate(1.45d).LeftUpperArm, Is.LessThan(-30d));

            // 整段序列期間手臂平均維持在一半以上的拉動幅度 → 動作沒有斷線。
            double total = 0d;
            int samples = 0;
            for (double t = 0d; t <= 1.5d + 1e-9d; t += FrameSeconds)
            {
                total += Math.Abs(evaluator.Evaluate(t).LeftUpperArm);
                samples++;
            }

            Assert.That(total / samples, Is.GreaterThan(12d));
        }

        [Test]
        public void Hold_KeepsTheArmRaisedForTheWholeHold()
        {
            var evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            evaluator.BeginHold(1 << 0, 1d);

            foreach (double songTime in new[] { 1.4d, 1.6d, 1.8d, 2d, 2.2d, 2.6d })
            {
                PuppetPose pose = evaluator.Evaluate(songTime);
                Assert.That(pose.LeftUpperArm, Is.EqualTo(-52d).Within(8d), $"songTime={songTime}");
                Assert.That(pose.LeftHandTension, Is.EqualTo(1d).Within(0.01d), $"songTime={songTime}");
            }
        }

        [Test]
        public void Hold_SustainsWhileATapDecays()
        {
            var hold = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            var tap = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            hold.BeginHold(1 << 0, 0d);
            tap.Trigger(1 << 0, 0d);

            double raised = Math.Abs(hold.Evaluate(1.2d).LeftUpperArm);
            double decayed = Math.Abs(tap.Evaluate(1.2d).LeftUpperArm);

            Assert.That(raised, Is.GreaterThan(45d));
            Assert.That(decayed, Is.LessThan(12d));
        }

        private static double Extreme(
            PuppetPoseEvaluator evaluator,
            Func<PuppetPose, double> selector,
            double fromSeconds,
            double toSeconds)
        {
            double extreme = 0d;
            for (double t = fromSeconds; t <= toSeconds + 1e-9d; t += FrameSeconds)
            {
                double value = selector(evaluator.Evaluate(t));
                if (Math.Abs(value) > Math.Abs(extreme))
                {
                    extreme = value;
                }
            }

            double last = selector(evaluator.Evaluate(toSeconds));
            return Math.Abs(last) > Math.Abs(extreme) ? last : extreme;
        }

        private static double Maximum(
            PuppetPoseEvaluator evaluator,
            Func<PuppetPose, double> selector,
            double fromSeconds,
            double toSeconds)
        {
            double maximum = double.NegativeInfinity;
            for (double t = fromSeconds; t <= toSeconds + 1e-9d; t += FrameSeconds)
            {
                maximum = Math.Max(maximum, selector(evaluator.Evaluate(t)));
            }

            return maximum;
        }

        private static double Minimum(
            PuppetPoseEvaluator evaluator,
            Func<PuppetPose, double> selector,
            double fromSeconds,
            double toSeconds)
        {
            double minimum = double.PositiveInfinity;
            for (double t = fromSeconds; t <= toSeconds + 1e-9d; t += FrameSeconds)
            {
                minimum = Math.Min(minimum, selector(evaluator.Evaluate(t)));
            }

            return minimum;
        }

        private static double ExtremeTime(
            PuppetPoseEvaluator evaluator,
            Func<PuppetPose, double> selector,
            double fromSeconds,
            double toSeconds)
        {
            double extreme = 0d;
            double extremeTime = fromSeconds;
            for (double t = fromSeconds; t <= toSeconds + 1e-9d; t += StepSeconds)
            {
                double value = selector(evaluator.Evaluate(t));
                if (Math.Abs(value) > Math.Abs(extreme))
                {
                    extreme = value;
                    extremeTime = t;
                }
            }

            return extremeTime;
        }
    }
}
