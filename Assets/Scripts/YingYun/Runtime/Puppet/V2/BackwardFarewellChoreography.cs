using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>18 回望退场：先伸掌告别，再以四次交替小步后退至幕侧收势。</summary>
    public static class BackwardFarewellChoreography
    {
        public const string Name = "回望退场";
        public const int Beats = 12;
        public const double GroundY = -2.08d;
        public const double EndX = -0.88d;
        public const double StepRetreat = 0.44d;
        public const double LiftHeight = 0.045d;

        private const double RetreatStart = 5d / Beats;
        private const double RetreatEnd = 10d / Beats;
        private const double StepDuration = (RetreatEnd - RetreatStart) / 4d;
        private const double SwingPortion = 0.78d;

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            double retreat = Clamp01((t - RetreatStart) / (RetreatEnd - RetreatStart));
            double rootX = EndX * Smooth(retreat);
            double rootY = -0.40d;
            double leftX = -0.34d;
            double rightX = 0.34d;
            double leftY = GroundY;
            double rightY = GroundY;
            double leftShoe = 0d;
            double rightShoe = 0d;
            bool leftPlanted = true;
            bool rightPlanted = true;

            for (int step = 0; step < 4; step++)
            {
                double start = RetreatStart + (step * StepDuration);
                double end = start + (StepDuration * SwingPortion);
                bool leftSwing = (step & 1) == 0;
                double from = (leftSwing ? -0.34d : 0.34d) - ((step / 2) * StepRetreat);
                if (t >= end)
                {
                    if (leftSwing) leftX = from - StepRetreat;
                    else rightX = from - StepRetreat;
                }
                else if (t > start)
                {
                    double phase = Clamp01((t - start) / (end - start));
                    double x = from - (StepRetreat * Smooth(phase));
                    double y = GroundY + (LiftHeight * Math.Sin(Math.PI * phase));
                    double shoe = -4d * Math.Sin(Math.PI * phase);
                    rootY += 0.014d * Math.Sin(Math.PI * phase);
                    if (leftSwing)
                    {
                        leftX = x;
                        leftY = y;
                        leftShoe = shoe;
                        leftPlanted = false;
                    }
                    else
                    {
                        rightX = x;
                        rightY = y;
                        rightShoe = shoe;
                        rightPlanted = false;
                    }
                }
            }

            double farewell = Smooth(Clamp01(t / (3d / Beats)));
            double release = Smooth(Clamp01((t - (9d / Beats)) / (3d / Beats)));
            double rightHandX = Lerp(0.48d, 1.42d, farewell);
            double rightHandY = Lerp(-0.28d, 0.28d, farewell);
            // 后退期间告别手相对身体保持伸出，不能留在舞台世界坐标把手臂拉长。
            rightHandX += rootX;
            rightHandX = Lerp(rightHandX, rootX + 0.48d, release);
            rightHandY = Lerp(rightHandY, -0.72d, release);
            double leftHandX = rootX - 0.48d;
            double leftHandY = -0.68d;
            double head = Lerp(0d, -4d, farewell);
            head = Lerp(head, 5d, Smooth(Clamp01((t - RetreatStart) / (1d - RetreatStart))));
            double torso = Lerp(2d, 5d, Smooth(Clamp01((t - RetreatStart) / (1d - RetreatStart))));
            return new PuppetV2Pose(rootX, rootY, torso, head,
                leftHandX, leftHandY, rightHandX, rightHandY,
                0d, Lerp(-10d, 15d, farewell) * (1d - release),
                leftX, leftY, rightX, rightY, leftShoe, rightShoe,
                1d, PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                leftPlanted, rightPlanted);
        }

        private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
        private static double Smooth(double value) => value * value * (3d - (2d * value));
        private static double Lerp(double from, double to, double value) => from + ((to - from) * value);
    }
}
