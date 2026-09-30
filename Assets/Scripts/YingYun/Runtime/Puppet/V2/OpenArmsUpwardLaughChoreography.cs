using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>12 展臂仰笑：由胸前内收突然向斜上展开，以抬头开胸表达明朗情绪。</summary>
    public static class OpenArmsUpwardLaughChoreography
    {
        public const string Name = "展臂仰笑";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.25d, 0.375d, 0.50d, 0.625d, 0.75d, 0.875d, 1d };
        private static readonly double[] RootX = { 0d, 0d, -0.005d, -0.015d, -0.025d, -0.03d, -0.025d, -0.02d };
        private static readonly double[] RootY = { -0.48d, -0.49d, -0.48d, -0.46d, -0.44d, -0.43d, -0.43d, -0.44d };
        private static readonly double[] Torso = { -6d, -5d, -1d, 4d, 8d, 10d, 9d, 7d };
        private static readonly double[] Head = { -6d, -4d, -5d, -9d, -14d, -16d, -13d, -10d };

        // 先在胸前聚势，再沿肩外侧斜上展开；终点高于山膀，避免误读成水平定架。
        private static readonly double[] LeftX = { -0.50d, -0.65d, -0.72d, -0.92d, -1.28d, -1.53d, -1.55d, -1.45d };
        private static readonly double[] LeftY = { 0.36d, 0.45d, 0.68d, 0.92d, 1.12d, 1.24d, 1.22d, 1.12d };
        private static readonly double[] RightX = { 0.93d, 0.70d, 0.88d, 1.12d, 1.38d, 1.53d, 1.55d, 1.45d };
        private static readonly double[] RightY = { 0.43d, 0.50d, 0.72d, 0.94d, 1.14d, 1.26d, 1.24d, 1.14d };
        private static readonly double[] LeftWrist = { 20d, 28d, 18d, 4d, -10d, -18d, -16d, -12d };
        private static readonly double[] RightWrist = { 4d, -20d, -12d, 2d, 14d, 20d, 18d, 14d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            return new PuppetV2Pose(
                Sample(RootX, t), Sample(RootY, t), Sample(Torso, t), Sample(Head, t),
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
