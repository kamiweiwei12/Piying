using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>04 云手展圆：左手先走外侧上弧，右手延后走胸腹下弧，错拍接圆后展势。</summary>
    public static class CloudHandsCircleChoreography
    {
        public const string Name = "云手展圆";
        public const int Beats = 12;

        private static readonly double[] Times = { 0d, 0.20d, 0.30d, 0.46d, 0.60d, 0.72d, 0.84d, 1d };
        private static readonly double[] RootX = { 0d, -0.01d, -0.03d, -0.02d, 0d, 0.03d, 0.04d, 0.02d };
        private static readonly double[] RootY = { -0.40d, -0.40d, -0.41d, -0.42d, -0.41d, -0.40d, -0.40d, -0.40d };
        private static readonly double[] Torso = { 0d, -2d, -5d, -4d, 1d, 5d, 4d, 2d };
        private static readonly double[] Head = { 5d, 4d, 1d, -4d, -9d, -10d, -7d, -5d };

        // 左手先走上圆：由胸前向左外侧展开，再越过眉上最高点，最后斜上展势。
        private static readonly double[] LeftX = { -0.34d, -0.16d, -1.82d, -1.20d, -0.75d, -0.25d, -0.20d, -1.12d };
        private static readonly double[] LeftY = { 0.05d, 0.30d, 0.55d, 1.15d, 1.62d, 1.72d, 1.48d, 1.18d };
        private static readonly double[] LeftWrist = { 15d, 20d, 10d, -8d, -24d, -35d, -20d, -16d };

        // 右手延后约 1.5 拍走下圆：由胸前向右外侧沉落，经腹下圆底后回到胸前半收。
        private static readonly double[] RightX = { 0.34d, 0.34d, 1.88d, 1.15d, 0.95d, 0.55d, 0.35d, 0.28d };
        private static readonly double[] RightY = { 0.05d, 0.08d, 0.48d, 0.05d, -0.50d, -0.68d, -0.30d, 0.38d };
        private static readonly double[] RightWrist = { -15d, -15d, -10d, 0d, 12d, 10d, 4d, -8d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            PuppetHandShape leftShape = t >= 0.60d ? PuppetHandShape.SupportPalm : PuppetHandShape.NaturalPalm;
            return new PuppetV2Pose(
                Sample(RootX, t), Sample(RootY, t), Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, 0d, 1d,
                leftShape, PuppetHandShape.NaturalPalm,
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
