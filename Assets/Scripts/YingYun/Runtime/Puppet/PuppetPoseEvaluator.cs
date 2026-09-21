using System;

namespace YingYun.Rhythm.Puppet
{
    public readonly struct PuppetPose
    {
        public PuppetPose(
            double head,
            double torso,
            double leftUpperArm,
            double leftForearm,
            double rightUpperArm,
            double rightForearm,
            double leftThigh,
            double leftShin,
            double rightThigh,
            double rightShin,
            double leftHandTension,
            double headTension,
            double rightHandTension,
            double leftFootTension,
            double torsoTension,
            double rightFootTension,
            double failureWeight)
        {
            Head = head;
            Torso = torso;
            LeftUpperArm = leftUpperArm;
            LeftForearm = leftForearm;
            RightUpperArm = rightUpperArm;
            RightForearm = rightForearm;
            LeftThigh = leftThigh;
            LeftShin = leftShin;
            RightThigh = rightThigh;
            RightShin = rightShin;
            LeftHandTension = leftHandTension;
            HeadTension = headTension;
            RightHandTension = rightHandTension;
            LeftFootTension = leftFootTension;
            TorsoTension = torsoTension;
            RightFootTension = rightFootTension;
            FailureWeight = failureWeight;
        }

        public double Head { get; }
        public double Torso { get; }
        public double LeftUpperArm { get; }
        public double LeftForearm { get; }
        public double RightUpperArm { get; }
        public double RightForearm { get; }
        public double LeftThigh { get; }
        public double LeftShin { get; }
        public double RightThigh { get; }
        public double RightShin { get; }
        public double LeftHandTension { get; }
        public double HeadTension { get; }
        public double RightHandTension { get; }
        public double LeftFootTension { get; }
        public double TorsoTension { get; }
        public double RightFootTension { get; }
        public double FailureWeight { get; }
    }

    /// <summary>
    /// 連續操偶核心：每個關節都是帶慣性的彈簧－阻尼系統。
    /// 輸入事件只代表操偶師「拉繩」，關節由角速度積分逐步加速、慣性滑行、回彈，
    /// 因此相鄰輸入的力量會自然疊加，不會每顆音符結束就回到零姿態。
    /// 只讀歌曲時間與輸入事件；不參與輸入、判定與計分。
    /// </summary>
    public sealed class PuppetPoseEvaluator
    {
        /// <summary>固定積分步長（秒）。與幀率無關：同一組事件在任何取樣率下得到相同姿態。</summary>
        public const double FixedStepSeconds = 1d / 240d;

        private const int LaneCount = 6;
        private const int JointCount = 10;
        private const int DrivesPerLane = 4;

        private const int JointHead = 0;
        private const int JointTorso = 1;
        private const int JointLeftUpperArm = 2;
        private const int JointLeftForearm = 3;
        private const int JointRightUpperArm = 4;
        private const int JointRightForearm = 5;
        private const int JointLeftThigh = 6;
        private const int JointLeftShin = 7;
        private const int JointRightThigh = 8;
        private const int JointRightShin = 9;

        // 各關節自然頻率（rad/s）與阻尼比：近端（肩／髖）最快，遠端（肘／膝）稍慢，
        // 軀幹與頭最重最慢，於是形成約 30–90 ms 的連鎖延遲，而不是整具木偶同時硬轉。
        private static readonly double[] JointFrequency =
        {
            9.0d, 6.0d, 11.0d, 8.5d, 11.0d, 8.5d, 10.0d, 8.0d, 10.0d, 8.0d
        };

        private static readonly double[] JointDamping =
        {
            0.60d, 0.72d, 0.50d, 0.45d, 0.50d, 0.45d, 0.50d, 0.45d, 0.50d, 0.45d
        };

        // 關節角度上限，避免回彈累積出非人體工學的姿勢。
        private static readonly double[] JointLimitDegrees =
        {
            30.0d, 22.0d, 78.0d, 70.0d, 78.0d, 70.0d, 46.0d, 62.0d, 46.0d, 62.0d
        };

        // 手臂與腿的拉力會以較小比例帶動頭與軀幹（重心轉移）；比例小到不會變成「身體自己在轉」。
        private static readonly double[] HeadCoupling = { -0.05d, 0.00d, 0.05d, 0.00d, 0.04d, 0.00d };
        private static readonly double[] TorsoCoupling = { -0.10d, 0.04d, 0.10d, 0.05d, 0.00d, -0.05d };

        private const double DriveAttackSeconds = 0.06d;
        private const double DriveReleaseSeconds = 0.45d;
        private const double TensionReleaseSeconds = 0.30d;

