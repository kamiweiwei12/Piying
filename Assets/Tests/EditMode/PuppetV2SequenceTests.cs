using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2SequenceTests
    {
        private static readonly string[] ExpectedOrder =
        {
            "缓步入场", "整冠肃立", "拱手致礼", "云手展圆", "托月仰望", "指路远眺",
            "遮额探看", "掩面低诉", "展臂仰笑", "双展山膀", "斜展顺风旗", "回身定相",
            "穿掌送势", "弓步推势", "提膝挂脚", "蹉步移位", "躬身请势", "回望退场"
        };

        [Test]
        public void P6_ContainsApprovedEighteenMoveOrderAndExactlyOneHundredSixtyEightBeats()
        {
            Assert.That(PuppetV2SequenceChoreography.Count, Is.EqualTo(18));
            Assert.That(PuppetV2SequenceChoreography.TotalBeats, Is.EqualTo(168));
            int sum = 0;
            for (int i = 0; i < ExpectedOrder.Length; i++)
            {
                Assert.That(PuppetV2SequenceChoreography.GetName(i), Is.EqualTo(ExpectedOrder[i]));
                Assert.That(PuppetV2SequenceChoreography.GetStartBeat(i), Is.EqualTo(sum));
                sum += PuppetV2SequenceChoreography.GetBeats(i);
            }
            Assert.That(sum, Is.EqualTo(168));
        }

        [Test]
        public void P6_AllSeventeenBoundariesArePositionContinuousAndFacingPersistsAfterTurn()
        {
            const double epsilon = 1d / 240d;
            for (int i = 1; i < PuppetV2SequenceChoreography.Count; i++)
            {
                double boundary = PuppetV2SequenceChoreography.GetStartBeat(i);
                PuppetV2Pose before = PuppetV2SequenceChoreography.Evaluate(boundary - epsilon);
                PuppetV2Pose at = PuppetV2SequenceChoreography.Evaluate(boundary);
                PuppetV2Pose after = PuppetV2SequenceChoreography.Evaluate(boundary + epsilon);
                AssertPoseNear(before, at, 0.015d, $"boundary {i} before");
                AssertPoseNear(at, after, 0.015d, $"boundary {i} after");
            }

            int turnIndex = 11;
            double afterTurn = PuppetV2SequenceChoreography.GetStartBeat(turnIndex) +
                PuppetV2SequenceChoreography.GetBeats(turnIndex) + 0.5d;
            Assert.That(PuppetV2SequenceChoreography.Evaluate(afterTurn).Facing, Is.LessThan(-0.9d));
            Assert.That(PuppetV2SequenceChoreography.Evaluate(167d).Facing, Is.LessThan(-0.9d));
        }

        [Test]
        public void P6_FullSequenceKeepsReachableHandsAndLockedFeetStable()
        {
            DestroyNamed("V2 P6 Continuity Test");
            var root = new GameObject("V2 P6 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Vector3 previousLeft = Vector3.zero;
            Vector3 previousRight = Vector3.zero;
            PuppetV2Pose previousPose = PuppetV2SequenceChoreography.Evaluate(0d);
            for (int i = 0; i <= 5040; i++)
            {
                float beat = i / 30f;
                PuppetV2Pose pose = PuppetV2SequenceChoreography.Evaluate(beat);
                presenter.PreviewV2FullSequence(beat);
                // World-space wrist targets are directly comparable only before the parent-facing
                // mirror begins; the pose-boundary test covers hand continuity across and after it.
                int turnStart = PuppetV2SequenceChoreography.GetStartBeat(11);
                if (beat < turnStart)
                {
                    Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                        Is.LessThan(0.0032f), $"left wrist at beat {beat:F3}");
                    Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                        Is.LessThan(0.0032f), $"right wrist at beat {beat:F3}");
                }
                if (i > 0 && pose.LeftFootPlanted && previousPose.LeftFootPlanted)
                    Assert.That(Vector3.Distance(presenter.LeftAnklePosition, previousLeft),
                        Is.LessThan(0.005f), $"left planted foot at beat {beat:F3}");
                if (i > 0 && pose.RightFootPlanted && previousPose.RightFootPlanted)
                    Assert.That(Vector3.Distance(presenter.RightAnklePosition, previousRight),
                        Is.LessThan(0.005f), $"right planted foot at beat {beat:F3}");
                previousLeft = presenter.LeftAnklePosition;
                previousRight = presenter.RightAnklePosition;
                previousPose = pose;
            }
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P8Correction_AllSequenceElbowsFoldAwayFromTheTorso()
        {
            var root = new GameObject("V2 P8 Elbow Direction Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            for (int i = 0; i <= PuppetV2SequenceChoreography.TotalBeats * 4; i++)
            {
                float beat = i / 4f;
                int turnStart = PuppetV2SequenceChoreography.GetStartBeat(11);
                // 序列器在动作末尾保留一拍过门，换面侧影直到该过门结束才有稳定外侧。
                int turnEnd = turnStart + PuppetV2SequenceChoreography.GetBeats(11) + 1;
                if (beat >= turnStart && beat <= turnEnd) continue;
                bool inTransition = false;
                for (int move = 1; move < PuppetV2SequenceChoreography.Count; move++)
                {
                    int start = PuppetV2SequenceChoreography.GetStartBeat(move);
                    if (beat >= start && beat < start + 1f)
                    {
                        inTransition = true;
                        break;
                    }
                }
                if (inTransition) continue;
                presenter.PreviewV2FullSequence(beat);
                Vector3 leftShoulder = presenter.LeftShoulderPosition;
                Vector3 rightShoulder = presenter.RightShoulderPosition;
                Vector3 leftElbow = presenter.LeftElbowPosition;
                Vector3 rightElbow = presenter.RightElbowPosition;
                bool leftIsScreenLeft = leftShoulder.x < rightShoulder.x;
                // 侧身换面时两肩会在幕面投影中交叉；此时没有稳定的“身体外侧”。
                if (Mathf.Abs(leftShoulder.x - rightShoulder.x) < 0.5f) continue;

                // 胸前推掌、穿掌等属于明确抬手动作；这里只锁定自然低手／下垂区。
                bool leftNotRaised = presenter.V2LeftHandTarget.y <= leftShoulder.y - 0.35f;
                bool rightNotRaised = presenter.V2RightHandTarget.y <= rightShoulder.y - 0.35f;
                if (leftIsScreenLeft)
                {
                    if (leftNotRaised)
                        Assert.That(leftElbow.x, Is.LessThanOrEqualTo(leftShoulder.x + 0.02f),
                            $"left elbow folded inward at beat {beat:F2}");
                    if (rightNotRaised)
                        Assert.That(rightElbow.x, Is.GreaterThanOrEqualTo(rightShoulder.x - 0.02f),
                            $"right elbow folded inward at beat {beat:F2}");
                }
                else
                {
                    if (leftNotRaised)
                        Assert.That(leftElbow.x, Is.GreaterThanOrEqualTo(leftShoulder.x - 0.02f),
                            $"left elbow folded inward after turn at beat {beat:F2}");
                    if (rightNotRaised)
                        Assert.That(rightElbow.x, Is.LessThanOrEqualTo(rightShoulder.x + 0.02f),
                            $"right elbow folded inward after turn at beat {beat:F2}");
                }
            }
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P6_RendersActionAndBoundaryEvidenceWithFullSequenceFrames()
        {
            DestroyNamed("V2 P6 Visual Test");
            DestroyNamed("V2 P6 Camera");
            DestroyNamed("V2 P6 Continuity Test");
            var root = new GameObject("V2 P6 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P6-FullSequence");
            string actions = Path.Combine(directory, "actions");
            string boundaries = Path.Combine(directory, "boundaries");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(actions);
            Directory.CreateDirectory(boundaries);
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P6 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0.2f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;

            for (int i = 0; i < PuppetV2SequenceChoreography.Count; i++)
            {
                float beat = PuppetV2SequenceChoreography.GetStartBeat(i) +
                    (PuppetV2SequenceChoreography.GetBeats(i) * 0.5f);
                presenter.PreviewV2FullSequence(beat);
                SaveFrame(camera, target, capture, Path.Combine(actions, $"action-{i + 1:00}.png"));
            }
            for (int i = 1; i < PuppetV2SequenceChoreography.Count; i++)
            {
                presenter.PreviewV2FullSequence(PuppetV2SequenceChoreography.GetStartBeat(i));
                SaveFrame(camera, target, capture, Path.Combine(boundaries, $"boundary-{i:00}.png"));
            }
            for (int beat = 0; beat <= PuppetV2SequenceChoreography.TotalBeats; beat++)
            {
                presenter.PreviewV2FullSequence(beat);
                SaveFrame(camera, target, capture, Path.Combine(frames, $"frame-{beat:000}.png"));
            }

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
            Assert.That(Directory.GetFiles(actions, "*.png").Length, Is.EqualTo(18));
            Assert.That(Directory.GetFiles(boundaries, "*.png").Length, Is.EqualTo(17));
            Assert.That(Directory.GetFiles(frames, "*.png").Length, Is.EqualTo(169));
        }

        private static void AssertPoseNear(PuppetV2Pose a, PuppetV2Pose b, double tolerance, string label)
        {
            Assert.That(Distance(a.RootX, a.RootY, b.RootX, b.RootY), Is.LessThan(tolerance), label + " root");
            Assert.That(Distance(a.LeftHandX, a.LeftHandY, b.LeftHandX, b.LeftHandY), Is.LessThan(tolerance), label + " left hand");
            Assert.That(Distance(a.RightHandX, a.RightHandY, b.RightHandX, b.RightHandY), Is.LessThan(tolerance), label + " right hand");
            Assert.That(Distance(a.LeftFootX, a.LeftFootY, b.LeftFootX, b.LeftFootY), Is.LessThan(tolerance), label + " left foot");
            Assert.That(Distance(a.RightFootX, a.RightFootY, b.RightFootX, b.RightFootY), Is.LessThan(tolerance), label + " right foot");
        }

        private static double Distance(double ax, double ay, double bx, double by)
        {
            double dx = bx - ax;
            double dy = by - ay;
            return System.Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static void DestroyNamed(string name)
        {
            GameObject existing;
            while ((existing = GameObject.Find(name)) != null)
                Object.DestroyImmediate(existing);
        }

        private static void SaveFrame(Camera camera, RenderTexture target, Texture2D capture, string path)
        {
            camera.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(path, capture.EncodeToPNG());
        }
    }
}
