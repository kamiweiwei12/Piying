using System;

namespace YingYun.Rhythm.Puppet.V2
{
    /// <summary>P6 十八式完整序列；累计舞台位置、持久朝向，并在每个边界做一拍完整 Pose 过门。</summary>
    public static class PuppetV2SequenceChoreography
    {
        public const int TotalBeats = 168;
        public const double TransitionBeats = 1d;

        private readonly struct Segment
        {
            public readonly string Name;
            public readonly int Beats;
            public readonly Func<double, PuppetV2Pose> Evaluate;

            public Segment(string name, int beats, Func<double, PuppetV2Pose> evaluate)
            {
                Name = name;
                Beats = beats;
                Evaluate = evaluate;
            }
        }

        private static readonly Segment[] Segments =
        {
            new(SlowEntranceChoreography.Name, SlowEntranceChoreography.Beats, SlowEntranceChoreography.Evaluate),
            new(StraightenCrownChoreography.Name, StraightenCrownChoreography.Beats, StraightenCrownChoreography.Evaluate),
            new(BowingSaluteChoreography.Name, BowingSaluteChoreography.Beats, BowingSaluteChoreography.Evaluate),
            new(CloudHandsCircleChoreography.Name, CloudHandsCircleChoreography.Beats, CloudHandsCircleChoreography.Evaluate),
            new(MoonOfferingGazeChoreography.Name, MoonOfferingGazeChoreography.Beats, MoonOfferingGazeChoreography.Evaluate),
            new(PointingFarGazeChoreography.Name, PointingFarGazeChoreography.Beats, PointingFarGazeChoreography.Evaluate),
            new(ShadingBrowSearchChoreography.Name, ShadingBrowSearchChoreography.Beats, ShadingBrowSearchChoreography.Evaluate),
            new(VeiledFaceLamentChoreography.Name, VeiledFaceLamentChoreography.Beats, VeiledFaceLamentChoreography.Evaluate),
            new(OpenArmsUpwardLaughChoreography.Name, OpenArmsUpwardLaughChoreography.Beats, OpenArmsUpwardLaughChoreography.Evaluate),
            new(DoubleMountainArmChoreography.Name, DoubleMountainArmChoreography.Beats, DoubleMountainArmChoreography.Evaluate),
            new(WindFlagDiagonalChoreography.Name, WindFlagDiagonalChoreography.Beats, WindFlagDiagonalChoreography.Evaluate),
            new(TurnBackSetPoseChoreography.Name, TurnBackSetPoseChoreography.Beats, TurnBackSetPoseChoreography.Evaluate),
            new(PiercingPalmDriveChoreography.Name, PiercingPalmDriveChoreography.Beats, PiercingPalmDriveChoreography.Evaluate),
            new(LungeDoublePalmPushChoreography.Name, LungeDoublePalmPushChoreography.Beats, LungeDoublePalmPushChoreography.Evaluate),
            new(RaisedKneeHookedFootChoreography.Name, RaisedKneeHookedFootChoreography.Beats, RaisedKneeHookedFootChoreography.Evaluate),
            new(CrossStepShuffleChoreography.Name, CrossStepShuffleChoreography.Beats, CrossStepShuffleChoreography.Evaluate),
            new(BowingInvitationChoreography.Name, BowingInvitationChoreography.Beats, BowingInvitationChoreography.Evaluate),
            new(BackwardFarewellChoreography.Name, BackwardFarewellChoreography.Beats, BackwardFarewellChoreography.Evaluate)
        };

        public static int Count => Segments.Length;

        public static string GetName(int index) => Segments[index].Name;
        public static int GetBeats(int index) => Segments[index].Beats;

        public static int GetStartBeat(int index)
        {
            int beat = 0;
            for (int i = 0; i < index; i++) beat += Segments[i].Beats;
            return beat;
        }

        public static PuppetV2Pose Evaluate(double beat)
        {
            double clampedBeat = Math.Max(0d, Math.Min(TotalBeats, beat));
            double anchorX = Segments[0].Evaluate(0d).RootX;
            double anchorY = Segments[0].Evaluate(0d).RootY;
            double direction = 1d;
            int startBeat = 0;

            for (int i = 0; i < Segments.Length; i++)
            {
                Segment segment = Segments[i];
                int endBeat = startBeat + segment.Beats;
                PuppetV2Pose rawStart = segment.Evaluate(0d);
                PuppetV2Pose rawEnd = segment.Evaluate(1d);
                PuppetV2Pose previousEnd = default;
                bool hasPrevious = i > 0;
                if (hasPrevious)
                    previousEnd = EvaluateSegmentEnd(i - 1);

                if (clampedBeat < endBeat || i == Segments.Length - 1)
                {
                    double localBeat = Math.Max(0d, clampedBeat - startBeat);
                    double progress = segment.Beats <= 0 ? 0d : localBeat / segment.Beats;
                    PuppetV2Pose current = Transform(segment.Evaluate(progress), rawStart,
                        anchorX, anchorY, direction);
                    if (!hasPrevious || localBeat >= TransitionBeats) return current;
                    double blend = Smooth(localBeat / TransitionBeats);
                    return Blend(previousEnd, current, blend);
                }

                PuppetV2Pose transformedEnd = Transform(rawEnd, rawStart, anchorX, anchorY, direction);
                anchorX = transformedEnd.RootX;
                anchorY = transformedEnd.RootY;
                direction = transformedEnd.Facing < 0d ? -1d : 1d;
                startBeat = endBeat;
            }

            return EvaluateSegmentEnd(Segments.Length - 1);
        }

