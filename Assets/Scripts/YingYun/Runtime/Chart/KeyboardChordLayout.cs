using System;

namespace YingYun.Rhythm.Chart
{
    /// <summary>T/Y/U/G/H/J 六键中可用于困难谱的双键组合。</summary>
    public static class KeyboardChordLayout
    {
        public const int LaneCount = 6;

        // 仅排除 T+J、Y+H、U+G；其余十二种无序双键组合全部保留。
        private static readonly int[] AllowedMasks =
        {
            Mask(0, 1), // TY
            Mask(0, 2), // TU
            Mask(0, 3), // TG
            Mask(0, 4), // TH
            Mask(1, 2), // YU
            Mask(1, 3), // YG
            Mask(1, 5), // YJ
            Mask(2, 4), // UH
            Mask(2, 5), // UJ
            Mask(3, 4), // GH
            Mask(3, 5), // GJ
            Mask(4, 5), // HJ
        };

        public static int AllowedCount => AllowedMasks.Length;

        public static int GetAllowedMask(int index)
        {
            int wrapped = index % AllowedMasks.Length;
            if (wrapped < 0)
            {
                wrapped += AllowedMasks.Length;
            }

            return AllowedMasks[wrapped];
        }

        public static bool IsAllowed(int mask)
        {
            for (int i = 0; i < AllowedMasks.Length; i++)
            {
                if (AllowedMasks[i] == mask)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsLane(int mask, int lane)
        {
            if (lane < 0 || lane >= LaneCount)
            {
                throw new ArgumentOutOfRangeException(nameof(lane));
            }

            return (mask & (1 << lane)) != 0;
        }

        private static int Mask(int firstLane, int secondLane)
        {
            return (1 << firstLane) | (1 << secondLane);
        }
    }
}
