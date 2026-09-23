using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Puppet
{
    public enum DanceJoint
    {
        Head, Torso, LeftShoulder, LeftElbow, RightShoulder, RightElbow,
        LeftHip, LeftKnee, RightHip, RightKnee
    }

    public enum DanceAction
    {
        SingleMountainArm, CloudHand, WindFlag, Turn, RaiseSleeve,
        DoubleMountainArm, ReverseCloudHand, FinalPose, PressPalm, SupportPalm,
        ThreadPalm, TurnWrist
    }

    public enum SwingFoot
    {
        None, Left, Right
    }

    public enum DancePerformanceKind
    {
        Waiting, Pending, Performing, Closing, Holding, Interrupted
    }

    /// <summary>只描述實際操演狀態；Cue 為待開始或漏擊的舞句，不冒充已執行動作。</summary>
    public readonly struct DancePerformanceStatus
    {
        public DancePerformanceStatus(DancePerformanceKind kind, DancePhrase performed, DancePhrase cue = null)
        {
            Kind = kind;
            Performed = performed;
            Cue = cue;
        }

        public DancePerformanceKind Kind { get; }
        public DancePhrase Performed { get; }
        public DancePhrase Cue { get; }
    }

    /// <summary>一段已在載入階段取樣完成的舞句；運行時不再生成動作軌跡。</summary>
    public sealed class DancePhrase
    {
        public const int SampleCount = 129;

        internal DancePhrase(int startBeat, int durationBeats, int anchorNoteId,
            DanceAction action, string name, DanceJoint firstJoint, DanceJoint secondJoint,
            double startSeconds, double durationSeconds, double[] first, double[] second,
            double[] recoveryBlend, double[] linkedBlend, double[] turnWidth, SwingFoot swingFoot,
            bool hasClosing, HandGesturePhrase handGesture,
            double[] footReach, double[] footLift, double[] weightShift)
        {
            StartBeat = startBeat;
            DurationBeats = durationBeats;
            AnchorNoteId = anchorNoteId;
            Action = action;
            Name = name;
            FirstJoint = firstJoint;
            SecondJoint = secondJoint;
            StartSeconds = startSeconds;
            DurationSeconds = durationSeconds;
            FirstSamples = first;
            SecondSamples = second;
            RecoveryBlendSamples = recoveryBlend;
            LinkedBlendSamples = linkedBlend;
            TurnWidthSamples = turnWidth;
            StepFoot = swingFoot;
            HasClosing = hasClosing;
            HandGesture = handGesture;
            FootReachSamples = footReach;
            FootLiftSamples = footLift;
            WeightShiftSamples = weightShift;
        }

        public int StartBeat { get; }
        public int DurationBeats { get; }
        public int AnchorNoteId { get; }
        public DanceAction Action { get; }
        public string Name { get; }
        public DanceJoint FirstJoint { get; }
        public DanceJoint SecondJoint { get; }
        public double StartSeconds { get; }
        public double DurationSeconds { get; }
        public int ActiveJointCount => FirstJoint == SecondJoint ? 1 : 2;
        public int ActiveRodCount
        {
            get
            {
                int first = RodFor(FirstJoint);
                int second = RodFor(SecondJoint);
                int foot = StepFoot == SwingFoot.Left ? 3 : StepFoot == SwingFoot.Right ? 5 : -1;
                return 1 + (second == first ? 0 : 1) +
                    (foot < 0 || foot == first || foot == second ? 0 : 1);
            }
        }
        public SwingFoot StepFoot { get; }
        public bool HasClosing { get; }
        public HandGesturePhrase HandGesture { get; }
        public string JointDisplay => $"{JointName(FirstJoint)}、{JointName(SecondJoint)}";
        public string Display => $"【{StartBeat}，{Name}，{JointName(FirstJoint)}、{JointName(SecondJoint)}，{DurationBeats}】";

        internal double[] FirstSamples { get; }
        internal double[] SecondSamples { get; }
        internal double[] RecoveryBlendSamples { get; }
        internal double[] LinkedBlendSamples { get; }
        internal double[] TurnWidthSamples { get; }
        internal double[] FootReachSamples { get; }
        internal double[] FootLiftSamples { get; }
        internal double[] WeightShiftSamples { get; }

        private static int RodFor(DanceJoint joint)
        {
            switch (joint)
            {
                case DanceJoint.Head: return 1;
                case DanceJoint.Torso: return 4;
                case DanceJoint.LeftShoulder:
                case DanceJoint.LeftElbow: return 0;
                case DanceJoint.RightShoulder:
                case DanceJoint.RightElbow: return 2;
                case DanceJoint.LeftHip:
                case DanceJoint.LeftKnee: return 3;
                default: return 5;
            }
        }

        private static string JointName(DanceJoint joint)
        {
            switch (joint)
            {
                case DanceJoint.Head: return "頭部";
                case DanceJoint.Torso: return "軀幹";
                case DanceJoint.LeftShoulder: return "左肩";
                case DanceJoint.LeftElbow: return "左肘";
                case DanceJoint.RightShoulder: return "右肩";
                case DanceJoint.RightElbow: return "右肘";
                case DanceJoint.LeftHip: return "左胯";
                case DanceJoint.LeftKnee: return "左膝";
                case DanceJoint.RightHip: return "右胯";
                default: return "右膝";
            }
        }

        internal static double Sample(double[] values, double progress)
        {
            double index = Math.Max(0d, Math.Min(1d, progress)) * (SampleCount - 1);
            int low = (int)index;
            int high = Math.Min(low + 1, SampleCount - 1);
            return values[low] + ((values[high] - values[low]) * (index - low));
        }
    }

    public static class DanceChoreography
    {
        private const int BeatsPerPhrase = 8;

        public static DancePhrase[] Create(NoteData[] notes, double bpm, double durationSeconds)
        {
            if (notes == null) throw new ArgumentNullException(nameof(notes));
            if (bpm <= 0d) throw new ArgumentOutOfRangeException(nameof(bpm));
            if (durationSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            double beatSeconds = 60d / bpm;
            int phraseCount = (int)Math.Floor(durationSeconds / (BeatsPerPhrase * beatSeconds));
            var result = new List<DancePhrase>(phraseCount);
            for (int index = 0; index < phraseCount; index++)
            {
                int beat = index * BeatsPerPhrase;
                double start = beat * beatSeconds;
                int anchor = FindAnchor(notes, start);
                if (anchor < 0) continue;

                DanceAction action = (DanceAction)(index % 12);
                result.Add(Build(beat, anchor, action, beatSeconds));
            }

            return result.ToArray();
        }

        private static int FindAnchor(NoteData[] notes, double startSeconds)
        {
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i].TimeSec > startSeconds + 0.000001d) break;
                if (Math.Abs(notes[i].TimeSec - startSeconds) <= 0.000001d &&
                    notes[i].Kind == NoteKind.Tap)
                {
                    return notes[i].Id;
                }
            }

            return -1;
        }

        private static DancePhrase Build(int beat, int anchor, DanceAction action, double beatSeconds)
        {
            DanceJoint first;
            DanceJoint second;
            string name;
            double[] firstKeys;
            double[] secondKeys;
            HandGesturePhrase handGesture = null;
            switch (action)
            {
                case DanceAction.SingleMountainArm:
                    name = "單山膀"; first = DanceJoint.LeftShoulder; second = DanceJoint.LeftElbow;
                    firstKeys = new[] { 0d, -55d, -66d, -66d, -60d };
                    secondKeys = new[] { 0d, -12d, -22d, -22d, -18d }; break;
                case DanceAction.CloudHand:
                    name = "雲手"; first = DanceJoint.LeftShoulder; second = DanceJoint.LeftElbow;
                    firstKeys = new[] { -60d, -22d, 35d, 12d, -24d };
                    // 接順風旗前收肘，肩仍保留入勢方向。
                    secondKeys = new[] { -18d, -48d, -25d, -10d, 0d }; break;
                case DanceAction.WindFlag:
                    name = "順風旗"; first = DanceJoint.LeftShoulder; second = DanceJoint.RightShoulder;
                    firstKeys = new[] { -24d, -45d, -62d, -62d, -60d };
                    secondKeys = new[] { 0d, -65d, -145d, -145d, -138d }; break;
                case DanceAction.Turn:
                    name = "轉身"; first = DanceJoint.Torso; second = DanceJoint.LeftShoulder;
                    firstKeys = new[] { 0d, 4d, 0d, -4d, 0d };
                    secondKeys = new[] { -60d, -48d, -40d, -48d, -60d }; break;
                case DanceAction.RaiseSleeve:
                    name = "揚袖"; first = DanceJoint.RightShoulder; second = DanceJoint.RightElbow;
                    firstKeys = new[] { -138d, -75d, -135d, -150d, -115d };
                    secondKeys = new[] { 0d, 18d, 38d, 22d, 12d }; break;
                case DanceAction.DoubleMountainArm:
                    name = "雙山膀"; first = DanceJoint.LeftShoulder; second = DanceJoint.RightShoulder;
                    // 先定住雙山膀，末兩拍左臂收勢；右臂留給反雲手。
                    firstKeys = new[] { -60d, -68d, -76d, -76d, 0d };
                    secondKeys = new[] { -115d, 20d, 76d, 76d, 68d }; break;
                case DanceAction.ReverseCloudHand:
                    name = "反雲手"; first = DanceJoint.RightShoulder; second = DanceJoint.RightElbow;
                    // 亮相前把右臂收回，不能讓前一招的手臂懸空殘留。
                    firstKeys = new[] { 68d, 25d, -35d, -12d, 0d };
                    secondKeys = new[] { 12d, 48d, 25d, 10d, 0d }; break;
                case DanceAction.FinalPose:
                    name = "亮相"; first = DanceJoint.Torso; second = DanceJoint.Head;
                    // 亮相定住至第六拍，再收身回到下一循環的起勢。
                    firstKeys = new[] { 0d, 6d, 8d, 8d, 0d };
                    secondKeys = new[] { 0d, -4d, -8d, -8d, 0d }; break;
                case DanceAction.PressPalm:
                    handGesture = HandGestureChoreography.GetFormal(HandGesture.PressPalm);
                    name = "按掌"; first = DanceJoint.LeftShoulder; second = DanceJoint.LeftElbow;
                    firstKeys = null; secondKeys = null; break;
                case DanceAction.SupportPalm:
                    handGesture = HandGestureChoreography.GetFormal(HandGesture.SupportPalm);
                    name = "托掌"; first = DanceJoint.LeftShoulder; second = DanceJoint.LeftElbow;
                    firstKeys = null; secondKeys = null; break;
                case DanceAction.ThreadPalm:
                    handGesture = HandGestureChoreography.GetFormal(HandGesture.ThreadPalm);
                    name = "穿掌"; first = DanceJoint.LeftShoulder; second = DanceJoint.LeftElbow;
                    firstKeys = null; secondKeys = null; break;
                case DanceAction.TurnWrist:
                    handGesture = HandGestureChoreography.GetFormal(HandGesture.TurnWrist);
                    name = "翻腕"; first = DanceJoint.LeftShoulder; second = DanceJoint.LeftElbow;
                    firstKeys = null; secondKeys = null; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            var firstSamples = new double[DancePhrase.SampleCount];
            var secondSamples = new double[DancePhrase.SampleCount];
            var blendSamples = new double[DancePhrase.SampleCount];
            var linkedBlendSamples = new double[DancePhrase.SampleCount];
            var widthSamples = new double[DancePhrase.SampleCount];
            var footReachSamples = new double[DancePhrase.SampleCount];
            var footLiftSamples = new double[DancePhrase.SampleCount];
            var weightShiftSamples = new double[DancePhrase.SampleCount];
            // 單側手簽舞句可搭配一支腳部輔助簽；雙手／身頭舞句不額外啟動腳簽。
            SwingFoot stepFoot = action == DanceAction.SingleMountainArm || action == DanceAction.CloudHand
                ? SwingFoot.Right
                : action == DanceAction.RaiseSleeve || action == DanceAction.ReverseCloudHand
                    ? SwingFoot.Left : SwingFoot.None;
            for (int i = 0; i < DancePhrase.SampleCount; i++)
            {
                double progress = (double)i / (DancePhrase.SampleCount - 1);
                firstSamples[i] = handGesture == null
                    ? KeyValue(firstKeys, progress) : handGesture.Shoulder(progress);
                secondSamples[i] = handGesture == null
                    ? KeyValue(secondKeys, progress) : handGesture.Elbow(progress);
                blendSamples[i] = Smooth(Math.Min(1d, progress * 4d));
                linkedBlendSamples[i] = 1d;
                widthSamples[i] = action == DanceAction.Turn
                    ? (progress < 0.5d
                        ? 1d - (0.92d * Smooth(progress * 2d))
                        : 0.08d - (1.08d * Smooth((progress - 0.5d) * 2d)))
                    : 1d;
                if (stepFoot != SwingFoot.None)
                {
                    // 工程驗證用的抬落腳弧線，非特定戲曲步法；端點速度為零。
                    double arc = Math.Sin(Math.PI * Smooth(progress));
                    footReachSamples[i] = 0.32d * arc;
                    footLiftSamples[i] = 0.34d * arc;
                    weightShiftSamples[i] = 0.14d * arc;
                }
            }

            return new DancePhrase(beat, BeatsPerPhrase, anchor, action, name, first, second,
                beat * beatSeconds, BeatsPerPhrase * beatSeconds,
                firstSamples, secondSamples, blendSamples, linkedBlendSamples, widthSamples,
                stepFoot,
                action == DanceAction.CloudHand || action == DanceAction.DoubleMountainArm ||
                action == DanceAction.ReverseCloudHand || action == DanceAction.FinalPose ||
                action == DanceAction.SupportPalm || action == DanceAction.TurnWrist,
                handGesture,
                footReachSamples, footLiftSamples, weightShiftSamples);
        }

        private static double KeyValue(double[] keys, double progress)
        {
            double scaled = progress * (keys.Length - 1);
            int low = Math.Min((int)scaled, keys.Length - 2);
            double t = scaled - low;
            double startSlope = KeySlope(keys, low);
            double endSlope = KeySlope(keys, low + 1);
            return (2d * t * t * t - 3d * t * t + 1d) * keys[low] +
                (t * t * t - 2d * t * t + t) * startSlope +
                (-2d * t * t * t + 3d * t * t) * keys[low + 1] +
                (t * t * t - t * t) * endSlope;
        }

        private static double KeySlope(double[] keys, int index)
        {
            if (index == 0 || index == keys.Length - 1) return 0d;
            double before = keys[index] - keys[index - 1];
            double after = keys[index + 1] - keys[index];
            // 只讓同向的段落穿過中間拍；反向與定勢仍要明確停住。
            return before * after > 0d ? (before + after) * 0.5d : 0d;
        }

        private static double Smooth(double t) => t * t * (3d - (2d * t));
    }

    /// <summary>判定只開啟預編舞句；每幀僅取樣已有軌跡，Miss 凍結當前姿態。</summary>
    public sealed class DancePlayback
    {
        private readonly DancePhrase[] _phrases;
        private readonly Dictionary<int, DancePhrase> _byAnchor = new Dictionary<int, DancePhrase>();
        private readonly double[] _angles = new double[10];
        private DancePhrase _active;
        private DancePhrase _pending;
        private DancePhrase _lastCompleted;
        private DancePhrase _lastPerformed;
        private double[] _activeBlendSamples;
        private bool _closingNotified;
        private double _activeStart;
        private double _firstStart;
        private double _secondStart;
        private double _facing = 1d;
        private double _turnFacing = 1d;
        private double _leftFootStartX = -0.34d;
        private double _leftFootStartY = -2.08d;
        private double _rightFootStartX = 0.34d;
        private double _rightFootStartY = -2.08d;
        private double _pelvisStartX;
        private double _wristStart;
        private double _fingerStart;

        public DancePlayback(DancePhrase[] phrases)
        {
            _phrases = phrases ?? throw new ArgumentNullException(nameof(phrases));
            foreach (DancePhrase phrase in phrases)
            {
                if (phrase.DurationBeats < 4 || phrase.ActiveJointCount > 2 ||
                    phrase.ActiveRodCount > 2 ||
                    phrase.AnchorNoteId <= 0 || _byAnchor.ContainsKey(phrase.AnchorNoteId))
                    throw new ArgumentException("Invalid dance phrase.", nameof(phrases));
                _byAnchor.Add(phrase.AnchorNoteId, phrase);
            }
        }

        public int PhraseCount => _phrases.Length;
        public DanceAction? ActiveAction => _active?.Action;
        public event Action<DancePerformanceStatus> StatusChanged;
        public double FacingScale { get; private set; } = 1d;
        public double LeftFootX { get; private set; } = -0.34d;
        public double LeftFootY { get; private set; } = -2.08d;
        public double RightFootX { get; private set; } = 0.34d;
        public double RightFootY { get; private set; } = -2.08d;
        public double PelvisX { get; private set; }
        public bool HasExplicitLeftHandPose { get; private set; }
        public double LeftWristAngle { get; private set; }
        public double LeftFingerAngle { get; private set; }
        public double Angle(DanceJoint joint) => _angles[(int)joint];

        public void OnJudged(JudgmentResult result, double songTimeSeconds)
        {
            if (result.EventKind != JudgmentEventKind.NoteJudged ||
                !_byAnchor.TryGetValue(result.NoteId, out DancePhrase phrase)) return;

            Evaluate(songTimeSeconds);
            if (result.Grade == JudgmentGrade.Miss)
            {
                _active = null;
                _pending = null;
                _lastCompleted = null;
                StatusChanged?.Invoke(new DancePerformanceStatus(
                    DancePerformanceKind.Interrupted, _lastPerformed, phrase));
                return;
            }

            if (songTimeSeconds < phrase.StartSeconds)
            {
                _pending = phrase;
                StatusChanged?.Invoke(new DancePerformanceStatus(
                    DancePerformanceKind.Pending, _lastPerformed, phrase));
                return;
            }

            Start(phrase, songTimeSeconds);
        }

        public void Evaluate(double songTimeSeconds)
        {
            if (_pending != null && songTimeSeconds >= _pending.StartSeconds)
            {
                DancePhrase ready = _pending;
                _pending = null;
                EvaluateActive(ready.StartSeconds);
                Start(ready, ready.StartSeconds);
            }

            EvaluateActive(songTimeSeconds);
        }

        private void EvaluateActive(double songTimeSeconds)
        {
            if (_active == null) return;
            double progress = Math.Max(0d, Math.Min(1d,
                (songTimeSeconds - _activeStart) / _active.DurationSeconds));
            double blend = DancePhrase.Sample(_activeBlendSamples, progress);
            _angles[(int)_active.FirstJoint] = _firstStart * (1d - blend) +
                DancePhrase.Sample(_active.FirstSamples, progress) * blend;
            _angles[(int)_active.SecondJoint] = _secondStart * (1d - blend) +
                DancePhrase.Sample(_active.SecondSamples, progress) * blend;
            double reach = DancePhrase.Sample(_active.FootReachSamples, progress);
            double lift = DancePhrase.Sample(_active.FootLiftSamples, progress);
            double weight = DancePhrase.Sample(_active.WeightShiftSamples, progress);
            LeftFootX = (_leftFootStartX * (1d - blend)) +
                ((-0.34d - (_active.StepFoot == SwingFoot.Left ? reach : 0d)) * blend);
            LeftFootY = (_leftFootStartY * (1d - blend)) +
                ((-2.08d + (_active.StepFoot == SwingFoot.Left ? lift : 0d)) * blend);
            RightFootX = (_rightFootStartX * (1d - blend)) +
                ((0.34d + (_active.StepFoot == SwingFoot.Right ? reach : 0d)) * blend);
            RightFootY = (_rightFootStartY * (1d - blend)) +
                ((-2.08d + (_active.StepFoot == SwingFoot.Right ? lift : 0d)) * blend);
            double targetPelvis = _active.StepFoot == SwingFoot.Right ? -weight :
                _active.StepFoot == SwingFoot.Left ? weight : 0d;
            PelvisX = (_pelvisStartX * (1d - blend)) + (targetPelvis * blend);
            FacingScale = _turnFacing * DancePhrase.Sample(_active.TurnWidthSamples, progress);
            if (_active.HandGesture != null)
            {
                LeftWristAngle = _wristStart * (1d - blend) +
                    _active.HandGesture.Wrist(progress) * blend;
                LeftFingerAngle = _fingerStart * (1d - blend) +
                    _active.HandGesture.Finger(progress) * blend;
            }
            if (!_closingNotified && _active.HasClosing && progress >= 0.75d && progress < 1d)
            {
                _closingNotified = true;
                StatusChanged?.Invoke(new DancePerformanceStatus(DancePerformanceKind.Closing, _active));
            }
            if (progress >= 1d)
            {
                _facing = FacingScale;
                _lastCompleted = _active;
                StatusChanged?.Invoke(new DancePerformanceStatus(DancePerformanceKind.Holding, _active));
                _active = null;
            }
        }

        private void Start(DancePhrase phrase, double songTimeSeconds)
        {
            // 相鄰且成功完成的舞句用預編直連；漏擊／跳句則選預編恢復曲線。
            bool linked = _lastCompleted != null &&
                _lastCompleted.StartBeat + _lastCompleted.DurationBeats == phrase.StartBeat;
            _active = phrase;
            _lastPerformed = phrase;
            _closingNotified = false;
            _activeBlendSamples = linked ? phrase.LinkedBlendSamples : phrase.RecoveryBlendSamples;
            _lastCompleted = null;
            // 舞句取樣固定對齊譜面起拍。若以實際命中時間平移整段，晚擊的轉身會在下一句
            // 到來時尚未提交新朝向，造成整個影人瞬間翻回舊方向。
            _activeStart = phrase.StartSeconds;
            _firstStart = _angles[(int)phrase.FirstJoint];
            _secondStart = _angles[(int)phrase.SecondJoint];
            _turnFacing = _facing;
            _leftFootStartX = LeftFootX;
            _leftFootStartY = LeftFootY;
            _rightFootStartX = RightFootX;
            _rightFootStartY = RightFootY;
            _pelvisStartX = PelvisX;
            _wristStart = LeftWristAngle;
            _fingerStart = LeftFingerAngle;
            HasExplicitLeftHandPose = phrase.HandGesture != null;
            StatusChanged?.Invoke(new DancePerformanceStatus(DancePerformanceKind.Performing, phrase));
        }
    }
}
