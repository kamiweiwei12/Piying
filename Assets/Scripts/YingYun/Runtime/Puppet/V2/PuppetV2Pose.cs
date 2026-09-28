using System;

namespace YingYun.Rhythm.Puppet.V2
{
    public enum PuppetHandShape
    {
        NaturalPalm,
        SupportPalm,
        DirectionPalm,
        ClosedPalm
    }

    /// <summary>V2 全身姿态数据；不依赖 Unity，也不直接修改 Transform。</summary>
    public readonly struct PuppetV2Pose
    {
        public readonly double RootX;
        public readonly double RootY;
        public readonly double Torso;
        public readonly double Head;
        public readonly double LeftHandX;
        public readonly double LeftHandY;
        public readonly double RightHandX;
        public readonly double RightHandY;
        public readonly double LeftWrist;
        public readonly double RightWrist;
        public readonly double LeftFootX;
        public readonly double LeftFootY;
        public readonly double RightFootX;
        public readonly double RightFootY;
        public readonly double LeftShoe;
        public readonly double RightShoe;
        public readonly double Facing;
        public readonly PuppetHandShape LeftHandShape;
        public readonly PuppetHandShape RightHandShape;
        public readonly bool LeftFootPlanted;
        public readonly bool RightFootPlanted;

        public PuppetV2Pose(double rootX, double rootY, double torso, double head,
            double leftHandX, double leftHandY, double rightHandX, double rightHandY,
            double leftWrist, double rightWrist, double leftFootX, double leftFootY,
            double rightFootX, double rightFootY, double leftShoe, double rightShoe,
            double facing, PuppetHandShape leftHandShape, PuppetHandShape rightHandShape,
            bool leftFootPlanted, bool rightFootPlanted)
        {
            RootX = rootX;
            RootY = rootY;
            Torso = torso;
            Head = head;
            LeftHandX = leftHandX;
            LeftHandY = leftHandY;
            RightHandX = rightHandX;
            RightHandY = rightHandY;
            LeftWrist = leftWrist;
            RightWrist = rightWrist;
            LeftFootX = leftFootX;
            LeftFootY = leftFootY;
            RightFootX = rightFootX;
            RightFootY = rightFootY;
            LeftShoe = leftShoe;
            RightShoe = rightShoe;
            Facing = facing;
            LeftHandShape = leftHandShape;
            RightHandShape = rightHandShape;
            LeftFootPlanted = leftFootPlanted;
            RightFootPlanted = rightFootPlanted;
        }
    }

    /// <summary>06 双展山膀：先收肘蓄势，再向角色左右展开，显势后微收。</summary>
    public static class DoubleMountainArmChoreography
    {
        public const string Name = "双展山膀";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.25d, 0.625d, 0.75d, 1d };
        // 双手始终先向身体外侧引出，再沿外弧上升到水平展翅；禁止先收向胸前。
        private static readonly double[] LeftX = { -0.34d, -0.70d, -1.35d, -1.84d, -1.76d };
        private static readonly double[] LeftY = { -0.86d, -0.66d, 0.18d, 0.66d, 0.62d };
        private static readonly double[] RightX = { 0.30d, 0.70d, 1.35d, 1.84d, 1.76d };
        private static readonly double[] RightY = { -0.86d, -0.66d, 0.18d, 0.66d, 0.62d };
        private static readonly double[] LeftWrist = { 0d, 5d, 9d, 12d, 9d };
        private static readonly double[] RightWrist = { 0d, -5d, -9d, -12d, -9d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Math.Max(0d, Math.Min(1d, progress));
            return new PuppetV2Pose(
                // 当前贴图髋到踝的自然直立距离要求骨盆高于旧动作零姿，
                // 否则双膝会被迫弯成菱形；P1 固定在该标定高度且不做位移。
                0d, -0.40d, 0d, 0d,
                Sample(LeftX, t), Sample(LeftY, t), Sample(RightX, t), Sample(RightY, t),
                Sample(LeftWrist, t), Sample(RightWrist, t),
                -0.34d, -2.08d, 0.34d, -2.08d,
                0d, 0d, 1d, PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                true, true);
        }

        private static double Sample(double[] values, double progress)
        {
            int segment = 0;
            while (segment < Times.Length - 2 && progress > Times[segment + 1]) segment++;
            double span = Times[segment + 1] - Times[segment];
            double t = span <= 0d ? 0d : (progress - Times[segment]) / span;
            t = Math.Max(0d, Math.Min(1d, t));
            double smooth = t * t * (3d - (2d * t));
            return values[segment] + ((values[segment + 1] - values[segment]) * smooth);
        }
    }

    /// <summary>15 提膝挂脚：左脚承重，右脚折提，落脚完成后才回正重心。</summary>
    public static class RaisedKneeHookedFootChoreography
    {
        public const string Name = "提膝挂脚";
        public const int Beats = 8;

        private static readonly double[] Times = { 0d, 0.25d, 0.45d, 0.65d, 0.85d, 1d };
        private static readonly double[] RootX = { 0d, -0.18d, -0.18d, -0.18d, -0.18d, 0d };
        // 右脚落在移重心后的真实可达点并锁住；骨盆回中时不拖动已落地的鞋。
        private static readonly double[] RightFootX = { 0.34d, 0.34d, 0.60d, 0.60d, 0.14d, 0.14d };
        private static readonly double[] RightFootY = { -2.08d, -2.08d, -1.65d, -1.65d, -2.08d, -2.08d };
        private static readonly double[] RightShoe = { 0d, 0d, -10d, -10d, 0d, 0d };
        private static readonly double[] LeftHandX = { -0.34d, -0.75d, -1.40d, -1.48d, -1.10d, -0.55d };
        private static readonly double[] LeftHandY = { -0.86d, -0.20d, 0.48d, 0.52d, 0.20d, -0.55d };
        private static readonly double[] RightHandX = { 0.30d, 0.52d, 0.62d, 0.62d, 0.48d, 0.34d };
        private static readonly double[] RightHandY = { -0.86d, -0.10d, 0.25d, 0.25d, -0.10d, -0.60d };

        public static PuppetV2Pose Evaluate(double progress)
        {
            double t = Math.Max(0d, Math.Min(1d, progress));
            bool rightPlanted = t <= 0.25d || t >= 0.85d;
            return new PuppetV2Pose(
                Sample(RootX, t), -0.40d, 0d, 0d,
                Sample(LeftHandX, t), Sample(LeftHandY, t),
                Sample(RightHandX, t), Sample(RightHandY, t),
                8d, -8d, -0.34d, -2.08d,
                Sample(RightFootX, t), Sample(RightFootY, t),
                0d, Sample(RightShoe, t), 1d,
                PuppetHandShape.NaturalPalm, PuppetHandShape.NaturalPalm,
                true, rightPlanted);
        }

        private static double Sample(double[] values, double progress)
        {
            int segment = 0;
            while (segment < Times.Length - 2 && progress > Times[segment + 1]) segment++;
            double span = Times[segment + 1] - Times[segment];
            double t = span <= 0d ? 0d : (progress - Times[segment]) / span;
            t = Math.Max(0d, Math.Min(1d, t));
            double smooth = t * t * (3d - (2d * t));
            return values[segment] + ((values[segment + 1] - values[segment]) * smooth);
        }
    }
}