        // 峰值對齊 ActionBinding 宣告的角度（彈簧回彈會略微過衝，因此驅動力先縮小）。
        private const double DriveAmplitudeScale = 0.88d;

        // 長按期間要維持在「宣告角度」而不是過渡幅度：手持續舉起、繩索持續拉緊。
        private const double HoldSustainScale = 1d / DriveAmplitudeScale;

        // 按下瞬間繩索先頓拉一下：這是「拉扯感」的來源，之後才由彈簧與慣性接手。
        private const double DriveKickRate = 2.0d;

        // 連續按同一鍵時在「抬手／伸手／收肘」之間變化，不要每次都一模一樣。
        private static readonly double[] VariationAmplitude = { 1.00d, 0.86d, 1.12d };
        private static readonly double[] VariationTaut = { 0.20d, 0.14d, 0.26d };
        private static readonly double[] VariationSecondary = { 1.00d, 0.78d, 1.22d };
        private const double VariationWindowSeconds = 1.20d;

        private const double FailureAttackSeconds = 0.08d;
        private const double FailureTautSeconds = 0.12d;
        private const double FailureReleaseSeconds = 0.60d;
        private const double FailureHeadDegrees = 9.0d;
        private const double FailureTorsoDegrees = 13.0d;

        private readonly ActionBinding[] _bindings = new ActionBinding[LaneCount];
        private readonly double[] _angle = new double[JointCount];
        private readonly double[] _velocity = new double[JointCount];
        private readonly double[] _target = new double[JointCount];
        private readonly double[] _primaryWeight = new double[LaneCount];
        private readonly double[] _secondaryWeight = new double[LaneCount];
        private readonly double[] _tension = new double[LaneCount];
        private readonly double[] _driveStart = new double[LaneCount * DrivesPerLane];
        private readonly double[] _driveAmplitude = new double[LaneCount * DrivesPerLane];
        private readonly double[] _driveTaut = new double[LaneCount * DrivesPerLane];
        private readonly double[] _driveSecondary = new double[LaneCount * DrivesPerLane];
        private readonly double[] _driveKickAmplitude = new double[LaneCount * DrivesPerLane];
        private readonly bool[] _driveKickPending = new bool[LaneCount * DrivesPerLane];
        private readonly int[] _nextDriveSlot = new int[LaneCount];
        private readonly double[] _lastPressSeconds = new double[LaneCount];
        private readonly int[] _variationIndex = new int[LaneCount];
        private readonly double[] _holdStartSeconds = new double[LaneCount];
        private readonly bool[] _holding = new bool[LaneCount];
        private double _failureStartSeconds = double.NegativeInfinity;
        private double _simulationTimeSeconds = double.NegativeInfinity;
        private bool _hasState;

        public PuppetPoseEvaluator(ActionBinding[] bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            if (bindings.Length != LaneCount)
            {
                throw new ArgumentException("Exactly six action bindings are required.", nameof(bindings));
            }

            var assigned = new bool[LaneCount];
            for (int i = 0; i < bindings.Length; i++)
            {
                int lane = bindings[i].Lane;
                if (assigned[lane])
                {
                    throw new ArgumentException("Action binding lanes must be unique.", nameof(bindings));
                }

                assigned[lane] = true;
                _bindings[lane] = bindings[i];
            }

            ClearState();
        }

        /// <summary>真實按鍵的拉繩：力道立刻記錄，關節依慣性慢慢到位。</summary>
        public void Trigger(int lanesMask, double songTimeSec)
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((lanesMask & (1 << lane)) == 0)
                {
                    continue;
                }

