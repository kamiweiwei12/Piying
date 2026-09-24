using System;

namespace YingYun.Rhythm.Chart
{
    /// <summary>Q/W/E/A/S/D 六键中可用于困难谱的双键组合。</summary>
    public static class KeyboardChordLayout
    {
        public const int LaneCount = 6;

        // 仅排除 Q+D、W+S、E+A；其余十二种无序双键组合全部保留。
        private static readonly int[] AllowedMasks =
        {
            Mask(0, 1), // QW
            Mask(0, 2), // QE
            Mask(0, 3), // QA
            Mask(0, 4), // QS
            Mask(1, 2), // WE
            Mask(1, 3), // WA
            Mask(1, 5), // WD
            Mask(2, 4), // ES
            Mask(2, 5), // ED
            Mask(3, 4), // AS
            Mask(3, 5), // AD
            Mask(4, 5), // SD
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
