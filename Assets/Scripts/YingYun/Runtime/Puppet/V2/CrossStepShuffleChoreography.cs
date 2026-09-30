using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>17 蹉步移位：上身守势，三次交替小步向角色前方短移后稳住。</summary>
    public static class CrossStepShuffleChoreography
    {
        public const string Name = "蹉步移位";
        public const int Beats = 8;
        public const double GroundY = -2.08d;
        public const double Travel = 0.42d;
        public const double StepAdvance = 0.28d;
        public const double LiftHeight = 0.035d;

        private const double StepStart = 2d / Beats;
        private const double StepEnd = 6d / Beats;
        private const double StepDuration = (StepEnd - StepStart) / 3d;
        private const double SwingPortion = 0.76d;

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            double travelPhase = Clamp01((t - StepStart) / (StepEnd - StepStart));
            double rootX = Travel * Smooth(travelPhase);
            double rootY = -0.40d;
            double leftX = -0.34d;
            double rightX = 0.34d;
            double leftY = GroundY;
            double rightY = GroundY;
            double leftShoe = 0d;
            double rightShoe = 0d;
            bool leftPlanted = true;
            bool rightPlanted = true;

            for (int step = 0; step < 3; step++)
            {
                double start = StepStart + (step * StepDuration);
                double end = start + (StepDuration * SwingPortion);
                bool rightSwing = (step & 1) == 0;
                double from = rightSwing ? 0.34d + ((step / 2) * StepAdvance) : -0.34d;
                if (t >= end)
                {
                    if (rightSwing) rightX = from + StepAdvance;
                    else leftX = from + StepAdvance;
                }
                else if (t > start)
                {
                    double phase = Clamp01((t - start) / (end - start));
                    double x = from + (StepAdvance * Smooth(phase));
                    double y = GroundY + (LiftHeight * Math.Sin(Math.PI * phase));
                    double shoe = 4d * Math.Sin(Math.PI * phase);
                    rootY += 0.012d * Math.Sin(Math.PI * phase);
                    if (rightSwing)
                    {
                        rightX = x;
                        rightY = y;
                        rightShoe = shoe;
                        rightPlanted = false;
                    }
                    else
                    {
                        leftX = x;
                        leftY = y;
                        leftShoe = shoe;
                        leftPlanted = false;
                    }
                }
            }

            // 手位随骨盆平移，避免移动期间手臂被世界坐标拖向身后。
            double settle = Smooth(Clamp01(t / StepStart));
            double leftHandX = rootX - 0.72d;
            double leftHandY = -0.12d + (0.12d * settle);
            double rightHandX = rootX + 0.30d;
            double rightHandY = -0.58d + (0.18d * settle);
            return new PuppetV2Pose(rootX, rootY, 2d * settle, 4d * (1d - settle),
                leftHandX, leftHandY, rightHandX, rightHandY,
                -20d * settle, 10d * settle,
                leftX, leftY, rightX, rightY, leftShoe, rightShoe,
                1d, PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                leftPlanted, rightPlanted);
        }

        private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
        private static double Smooth(double value) => value * value * (3d - (2d * value));
    }
}
