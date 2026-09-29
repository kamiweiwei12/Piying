using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>08 穿掌送势：前脚先引并落稳，身体随势前移，右掌再从胸前沿直线穿送。</summary>
    public static class PiercingPalmDriveChoreography
    {
        public const string Name = "穿掌送势";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.12d, 0.24d, 0.48d, 0.68d, 0.82d, 1d };
        private static readonly double[] RootX = { 0d, 0d, 0d, 0.08d, 0.16d, 0.16d, 0.10d };
        private static readonly double[] RootY = { -0.425d, -0.425d, -0.425d, -0.425d, -0.425d, -0.425d, -0.425d };
        private static readonly double[] Torso = { 0d, 0d, 0d, 4d, 7d, 7d, 3d };
        private static readonly double[] Head = { 3d, 3d, 3d, 2d, 0d, 0d, 1d };

        // 左手随身体守在腹前，只作低位呼应，不与穿掌手争抢直线路径。
        private static readonly double[] LeftX = { -0.18d, -0.12d, -0.05d, 0.02d, 0.08d, 0.08d, 0.02d };
        private static readonly double[] LeftY = { -0.20d, -0.17d, -0.15d, -0.12d, -0.10d, -0.10d, -0.14d };
        private static readonly double[] LeftWrist = { 10d, 10d, 9d, 8d, 7d, 7d, 8d };

        // 右掌先在胸前聚势，再沿近水平直线送出；末段只回收约三分之一。
        private static readonly double[] RightX = { 0.48d, 0.55d, 0.62d, 1.28d, 1.88d, 1.88d, 1.48d };
        private static readonly double[] RightY = { 0.10d, 0.16d, 0.25d, 0.50d, 0.695d, 0.695d, 0.52d };
        private static readonly double[] RightWrist = { 20d, 18d, 14d, 4d, -15d, -15d, -8d };

        // 右脚先引、轻抬、落稳；落地后才允许 root 前送。左脚全程作为后侧支撑点。
        private static readonly double[] RightFootX = { 0.34d, 0.50d, 0.50d, 0.50d, 0.50d, 0.50d, 0.50d };
        private static readonly double[] RightFootY = { -2.08d, -2.02d, -2.08d, -2.08d, -2.08d, -2.08d, -2.08d };
        private static readonly double[] RightShoe = { 0d, 6d, 0d, 0d, 0d, 0d, 0d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            bool rightFootPlanted = t <= 0d || t >= 0.24d;
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
