using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>11 掩面低诉：双手靠近下脸保留轮廓，低头收胸后只做一次克制沉肩。</summary>
    public static class VeiledFaceLamentChoreography
    {
        public const string Name = "掩面低诉";
        public const int Beats = 12;

        private static readonly double[] Times = { 0d, 0.25d, 0.4167d, 0.6667d, 0.75d, 0.8333d, 0.9167d, 1d };
        private static readonly double[] RootY = { -0.45d, -0.47d, -0.50d, -0.52d, -0.535d, -0.52d, -0.49d, -0.48d };
        private static readonly double[] Torso = { -2d, -3d, -7d, -11d, -13d, -10d, -7d, -6d };
        private static readonly double[] Head = { 2d, 0d, -4d, -8d, -10d, -8d, -6d, -6d };

        // 双手分别从脸的后侧与前侧靠近下脸，不在鼻眼区域交叉。
        private static readonly double[] LeftX = { -0.06d, -0.02d, 0.05d, 0.10d, 0.09d, 0.08d, 0.05d, 0.02d };
        private static readonly double[] LeftY = { -0.18d, 0.40d, 0.78d, 0.91d, 0.84d, 0.76d, 0.52d, 0.38d };
        private static readonly double[] RightX = { 0.96d, 0.86d, 0.74d, 0.68d, 0.69d, 0.70d, 0.68d, 0.64d };
        private static readonly double[] RightY = { 0.48d, 0.62d, 0.86d, 0.96d, 0.89d, 0.81d, 0.58d, 0.44d };
        private static readonly double[] LeftWrist = { 8d, 18d, 30d, 38d, 42d, 36d, 24d, 18d };
        private static readonly double[] RightWrist = { 16d, 2d, -18d, -30d, -34d, -28d, -8d, 4d };

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
