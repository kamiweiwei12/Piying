using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>10 遮额探看：右手先搭到眉上留出间隙，身体随后前探，看定后回身落手。</summary>
    public static class ShadingBrowSearchChoreography
    {
        public const string Name = "遮额探看";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.1875d, 0.375d, 0.50d, 0.625d, 0.75d, 0.875d, 1d };
        private static readonly double[] RootX = { 0d, 0d, 0.01d, 0.045d, 0.065d, 0.065d, 0.04d, 0.02d };
        private static readonly double[] RootY = { -0.45d, -0.45d, -0.46d, -0.48d, -0.49d, -0.49d, -0.47d, -0.45d };
        private static readonly double[] Torso = { 0d, 0d, -1d, -8d, -12d, -12d, -6d, -3d };
        private static readonly double[] Head = { 3d, 5d, 7d, 12d, 18d, 18d, 10d, 5d };

        // 左手守在腰腹前，只做前探时的轻微呼应。
        private static readonly double[] LeftX = { 0.00d, 0.00d, -0.02d, -0.04d, -0.05d, -0.05d, -0.03d, 0.00d };
        private static readonly double[] LeftY = { -0.18d, -0.18d, -0.19d, -0.21d, -0.22d, -0.22d, -0.20d, -0.18d };
        private static readonly double[] LeftWrist = { 8d, 8d, 8d, 7d, 7d, 7d, 8d, 8d };

        // 右手先走外侧上弧到眉前，再由身体前探；腕点始终在额侧前方，禁止穿头冠。
        private static readonly double[] RightX = { 0.92d, 1.04d, 1.00d, 0.96d, 0.93d, 0.93d, 0.98d, 0.96d };
        private static readonly double[] RightY = { 0.42d, 0.80d, 1.24d, 1.38d, 1.42d, 1.42d, 0.96d, 0.48d };
        private static readonly double[] RightWrist = { 16d, -40d, -110d, -135d, -140d, -140d, -50d, 16d };

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
