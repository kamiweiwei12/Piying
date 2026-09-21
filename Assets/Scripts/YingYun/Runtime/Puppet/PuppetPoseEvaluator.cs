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

    /// <summary>只依絕對歌曲時間產生關節姿態；不參與輸入或判定。</summary>
    public sealed class PuppetPoseEvaluator
    {
        private const double PullDurationSec = 0.68d;
        private const double FailureDurationSec = 0.48d;
        private readonly ActionBinding[] _bindings;
        private readonly double[] _pullStartSec = new double[6];
        private readonly double[] _releaseStartSec = new double[6];
        private readonly bool[] _held = new bool[6];
        private double _failureStartSec = double.NegativeInfinity;

        public PuppetPoseEvaluator(ActionBinding[] bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            if (bindings.Length != 6)
            {
                throw new ArgumentException("Exactly six action bindings are required.", nameof(bindings));
            }

            _bindings = new ActionBinding[6];
            var assigned = new bool[6];
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

            for (int i = 0; i < 6; i++)
            {
                _pullStartSec[i] = double.NegativeInfinity;
                _releaseStartSec[i] = double.NegativeInfinity;
            }
        }

        public void Trigger(int lanesMask, double songTimeSec)
        {
            ForEachLane(lanesMask, lane => _pullStartSec[lane] = songTimeSec);
        }

        public void BeginHold(int lanesMask, double songTimeSec)
        {
            ForEachLane(lanesMask, lane =>
            {
                _held[lane] = true;
                _pullStartSec[lane] = songTimeSec;
                _releaseStartSec[lane] = double.NegativeInfinity;
            });
        }

        public void Resolve(int lanesMask, bool success, double songTimeSec)
        {
            bool releasedHold = false;
            ForEachLane(lanesMask, lane =>
            {
                if (_held[lane])
                {
                    _held[lane] = false;
                    _releaseStartSec[lane] = songTimeSec;
                    releasedHold = true;
                }
            });

            if (success && !releasedHold)
            {
                Trigger(lanesMask, songTimeSec);
            }
            else if (!success)
            {
                _failureStartSec = songTimeSec;
            }
        }

        public PuppetPose Evaluate(double songTimeSec)
        {
            double leftHand = WeightForLane(0, songTimeSec);
            double head = WeightForLane(1, songTimeSec);
            double rightHand = WeightForLane(2, songTimeSec);
            double leftFoot = WeightForLane(3, songTimeSec);
            double torso = WeightForLane(4, songTimeSec);
            double rightFoot = WeightForLane(5, songTimeSec);
            double failure = Envelope(songTimeSec - _failureStartSec, FailureDurationSec);
            return new PuppetPose(
                (_bindings[1].PrimaryDegrees * head) + (9d * failure),
                (_bindings[4].PrimaryDegrees * torso) + (13d * failure),
                _bindings[0].PrimaryDegrees * leftHand,
                _bindings[0].SecondaryDegrees * leftHand,
                _bindings[2].PrimaryDegrees * rightHand,
                _bindings[2].SecondaryDegrees * rightHand,
                _bindings[3].PrimaryDegrees * leftFoot,
                _bindings[3].SecondaryDegrees * leftFoot,
                _bindings[5].PrimaryDegrees * rightFoot,
                _bindings[5].SecondaryDegrees * rightFoot,
                PositiveTension(leftHand),
                PositiveTension(head),
                PositiveTension(rightHand),
                PositiveTension(leftFoot),
                PositiveTension(torso),
                PositiveTension(rightFoot),
                failure);
        }

        private double WeightForLane(int lane, double songTimeSec)
        {
            if (_held[lane])
            {
                double heldFor = Math.Max(0d, songTimeSec - _pullStartSec[lane]);
                return 1d + (Math.Sin(heldFor * 11d) * 0.035d);
            }

            if (!double.IsNegativeInfinity(_releaseStartSec[lane]))
            {
                return PullSpring(songTimeSec - _releaseStartSec[lane]);
            }

            return PullSpring(songTimeSec - _pullStartSec[lane]);
        }

        private static double PullSpring(double elapsedSec)
        {
            if (elapsedSec < 0d || elapsedSec >= PullDurationSec)
            {
                return 0d;
            }

            double t = elapsedSec / PullDurationSec;
            return (1d - t) * Math.Cos(t * Math.PI * 1.65d);
        }

        private static double Envelope(double elapsedSec, double durationSec)
        {
            if (elapsedSec < 0d || elapsedSec >= durationSec)
            {
                return 0d;
            }

            double t = elapsedSec / durationSec;
            return 1d - (t * t * (3d - (2d * t)));
        }

        private static double PositiveTension(double weight)
        {
            return Math.Max(0d, Math.Min(1d, Math.Abs(weight)));
        }

        private static void ForEachLane(int lanesMask, Action<int> action)
        {
            for (int lane = 0; lane < 6; lane++)
            {
                if ((lanesMask & (1 << lane)) != 0)
                {
                    action(lane);
                }
            }
        }
    }
}
