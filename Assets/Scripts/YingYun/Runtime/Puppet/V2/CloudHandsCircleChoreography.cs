using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>04 云手展圆：左手先走额上圆弧，右手延后走腹前下弧，展圆后沿原弧回收聚手。</summary>
    public static class CloudHandsCircleChoreography
    {
        public const string Name = "云手展圆";
        public const int Beats = 12;

        private static readonly double[] Times = { 0d, 0.125d, 0.28d, 0.46d, 0.58d, 0.70d, 0.82d, 1d };
        private static readonly double[] Torso = { 0d, -1d, -2d, -1d, 1d, 2d, 1d, 0d };
        private static readonly double[] Head = { 5d, 2d, -4d, -8d, -10d, -6d, -2d, 2d };

        // 左手先走上圆：由胸前沿左外侧越过额上，在第七至第九拍展直，再沿原弧下降回收。
        private static readonly double[] LeftX = { -0.34d, -0.55d, -1.15d, -0.55d, -0.20d, -1.83d, -1.87d, -0.18d };
        private static readonly double[] LeftY = { 0.05d, 0.55d, 1.25d, 1.68d, 1.72d, 1.17d, 0.95d, 0.32d };
        private static readonly double[] LeftWrist = { 15d, 18d, 8d, -18d, -35d, -20d, -5d, 10d };

        // 右手延后 1.5 拍走下圆：由腹前托起，经右侧圆底展开，再由下方向上回到身前。
        private static readonly double[] RightX = { 0.34d, 0.34d, 0.55d, 1.10d, 0.85d, 1.66d, 1.78d, 0.18d };
        private static readonly double[] RightY = { 0.05d, 0.05d, 0.22d, -0.15d, -0.65d, 0.04d, 0.34d, 0.24d };
        private static readonly double[] RightWrist = { -15d, -15d, -10d, 0d, 10d, 18d, 5d, -10d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            PuppetHandShape leftShape = t >= 0.40d && t < 0.90d
                ? PuppetHandShape.SupportPalm : PuppetHandShape.NaturalPalm;
            PuppetHandShape rightShape = t >= 0.46d && t < 0.94d
                ? PuppetHandShape.SupportPalm : PuppetHandShape.NaturalPalm;
            return new PuppetV2Pose(
                0d, -0.40d, Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, 0d, 1d,
                leftShape, rightShape,
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
