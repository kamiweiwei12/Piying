using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>09 指路远眺：头部先提示目标，右臂随后斜前伸，第五至六拍以指向掌停顿远眺。</summary>
    public static class PointingFarGazeChoreography
    {
        public const string Name = "指路远眺";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.25d, 0.50d, 0.625d, 0.75d, 0.875d, 1d };
        private static readonly double[] Torso = { 0d, 1d, 4d, 4d, 4d, 3d, 1d };
        private static readonly double[] Head = { 1d, 7d, 8d, 8d, 8d, 6d, 3d };

        // 左手全程守在腰腹前，只随躯干轻微呼应。
        private static readonly double[] LeftX = { 0.02d, 0.00d, -0.04d, -0.06d, -0.06d, -0.04d, 0.00d };
        private static readonly double[] LeftY = { -0.18d, -0.18d, -0.20d, -0.20d, -0.20d, -0.19d, -0.18d };
        private static readonly double[] LeftWrist = { 8d, 8d, 7d, 7d, 7d, 8d, 8d };

        // 右手先保持半收，头部提示后才沿单一斜线伸向远方；末两拍回掌半收。
        private static readonly double[] RightX = { 0.92d, 0.94d, 1.42d, 1.71d, 1.71d, 1.40d, 0.92d };
        private static readonly double[] RightY = { 0.42d, 0.44d, 0.76d, 1.02d, 1.02d, 0.82d, 0.42d };
        private static readonly double[] RightWrist = { 16d, 15d, 10d, 5d, 5d, 10d, 16d };
        private static readonly double[] RightShoe = { 0d, 6d, 0d, 0d, 0d, 0d, 0d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            PuppetHandShape rightShape = t >= 0.50d && t < 0.875d
                ? PuppetHandShape.DirectionPalm : PuppetHandShape.NaturalPalm;
            return new PuppetV2Pose(
                0d, -0.45d, Sample(Torso, t), Sample(Head, t),
                Sample(LeftX, t), Sample(LeftY, t),
                Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, Sample(RightShoe, t), 1d,
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
