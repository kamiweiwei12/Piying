using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.View
{
    /// <summary>
    /// 判定結果的文字標籤。只做顯示：把長按的「早放」與「未撐住」從一般 Miss 分開，
    /// 讓玩家知道自己錯在哪，不改變任何判定、計分或連擊規則。
    /// </summary>
    public static class JudgmentLabels
    {
        public const string TapMiss = "空引";
        public const string HoldReleasedEarly = "早放";
        public const string HoldNotSustained = "未撐住";
        public const string HoldHolding = "按住";

        public static string GradeText(JudgmentGrade grade)
        {
            switch (grade)
            {
                case JudgmentGrade.Perfect: return "契合";
                case JudgmentGrade.Great: return "协律";
                case JudgmentGrade.Good: return "应拍";
                case JudgmentGrade.Miss: return TapMiss;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 依音符種類與長按狀態挑選文字。
        /// 提早放開（誤差為負）→ 早放；按住但沒撐到最後（含太晚放開）→ 未撐住；完全沒按到 → 空引。
        /// </summary>
        public static string For(JudgmentResult result, bool isHoldNote, bool wasHolding)
        {
            if (result.Grade != JudgmentGrade.Miss)
            {
                return GradeText(result.Grade);
            }

            if (!isHoldNote || !wasHolding)
            {
                return TapMiss;
            }

            return result.ErrorMs < 0d ? HoldReleasedEarly : HoldNotSustained;
        }
    }
}
