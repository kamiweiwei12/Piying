using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2LungeDoublePalmPushTests
    {
        [Test]
        public void P5_14_PlantsFrontFootBeforeWeightShiftAndDoublePalmPush()
        {
            Assert.That(LungeDoublePalmPushChoreography.Name, Is.EqualTo("弓步推势"));
            Assert.That(LungeDoublePalmPushChoreography.Beats, Is.EqualTo(12));
            PuppetV2Pose start = LungeDoublePalmPushChoreography.Evaluate(0d);
            PuppetV2Pose lifted = LungeDoublePalmPushChoreography.Evaluate(0.16d);
            PuppetV2Pose planted = LungeDoublePalmPushChoreography.Evaluate(0.25d);
            PuppetV2Pose shifted = LungeDoublePalmPushChoreography.Evaluate(0.40d);
            PuppetV2Pose pushed = LungeDoublePalmPushChoreography.Evaluate(0.75d);
            PuppetV2Pose finish = LungeDoublePalmPushChoreography.Evaluate(1d);
            Assert.That(lifted.RootX, Is.Zero.Within(0.001d));
            Assert.That(lifted.RightFootX, Is.GreaterThan(start.RightFootX + 0.27d));
            Assert.That(lifted.RightFootY, Is.GreaterThan(start.RightFootY + 0.07d));
            Assert.That(lifted.RightFootPlanted, Is.False);
            Assert.That(planted.RootX, Is.Zero.Within(0.001d));
            Assert.That(planted.RightFootY, Is.EqualTo(start.RightFootY).Within(0.001d));
            Assert.That(planted.RightFootPlanted, Is.True);
            Assert.That(shifted.RootX, Is.GreaterThan(0.05d));
            Assert.That(pushed.RootX, Is.GreaterThan(0.15d));
            Assert.That(pushed.RootY, Is.LessThan(start.RootY - 0.02d));
            Assert.That(pushed.LeftHandX, Is.GreaterThan(1.10d));
            Assert.That(pushed.RightHandX, Is.GreaterThan(1.50d));
            Assert.That(pushed.LeftHandY - pushed.RightHandY, Is.GreaterThan(0.14d));
            Assert.That(pushed.Torso, Is.GreaterThan(9d));
            Assert.That(finish.RootX, Is.LessThan(pushed.RootX - 0.05d));
            Assert.That(finish.RightFootX, Is.EqualTo(planted.RightFootX).Within(0.001d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_14_LungeKeepsRearFootAndLandedFrontFootLockedWithContinuousArms()
        {
            DestroyNamed("V2 P5-14 Continuity Test");
            var root = new GameObject("V2 P5-14 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Transform leftHandArt = root.GetComponentsInChildren<Transform>(true)
                .First(transform => transform.name == "Art Left Hand");
            Transform rightHandArt = root.GetComponentsInChildren<Transform>(true)
                .First(transform => transform.name == "Art Right Hand");
            presenter.PreviewV2LungeDoublePalmPush(0f);
            Vector3 rearFoot = presenter.LeftAnklePosition;
            Vector3 plantedFrontFoot = Vector3.zero;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float previousLeftPush = float.NegativeInfinity;
            float previousRightPush = float.NegativeInfinity;
            float maximumRearFootResidual = 0f;
            float maximumFrontFootResidual = 0f;
            for (int i = 1; i <= 600; i++)
            {
                float progress = i / 600f;
                presenter.PreviewV2LungeDoublePalmPush(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                maximumRearFootResidual = Mathf.Max(maximumRearFootResidual,
                    Vector3.Distance(presenter.LeftAnklePosition, rearFoot));
                if (i == 151) plantedFrontFoot = presenter.RightAnklePosition;
                if (i >= 151)
                    maximumFrontFootResidual = Mathf.Max(maximumFrontFootResidual,
                        Vector3.Distance(presenter.RightAnklePosition, plantedFrontFoot));
                if (progress < 0.25f)
                    Assert.That(presenter.PelvisPosition.x, Is.Zero.Within(0.0001f));
                if (progress >= 0.40f && progress <= 0.75f)
                {
                    Assert.That(presenter.LeftWristPosition.x + 0.0001f, Is.GreaterThanOrEqualTo(previousLeftPush));
                    Assert.That(presenter.RightWristPosition.x + 0.0001f, Is.GreaterThanOrEqualTo(previousRightPush));
                    Assert.That(presenter.LeftElbowPosition.x, Is.LessThan(presenter.LeftWristPosition.x - 0.15f));
                    Assert.That(presenter.RightElbowPosition.x, Is.LessThan(presenter.RightWristPosition.x - 0.15f));
                    Assert.That(leftHandArt.localScale.x, Is.LessThan(0f), "left palm must face forward");
                    Assert.That(rightHandArt.localScale.x, Is.LessThan(0f), "right palm must face forward");
                    Assert.That(Quaternion.Angle(leftHandArt.rotation, rightHandArt.rotation),
                        Is.LessThan(0.01f), "both upright palms must point their fingertips upward together");
                    previousLeftPush = presenter.LeftWristPosition.x;
                    previousRightPush = presenter.RightWristPosition.x;
                }
                Assert.That(Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow), Is.LessThan(0.05f));
                Assert.That(Vector3.Distance(presenter.RightElbowPosition, previousRightElbow), Is.LessThan(0.05f));
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            Assert.That(maximumRearFootResidual, Is.LessThan(0.01f), "rear foot visible residual");
            Assert.That(maximumFrontFootResidual, Is.LessThan(0.01f), "front foot visible residual");
            presenter.PreviewV2LungeDoublePalmPush(0.75f);
            Assert.That(presenter.V2LeftHandTarget.y - presenter.V2RightHandTarget.y, Is.GreaterThan(0.14f));
            presenter.PreviewV2LungeDoublePalmPush(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_14_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-14 Visual Test");
            DestroyNamed("V2 P5-14 Camera");
            DestroyNamed("V2 P5-14 Continuity Test");
            var root = new GameObject("V2 P5-14 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-14-LungeDoublePalmPush");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-14 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.16f, 0.25f, 0.40f, 0.58f, 0.75f, 1f };
            string[] names = { "01-gather", "02-front-foot-leads", "03-front-foot-planted", "04-weight-shifts", "05-palms-drive", "06-lunge-holds", "07-weight-recovers" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2LungeDoublePalmPush(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2LungeDoublePalmPush(i / 80f);
                SaveFrame(camera, target, capture, Path.Combine(frames, $"frame-{i:000}.png"));
            }
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
            for (int i = 0; i < names.Length; i++)
                Assert.That(new FileInfo(Path.Combine(directory, names[i] + ".png")).Length,
                    Is.GreaterThan(20000), names[i]);
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
