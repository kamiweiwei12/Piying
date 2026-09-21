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

    /// <summary>將輸入軌映射成可替換的操偶部位與拉動角度。</summary>
    public readonly struct ActionBinding
    {
        public ActionBinding(int lane, string keyLabel, PuppetPart part, double primaryDegrees, double secondaryDegrees)
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
            PrimaryDegrees = primaryDegrees;
            SecondaryDegrees = secondaryDegrees;
        }

        public int Lane { get; }
        public int LaneMask => 1 << Lane;
        public string KeyLabel { get; }
        public PuppetPart Part { get; }
        public double PrimaryDegrees { get; }
        public double SecondaryDegrees { get; }
    }

    public static class PrototypeActionBindings
    {
        public static readonly ActionBinding[] All =
        {
            new ActionBinding(0, "Q", PuppetPart.LeftHand, -52d, -28d),
            new ActionBinding(1, "W", PuppetPart.Head, -14d, 0d),
            new ActionBinding(2, "E", PuppetPart.RightHand, 52d, 28d),
            new ActionBinding(3, "A", PuppetPart.LeftFoot, -24d, 18d),
            new ActionBinding(4, "S", PuppetPart.Torso, 11d, -5d),
            new ActionBinding(5, "D", PuppetPart.RightFoot, 24d, -18d)
        };
    }
}
