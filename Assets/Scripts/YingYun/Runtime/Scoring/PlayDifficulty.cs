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
                case PlayDifficulty.Easy: return "单键点按 · 适合熟悉六根竹杆";
                case PlayDifficulty.Hard: return "点按长按 · 多键合奏 · 连续操演";
                default: return "单键点按与长按 · 标准演出";
            }
        }
    }
}
