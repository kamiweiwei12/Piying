using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>07 斜展顺风旗：左臂横展、右臂斜上扬起，以稳定高低手构成强对角线。</summary>
    public static class WindFlagDiagonalChoreography
    {
        public const string Name = "斜展顺风旗";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.20d, 0.45d, 0.68d, 0.85d, 1d };
        private static readonly double[] Torso = { 0d, 1d, 3d, 4d, 4d, 2d };
        private static readonly double[] Head = { 0d, 1d, 4d, 8d, 8d, 6d };

        // 左手由身侧向外平展，始终留在左侧，不先横穿胸口。
        private static readonly double[] LeftX = { -0.42d, -0.78d, -1.30d, -1.91d, -1.92d, -1.90d };
        private static readonly double[] LeftY = { -0.30d, -0.08d, 0.26d, 0.58d, 0.58d, 0.58d };
        private static readonly double[] LeftWrist = { 4d, 7d, 10d, 12d, 12d, 9d };

        // 右手沿肩外斜线升高；高臂不贴冠，显势后保留高度供下一招衔接。
        private static readonly double[] RightX = { 0.48d, 0.74d, 1.02d, 1.28d, 1.27d, 1.32d };
        private static readonly double[] RightY = { -0.12d, 0.34d, 1.10d, 1.78d, 1.82d, 1.78d };
        private static readonly double[] RightWrist = { -4d, 2d, 12d, 22d, 24d, 20d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            return new PuppetV2Pose(
                0d, -0.40d, Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, 0d, 1d,
                PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                true, true);
        }

        private static double Sample(double[] values, double progress)
        {
            int segment = 0;
            while (segment < Times.Length - 2 && progress > Times[segment + 1]) segment++;
            double span = Times[segment + 1] - Times[segment];
            double local = span <= 0d ? 0d : (progress - Times[segment]) / span;
            local = Clamp01(local);
            double smooth = local * local * (3d - (2d * local));
            return values[segment] + ((values[segment + 1] - values[segment]) * smooth);
        }

        private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
    }
}
