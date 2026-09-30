using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>13 躬身请势：右掌翻起向前下方相请，左手守胸，腰带头完成深躬。</summary>
    public static class BowingInvitationChoreography
    {
        public const string Name = "躬身请势";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.25d, 0.375d, 0.625d, 0.75d, 0.875d, 1d };
        private static readonly double[] RootX = { 0d, 0d, 0.015d, 0.035d, 0.04d, 0.025d, 0.015d };
        private static readonly double[] RootY = { -0.40d, -0.40d, -0.415d, -0.455d, -0.48d, -0.45d, -0.425d };
        private static readonly double[] Torso = { 0d, -2d, -6d, -15d, -21d, -11d, -5d };
        private static readonly double[] Head = { 3d, 1d, -2d, -8d, -13d, -6d, -1d };
        private static readonly double[] LeftX = { -0.52d, -0.40d, -0.28d, -0.20d, -0.18d, -0.23d, -0.34d };
        private static readonly double[] LeftY = { -0.50d, -0.12d, 0.12d, 0.24d, 0.20d, 0.22d, 0.08d };
        private static readonly double[] RightX = { 0.50d, 0.64d, 0.86d, 1.32d, 1.46d, 1.18d, 0.94d };
        private static readonly double[] RightY = { -0.52d, -0.42d, -0.48d, -0.74d, -0.86d, -0.68d, -0.58d };
        private static readonly double[] LeftWrist = { 4d, 9d, 14d, 17d, 18d, 15d, 10d };
        private static readonly double[] RightWrist = { -5d, -18d, -30d, -41d, -45d, -38d, -28d };

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
                PuppetHandShape.SupportPalm, PuppetHandShape.NaturalPalm,
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
