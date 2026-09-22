namespace YingYun.Rhythm.Puppet
{
    /// <summary>M8.1 第一批實驗手勢；通過實機剪影驗收前不接入正式譜面。</summary>
    public enum HandGesture
    {
        PressPalm,
        SupportPalm
    }

    /// <summary>由同一支手簽帶動肩、肘、腕及掌形的預編取樣。</summary>
    public sealed class HandGesturePhrase
    {
        public const int SampleCount = 129;

        internal HandGesturePhrase(HandGesture gesture, string name, double[] shoulder,
            double[] elbow, double[] wrist, double[] finger)
        {
            Gesture = gesture;
            Name = name;
            ShoulderSamples = shoulder;
            ElbowSamples = elbow;
            WristSamples = wrist;
            FingerSamples = finger;
        }

        public HandGesture Gesture { get; }
        public string Name { get; }
        public int ActiveRodCount => 1;
        internal double[] ShoulderSamples { get; }
        internal double[] ElbowSamples { get; }
        internal double[] WristSamples { get; }
        internal double[] FingerSamples { get; }

        public double Shoulder(double progress) => Sample(ShoulderSamples, progress);
        public double Elbow(double progress) => Sample(ElbowSamples, progress);
        public double Wrist(double progress) => Sample(WristSamples, progress);
        public double Finger(double progress) => Sample(FingerSamples, progress);

        private static double Sample(double[] values, double progress)
        {
            double index = System.Math.Max(0d, System.Math.Min(1d, progress)) * (SampleCount - 1);
            int low = (int)index;
            int high = System.Math.Min(low + 1, SampleCount - 1);
            return values[low] + ((values[high] - values[low]) * (index - low));
        }
    }

    public static class HandGestureChoreography
    {
        private static readonly HandGesturePhrase PressPalm = Build(HandGesture.PressPalm);
        private static readonly HandGesturePhrase SupportPalm = Build(HandGesture.SupportPalm);

        public static HandGesturePhrase Get(HandGesture gesture)
        {
            return gesture == HandGesture.PressPalm ? PressPalm : SupportPalm;
        }

        private static HandGesturePhrase Build(HandGesture gesture)
        {
            bool press = gesture == HandGesture.PressPalm;
            string name = press ? "按掌（實驗）" : "托掌（實驗）";
            // 按掌位於身前、沉肩圓肘；托掌抬高手位並翻出掌面。角度是本作灰盒轉譯，非教材量測值。
            double[] shoulderKeys = press
                ? new[] { 0d, -22d, -38d, -42d }
                : new[] { 0d, -48d, -82d, -96d };
            double[] elbowKeys = press
                ? new[] { 0d, 32d, 58d, 64d }
                : new[] { 0d, 18d, 36d, 42d };
            double[] wristKeys = press
                ? new[] { 0d, 24d, 58d, 72d }
                : new[] { 0d, -28d, -64d, -82d };
            double[] fingerKeys = press
                ? new[] { 0d, -6d, -14d, -18d }
                : new[] { 0d, 8d, 18d, 24d };

            var shoulder = new double[HandGesturePhrase.SampleCount];
            var elbow = new double[HandGesturePhrase.SampleCount];
            var wrist = new double[HandGesturePhrase.SampleCount];
            var finger = new double[HandGesturePhrase.SampleCount];
            for (int i = 0; i < HandGesturePhrase.SampleCount; i++)
            {
                double progress = (double)i / (HandGesturePhrase.SampleCount - 1);
                shoulder[i] = KeyValue(shoulderKeys, progress);
                elbow[i] = KeyValue(elbowKeys, progress);
                wrist[i] = KeyValue(wristKeys, progress);
                finger[i] = KeyValue(fingerKeys, progress);
            }

            return new HandGesturePhrase(gesture, name, shoulder, elbow, wrist, finger);
        }

        private static double KeyValue(double[] keys, double progress)
        {
            double scaled = progress * (keys.Length - 1);
            int low = System.Math.Min((int)scaled, keys.Length - 2);
            double t = scaled - low;
            double smooth = t * t * (3d - (2d * t));
            return keys[low] + ((keys[low + 1] - keys[low]) * smooth);
        }
    }
}
