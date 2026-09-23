namespace YingYun.Rhythm.Puppet
{
    /// <summary>M8.1 預編手勢；正式舞句只可透過 GetFormal 取得已通過剪影驗收的版本。</summary>
    public enum HandGesture
    {
        PressPalm,
        SupportPalm,
        ThreadPalm,
        TurnWrist,
        SingleFinger,
        SwordFinger
    }

    /// <summary>由同一支手簽帶動肩、肘、腕及掌形的預編取樣。</summary>
    public sealed class HandGesturePhrase
    {
        public const int SampleCount = 129;

        internal HandGesturePhrase(HandGesture gesture, string name, double[] shoulder,
            double[] elbow, double[] wrist, double[] finger, double[] fingerWidth,
            double[] fingerLength, double[] pointFinger, double[] secondPointFinger)
        {
            Gesture = gesture;
            Name = name;
            ShoulderSamples = shoulder;
            ElbowSamples = elbow;
            WristSamples = wrist;
            FingerSamples = finger;
            FingerWidthSamples = fingerWidth;
            FingerLengthSamples = fingerLength;
            PointFingerSamples = pointFinger;
            SecondPointFingerSamples = secondPointFinger;
        }

        public HandGesture Gesture { get; }
        public string Name { get; }
        public int ActiveRodCount => 1;
        internal double[] ShoulderSamples { get; }
        internal double[] ElbowSamples { get; }
        internal double[] WristSamples { get; }
        internal double[] FingerSamples { get; }
        internal double[] FingerWidthSamples { get; }
        internal double[] FingerLengthSamples { get; }
        internal double[] PointFingerSamples { get; }
        internal double[] SecondPointFingerSamples { get; }

        public double Shoulder(double progress) => Sample(ShoulderSamples, progress);
        public double Elbow(double progress) => Sample(ElbowSamples, progress);
        public double Wrist(double progress) => Sample(WristSamples, progress);
        public double Finger(double progress) => Sample(FingerSamples, progress);
        public double FingerWidth(double progress) => Sample(FingerWidthSamples, progress);
        public double FingerLength(double progress) => Sample(FingerLengthSamples, progress);
        public double PointFinger(double progress) => Sample(PointFingerSamples, progress);
        public double SecondPointFinger(double progress) => Sample(SecondPointFingerSamples, progress);

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
        private static readonly HandGesturePhrase PressPalm = Build(HandGesture.PressPalm, false);
        private static readonly HandGesturePhrase SupportPalm = Build(HandGesture.SupportPalm, false);
        private static readonly HandGesturePhrase SupportPalmWithClosing = Build(HandGesture.SupportPalm, true);
        private static readonly HandGesturePhrase ThreadPalm = Build(HandGesture.ThreadPalm, false);
        private static readonly HandGesturePhrase TurnWrist = Build(HandGesture.TurnWrist, false);
        private static readonly HandGesturePhrase TurnWristWithClosing = Build(HandGesture.TurnWrist, true);
        private static readonly HandGesturePhrase SingleFinger = Build(HandGesture.SingleFinger, false);
        private static readonly HandGesturePhrase SingleFingerWithClosing = Build(HandGesture.SingleFinger, true);
        private static readonly HandGesturePhrase SwordFinger = Build(HandGesture.SwordFinger, false);
        private static readonly HandGesturePhrase SwordFingerWithClosing = Build(HandGesture.SwordFinger, true);

        public static HandGesturePhrase Get(HandGesture gesture)
        {
            switch (gesture)
            {
                case HandGesture.PressPalm: return PressPalm;
                case HandGesture.SupportPalm: return SupportPalm;
                case HandGesture.ThreadPalm: return ThreadPalm;
                case HandGesture.TurnWrist: return TurnWrist;
                case HandGesture.SingleFinger: return SingleFinger;
                case HandGesture.SwordFinger: return SwordFinger;
                default: throw new System.ArgumentOutOfRangeException(nameof(gesture), gesture, null);
            }
        }

        public static HandGesturePhrase GetFormal(HandGesture gesture)
        {
            switch (gesture)
            {
                case HandGesture.PressPalm: return PressPalm;
                case HandGesture.SupportPalm: return SupportPalmWithClosing;
                case HandGesture.ThreadPalm: return ThreadPalm;
                case HandGesture.TurnWrist: return TurnWristWithClosing;
                case HandGesture.SingleFinger: return SingleFingerWithClosing;
                case HandGesture.SwordFinger: return SwordFingerWithClosing;
                default: throw new System.ArgumentOutOfRangeException(nameof(gesture), gesture, null);
            }
        }

        private static HandGesturePhrase Build(HandGesture gesture, bool close)
        {
            string name;
            double[] shoulderKeys;
            double[] elbowKeys;
            double[] wristKeys;
            double[] fingerKeys;
            double[] fingerWidthKeys = { 1d, 1d };
            double[] fingerLengthKeys = { 1d, 1d };
            double[] pointFingerKeys = { 0d, 0d };
            double[] secondPointFingerKeys = { 0d, 0d };
            switch (gesture)
            {
                case HandGesture.PressPalm:
                    name = "按掌（實驗）";
                    shoulderKeys = new[] { 0d, -22d, -38d, -42d };
                    elbowKeys = new[] { 0d, 32d, 58d, 64d };
                    wristKeys = new[] { 0d, 24d, 58d, 72d };
                    fingerKeys = new[] { 0d, -6d, -14d, -18d };
                    break;
                case HandGesture.SupportPalm:
                    name = "托掌（實驗）";
                    shoulderKeys = close ? new[] { -42d, -56d, -82d, -96d, 0d }
                        : new[] { -42d, -56d, -82d, -96d };
                    elbowKeys = close ? new[] { 64d, 54d, 46d, 42d, 0d }
                        : new[] { 64d, 54d, 46d, 42d };
                    wristKeys = close ? new[] { 72d, 28d, -46d, -82d, 0d }
                        : new[] { 72d, 28d, -46d, -82d };
                    fingerKeys = close ? new[] { -18d, -6d, 12d, 24d, 0d }
                        : new[] { -18d, -6d, 12d, 24d };
                    break;
                case HandGesture.ThreadPalm:
                    name = "穿掌（實驗）";
                    // 由身前屈肘聚手後向斜前方穿出；僅驗證現有單手簽能否形成可辨路徑。
                    shoulderKeys = new[] { 0d, -16d, -52d, -102d };
                    elbowKeys = new[] { 0d, 76d, 54d, 18d };
                    wristKeys = new[] { 0d, -24d, -12d, 8d };
                    fingerKeys = new[] { 0d, 6d, 10d, 14d };
                    break;
                case HandGesture.TurnWrist:
                    name = "翻腕（實驗）";
                    // 固定手位後翻換腕面；來源只支持翻托掌／腕部訓練語義，角度不是教材量測值。
                    shoulderKeys = close ? new[] { -102d, -78d, -52d, -52d, -52d, 0d }
                        : new[] { 0d, -34d, -52d, -52d, -52d };
                    elbowKeys = close ? new[] { 18d, 38d, 58d, 58d, 58d, 0d }
                        : new[] { 0d, 44d, 58d, 58d, 58d };
                    wristKeys = close ? new[] { 8d, 30d, 78d, -78d, -104d, 0d }
                        : new[] { 0d, 18d, 78d, -78d, -104d };
                    fingerKeys = close ? new[] { 14d, 4d, -14d, 12d, 18d, 0d }
                        : new[] { 0d, -6d, -14d, 12d, 18d };
                    break;
                case HandGesture.SingleFinger:
                    name = "單指（實驗）";
                    // 教材只支持「單指」指法名稱；手位用於隔離檢查指型剪影，不宣稱是行當規格。
                    shoulderKeys = close ? new[] { 0d, -52d, -102d, -102d, 0d }
                        : new[] { 0d, -52d, -102d };
                    elbowKeys = close ? new[] { 0d, 54d, 18d, 18d, 0d }
                        : new[] { 0d, 54d, 18d };
                    wristKeys = close ? new[] { 0d, -12d, 8d, 8d, 0d }
                        : new[] { 0d, -12d, 8d };
                    fingerKeys = close ? new[] { 0d, 8d, 14d, 14d, 0d }
                        : new[] { 0d, 8d, 14d };
                    fingerWidthKeys = close ? new[] { 1d, 0.72d, 0.48d, 0.48d, 1d }
                        : new[] { 1d, 0.72d, 0.48d };
                    fingerLengthKeys = close ? new[] { 1d, 0.82d, 0.62d, 0.62d, 1d }
                        : new[] { 1d, 0.82d, 0.62d };
                    pointFingerKeys = close ? new[] { 0d, 0.35d, 1d, 1d, 0d }
                        : new[] { 0d, 0.35d, 1d };
                    break;
                case HandGesture.SwordFinger:
                    name = "劍指（實驗）";
                    // 只驗證食、中二指並直與其餘指片收攏的剪影；展示手位不是行當教材角度。
                    shoulderKeys = close ? new[] { 0d, -52d, -102d, -102d, 0d }
                        : new[] { 0d, -52d, -102d };
                    elbowKeys = close ? new[] { 0d, 54d, 18d, 18d, 0d }
                        : new[] { 0d, 54d, 18d };
                    wristKeys = close ? new[] { 0d, -4d, 14d, 14d, 0d }
                        : new[] { 0d, -4d, 14d };
                    fingerKeys = close ? new[] { 0d, 8d, 14d, 14d, 0d }
                        : new[] { 0d, 8d, 14d };
                    fingerWidthKeys = close ? new[] { 1d, 0.68d, 0.42d, 0.42d, 1d }
                        : new[] { 1d, 0.68d, 0.42d };
                    fingerLengthKeys = close ? new[] { 1d, 0.78d, 0.56d, 0.56d, 1d }
                        : new[] { 1d, 0.78d, 0.56d };
                    pointFingerKeys = close ? new[] { 0d, 0.35d, 1d, 1d, 0d }
                        : new[] { 0d, 0.35d, 1d };
                    secondPointFingerKeys = close ? new[] { 0d, 0.35d, 1d, 1d, 0d }
                        : new[] { 0d, 0.35d, 1d };
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(gesture), gesture, null);
            }

            var shoulder = new double[HandGesturePhrase.SampleCount];
            var elbow = new double[HandGesturePhrase.SampleCount];
            var wrist = new double[HandGesturePhrase.SampleCount];
            var finger = new double[HandGesturePhrase.SampleCount];
            var fingerWidth = new double[HandGesturePhrase.SampleCount];
            var fingerLength = new double[HandGesturePhrase.SampleCount];
            var pointFinger = new double[HandGesturePhrase.SampleCount];
            var secondPointFinger = new double[HandGesturePhrase.SampleCount];
            for (int i = 0; i < HandGesturePhrase.SampleCount; i++)
            {
                double progress = (double)i / (HandGesturePhrase.SampleCount - 1);
                shoulder[i] = KeyValue(shoulderKeys, progress);
                elbow[i] = KeyValue(elbowKeys, progress);
                wrist[i] = KeyValue(wristKeys, progress);
                finger[i] = KeyValue(fingerKeys, progress);
                fingerWidth[i] = KeyValue(fingerWidthKeys, progress);
                fingerLength[i] = KeyValue(fingerLengthKeys, progress);
                pointFinger[i] = KeyValue(pointFingerKeys, progress);
                secondPointFinger[i] = KeyValue(secondPointFingerKeys, progress);
            }

            return new HandGesturePhrase(gesture, name, shoulder, elbow, wrist, finger,
                fingerWidth, fingerLength, pointFinger, secondPointFinger);
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

    /// <summary>M8.1 雙手拳掌禮能力；名稱與戲曲行當用法在正式接線前另行核實。</summary>
    public sealed class FistPalmSalutePhrase
    {
        public const int SampleCount = 129;

        internal FistPalmSalutePhrase(double[] leftShoulder, double[] leftElbow,
            double[] leftWrist, double[] leftFinger, double[] rightShoulder,
            double[] rightElbow, double[] rightWrist, double[] rightFinger,
            double[] rightClosure)
        {
            LeftShoulderSamples = leftShoulder;
            LeftElbowSamples = leftElbow;
            LeftWristSamples = leftWrist;
            LeftFingerSamples = leftFinger;
            RightShoulderSamples = rightShoulder;
            RightElbowSamples = rightElbow;
            RightWristSamples = rightWrist;
            RightFingerSamples = rightFinger;
            RightClosureSamples = rightClosure;
        }

        public string Name => "拳掌禮（抱拳／拱手名稱待核）";
        public int ActiveRodCount => 2;
        private double[] LeftShoulderSamples { get; }
        private double[] LeftElbowSamples { get; }
        private double[] LeftWristSamples { get; }
        private double[] LeftFingerSamples { get; }
        private double[] RightShoulderSamples { get; }
        private double[] RightElbowSamples { get; }
        private double[] RightWristSamples { get; }
        private double[] RightFingerSamples { get; }
        private double[] RightClosureSamples { get; }

        public double LeftShoulder(double progress) => Sample(LeftShoulderSamples, progress);
        public double LeftElbow(double progress) => Sample(LeftElbowSamples, progress);
        public double LeftWrist(double progress) => Sample(LeftWristSamples, progress);
        public double LeftFinger(double progress) => Sample(LeftFingerSamples, progress);
        public double RightShoulder(double progress) => Sample(RightShoulderSamples, progress);
        public double RightElbow(double progress) => Sample(RightElbowSamples, progress);
        public double RightWrist(double progress) => Sample(RightWristSamples, progress);
        public double RightFinger(double progress) => Sample(RightFingerSamples, progress);
        public double RightClosure(double progress) => Sample(RightClosureSamples, progress);

        private static double Sample(double[] values, double progress)
        {
            double index = System.Math.Max(0d, System.Math.Min(1d, progress)) * (SampleCount - 1);
            int low = (int)index;
            int high = System.Math.Min(low + 1, SampleCount - 1);
            return values[low] + ((values[high] - values[low]) * (index - low));
        }
    }

    public static class FistPalmSaluteChoreography
    {
        private static readonly FistPalmSalutePhrase Phrase = Build(false);
        private static readonly FistPalmSalutePhrase FormalPhrase = Build(true);

        public static FistPalmSalutePhrase Get() => Phrase;
        public static FistPalmSalutePhrase GetFormal() => FormalPhrase;

        private static FistPalmSalutePhrase Build(bool close)
        {
            double[] leftShoulder = Samples(close ? new[] { 0d, -18d, -36d, -36d, 0d }
                : new[] { 0d, -18d, -36d });
            double[] leftElbow = Samples(close ? new[] { 0d, 88d, 150d, 150d, 0d }
                : new[] { 0d, 88d, 150d });
            double[] leftWrist = Samples(close ? new[] { 0d, 10d, 22d, 22d, 0d }
                : new[] { 0d, 10d, 22d });
            double[] leftFinger = Samples(close ? new[] { 0d, -8d, -12d, -12d, 0d }
                : new[] { 0d, -8d, -12d });
            double[] rightShoulder = Samples(close ? new[] { 0d, 18d, 36d, 36d, 0d }
                : new[] { 0d, 18d, 36d });
            double[] rightElbow = Samples(close ? new[] { 0d, -88d, -150d, -150d, 0d }
                : new[] { 0d, -88d, -150d });
            double[] rightWrist = Samples(close ? new[] { 0d, -10d, -22d, -22d, 0d }
                : new[] { 0d, -10d, -22d });
            double[] rightFinger = Samples(close ? new[] { 0d, 5d, 8d, 8d, 0d }
                : new[] { 0d, 5d, 8d });
            double[] rightClosure = Samples(close ? new[] { 0d, 0.45d, 0.82d, 0.82d, 0d }
                : new[] { 0d, 0.45d, 0.82d });
            return new FistPalmSalutePhrase(leftShoulder, leftElbow, leftWrist, leftFinger,
                rightShoulder, rightElbow, rightWrist, rightFinger, rightClosure);
        }

        private static double[] Samples(double[] keys)
        {
            var values = new double[FistPalmSalutePhrase.SampleCount];
            for (int i = 0; i < values.Length; i++)
            {
                double progress = (double)i / (values.Length - 1);
                double scaled = progress * (keys.Length - 1);
                int low = System.Math.Min((int)scaled, keys.Length - 2);
                double t = scaled - low;
                double smooth = t * t * (3d - (2d * t));
                values[i] = keys[low] + ((keys[low + 1] - keys[low]) * smooth);
            }
            return values;
        }
    }
}
