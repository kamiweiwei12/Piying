using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>14 弓步推势：右脚先开并落稳，重心随后前移，双掌最后错层推出。</summary>
    public static class LungeDoublePalmPushChoreography
    {
        public const string Name = "弓步推势";
        public const int Beats = 12;

        private static readonly double[] Times = { 0d, 0.16d, 0.25d, 0.40d, 0.58d, 0.75d, 0.83d, 1d };
        private static readonly double[] RootX = { 0d, 0d, 0d, 0.06d, 0.13d, 0.16d, 0.16d, 0.10d };
        private static readonly double[] RootY = { -0.45d, -0.45d, -0.45d, -0.46d, -0.475d, -0.48d, -0.48d, -0.46d };
        private static readonly double[] Torso = { 0d, 0d, 1d, 4d, 8d, 10d, 10d, 5d };
        private static readonly double[] Head = { 2d, 2d, 2d, 1d, 0d, -1d, -1d, 1d };

        // 双掌先在胸前上下错开；前脚落稳后才共同向角色前方推出，避免贴成一片。
        private static readonly double[] LeftX = { -0.22d, -0.15d, -0.08d, 0.18d, 0.78d, 1.16d, 1.16d, 0.94d };
        private static readonly double[] LeftY = { 0.18d, 0.24d, 0.30d, 0.38d, 0.51d, 0.58d, 0.58d, 0.47d };
        private static readonly double[] RightX = { 0.52d, 0.48d, 0.50d, 0.68d, 1.08d, 1.56d, 1.56d, 1.24d };
        private static readonly double[] RightY = { 0.00d, 0.06d, 0.12d, 0.20d, 0.34d, 0.42d, 0.42d, 0.30d };
        private static readonly double[] LeftWrist = { 20d, 28d, 36d, 42d, 52d, 60d, 60d, 46d };
        private static readonly double[] RightWrist = { -8d, 2d, 14d, 24d, 38d, 52d, 52d, 36d };

        // 右脚在前三拍前送、短暂离地并于第四拍前落稳；之后脚点保持不变。
        private static readonly double[] RightFootX = { 0.34d, 0.62d, 0.64d, 0.64d, 0.64d, 0.64d, 0.64d, 0.64d };
        private static readonly double[] RightFootY = { -2.08d, -2.00d, -2.08d, -2.08d, -2.08d, -2.08d, -2.08d, -2.08d };
        private static readonly double[] RightShoe = { 0d, 7d, 0d, 0d, 0d, 0d, 0d, 0d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            bool rightFootPlanted = t <= 0d || t >= 0.25d;
            return new PuppetV2Pose(
                Sample(RootX, t), Sample(RootY, t), Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, Sample(RightFootX, t), Sample(RightFootY, t),
                0d, Sample(RightShoe, t), 1d,
                PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                true, rightFootPlanted);
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
