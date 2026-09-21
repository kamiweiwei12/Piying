using NUnit.Framework;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    /// <summary>
    /// 長按失誤的顯示文字：判定本身不變，只有提示文字把「早放」與「空引」分開。
    /// </summary>
    public sealed class JudgmentLabelsTests
    {
        [Test]
        public void HoldReleasedEarly_ShowsEarlyReleaseInsteadOfEmptyPull()
        {
            string label = JudgmentLabels.For(Result(JudgmentGrade.Miss, -450d), isHoldNote: true, wasHolding: true);

            Assert.That(label, Is.EqualTo(JudgmentLabels.HoldReleasedEarly));
        }

        [Test]
        public void HoldNotSustained_ShowsItsOwnLabel()
        {
            string label = JudgmentLabels.For(Result(JudgmentGrade.Miss, 220d), isHoldNote: true, wasHolding: true);

            Assert.That(label, Is.EqualTo(JudgmentLabels.HoldNotSustained));
        }

        [Test]
        public void MissedTapAndNeverPressedHold_StillShowEmptyPull()
        {
            JudgmentResult miss = Result(JudgmentGrade.Miss, 120d);

            Assert.That(JudgmentLabels.For(miss, isHoldNote: false, wasHolding: false), Is.EqualTo(JudgmentLabels.TapMiss));
            Assert.That(JudgmentLabels.For(miss, isHoldNote: true, wasHolding: false), Is.EqualTo(JudgmentLabels.TapMiss));
        }

        [Test]
        public void SuccessfulGrades_KeepTheExistingLabels()
        {
            Assert.That(
                JudgmentLabels.For(Result(JudgmentGrade.Perfect, 0d), isHoldNote: true, wasHolding: true),
                Is.EqualTo("契合"));
            Assert.That(
                JudgmentLabels.For(Result(JudgmentGrade.Good, 80d), isHoldNote: false, wasHolding: false),
                Is.EqualTo("应拍"));
        }

        private static JudgmentResult Result(JudgmentGrade grade, double errorMs)
        {
            return new JudgmentResult(JudgmentEventKind.NoteJudged, 1, 0, grade, errorMs, 0, 0, 0d, 1);
        }
    }
}
