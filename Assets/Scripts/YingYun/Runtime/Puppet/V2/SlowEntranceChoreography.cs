using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>01 缓步入场：十二拍内先望向场中，八次交替小步后缓停。</summary>
    public static class SlowEntranceChoreography
    {
        public const string Name = "缓步入场";
        public const int Beats = 12;
        public const double StartX = -0.625d;
        public const double EndX = 0.625d;
        public const double GroundY = -2.08d;
        public const double StepAdvance = 0.3125d;
        public const double LiftHeight = 0.052d;

        private const double WalkStart = 2d / Beats;
        private const double WalkEnd = 10d / Beats;
        private const double StepDuration = (WalkEnd - WalkStart) / 8d;
        private const double SwingPortion = 0.78d;

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Clamp01(progress);
            double walk = Clamp01((t - WalkStart) / (WalkEnd - WalkStart));
            double root = StartX + ((EndX - StartX) * Smooth(walk));
            double leftX = StartX - 0.34d;
            double rightX = StartX + 0.34d;
            double leftY = GroundY;
            double rightY = GroundY;
            bool leftPlanted = true;
            bool rightPlanted = true;

            for (int step = 0; step < 8; step++)
            {
                double start = WalkStart + (step * StepDuration);
                double end = start + (StepDuration * SwingPortion);
                bool leftSwing = (step & 1) == 0;
                double from = (leftSwing ? StartX - 0.34d : StartX + 0.34d) +
                    ((step / 2) * StepAdvance);
                if (t >= end)
                {
                    if (leftSwing) leftX = from + StepAdvance;
                    else rightX = from + StepAdvance;
                }
                else if (t > start)
                {
                    double phase = Clamp01((t - start) / (end - start));
                    double x = from + (StepAdvance * Smooth(phase));
                    double y = GroundY + (LiftHeight * Math.Sin(Math.PI * phase));
                    if (leftSwing)
                    {
                        leftX = x;
                        leftY = y;
                        leftPlanted = false;
                    }
                    else
                    {
                        rightX = x;
                        rightY = y;
                        rightPlanted = false;
                    }
                }
            }

            // 承接动作在第十拍以后已停止摆臂；末两拍只保留落定的呼吸。
            double armEnvelope = Math.Sin(Math.PI * walk);
            double armPhase = Math.Sin(walk * Math.PI * 8d);
            double leftHandX = root - 0.68d + (0.06d * armEnvelope * armPhase);
            double rightHandX = root + 0.68d - (0.06d * armEnvelope * armPhase);
            // 手臂保持接近伸直的自然垂落，只随步幅小摆；避免步行时肘部过度蜷曲。
            double leftHandY = -0.78d + (0.015d * armEnvelope * armPhase);
            double rightHandY = -0.78d - (0.015d * armEnvelope * armPhase);
            double head = 6d * (1d - Smooth(Clamp01(t / WalkStart)));
            double torso = 3d * armEnvelope;
            return new PuppetV2Pose(root, -0.40d, torso, head,
                leftHandX, leftHandY, rightHandX, rightHandY,
                6d * armEnvelope * armPhase, -6d * armEnvelope * armPhase,
                leftX, leftY, rightX, rightY,
                0d, 0d, 1d, PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                leftPlanted, rightPlanted);
        }

        private static double Clamp01(double value) => Math.Max(0d, Math.Min(1d, value));
        private static double Smooth(double value) => value * value * (3d - (2d * value));
    }
}
