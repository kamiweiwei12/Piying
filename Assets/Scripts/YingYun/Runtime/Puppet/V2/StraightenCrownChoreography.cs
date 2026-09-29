using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>02 整冠肃立：双肘从体侧抬起，双手在冠侧完成扶正、轻提复位与压稳，再沿两侧落回。</summary>
    public static class StraightenCrownChoreography
    {
        public const string Name = "整冠肃立";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.22d, 0.38d, 0.50d, 0.61d, 0.72d, 0.82d, 1d };
        private static readonly double[] LeftX = { -0.68d, -1.10d, -0.50d, -0.43d, -0.49d, -0.44d, -0.78d, -0.68d };
        private static readonly double[] LeftY = { -0.78d, 0.60d, 1.60d, 1.64d, 1.70d, 1.61d, 0.85d, -0.78d };
        private static readonly double[] RightX = { 0.68d, 1.10d, 0.50d, 0.43d, 0.49d, 0.44d, 0.78d, 0.68d };
        private static readonly double[] RightY = { -0.78d, 0.60d, 1.60d, 1.64d, 1.70d, 1.61d, 0.85d, -0.78d };
        private static readonly double[] LeftWrist = { 0d, 8d, 18d, 30d, 14d, 26d, 10d, 0d };
        private static readonly double[] RightWrist = { 0d, -8d, -18d, -30d, -14d, -26d, -10d, 0d };
        private static readonly double[] Head = { 8d, 5d, 2d, 3d, 1d, 2d, 2d, 2d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            return new PuppetV2Pose(
                0d, -0.40d, 0d, Sample(Head, t),
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
