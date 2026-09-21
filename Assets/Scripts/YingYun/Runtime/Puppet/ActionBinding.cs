using System;

namespace YingYun.Rhythm.Puppet
{
    public enum PuppetPart
    {
        LeftHand,
        Head,
        RightHand,
        LeftFoot,
        Torso,
        RightFoot
    }

    /// <summary>六個輸入方向所代表的皮影操演意圖，而不是裸露的骨骼或關節名稱。</summary>
    public enum PerformanceIntent
    {
        LeftLead,
        Lift,
        RightLead,
        LeftStep,
        Sink,
        RightStep
    }

    /// <summary>將輸入軌映射成可替換的操偶部位與拉動角度。</summary>
    public readonly struct ActionBinding
    {
        public ActionBinding(
            int lane,
            string keyLabel,
            PuppetPart part,
            PerformanceIntent intent,
            double primaryDegrees,
            double secondaryDegrees)
        {
            if (lane < 0 || lane >= 6)
            {
                throw new ArgumentOutOfRangeException(nameof(lane));
            }

            if (string.IsNullOrWhiteSpace(keyLabel))
            {
                throw new ArgumentException("Key label is required.", nameof(keyLabel));
            }

            Lane = lane;
            KeyLabel = keyLabel;
            Part = part;
            Intent = intent;
            PrimaryDegrees = primaryDegrees;
            SecondaryDegrees = secondaryDegrees;
        }

        public int Lane { get; }
        public int LaneMask => 1 << Lane;
        public string KeyLabel { get; }
        public PuppetPart Part { get; }
        public PerformanceIntent Intent { get; }
        public double PrimaryDegrees { get; }
        public double SecondaryDegrees { get; }
    }

    public static class PrototypeActionBindings
    {
        public static readonly ActionBinding[] All =
        {
            new ActionBinding(0, "Q", PuppetPart.LeftHand, PerformanceIntent.LeftLead, -52d, -28d),
            new ActionBinding(1, "W", PuppetPart.Head, PerformanceIntent.Lift, -14d, 0d),
            new ActionBinding(2, "E", PuppetPart.RightHand, PerformanceIntent.RightLead, 52d, 28d),
            new ActionBinding(3, "A", PuppetPart.LeftFoot, PerformanceIntent.LeftStep, -24d, 18d),
            new ActionBinding(4, "S", PuppetPart.Torso, PerformanceIntent.Sink, 11d, -5d),
            new ActionBinding(5, "D", PuppetPart.RightFoot, PerformanceIntent.RightStep, 24d, -18d)
        };
    }
}
