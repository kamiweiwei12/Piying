namespace YingYun.Rhythm.Puppet
{
    /// <summary>M8.2 水袖能力候選；目前只供結構與剪影預覽，不接入正式舞句。</summary>
    public enum WaterSleeveGesture
    {
        FlingSleeve
    }

    /// <summary>固定皮片尺寸；以整條手臂 1.54 單位為比例基準。</summary>
    public static class WaterSleeveRigDesign
    {
        public const int PlateCount = 4;
        public const int PivotCount = 4;
        public const double ArmLength = 1.54d;

        private static readonly double[] Lengths = { 0.36d, 0.50d, 0.43d, 0.33d };
        private static readonly double[] Widths = { 0.58d, 0.50d, 0.42d, 0.32d };

        public static double PlateLength(int index) => Lengths[Validate(index)];
        public static double PlateWidth(int index) => Widths[Validate(index)];

        public static double TotalLength
        {
            get
            {
                double total = 0d;
                for (int i = 0; i < Lengths.Length; i++) total += Lengths[i];
                return total;
            }
        }

        private static int Validate(int index)
        {
            if (index < 0 || index >= PlateCount)
                throw new System.ArgumentOutOfRangeException(nameof(index), index, null);
            return index;
        }
    }

    /// <summary>肩、肘、腕與四個被動袖節的固定取樣；運行時只查表與線性插值。</summary>
    public sealed class WaterSleevePhrase
    {
        public const int SampleCount = 129;

        internal WaterSleevePhrase(WaterSleeveGesture gesture, string name,
            double[] shoulder, double[] elbow, double[] wrist, double[][] pivots)
        {
            Gesture = gesture;
            Name = name;
            ShoulderSamples = shoulder;
            ElbowSamples = elbow;
            WristSamples = wrist;
            PivotSamples = pivots;
        }

        public WaterSleeveGesture Gesture { get; }
        public string Name { get; }
        public int ActiveRodCount => 1;
        internal double[] ShoulderSamples { get; }
        internal double[] ElbowSamples { get; }
        internal double[] WristSamples { get; }
        internal double[][] PivotSamples { get; }

        public double Shoulder(double progress) => Sample(ShoulderSamples, progress);
        public double Elbow(double progress) => Sample(ElbowSamples, progress);
        public double Wrist(double progress) => Sample(WristSamples, progress);

        public double Pivot(int index, double progress)
        {
            if (index < 0 || index >= WaterSleeveRigDesign.PivotCount)
                throw new System.ArgumentOutOfRangeException(nameof(index), index, null);
            return Sample(PivotSamples[index], progress);
        }

        private static double Sample(double[] values, double progress)
        {
            double index = System.Math.Max(0d, System.Math.Min(1d, progress)) * (SampleCount - 1);
            int low = (int)index;
            int high = System.Math.Min(low + 1, SampleCount - 1);
            return values[low] + ((values[high] - values[low]) * (index - low));
        }
    }

    public static class WaterSleeveChoreography
    {
        private static readonly WaterSleevePhrase FlingSleeve = BuildFlingSleeve();

        public static WaterSleevePhrase Get(WaterSleeveGesture gesture)
        {
            switch (gesture)
            {
                case WaterSleeveGesture.FlingSleeve: return FlingSleeve;
                default: throw new System.ArgumentOutOfRangeException(nameof(gesture), gesture, null);
            }
        }

        private static WaterSleevePhrase BuildFlingSleeve()
        {
            // 肩臂先送、腕再發力；四片皮袖依次延遲達峰後收回，避免持續自由擺盪。
            double[] times = { 0d, 0.14d, 0.30d, 0.46d, 0.58d, 0.68d, 0.76d, 0.84d, 0.92d, 1d };
            double[] shoulderKeys = { 0d, -18d, -50d, -88d, -104d, -78d, -54d, -34d, -16d, 0d };
            double[] elbowKeys = { 0d, 12d, 36d, 52d, 24d, 34d, 40d, 30d, 16d, 0d };
            double[] wristKeys = { 0d, -12d, -18d, -56d, -82d, -38d, -12d, 12d, 6d, 0d };
            double[][] pivotKeys =
            {
                new[] { 0d, 0d, 6d, -10d, -28d, -44d, -24d, -8d, 4d, 0d },
                new[] { 0d, 0d, 0d, 4d, -8d, -26d, -50d, -24d, -6d, 0d },
                new[] { 0d, 0d, 0d, 0d, 3d, -8d, -24d, -52d, -20d, 0d },
                new[] { 0d, 0d, 0d, 0d, 0d, 2d, -8d, -24d, -34d, 0d }
            };

            double[] shoulder = Samples(times, shoulderKeys);
            double[] elbow = Samples(times, elbowKeys);
            double[] wrist = Samples(times, wristKeys);
            var pivots = new double[WaterSleeveRigDesign.PivotCount][];
            for (int i = 0; i < pivots.Length; i++) pivots[i] = Samples(times, pivotKeys[i]);
            return new WaterSleevePhrase(WaterSleeveGesture.FlingSleeve,
                "甩袖（皮影分片實驗）", shoulder, elbow, wrist, pivots);
        }

        private static double[] Samples(double[] times, double[] values)
        {
            var result = new double[WaterSleevePhrase.SampleCount];
            for (int i = 0; i < result.Length; i++)
            {
                double progress = (double)i / (result.Length - 1);
                int segment = 0;
                while (segment < times.Length - 2 && progress > times[segment + 1]) segment++;
                double duration = times[segment + 1] - times[segment];
                double t = duration <= 0d ? 1d : (progress - times[segment]) / duration;
                t = System.Math.Max(0d, System.Math.Min(1d, t));
                double smooth = t * t * (3d - (2d * t));
                result[i] = values[segment] + ((values[segment + 1] - values[segment]) * smooth);
            }
            return result;
        }
    }
}
