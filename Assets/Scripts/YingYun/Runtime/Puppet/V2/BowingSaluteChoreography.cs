using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>03 拱手致礼：双手胸前合礼，保持礼位完成躬身，再起身半收。</summary>
    public static class BowingSaluteChoreography
    {
        public const string Name = "拱手致礼";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.20d, 0.32d, 0.48d, 0.62d, 0.75d, 0.88d, 1d };
        private static readonly double[] RootY = { -0.40d, -0.40d, -0.40d, -0.40d, -0.415d, -0.44d, -0.415d, -0.40d };
        private static readonly double[] Torso = { 0d, 0d, 0d, 0d, -7d, -14d, -7d, 0d };
        private static readonly double[] Head = { 5d, 5d, 5d, 5d, 0d, -6d, 0d, 5d };
        private static readonly double[] LeftX = { -0.68d, -0.58d, -0.30d, -0.13d, -0.09d, -0.03d, -0.09d, -0.34d };
        private static readonly double[] LeftY = { -0.78d, -0.14d, 0.20d, 0.34d, 0.30d, 0.22d, 0.30d, 0.05d };
        private static readonly double[] RightX = { 0.68d, 0.58d, 0.30d, 0.13d, 0.17d, 0.23d, 0.17d, 0.34d };
        private static readonly double[] RightY = { -0.78d, -0.14d, 0.20d, 0.34d, 0.30d, 0.22d, 0.30d, 0.05d };
        private static readonly double[] LeftWrist = { 0d, 8d, 15d, 15d, 15d, 15d, 13d, 8d };
        private static readonly double[] RightWrist = { 0d, -8d, -15d, -15d, -15d, -15d, -13d, -8d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            return new PuppetV2Pose(
                0d, Sample(RootY, t), Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, 0d, 1d,
                PuppetHandShape.SupportPalm, PuppetHandShape.ClosedPalm,
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
