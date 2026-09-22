using System;
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
        public void SuccessfulPhrase_LiftsOneFootWhileOtherRemainsPlanted_ThenLands()
        {
            DancePhrase[] phrases = Phrases();
            var player = new DancePlayback(phrases);
            player.OnJudged(Result(phrases[0].AnchorNoteId, JudgmentGrade.Perfect), 0d);
            player.Evaluate(2d);
            Assert.That(player.LeftFootX, Is.EqualTo(-0.34d).Within(0.00001d));
            Assert.That(player.LeftFootY, Is.EqualTo(-2.08d).Within(0.00001d));
            Assert.That(player.RightFootX, Is.GreaterThan(0.60d));
            Assert.That(player.RightFootY, Is.GreaterThan(-1.80d));
            Assert.That(player.PelvisX, Is.LessThan(-0.10d));
            player.Evaluate(4d);
            Assert.That(player.RightFootX, Is.EqualTo(0.34d).Within(0.00001d));
            Assert.That(player.RightFootY, Is.EqualTo(-2.08d).Within(0.00001d));
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

        private static DancePhrase[] Phrases() => DanceChoreography.Create(
            PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Normal), 120d, 64d);
    }
}
