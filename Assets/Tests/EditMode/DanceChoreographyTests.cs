using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Tests
{
    public sealed class DanceChoreographyTests
    {
        [Test]
        public void ExperimentalPalmGestures_ArePrecomputedAndUseOneHandRod()
        {
            HandGesturePhrase press = HandGestureChoreography.Get(HandGesture.PressPalm);
            HandGesturePhrase support = HandGestureChoreography.Get(HandGesture.SupportPalm);

            Assert.That(press.Name, Does.Contain("按掌"));
            Assert.That(support.Name, Does.Contain("托掌"));
            Assert.That(press.ActiveRodCount, Is.EqualTo(1));
            Assert.That(support.ActiveRodCount, Is.EqualTo(1));
            Assert.That(press.Shoulder(0d), Is.Zero.Within(0.000001d));
            Assert.That(support.Shoulder(0d), Is.EqualTo(press.Shoulder(1d)).Within(0.000001d));
            Assert.That(support.Elbow(0d), Is.EqualTo(press.Elbow(1d)).Within(0.000001d));
            Assert.That(support.Wrist(0d), Is.EqualTo(press.Wrist(1d)).Within(0.000001d));
            Assert.That(press.Wrist(1d), Is.GreaterThan(55d));
            Assert.That(support.Wrist(1d), Is.LessThan(-65d));
        }

        [Test]
        public void ExperimentalThreadPalmAndTurnWrist_ArePrecomputedOneRodCapabilities()
        {
            HandGesturePhrase thread = HandGestureChoreography.Get(HandGesture.ThreadPalm);
            HandGesturePhrase turn = HandGestureChoreography.Get(HandGesture.TurnWrist);

            Assert.That(thread.Name, Does.Contain("穿掌"));
            Assert.That(turn.Name, Does.Contain("翻腕"));
            Assert.That(thread.ActiveRodCount, Is.EqualTo(1));
            Assert.That(turn.ActiveRodCount, Is.EqualTo(1));
            Assert.That(thread.Shoulder(0d), Is.Zero.Within(0.000001d));
            Assert.That(thread.Elbow(0.34d), Is.GreaterThan(65d));
            Assert.That(thread.Elbow(1d), Is.LessThan(25d));
            Assert.That(System.Math.Abs(turn.Wrist(0.5d) - turn.Wrist(1d)), Is.GreaterThan(170d));
        }

        [Test]
        public void ExperimentalFistPalmSalute_IsPrecomputedAndUsesBothHandRods()
        {
            FistPalmSalutePhrase salute = FistPalmSaluteChoreography.Get();
            Assert.That(salute.Name, Does.Contain("名稱待核"));
            Assert.That(salute.ActiveRodCount, Is.EqualTo(2));
            Assert.That(salute.LeftShoulder(0d), Is.Zero.Within(0.000001d));
            Assert.That(salute.RightShoulder(0d), Is.Zero.Within(0.000001d));
            Assert.That(salute.RightClosure(1d), Is.GreaterThan(0.75d));
        }

        [Test]
        public void AcceptedHandGestures_AreFormalAnchoredPhrasesAfterTheOriginalEight()
        {
            DancePhrase[] phrases = Phrases();
            Assert.That(phrases[8].Action, Is.EqualTo(DanceAction.PressPalm));
            Assert.That(phrases[8].Name, Is.EqualTo("按掌"));
            Assert.That(phrases[8].HandGesture.Gesture, Is.EqualTo(HandGesture.PressPalm));
            Assert.That(phrases[8].ActiveRodCount, Is.EqualTo(1));
            Assert.That(phrases[9].Action, Is.EqualTo(DanceAction.SupportPalm));
            Assert.That(phrases[9].Name, Is.EqualTo("托掌"));
            Assert.That(phrases[9].HandGesture.Gesture, Is.EqualTo(HandGesture.SupportPalm));
            Assert.That(phrases[9].DurationBeats, Is.EqualTo(8));
            Assert.That(phrases[9].HasClosing, Is.True);
            Assert.That(phrases[9].HandGesture.Wrist(0.75d), Is.LessThan(-65d));
            Assert.That(phrases[9].HandGesture.Wrist(1d), Is.Zero.Within(0.000001d));
            Assert.That(phrases[10].Action, Is.EqualTo(DanceAction.ThreadPalm));
            Assert.That(phrases[10].Name, Is.EqualTo("穿掌"));
            Assert.That(phrases[10].HandGesture.Gesture, Is.EqualTo(HandGesture.ThreadPalm));
            Assert.That(phrases[10].HasClosing, Is.False);
            Assert.That(phrases[11].Action, Is.EqualTo(DanceAction.TurnWrist));
            Assert.That(phrases[11].Name, Is.EqualTo("翻腕"));
            Assert.That(phrases[11].HandGesture.Gesture, Is.EqualTo(HandGesture.TurnWrist));
            Assert.That(phrases[11].HasClosing, Is.True);
            Assert.That(phrases[11].HandGesture.Wrist(0d),
                Is.EqualTo(phrases[10].HandGesture.Wrist(1d)).Within(0.000001d));
            Assert.That(phrases[11].HandGesture.Wrist(1d), Is.Zero.Within(0.000001d));
            Assert.That(phrases[12].Action, Is.EqualTo(DanceAction.FistPalmSalute));
            Assert.That(phrases[12].Name, Is.EqualTo("拳掌禮"));
            Assert.That(phrases[12].FistPalmSalute, Is.Not.Null);
            Assert.That(phrases[12].ActiveRodCount, Is.EqualTo(2));
            Assert.That(phrases[12].HasClosing, Is.True);
            Assert.That(phrases[12].FistPalmSalute.RightClosure(0.7d), Is.GreaterThan(0.75d));
            Assert.That(phrases[12].FistPalmSalute.RightClosure(1d), Is.Zero.Within(0.000001d));
            Assert.That(phrases[13].Action, Is.EqualTo(DanceAction.SingleFinger));
            Assert.That(phrases[13].Name, Is.EqualTo("單指"));
            Assert.That(phrases[13].HandGesture.Gesture, Is.EqualTo(HandGesture.SingleFinger));
            Assert.That(phrases[13].ActiveRodCount, Is.EqualTo(1));
            Assert.That(phrases[13].HasClosing, Is.True);
            Assert.That(phrases[13].HandGesture.PointFinger(0.7d), Is.GreaterThan(0.9d));
            Assert.That(phrases[13].HandGesture.PointFinger(1d), Is.Zero.Within(0.000001d));
        }

        [Test]
        public void FormalSingleFinger_UsesSuccessfulAnchorClosesAndMissFreezesShape()
        {
            DancePhrase[] phrases = Phrases();
            DancePhrase singleFinger = phrases.First(p => p.Action == DanceAction.SingleFinger);
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(singleFinger.AnchorNoteId, JudgmentGrade.Perfect), singleFinger.StartSeconds);
            player.Evaluate(singleFinger.StartSeconds + (singleFinger.DurationSeconds * 0.7d));

            Assert.That(player.HasExplicitLeftHandPose, Is.True);
            Assert.That(player.LeftFingerWidth, Is.LessThan(0.55d));
            Assert.That(player.LeftFingerLength, Is.LessThan(0.7d));
            Assert.That(player.LeftPointFingerAmount, Is.GreaterThan(0.9d));

            double heldPointFinger = player.LeftPointFingerAmount;
            DancePhrase next = phrases.First(p => p.StartBeat == singleFinger.StartBeat + singleFinger.DurationBeats);
            player.OnJudged(Result(next.AnchorNoteId, JudgmentGrade.Miss),
                singleFinger.StartSeconds + (singleFinger.DurationSeconds * 0.7d));
            player.Evaluate(next.StartSeconds + next.DurationSeconds);
            Assert.That(player.LeftPointFingerAmount, Is.EqualTo(heldPointFinger).Within(0.000001d));

            var closingPlayer = new DancePlayback(phrases);
            closingPlayer.OnJudged(Result(singleFinger.AnchorNoteId, JudgmentGrade.Perfect), singleFinger.StartSeconds);
            closingPlayer.Evaluate(singleFinger.StartSeconds + singleFinger.DurationSeconds);
            Assert.That(closingPlayer.LeftFingerWidth, Is.EqualTo(1d).Within(0.000001d));
            Assert.That(closingPlayer.LeftFingerLength, Is.EqualTo(1d).Within(0.000001d));
            Assert.That(closingPlayer.LeftPointFingerAmount, Is.Zero.Within(0.000001d));
        }

        [Test]
        public void FormalFistPalmIntoSingleFingerIntoNextCycle_HasContinuousBoundaries()
        {
            DancePhrase[] phrases = Phrases();
            DancePhrase salute = phrases.First(p => p.Action == DanceAction.FistPalmSalute);
            DancePhrase singleFinger = phrases.First(p => p.Action == DanceAction.SingleFinger);
            DancePhrase next = phrases.First(p => p.StartBeat == singleFinger.StartBeat + singleFinger.DurationBeats);
            var player = new DancePlayback(phrases);

            player.OnJudged(Result(salute.AnchorNoteId, JudgmentGrade.Perfect), salute.StartSeconds);
            player.Evaluate(singleFinger.StartSeconds);
            double shoulder = player.Angle(DanceJoint.LeftShoulder);
            double elbow = player.Angle(DanceJoint.LeftElbow);
            double wrist = player.LeftWristAngle;
            double width = player.LeftFingerWidth;
            double point = player.LeftPointFingerAmount;
            player.OnJudged(Result(singleFinger.AnchorNoteId, JudgmentGrade.Perfect), singleFinger.StartSeconds);
            player.Evaluate(singleFinger.StartSeconds + 0.0001d);
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.EqualTo(shoulder).Within(0.01d));
            Assert.That(player.Angle(DanceJoint.LeftElbow), Is.EqualTo(elbow).Within(0.01d));
            Assert.That(player.LeftWristAngle, Is.EqualTo(wrist).Within(0.01d));
            Assert.That(player.LeftFingerWidth, Is.EqualTo(width).Within(0.01d));
            Assert.That(player.LeftPointFingerAmount, Is.EqualTo(point).Within(0.01d));

            player.Evaluate(next.StartSeconds);
            shoulder = player.Angle(DanceJoint.LeftShoulder);
            elbow = player.Angle(DanceJoint.LeftElbow);
            wrist = player.LeftWristAngle;
            width = player.LeftFingerWidth;
            point = player.LeftPointFingerAmount;
            player.OnJudged(Result(next.AnchorNoteId, JudgmentGrade.Perfect), next.StartSeconds);
            player.Evaluate(next.StartSeconds + 0.0001d);
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.EqualTo(shoulder).Within(0.01d));
            Assert.That(player.Angle(DanceJoint.LeftElbow), Is.EqualTo(elbow).Within(0.01d));
            Assert.That(player.LeftWristAngle, Is.EqualTo(wrist).Within(0.01d));
            Assert.That(player.LeftFingerWidth, Is.EqualTo(width).Within(0.01d));
            Assert.That(player.LeftPointFingerAmount, Is.EqualTo(point).Within(0.01d));
        }

        [Test]
        public void FormalFistPalmSalute_UsesTwoHandsAndMissFreezesTheCompletedPose()
        {
            DancePhrase[] phrases = Phrases();
            DancePhrase salute = phrases.First(p => p.Action == DanceAction.FistPalmSalute);
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(salute.AnchorNoteId, JudgmentGrade.Perfect), salute.StartSeconds);
            player.Evaluate(salute.StartSeconds + (salute.DurationSeconds * 0.7d));

            Assert.That(player.HasExplicitLeftHandPose, Is.True);
            Assert.That(player.HasExplicitRightHandPose, Is.True);
            Assert.That(player.RightHandClosure, Is.GreaterThan(0.75d));
            Assert.That(player.Angle(DanceJoint.LeftElbow), Is.GreaterThan(140d));
            Assert.That(player.Angle(DanceJoint.RightElbow), Is.LessThan(-140d));

            double heldClosure = player.RightHandClosure;
            DancePhrase next = phrases.First(p => p.StartBeat == salute.StartBeat + salute.DurationBeats);
            player.OnJudged(Result(next.AnchorNoteId, JudgmentGrade.Miss),
                salute.StartSeconds + (salute.DurationSeconds * 0.7d));
            player.Evaluate(next.StartSeconds);
            Assert.That(player.RightHandClosure, Is.EqualTo(heldClosure).Within(0.000001d));
        }

        [Test]
        public void FormalThreadPalmIntoTurnWrist_HasContinuousHandBoundaryAndClosesForNextCycle()
        {
            DancePhrase[] phrases = Phrases();
            DancePhrase thread = phrases.First(p => p.Action == DanceAction.ThreadPalm);
            DancePhrase turn = phrases.First(p => p.Action == DanceAction.TurnWrist);
            DancePhrase next = phrases.First(p => p.StartBeat == turn.StartBeat + turn.DurationBeats);
            var player = new DancePlayback(phrases);

            player.OnJudged(Result(thread.AnchorNoteId, JudgmentGrade.Perfect), thread.StartSeconds);
            player.Evaluate(turn.StartSeconds);
            double shoulder = player.Angle(DanceJoint.LeftShoulder);
            double elbow = player.Angle(DanceJoint.LeftElbow);
            double wrist = player.LeftWristAngle;
            double finger = player.LeftFingerAngle;

            player.OnJudged(Result(turn.AnchorNoteId, JudgmentGrade.Perfect), turn.StartSeconds);
            player.Evaluate(turn.StartSeconds + 0.0001d);
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.EqualTo(shoulder).Within(0.01d));
            Assert.That(player.Angle(DanceJoint.LeftElbow), Is.EqualTo(elbow).Within(0.01d));
            Assert.That(player.LeftWristAngle, Is.EqualTo(wrist).Within(0.01d));
            Assert.That(player.LeftFingerAngle, Is.EqualTo(finger).Within(0.01d));

            player.Evaluate(next.StartSeconds);
            Assert.That(player.LeftWristAngle, Is.Zero.Within(0.001d));
            Assert.That(player.LeftFingerAngle, Is.Zero.Within(0.001d));
        }

        [Test]
        public void FormalPalmGesture_RequiresSuccessfulAnchorAndMissFreezesHandPose()
        {
            DancePhrase[] phrases = Phrases();
            DancePhrase press = phrases.First(p => p.Action == DanceAction.PressPalm);
            DancePhrase support = phrases.First(p => p.Action == DanceAction.SupportPalm);
            var player = new DancePlayback(phrases);

            player.OnJudged(Result(press.AnchorNoteId, JudgmentGrade.Perfect), press.StartSeconds);
            player.Evaluate(press.StartSeconds + press.DurationSeconds);
            Assert.That(player.HasExplicitLeftHandPose, Is.True);
            Assert.That(player.LeftWristAngle, Is.GreaterThan(55d));
            double heldWrist = player.LeftWristAngle;

            player.OnJudged(Result(support.AnchorNoteId, JudgmentGrade.Miss), support.StartSeconds);
            player.Evaluate(support.StartSeconds + support.DurationSeconds);
            Assert.That(player.ActiveAction, Is.Null);
            Assert.That(player.LeftWristAngle, Is.EqualTo(heldWrist).Within(0.000001d));
        }

        private static JudgmentResult Result(int noteId, JudgmentGrade grade) =>
            new JudgmentResult(JudgmentEventKind.NoteJudged, noteId, 0, grade, 0d, 0, 0, 0d);

        [TestCase(PlayDifficulty.Easy)]
        [TestCase(PlayDifficulty.Normal)]
        [TestCase(PlayDifficulty.Hard)]
        public void Load_BuildsWholeSongWithValidTapAnchors(PlayDifficulty difficulty)
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 180d, difficulty);
            DancePhrase[] phrases = DanceChoreography.Create(notes, 120d, 180d);

            Assert.That(phrases.Length, Is.EqualTo(45));
            Assert.That(phrases.Select(p => p.StartBeat), Is.EqualTo(Enumerable.Range(0, 45).Select(i => i * 8)));
            Assert.That(phrases.All(p => p.DurationBeats >= 4 && p.ActiveJointCount <= 2), Is.True);
            Assert.That(phrases.All(p => p.ActiveRodCount <= 2), Is.True);
            Assert.That(phrases.All(p => notes.Any(n => n.Id == p.AnchorNoteId && n.Kind == NoteKind.Tap &&
                Math.Abs(n.TimeSec - p.StartSeconds) < 0.000001d)), Is.True);
            Assert.That(phrases.Any(p => p.Action == DanceAction.Turn), Is.True);
            Assert.That(phrases[0].StepFoot, Is.EqualTo(SwingFoot.Right));
            Assert.That(phrases[2].StepFoot, Is.EqualTo(SwingFoot.None));
            Assert.That(phrases[4].StepFoot, Is.EqualTo(SwingFoot.Left));
            Assert.That(phrases[0].Display, Is.EqualTo("【0，單山膀，左肩、左肘，8】"));
        }

        [Test]
        public void TrialLightRoutine_UsesFixedRiseDevelopTurnCloseOrderAndEndsInFinalPose()
        {
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 180d, PlayDifficulty.Normal), 120d, 180d);
            DanceAction[] expected =
            {
                DanceAction.SingleMountainArm, DanceAction.CloudHand,
                DanceAction.WindFlag, DanceAction.Turn,
                DanceAction.RaiseSleeve, DanceAction.DoubleMountainArm,
                DanceAction.ReverseCloudHand, DanceAction.FinalPose,
                DanceAction.PressPalm, DanceAction.SupportPalm,
                DanceAction.ThreadPalm, DanceAction.TurnWrist,
                DanceAction.FistPalmSalute, DanceAction.SingleFinger,
                DanceAction.SingleMountainArm, DanceAction.CloudHand,
                DanceAction.PressPalm, DanceAction.SupportPalm,
                DanceAction.ThreadPalm, DanceAction.TurnWrist,
                DanceAction.WindFlag, DanceAction.RaiseSleeve,
                DanceAction.Turn, DanceAction.ReverseCloudHand,
                DanceAction.DoubleMountainArm, DanceAction.WindFlag,
                DanceAction.Turn, DanceAction.RaiseSleeve,
                DanceAction.ReverseCloudHand, DanceAction.CloudHand,
                DanceAction.Turn, DanceAction.WindFlag,
                DanceAction.DoubleMountainArm, DanceAction.ReverseCloudHand,
                DanceAction.FinalPose, DanceAction.PressPalm,
                DanceAction.SupportPalm, DanceAction.ThreadPalm,
                DanceAction.TurnWrist, DanceAction.SingleFinger,
                DanceAction.SingleMountainArm, DanceAction.CloudHand,
                DanceAction.DoubleMountainArm, DanceAction.FistPalmSalute,
                DanceAction.FinalPose
            };

            Assert.That(phrases.Select(p => p.Action), Is.EqualTo(expected));
            Assert.That(phrases[16].Action, Is.EqualTo(DanceAction.PressPalm));
            Assert.That(phrases[16].Action, Is.Not.EqualTo((DanceAction)(16 % 14)));
            Assert.That(phrases.Take(8).Last().Action, Is.EqualTo(DanceAction.FinalPose));
            Assert.That(phrases.Skip(22).Take(13).Count(p => p.Action == DanceAction.Turn),
                Is.EqualTo(3));
            Assert.That(phrases.Last().Action, Is.EqualTo(DanceAction.FinalPose));
            Assert.That(Enum.GetValues(typeof(DanceAction)).Cast<DanceAction>()
                .All(action => phrases.Any(p => p.Action == action)), Is.True);
        }

        [Test]
        public void RoutineGeneration_IsDeterministicAndShortChartsStillCloseWithFinalPose()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 112d, PlayDifficulty.Normal);
            DancePhrase[] first = DanceChoreography.Create(notes, 120d, 112d);
            DancePhrase[] second = DanceChoreography.Create(notes, 120d, 112d);

            Assert.That(second.Select(p => p.Action), Is.EqualTo(first.Select(p => p.Action)));
            Assert.That(second.Select(p => p.AnchorNoteId), Is.EqualTo(first.Select(p => p.AnchorNoteId)));
            Assert.That(first.Last().Action, Is.EqualTo(DanceAction.FinalPose));
        }

        [Test]
        public void SuccessfulPhrase_LiftsOneFootWhileOtherRemainsPlanted_ThenLands()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Perfect), 0d);
            player.Evaluate(2d);
            Assert.That(player.LeftFootX, Is.EqualTo(-0.34d).Within(0.00001d));
            Assert.That(player.LeftFootY, Is.EqualTo(-2.08d).Within(0.00001d));
            Assert.That(player.RightFootX, Is.GreaterThan(0.80d));
            Assert.That(player.RightFootY, Is.GreaterThan(-1.62d));
            Assert.That(player.PelvisX, Is.LessThan(-0.18d));
            player.Evaluate(4d);
            Assert.That(player.RightFootX, Is.EqualTo(0.34d).Within(0.00001d));
            Assert.That(player.RightFootY, Is.EqualTo(-2.08d).Within(0.00001d));
        }

        [Test]
        public void CloudHand_UsesRaisedShoulderPeakWithoutChangingItsFivePartTiming()
        {
            DancePhrase cloud = Phrases().First(p => p.Action == DanceAction.CloudHand);
            var player = new DancePlayback(new[] { cloud });
            player.OnJudged(Result(cloud.AnchorNoteId, JudgmentGrade.Perfect), cloud.StartSeconds);
            double raisedShoulder = double.MaxValue;
            for (int i = 0; i < DancePhrase.SampleCount; i++)
            {
                player.Evaluate(cloud.StartSeconds + (cloud.DurationSeconds * i / (DancePhrase.SampleCount - 1d)));
                raisedShoulder = Math.Min(raisedShoulder, player.Angle(DanceJoint.LeftShoulder));
            }

            Assert.That(raisedShoulder, Is.LessThan(-148d));
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.EqualTo(-24d).Within(0.001d));
        }

        [Test]
        public void MissedStep_DoesNotMoveLowerBody_AndNextHitStartsContinuously()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Miss), 0d);
            player.Evaluate(2d);
            Assert.That(player.RightFootY, Is.EqualTo(-2.08d));
            player.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Good), 4d);
            player.Evaluate(4d);
            Assert.That(player.RightFootY, Is.EqualTo(-2.08d));
            player.Evaluate(6d);
            Assert.That(player.RightFootY, Is.GreaterThan(-1.80d));
        }

        [Test]
        public void Miss_FreezesPoseUntilNextSuccessfulAnchor()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Perfect), 0d);
            player.Evaluate(1d);
            double beforeMiss = player.Angle(DanceJoint.LeftShoulder);
            Assert.That(Math.Abs(beforeMiss), Is.GreaterThan(10d));

            player.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Miss), 4d);
            double frozen = player.Angle(DanceJoint.LeftShoulder);
            player.Evaluate(7d);
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.EqualTo(frozen).Within(0.000001d));
            Assert.That(player.ActiveAction, Is.Null);

            player.OnJudged(Result(phrases[2].AnchorNoteId, JudgmentGrade.Good), 8.10d);
            player.Evaluate(8.5d);
            Assert.That(player.ActiveAction, Is.EqualTo(DanceAction.WindFlag));
        }

        [Test]
        public void EarlyHit_WaitsForBeatAndUnboundHitDoesNotMovePuppet()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(99999, JudgmentGrade.Perfect), 0d);
            player.Evaluate(1d);
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.Zero);

            player.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Perfect), 3.90d);
            player.Evaluate(3.99d);
            Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.Zero);
            player.Evaluate(4.5d);
            Assert.That(Math.Abs(player.Angle(DanceJoint.LeftShoulder)), Is.GreaterThan(1d));
        }

        [Test]
        public void Turn_ChangesFacingContinuouslyAndCanTurnBack()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            DancePhrase firstTurn = phrases.First(p => p.Action == DanceAction.Turn);
            player.OnJudged(Result(firstTurn.AnchorNoteId, JudgmentGrade.Perfect), firstTurn.StartSeconds);
            double previous = player.FacingScale;
            for (double time = firstTurn.StartSeconds + (1d / 60d);
                 time <= firstTurn.StartSeconds + firstTurn.DurationSeconds + 0.000001d;
                 time += 1d / 60d)
            {
                player.Evaluate(time);
                Assert.That(Math.Abs(player.FacingScale - previous), Is.LessThan(0.04d));
                previous = player.FacingScale;
            }

            Assert.That(player.FacingScale, Is.EqualTo(-1d).Within(0.001d));
            Assert.That(phrases[3].Name, Is.EqualTo("轉身"));
            Assert.That(phrases[3].ActiveJointCount, Is.EqualTo(2));
            Assert.That(phrases[3].ActiveRodCount, Is.EqualTo(2));
            Assert.That(phrases[3].StepFoot, Is.EqualTo(SwingFoot.None));
            Assert.That(phrases[3].Display, Does.Contain("軀幹、左肩"));

            DancePhrase secondTurn = phrases.Last(p => p.Action == DanceAction.Turn);
            player.OnJudged(Result(secondTurn.AnchorNoteId, JudgmentGrade.Great), secondTurn.StartSeconds);
            player.Evaluate(secondTurn.StartSeconds + secondTurn.DurationSeconds);
            Assert.That(player.FacingScale, Is.EqualTo(1d).Within(0.001d));
        }

        [Test]
        public void Turn_KeepsBothFeetOnTheStageHorizon()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            DancePhrase turn = phrases[3];
            player.OnJudged(Result(turn.AnchorNoteId, JudgmentGrade.Perfect), turn.StartSeconds);
            for (int i = 0; i <= 60; i++)
            {
                player.Evaluate(turn.StartSeconds + (turn.DurationSeconds * i / 60d));
                Assert.That(player.LeftFootX, Is.EqualTo(-0.34d).Within(0.00001d));
                Assert.That(player.RightFootX, Is.EqualTo(0.34d).Within(0.00001d));
                Assert.That(player.LeftFootY, Is.EqualTo(-2.08d).Within(0.00001d));
                Assert.That(player.RightFootY, Is.EqualTo(-2.08d).Within(0.00001d));
            }
        }

        [Test]
        public void MissedTurn_DoesNotFlipFacingOrMoveFeet()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            DancePhrase turn = phrases[3];
            player.OnJudged(Result(turn.AnchorNoteId, JudgmentGrade.Miss), turn.StartSeconds);
            player.Evaluate(turn.StartSeconds + turn.DurationSeconds);
            Assert.That(player.FacingScale, Is.EqualTo(1d));
            Assert.That(player.LeftFootX, Is.EqualTo(-0.34d));
            Assert.That(player.RightFootX, Is.EqualTo(0.34d));
        }

        [Test]
        public void EarlyNextPhrase_PreservesCompletedTurnFacing()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            DancePhrase turn = phrases[3];
            DancePhrase next = phrases[4];
            player.OnJudged(Result(turn.AnchorNoteId, JudgmentGrade.Perfect), turn.StartSeconds);
            player.Evaluate(next.StartSeconds - 0.1d);
            player.OnJudged(Result(next.AnchorNoteId, JudgmentGrade.Great), next.StartSeconds - 0.05d);
            player.Evaluate(next.StartSeconds + 0.01d);

            Assert.That(player.ActiveAction, Is.EqualTo(next.Action));
            Assert.That(player.FacingScale, Is.LessThan(-0.9d));
        }

        [Test]
        public void LateTurnHit_CompletesOnChartBoundaryWithoutFacingSnapIntoNextMove()
        {
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 96d, PlayDifficulty.Normal), 120d, 96d);
            DancePhrase[] turns = phrases.Where(p => p.Action == DanceAction.Turn).ToArray();
            Assert.That(turns.Length, Is.GreaterThanOrEqualTo(2));

            var player = new DancePlayback(phrases);
            foreach (DancePhrase turn in turns.Take(2))
            {
                DancePhrase next = phrases.First(p => p.StartBeat == turn.StartBeat + turn.DurationBeats);
                player.OnJudged(Result(turn.AnchorNoteId, JudgmentGrade.Good), turn.StartSeconds + 0.08d);
                player.Evaluate(next.StartSeconds);
                double completedFacing = player.FacingScale;
                Assert.That(Math.Abs(completedFacing), Is.EqualTo(1d).Within(0.001d));

                player.OnJudged(Result(next.AnchorNoteId, JudgmentGrade.Perfect), next.StartSeconds);
                player.Evaluate(next.StartSeconds + 0.0001d);
                Assert.That(player.FacingScale, Is.EqualTo(completedFacing).Within(0.001d),
                    $"{turn.StartBeat} 拍轉身接 {next.Name} 不得翻回舊方向");
            }
        }

        [Test]
        public void RestartingWithSameChart_ReplaysTheSamePose()
        {
            DancePhrase[] phrases = Phrases();
            var first = new DancePlayback(phrases);
            var second = new DancePlayback(phrases);
            foreach (DancePlayback player in new[] { first, second })
            {
                player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Great), 0d);
                player.Evaluate(1.5d);
                player.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Great), 4d);
                player.Evaluate(6d);
            }

            Assert.That(first.Angle(DanceJoint.LeftShoulder),
                Is.EqualTo(second.Angle(DanceJoint.LeftShoulder)).Within(0.000001d));
            Assert.That(first.Angle(DanceJoint.LeftElbow),
                Is.EqualTo(second.Angle(DanceJoint.LeftElbow)).Within(0.000001d));
        }

        [Test]
        public void LinkedPhrases_KeepIntendedCarriesButReleaseArmsBeforeUnrelatedMoves()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            for (int i = 0; i < 9; i++)
            {
                DancePhrase phrase = phrases[i];
                player.OnJudged(Result(phrase.AnchorNoteId, JudgmentGrade.Perfect), phrase.StartSeconds);
                player.Evaluate(phrase.StartSeconds + phrase.DurationSeconds);
                switch (phrase.Action)
                {
                    case DanceAction.CloudHand:
                        Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.EqualTo(-24d).Within(0.001d));
                        Assert.That(player.Angle(DanceJoint.LeftElbow), Is.Zero.Within(0.001d));
                        break;
                    case DanceAction.WindFlag:
                        Assert.That(player.Angle(DanceJoint.RightShoulder), Is.EqualTo(138d).Within(0.001d));
                        break;
                    case DanceAction.DoubleMountainArm:
                        Assert.That(player.Angle(DanceJoint.LeftShoulder), Is.Zero.Within(0.001d));
                        Assert.That(player.Angle(DanceJoint.RightShoulder), Is.EqualTo(68d).Within(0.001d));
                        break;
                    case DanceAction.ReverseCloudHand:
                        Assert.That(player.Angle(DanceJoint.RightShoulder), Is.Zero.Within(0.001d));
                        Assert.That(player.Angle(DanceJoint.RightElbow), Is.Zero.Within(0.001d));
                        break;
                    case DanceAction.FinalPose:
                        Assert.That(player.Angle(DanceJoint.Torso), Is.Zero.Within(0.001d));
                        Assert.That(player.Angle(DanceJoint.Head), Is.Zero.Within(0.001d));
                        break;
                }
            }
        }

        [Test]
        public void LinkedPhrase_UsesDirectPreparedEntry_WhileMissUsesRecoveryEntry()
        {
            DancePhrase[] phrases = Phrases();
            var linked = new DancePlayback(phrases);
            linked.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Perfect), 0d);
            linked.Evaluate(4d);
            linked.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Perfect), 4d);
            linked.Evaluate(4.25d);
            Assert.That(linked.Angle(DanceJoint.LeftShoulder), Is.GreaterThan(-59d));

            var recovering = new DancePlayback(phrases);
            recovering.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Miss), 0d);
            recovering.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Good), 4d);
            recovering.Evaluate(4d);
            Assert.That(recovering.Angle(DanceJoint.LeftShoulder), Is.Zero.Within(0.001d));
            recovering.Evaluate(4.25d);
            Assert.That(Math.Abs(recovering.Angle(DanceJoint.LeftShoulder)),
                Is.LessThan(Math.Abs(linked.Angle(DanceJoint.LeftShoulder))));
        }

        [Test]
        public void WindFlagAndRaiseSleeve_KeepTheRightArmOnTheRightSide()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            foreach (DanceAction action in new[] { DanceAction.WindFlag, DanceAction.RaiseSleeve })
            {
                DancePhrase phrase = phrases.First(p => p.Action == action);
                player.OnJudged(Result(phrase.AnchorNoteId, JudgmentGrade.Perfect), phrase.StartSeconds);
                player.Evaluate(phrase.StartSeconds + (phrase.DurationSeconds * 0.65d));
                Assert.That(player.Angle(DanceJoint.RightShoulder), Is.GreaterThan(100d),
                    $"{phrase.Name} 的右臂不得套用左臂的负角符号");
            }
        }

        [Test]
        public void UnusedArm_ReturnsToNeutralInsteadOfLeakingFromPreviousPhrase()
        {
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 180d, PlayDifficulty.Normal), 120d, 180d);
            int raiseIndex = Enumerable.Range(0, phrases.Length - 1).First(i =>
                phrases[i].Action == DanceAction.RaiseSleeve &&
                phrases[i + 1].Action == DanceAction.Turn);

            var player = new DancePlayback(phrases);
            DancePhrase raise = phrases[raiseIndex];
            DancePhrase turn = phrases[raiseIndex + 1];
            player.OnJudged(Result(raise.AnchorNoteId, JudgmentGrade.Perfect), raise.StartSeconds);
            player.Evaluate(raise.StartSeconds + raise.DurationSeconds);
            Assert.That(player.Angle(DanceJoint.RightShoulder), Is.GreaterThan(100d));

            player.OnJudged(Result(turn.AnchorNoteId, JudgmentGrade.Perfect), turn.StartSeconds);
            player.Evaluate(turn.StartSeconds + turn.DurationSeconds);
            Assert.That(player.Angle(DanceJoint.RightShoulder), Is.Zero.Within(0.001d));
            Assert.That(player.Angle(DanceJoint.RightElbow), Is.Zero.Within(0.001d));
        }

        [Test]
        public void SuccessiveHits_ArePositionContinuousAtAllPhraseBoundaries()
        {
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 180d, PlayDifficulty.Normal), 120d, 180d);
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Perfect), phrases[0].StartSeconds);
            for (int i = 0; i < phrases.Length - 1; i++)
            {
                DancePhrase current = phrases[i];
                DancePhrase next = phrases[i + 1];
                player.Evaluate(next.StartSeconds);
                double[] before = Enum.GetValues(typeof(DanceJoint)).Cast<DanceJoint>()
                    .Select(player.Angle).ToArray();
                double leftFoot = player.LeftFootY;
                double rightFoot = player.RightFootY;
                double facing = player.FacingScale;
                player.OnJudged(Result(next.AnchorNoteId, JudgmentGrade.Great), next.StartSeconds);
                player.Evaluate(next.StartSeconds);
                int joint = 0;
                foreach (DanceJoint part in Enum.GetValues(typeof(DanceJoint)))
                    Assert.That(player.Angle(part), Is.EqualTo(before[joint++]).Within(0.001d),
                        $"{current.Action} → {next.Action}: {part}");
                Assert.That(player.LeftFootY, Is.EqualTo(leftFoot).Within(0.001d));
                Assert.That(player.RightFootY, Is.EqualTo(rightFoot).Within(0.001d));
                Assert.That(player.FacingScale, Is.EqualTo(facing).Within(0.001d));
            }
        }

        [Test]
        public void PerformanceStatus_ReportsOnlyPerformedMoves_AndDistinguishesMissedCue()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            var changes = new List<DancePerformanceStatus>();
            player.StatusChanged += changes.Add;

            player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Perfect), 0d);
            Assert.That(changes.Last().Kind, Is.EqualTo(DancePerformanceKind.Performing));
            Assert.That(changes.Last().Performed, Is.SameAs(phrases[0]));
            player.Evaluate(4d);
            Assert.That(changes.Last().Kind, Is.EqualTo(DancePerformanceKind.Holding));

            player.OnJudged(Result(phrases[1].AnchorNoteId, JudgmentGrade.Great), 4d);
            player.Evaluate(7.1d);
            Assert.That(changes.Last().Kind, Is.EqualTo(DancePerformanceKind.Closing));
            Assert.That(changes.Last().Performed, Is.SameAs(phrases[1]));
            player.Evaluate(8d);
            player.OnJudged(Result(phrases[2].AnchorNoteId, JudgmentGrade.Miss), 8d);
            Assert.That(changes.Last().Kind, Is.EqualTo(DancePerformanceKind.Interrupted));
            Assert.That(changes.Last().Performed, Is.SameAs(phrases[1]));
            Assert.That(changes.Last().Cue, Is.SameAs(phrases[2]));
            Assert.That(changes.Any(s => s.Kind == DancePerformanceKind.Performing &&
                s.Performed == phrases[2]), Is.False);

            player.OnJudged(Result(phrases[3].AnchorNoteId, JudgmentGrade.Perfect), 11.9d);
            Assert.That(changes.Last().Kind, Is.EqualTo(DancePerformanceKind.Pending));
            Assert.That(changes.Last().Cue, Is.SameAs(phrases[3]));
            player.Evaluate(12d);
            Assert.That(changes.Last().Kind, Is.EqualTo(DancePerformanceKind.Performing));
            Assert.That(changes.Last().Performed, Is.SameAs(phrases[3]));
        }

        private static DancePhrase[] Phrases() => DanceChoreography.Create(
            PrototypeDanceChart.Create(120d, 112d, PlayDifficulty.Normal), 120d, 112d);
    }
}
