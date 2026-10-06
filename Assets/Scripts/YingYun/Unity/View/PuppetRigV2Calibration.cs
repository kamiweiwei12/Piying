using System;
using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>
    /// V2 动作系统的只读绑定契约。数值描述现有 15 分片，不负责播放动作。
    /// 所有点均在 M6 Shadow Puppet Stage 的局部坐标中。
    /// </summary>
    public static class PuppetRigV2Calibration
    {
        public const float UpperArmLength = 0.82f;
        public const float ForearmLength = 0.72f;
        public const float ThighLength = 0.88f;
        public const float ShinLength = 0.82f;
        public const float ArmMaximumReach = UpperArmLength + ForearmLength;
        public const float ArmMinimumReach = UpperArmLength - ForearmLength;
        public const float LegMaximumReach = ThighLength + ShinLength;
        public const float LegMinimumReach = ThighLength - ShinLength;
        public const float GroundY = -2.15f;

        // 舞台木框内沿再留 0.08 单位，动作目标必须留在此区域。
        public static readonly Rect StageSafetyFrame = Rect.MinMaxRect(-2.54f, -2.86f, 2.54f, 2.86f);

        public static readonly PuppetJointCalibration[] Joints =
        {
            new PuppetJointCalibration("腰", "Joint Pelvis/Joint Waist", 0f, 0f),
            new PuppetJointCalibration("颈", "Joint Pelvis/Joint Waist/Joint Neck", 0f, 0f),
            new PuppetJointCalibration("左肩", "Joint Pelvis/Joint Waist/Joint Left Shoulder", 0f, -1f),
            new PuppetJointCalibration("左肘", "Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow", 0f, -1f),
            new PuppetJointCalibration("左腕", "Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow/Joint Left Wrist", 0f, -1f),
            new PuppetJointCalibration("右肩", "Joint Pelvis/Joint Waist/Joint Right Shoulder", 0f, 1f),
            new PuppetJointCalibration("右肘", "Joint Pelvis/Joint Waist/Joint Right Shoulder/Joint Right Elbow", 0f, 1f),
            new PuppetJointCalibration("右腕", "Joint Pelvis/Joint Waist/Joint Right Shoulder/Joint Right Elbow/Joint Right Wrist", 0f, 1f),
            new PuppetJointCalibration("左髋", "Joint Pelvis/Joint Left Hip", 0f, -1f),
            new PuppetJointCalibration("左膝", "Joint Pelvis/Joint Left Hip/Joint Left Knee", 0f, -1f),
            new PuppetJointCalibration("左踝", "Joint Pelvis/Joint Left Hip/Joint Left Knee/Joint Left Ankle", 0f, -1f),
            new PuppetJointCalibration("右髋", "Joint Pelvis/Joint Right Hip", 0f, 1f),
            new PuppetJointCalibration("右膝", "Joint Pelvis/Joint Right Hip/Joint Right Knee", 0f, 1f),
            new PuppetJointCalibration("右踝", "Joint Pelvis/Joint Right Hip/Joint Right Knee/Joint Right Ankle", 0f, 1f)
        };

        public static readonly PuppetArtCalibration[] Art =
        {
            new PuppetArtCalibration("puppet_head", "Joint Pelvis/Joint Waist/Joint Neck/Art Head", 1.42f, 0f),
            new PuppetArtCalibration("puppet_torso", "Joint Pelvis/Joint Waist/Art Torso", 1.45f, 0f),
            new PuppetArtCalibration("puppet_pelvis", "Joint Pelvis/Art Pelvis", 0.86f, 0f),
            new PuppetArtCalibration("puppet_left_upper_arm", "Joint Pelvis/Joint Waist/Joint Left Shoulder/Art Left Upper Arm", 1.03f, -7.2f),
            new PuppetArtCalibration("puppet_left_forearm", "Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow/Art Left Forearm", 0.89f, 5.8f),
            new PuppetArtCalibration("puppet_left_hand", "Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow/Joint Left Wrist/Art Left Hand", 0.42f, 0f),
            new PuppetArtCalibration("puppet_left_upper_arm", "Joint Pelvis/Joint Waist/Joint Right Shoulder/Art Right Upper Arm", 1.03f, 7.2f),
            new PuppetArtCalibration("puppet_right_forearm", "Joint Pelvis/Joint Waist/Joint Right Shoulder/Joint Right Elbow/Art Right Forearm", 0.90f, -1.4f),
            new PuppetArtCalibration("puppet_right_hand", "Joint Pelvis/Joint Waist/Joint Right Shoulder/Joint Right Elbow/Joint Right Wrist/Art Right Hand", 0.42f, 0f),
            new PuppetArtCalibration("puppet_left_thigh", "Joint Pelvis/Joint Left Hip/Art Left Thigh", 1.08f, 1.5f),
            new PuppetArtCalibration("puppet_left_shin", "Joint Pelvis/Joint Left Hip/Joint Left Knee/Art Left Shin", 0.99f, 0f),
            new PuppetArtCalibration("puppet_left_shoe", "Joint Pelvis/Joint Left Hip/Joint Left Knee/Joint Left Ankle/Art Left Shoe", 0.24f, 0f),
            new PuppetArtCalibration("puppet_right_thigh", "Joint Pelvis/Joint Right Hip/Art Right Thigh", 1.07f, 0f),
            new PuppetArtCalibration("puppet_right_shin", "Joint Pelvis/Joint Right Hip/Joint Right Knee/Art Right Shin", 0.99f, -5.4f),
            new PuppetArtCalibration("puppet_right_shoe", "Joint Pelvis/Joint Right Hip/Joint Right Knee/Joint Right Ankle/Art Right Shoe", 0.24f, 0f)
        };
    }

    [Serializable]
    public readonly struct PuppetJointCalibration
    {
        public readonly string Label;
        public readonly string Path;
        public readonly float BindAngle;
        public readonly float PositiveAxis;

        public PuppetJointCalibration(string label, string path, float bindAngle, float positiveAxis)
        {
            Label = label;
            Path = path;
            BindAngle = bindAngle;
            PositiveAxis = positiveAxis;
        }
    }

    [Serializable]
    public readonly struct PuppetArtCalibration
    {
        public readonly string SpriteName;
        public readonly string Path;
        public readonly float TargetHeight;
        public readonly float BindAngle;

        public PuppetArtCalibration(string spriteName, string path, float targetHeight, float bindAngle)
        {
            SpriteName = spriteName;
            Path = path;
            TargetHeight = targetHeight;
            BindAngle = bindAngle;
        }
    }

    public readonly struct PuppetRigCalibrationSnapshot
    {
        public readonly Bounds VisibleBounds;
        public readonly Vector2 LeftFingertip;
        public readonly Vector2 RightFingertip;
        public readonly Vector2 LeftSole;
        public readonly Vector2 RightSole;

        public float VisibleHeight => VisibleBounds.size.y;

        public PuppetRigCalibrationSnapshot(Bounds visibleBounds, Vector2 leftFingertip,
            Vector2 rightFingertip, Vector2 leftSole, Vector2 rightSole)
        {
            VisibleBounds = visibleBounds;
            LeftFingertip = leftFingertip;
            RightFingertip = rightFingertip;
            LeftSole = leftSole;
            RightSole = rightSole;
        }
    }
}