                int variation = ResolveVariation(lane, songTimeSec);
                AddDrive(
                    lane,
                    songTimeSec,
                    VariationAmplitude[variation],
                    VariationTaut[variation],
                    VariationSecondary[variation],
                    VariationAmplitude[variation]);
            }
        }

        public void BeginHold(int lanesMask, double songTimeSec)
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((lanesMask & (1 << lane)) == 0)
                {
                    continue;
                }

                _holding[lane] = true;
                _holdStartSeconds[lane] = songTimeSec;
                _lastPressSeconds[lane] = songTimeSec;
                _variationIndex[lane] = 0;

                // 長按的持續力道由平台負責，這裡只留下按下瞬間的頓拉與繩索張力，避免兩者相加而過度彎折。
                AddDrive(lane, songTimeSec, 0d, 0d, VariationSecondary[0], VariationAmplitude[0]);
            }
        }

        public void Resolve(int lanesMask, bool success, double songTimeSec)
        {
            bool releasedHold = ReleaseHold(lanesMask, songTimeSec);

            if (success && !releasedHold)
            {
                Trigger(lanesMask, songTimeSec);
            }
            else if (!success)
            {
                Fail(songTimeSec);
            }
        }

        public bool ReleaseHold(int lanesMask, double songTimeSec)
        {
            bool releasedHold = false;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((lanesMask & (1 << lane)) == 0 || !_holding[lane])
                {
                    continue;
                }

                _holding[lane] = false;
                releasedHold = true;

                // 放開後繩索回鬆：關節帶著剛才的慣性滑行，再由彈簧拉回並產生一次反向回彈。
                AddDrive(lane, songTimeSec, 1d, 0d, VariationSecondary[0], 0d);
            }

            return releasedHold;
        }

        /// <summary>失勢姿態：保留給明確的失敗演出使用；一般 Miss 不會呼叫。</summary>
        public void Fail(double songTimeSec)
        {
            _failureStartSeconds = songTimeSec;
        }

        /// <summary>
        /// 以固定步長積分到指定歌曲時間後輸出姿態。
        /// 同一組事件與同一個歌曲時間一定得到相同結果，取樣次數不影響結果。
        /// </summary>
        public PuppetPose Evaluate(double songTimeSec)
        {
            if (!_hasState)
            {
                Restart(songTimeSec);
            }
            else if (songTimeSec < _simulationTimeSeconds)
            {
                Restart(songTimeSec);
            }

            while (_simulationTimeSeconds + FixedStepSeconds <= songTimeSec)
            {
                _simulationTimeSeconds += FixedStepSeconds;
                Step(_simulationTimeSeconds);
            }

            AccumulateDriveWeights(songTimeSec);

            return new PuppetPose(
                _angle[JointHead],
                _angle[JointTorso],
                _angle[JointLeftUpperArm],
                _angle[JointLeftForearm],
                _angle[JointRightUpperArm],
                _angle[JointRightForearm],
                _angle[JointLeftThigh],
                _angle[JointLeftShin],
                _angle[JointRightThigh],
                _angle[JointRightShin],
                PositiveTension(_tension[0]),
                PositiveTension(_tension[1]),
                PositiveTension(_tension[2]),
                PositiveTension(_tension[3]),
                PositiveTension(_tension[4]),
                PositiveTension(_tension[5]),
                FailureEnvelope(songTimeSec - _failureStartSeconds));
        }

        /// <summary>
        /// 從最早的事件重新模擬到指定時間：姿態只取決於「事件清單 + 時間」，
        /// 因此回溯取樣、重新評估都不會失去先前按鍵累積的姿態。
        /// </summary>
        private void Restart(double songTimeSec)
        {
            for (int joint = 0; joint < JointCount; joint++)
            {
                _angle[joint] = 0d;
                _velocity[joint] = 0d;
            }

            for (int slot = 0; slot < _driveStart.Length; slot++)
            {
                _driveKickPending[slot] = !double.IsNegativeInfinity(_driveStart[slot]);
            }

            _simulationTimeSeconds = Math.Min(songTimeSec, EarliestEventSeconds());
            _hasState = true;
        }

        private double EarliestEventSeconds()
        {
            double earliest = double.PositiveInfinity;
            for (int slot = 0; slot < _driveStart.Length; slot++)
            {
                double start = _driveStart[slot];
                if (!double.IsNegativeInfinity(start) && start < earliest)
                {
                    earliest = start;
                }
            }

            for (int lane = 0; lane < LaneCount; lane++)
            {
                double holdStart = _holdStartSeconds[lane];
                if (!double.IsNegativeInfinity(holdStart) && holdStart < earliest)
                {
                    earliest = holdStart;
                }
            }

            if (!double.IsNegativeInfinity(_failureStartSeconds) && _failureStartSeconds < earliest)
            {
                earliest = _failureStartSeconds;
            }

            return earliest;
        }

        private void ClearState()
        {
            for (int joint = 0; joint < JointCount; joint++)
            {
                _angle[joint] = 0d;
                _velocity[joint] = 0d;
            }

            for (int slot = 0; slot < _driveStart.Length; slot++)
            {
                _driveStart[slot] = double.NegativeInfinity;
                _driveAmplitude[slot] = 0d;
                _driveTaut[slot] = 0d;
                _driveSecondary[slot] = 1d;
                _driveKickAmplitude[slot] = 0d;
                _driveKickPending[slot] = false;
            }

            for (int lane = 0; lane < LaneCount; lane++)
            {
                _nextDriveSlot[lane] = 0;
                _lastPressSeconds[lane] = double.NegativeInfinity;
                _variationIndex[lane] = 0;
                _holding[lane] = false;
                _holdStartSeconds[lane] = double.NegativeInfinity;
                _primaryWeight[lane] = 0d;
                _secondaryWeight[lane] = 0d;
                _tension[lane] = 0d;
            }

            _failureStartSeconds = double.NegativeInfinity;
        }

        private int ResolveVariation(int lane, double songTimeSec)
        {
            if (songTimeSec - _lastPressSeconds[lane] <= VariationWindowSeconds)
            {
                _variationIndex[lane] = (_variationIndex[lane] + 1) % VariationAmplitude.Length;
            }
            else
            {
                _variationIndex[lane] = 0;
            }

            _lastPressSeconds[lane] = songTimeSec;
            return _variationIndex[lane];
        }

        private void AddDrive(
            int lane,
            double songTimeSec,
            double amplitude,
            double tautSeconds,
            double secondary,
            double kickAmplitude)
        {
            int slot = (lane * DrivesPerLane) + _nextDriveSlot[lane];
            _nextDriveSlot[lane] = (_nextDriveSlot[lane] + 1) % DrivesPerLane;
            _driveStart[slot] = songTimeSec;
            _driveAmplitude[slot] = amplitude;
            _driveTaut[slot] = tautSeconds;
            _driveSecondary[slot] = secondary;
            _driveKickAmplitude[slot] = kickAmplitude;
            _driveKickPending[slot] = true;
        }

        /// <summary>事件時間一到才施加頓拉，因此事件可依真實時間抵達（或提前排定）而不改變結果。</summary>
        private void ApplyPendingKicks(double sampleTimeSec)
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                int firstSlot = lane * DrivesPerLane;
                for (int offset = 0; offset < DrivesPerLane; offset++)
                {
                    int slot = firstSlot + offset;
                    if (!_driveKickPending[slot] || sampleTimeSec < _driveStart[slot])
                    {
                        continue;
                    }

                    _driveKickPending[slot] = false;
                    ApplyKick(lane, _driveKickAmplitude[slot], _driveSecondary[slot]);
                }
            }
        }

        private void ApplyKick(int lane, double amplitude, double secondary)
        {
            ActionBinding binding = _bindings[lane];
            double kick = DriveKickRate * DriveAmplitudeScale * amplitude;
            _velocity[PrimaryJointOf(lane)] += kick * binding.PrimaryDegrees;

            int secondaryJoint = SecondaryJointOf(lane);
            if (secondaryJoint >= 0)
            {
                _velocity[secondaryJoint] += kick * binding.SecondaryDegrees * secondary;
            }

            _velocity[JointHead] += kick * binding.PrimaryDegrees * HeadCoupling[lane];
            _velocity[JointTorso] += kick * binding.PrimaryDegrees * TorsoCoupling[lane];
        }

        private void Step(double sampleTimeSec)
        {
            ApplyPendingKicks(sampleTimeSec);
            AccumulateDriveWeights(sampleTimeSec);

            _target[JointHead] = _bindings[1].PrimaryDegrees * _primaryWeight[1];
            _target[JointTorso] = _bindings[4].PrimaryDegrees * _primaryWeight[4];
            _target[JointLeftUpperArm] = _bindings[0].PrimaryDegrees * _primaryWeight[0];
            _target[JointLeftForearm] = _bindings[0].SecondaryDegrees * _secondaryWeight[0];
            _target[JointRightUpperArm] = _bindings[2].PrimaryDegrees * _primaryWeight[2];
            _target[JointRightForearm] = _bindings[2].SecondaryDegrees * _secondaryWeight[2];
            _target[JointLeftThigh] = _bindings[3].PrimaryDegrees * _primaryWeight[3];
            _target[JointLeftShin] = _bindings[3].SecondaryDegrees * _secondaryWeight[3];
            _target[JointRightThigh] = _bindings[5].PrimaryDegrees * _primaryWeight[5];
            _target[JointRightShin] = _bindings[5].SecondaryDegrees * _secondaryWeight[5];

            for (int lane = 0; lane < LaneCount; lane++)
            {
                double pull = _bindings[lane].PrimaryDegrees * _primaryWeight[lane];
                _target[JointHead] += pull * HeadCoupling[lane];
                _target[JointTorso] += pull * TorsoCoupling[lane];
            }

            double failure = FailureEnvelope(sampleTimeSec - _failureStartSeconds);
            _target[JointHead] += FailureHeadDegrees * failure;
            _target[JointTorso] += FailureTorsoDegrees * failure;

            for (int joint = 0; joint < JointCount; joint++)
            {
                double target = _target[joint] * DriveAmplitudeScale;
                double frequency = JointFrequency[joint];
                double acceleration =
                    (frequency * frequency * (target - _angle[joint])) -
                    (2d * JointDamping[joint] * frequency * _velocity[joint]);

                _velocity[joint] += acceleration * FixedStepSeconds;
                _angle[joint] += _velocity[joint] * FixedStepSeconds;

                double limit = JointLimitDegrees[joint];
                if (_angle[joint] > limit)
                {
                    _angle[joint] = limit;
                }
                else if (_angle[joint] < -limit)
                {
                    _angle[joint] = -limit;
                }
            }
        }

        private void AccumulateDriveWeights(double songTimeSec)
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                double primary = 0d;
                double secondary = 0d;
                double tension = 0d;

                int firstSlot = lane * DrivesPerLane;
                for (int offset = 0; offset < DrivesPerLane; offset++)
                {
                    int slot = firstSlot + offset;
                    if (double.IsNegativeInfinity(_driveStart[slot]))
                    {
                        continue;
                    }

                    double elapsed = songTimeSec - _driveStart[slot];
                    if (elapsed < 0d)
                    {
                        continue;
                    }

                    double taut = _driveTaut[slot];
                    double envelope = DriveEnvelope(elapsed, taut);
                    primary += envelope * _driveAmplitude[slot];
                    secondary += envelope * _driveAmplitude[slot] * _driveSecondary[slot];
                    tension += TensionEnvelope(elapsed, taut);
                }

                if (_holding[lane])
                {
                    double held = HoldWeight(songTimeSec - _holdStartSeconds[lane]) * HoldSustainScale;
                    primary += held;
                    secondary += held * VariationSecondary[0];
                    tension += held;
                }

                _primaryWeight[lane] = primary;
                _secondaryWeight[lane] = secondary;
                _tension[lane] = tension;
            }
        }

        private static double DriveEnvelope(double elapsedSec, double tautSeconds)
        {
            if (elapsedSec < 0d)
            {
                return 0d;
            }

            if (elapsedSec < DriveAttackSeconds)
            {
                double u = elapsedSec / DriveAttackSeconds;
                return u * u * (3d - (2d * u));
            }

            if (elapsedSec < DriveAttackSeconds + tautSeconds)
            {
                return 1d;
            }

            double released = elapsedSec - DriveAttackSeconds - tautSeconds;
            if (released >= DriveReleaseSeconds)
            {
                return 0d;
            }

            double release = released / DriveReleaseSeconds;
            return 1d - (release * release * (3d - (2d * release)));
        }

        private static double TensionEnvelope(double elapsedSec, double tautSeconds)
        {
            if (elapsedSec < 0d)
            {
                return 0d;
            }

            if (elapsedSec < tautSeconds)
            {
                return 1d;
            }

            double released = elapsedSec - tautSeconds;
            if (released >= TensionReleaseSeconds)
            {
                return 0d;
            }

            double release = released / TensionReleaseSeconds;
            return 1d - (release * release * (3d - (2d * release)));
        }

        private static double FailureEnvelope(double elapsedSec)
        {
            if (elapsedSec < 0d)
            {
                return 0d;
            }

            if (elapsedSec < FailureAttackSeconds)
            {
                double u = elapsedSec / FailureAttackSeconds;
                return u * u * (3d - (2d * u));
            }

            if (elapsedSec < FailureAttackSeconds + FailureTautSeconds)
            {
                return 1d;
            }

            double release = (elapsedSec - FailureAttackSeconds - FailureTautSeconds) / FailureReleaseSeconds;
            if (release >= 1d)
            {
                return 0d;
            }

            return 1d - (release * release * (3d - (2d * release)));
        }

        private static double HoldWeight(double heldForSec)
        {
            if (heldForSec < 0d)
            {
                return 0d;
            }

            return 1d + (Math.Sin(heldForSec * 11d) * 0.035d);
        }

        private static double PositiveTension(double tension)
        {
            return Math.Max(0d, Math.Min(1d, tension));
        }

        private static int PrimaryJointOf(int lane)
        {
            switch (lane)
            {
                case 0:
                    return JointLeftUpperArm;
                case 1:
                    return JointHead;
                case 2:
                    return JointRightUpperArm;
                case 3:
                    return JointLeftThigh;
                case 4:
                    return JointTorso;
                default:
                    return JointRightThigh;
            }
        }

        private static int SecondaryJointOf(int lane)
        {
            switch (lane)
            {
                case 0:
                    return JointLeftForearm;
                case 2:
                    return JointRightForearm;
                case 3:
                    return JointLeftShin;
                case 5:
                    return JointRightShin;
                default:
                    return -1;
            }
        }
    }
}