        private static PuppetV2Pose EvaluateSegmentEnd(int index)
        {
            double anchorX = Segments[0].Evaluate(0d).RootX;
            double anchorY = Segments[0].Evaluate(0d).RootY;
            double direction = 1d;
            PuppetV2Pose result = default;
            for (int i = 0; i <= index; i++)
            {
                PuppetV2Pose start = Segments[i].Evaluate(0d);
                result = Transform(Segments[i].Evaluate(1d), start, anchorX, anchorY, direction);
                anchorX = result.RootX;
                anchorY = result.RootY;
                direction = result.Facing < 0d ? -1d : 1d;
            }
            return result;
        }

        private static PuppetV2Pose Transform(PuppetV2Pose pose, PuppetV2Pose start,
            double anchorX, double anchorY, double direction)
        {
            double rootX = anchorX + (direction * (pose.RootX - start.RootX));
            double rootY = anchorY + (pose.RootY - start.RootY);
            return new PuppetV2Pose(rootX, rootY, pose.Torso, pose.Head,
                rootX + (direction * (pose.LeftHandX - pose.RootX)), rootY + (pose.LeftHandY - pose.RootY),
                rootX + (direction * (pose.RightHandX - pose.RootX)), rootY + (pose.RightHandY - pose.RootY),
                pose.LeftWrist, pose.RightWrist,
                rootX + (direction * (pose.LeftFootX - pose.RootX)), rootY + (pose.LeftFootY - pose.RootY),
                rootX + (direction * (pose.RightFootX - pose.RootX)), rootY + (pose.RightFootY - pose.RootY),
                pose.LeftShoe, pose.RightShoe, direction * pose.Facing,
                pose.LeftHandShape, pose.RightHandShape, pose.LeftFootPlanted, pose.RightFootPlanted);
        }

        private static PuppetV2Pose Blend(PuppetV2Pose from, PuppetV2Pose to, double amount)
        {
            bool leftLocked = from.LeftFootPlanted && to.LeftFootPlanted &&
                Distance(from.LeftFootX, from.LeftFootY, to.LeftFootX, to.LeftFootY) < 0.005d;
            bool rightLocked = from.RightFootPlanted && to.RightFootPlanted &&
                Distance(from.RightFootX, from.RightFootY, to.RightFootX, to.RightFootY) < 0.005d;
            return new PuppetV2Pose(
                Lerp(from.RootX, to.RootX, amount), Lerp(from.RootY, to.RootY, amount),
                Lerp(from.Torso, to.Torso, amount), Lerp(from.Head, to.Head, amount),
                Lerp(from.LeftHandX, to.LeftHandX, amount), Lerp(from.LeftHandY, to.LeftHandY, amount),
                Lerp(from.RightHandX, to.RightHandX, amount), Lerp(from.RightHandY, to.RightHandY, amount),
                Lerp(from.LeftWrist, to.LeftWrist, amount), Lerp(from.RightWrist, to.RightWrist, amount),
                Lerp(from.LeftFootX, to.LeftFootX, amount), Lerp(from.LeftFootY, to.LeftFootY, amount),
                Lerp(from.RightFootX, to.RightFootX, amount), Lerp(from.RightFootY, to.RightFootY, amount),
                Lerp(from.LeftShoe, to.LeftShoe, amount), Lerp(from.RightShoe, to.RightShoe, amount),
                Lerp(from.Facing, to.Facing, amount),
                amount < 0.5d ? from.LeftHandShape : to.LeftHandShape,
                amount < 0.5d ? from.RightHandShape : to.RightHandShape,
                leftLocked, rightLocked);
        }

        private static double Distance(double ax, double ay, double bx, double by)
        {
            double dx = bx - ax;
            double dy = by - ay;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static double Smooth(double value)
        {
            value = Math.Max(0d, Math.Min(1d, value));
            return value * value * (3d - (2d * value));
        }

        private static double Lerp(double from, double to, double amount) => from + ((to - from) * amount);
    }
}
