using System;

namespace YingYun.Rhythm.Scoring
{
    /// <summary>難度相關的結算門檻；結算只讀此設定，不在顯示層寫死數值。</summary>
    public sealed class DifficultyConfig
    {
        public DifficultyConfig(
            double tianChengMinAccuracy,
            double chuanShenMinAccuracy,
            double ruYunMinAccuracy,
            double chuChengMinAccuracy)
        {
            ValidateThreshold(tianChengMinAccuracy, nameof(tianChengMinAccuracy));
            ValidateThreshold(chuanShenMinAccuracy, nameof(chuanShenMinAccuracy));
            ValidateThreshold(ruYunMinAccuracy, nameof(ruYunMinAccuracy));
            ValidateThreshold(chuChengMinAccuracy, nameof(chuChengMinAccuracy));

            if (tianChengMinAccuracy < chuanShenMinAccuracy ||
                chuanShenMinAccuracy < ruYunMinAccuracy ||
                ruYunMinAccuracy < chuChengMinAccuracy)
            {
                throw new ArgumentException("Result rating thresholds must be ordered from highest to lowest.");
            }

            TianChengMinAccuracy = tianChengMinAccuracy;
            ChuanShenMinAccuracy = chuanShenMinAccuracy;
            RuYunMinAccuracy = ruYunMinAccuracy;
            ChuChengMinAccuracy = chuChengMinAccuracy;
        }

        public double TianChengMinAccuracy { get; }
        public double ChuanShenMinAccuracy { get; }
        public double RuYunMinAccuracy { get; }
        public double ChuChengMinAccuracy { get; }

        public static DifficultyConfig Prototype { get; } =
            new DifficultyConfig(0.95d, 0.90d, 0.80d, 0.70d);

        private static void ValidateThreshold(double value, string parameterName)
        {
            if (value < 0d || value > 1d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
