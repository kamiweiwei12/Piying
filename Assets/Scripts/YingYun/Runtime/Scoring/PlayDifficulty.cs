namespace YingYun.Rhythm.Scoring
{
    public enum PlayDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public static class PlayDifficultyInfo
    {
        public static string DisplayName(PlayDifficulty difficulty)
        {
            switch (difficulty)
            {
                case PlayDifficulty.Easy: return "入门";
                case PlayDifficulty.Hard: return "名角";
                default: return "行当";
            }
        }

        public static string Description(PlayDifficulty difficulty)
        {
            switch (difficulty)
            {
                case PlayDifficulty.Easy: return "疏拍 · 少合奏 · 适合熟悉六根竹杆";
                case PlayDifficulty.Hard: return "密拍 · 多合奏 · 连续操演";
                default: return "标准节奏 · 点按、长按与合奏齐全";
            }
        }
    }
}
