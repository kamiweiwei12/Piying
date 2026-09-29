using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>05 托月仰望：面向侧的右手沿斜线上托成掌，左手守于腰前，定掌后抬头凝望。</summary>
    public static class MoonOfferingGazeChoreography
    {
        public const string Name = "托月仰望";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.18d, 0.375d, 0.50d, 0.72d, 0.82d, 1d };
        private static readonly double[] Torso = { 0d, -1d, -3d, -5d, -7d, -7d, -4d };
        private static readonly double[] Head = { 0d, 2d, 6d, 14d, 24d, 24d, 16d };

        // 左手只作腰腹前的低位呼应，不与高托手画圆或争抢视觉中心。
        private static readonly double[] LeftX = { -0.18d, -0.26d, -0.36d, -0.43d, -0.46d, -0.46d, -0.42d };
        private static readonly double[] LeftY = { 0.24d, 0.06d, -0.14d, -0.25d, -0.28d, -0.28d, -0.22d };
        private static readonly double[] LeftWrist = { 10d, 8d, 5d, 0d, -4d, -4d, -2d };

        // 面向侧的右手从身前沿单一斜线上举；第四拍完成翻腕，第五至六拍保持高托，末两拍稍降。
        private static readonly double[] RightX = { 0.42d, 0.90d, 1.15d, 1.20d, 1.24d, 1.24d, 1.12d };
        private static readonly double[] RightY = { 0.10d, 0.48d, 1.28d, 1.65d, 1.85d, 1.85d, 1.55d };
        private static readonly double[] RightWrist = { -10d, -4d, 18d, 45d, 45d, 42d, 32d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            PuppetHandShape rightShape = t >= 0.375d
                ? PuppetHandShape.SupportPalm : PuppetHandShape.NaturalPalm;
            return new PuppetV2Pose(
                0d, -0.40d, Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, 0d, 1d,
                PuppetHandShape.NaturalPalm, rightShape,
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
