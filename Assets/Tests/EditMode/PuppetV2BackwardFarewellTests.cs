using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2BackwardFarewellTests
    {
        [Test]
        public void P5_18_FarewellPrecedesFourBackwardStepsAndLowHandExit()
        {
            Assert.That(BackwardFarewellChoreography.Name, Is.EqualTo("回望退场"));
            Assert.That(BackwardFarewellChoreography.Beats, Is.EqualTo(12));
            PuppetV2Pose start = BackwardFarewellChoreography.Evaluate(0d);
            PuppetV2Pose farewell = BackwardFarewellChoreography.Evaluate(3d / 12d);
            PuppetV2Pose hold = BackwardFarewellChoreography.Evaluate(5d / 12d);
            PuppetV2Pose retreat = BackwardFarewellChoreography.Evaluate(0.64d);
            PuppetV2Pose end = BackwardFarewellChoreography.Evaluate(1d);
            Assert.That(farewell.RightHandX, Is.GreaterThan(start.RightHandX + 0.85d));
            Assert.That(farewell.RightHandY, Is.GreaterThan(start.RightHandY + 0.50d));
            Assert.That(hold.RootX, Is.Zero.Within(0.0001d));
            Assert.That(retreat.RootX, Is.LessThan(-0.20d));
            Assert.That(end.RootX, Is.EqualTo(BackwardFarewellChoreography.EndX).Within(0.0001d));
            Assert.That(end.RightHandY, Is.LessThan(-0.68d));
            Assert.That(end.Head, Is.EqualTo(5d).Within(0.0001d));
            Assert.That(end.LeftFootPlanted && end.RightFootPlanted, Is.True);
            Assert.That((end.LeftFootX + end.RightFootX) * 0.5d,
                Is.EqualTo(end.RootX).Within(0.0001d));
        }

        [Test]
        public void P5_18_PlantedFeetLockAndRetreatStaysContinuousInsideStage()
        {
            DestroyNamed("V2 P5-18 Continuity Test");
            var root = new GameObject("V2 P5-18 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Transform leftKnee = root.GetComponentsInChildren<Transform>(true)
                .First(transform => transform.name == "Joint Left Knee");
            Transform rightKnee = root.GetComponentsInChildren<Transform>(true)
                .First(transform => transform.name == "Joint Right Knee");
            Vector3 previousLeft = Vector3.zero;
            Vector3 previousRight = Vector3.zero;
            Vector3 previousLeftKnee = Vector3.zero;
            Vector3 previousRightKnee = Vector3.zero;
            PuppetV2Pose previousPose = BackwardFarewellChoreography.Evaluate(0d);
            float previousRootX = float.PositiveInfinity;
            int airborneSamples = 0;
            for (int i = 0; i <= 600; i++)
            {
                float progress = i / 600f;
                PuppetV2Pose pose = BackwardFarewellChoreography.Evaluate(progress);
                presenter.PreviewV2BackwardFarewell(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                if (progress <= 5f / 12f)
                    Assert.That(presenter.PelvisPosition.x, Is.Zero.Within(0.0001f));
                else
                    Assert.That(presenter.PelvisPosition.x, Is.LessThanOrEqualTo(previousRootX + 0.0001f));
                Assert.That(presenter.PelvisPosition.x, Is.GreaterThan(-0.89f));
                if (i > 0 && pose.LeftFootPlanted && previousPose.LeftFootPlanted)
                    Assert.That(Vector3.Distance(presenter.LeftAnklePosition, previousLeft), Is.LessThan(0.002f));
                if (i > 0 && pose.RightFootPlanted && previousPose.RightFootPlanted)
                    Assert.That(Vector3.Distance(presenter.RightAnklePosition, previousRight), Is.LessThan(0.002f));
                if (!pose.LeftFootPlanted || !pose.RightFootPlanted) airborneSamples++;
                if (i > 0)
                {
                    Assert.That(Vector3.Distance(leftKnee.position, previousLeftKnee), Is.LessThan(0.05f));
                    Assert.That(Vector3.Distance(rightKnee.position, previousRightKnee), Is.LessThan(0.05f));
                }
                previousRootX = presenter.PelvisPosition.x;
                previousLeft = presenter.LeftAnklePosition;
                previousRight = presenter.RightAnklePosition;
                previousLeftKnee = leftKnee.position;
                previousRightKnee = rightKnee.position;
                previousPose = pose;
            }
            Assert.That(airborneSamples, Is.GreaterThan(100), "four retreat steps must visibly leave the floor");
            presenter.PreviewV2BackwardFarewell(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_18_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-18 Visual Test");
            DestroyNamed("V2 P5-18 Camera");
            DestroyNamed("V2 P5-18 Continuity Test");
            var root = new GameObject("V2 P5-18 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-18-BackwardFarewell");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-18 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(-0.35f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 5f / 12f, 0.55f, 0.70f, 0.84f, 1f };
            string[] names = { "01-prepare", "02-farewell", "03-farewell-holds", "04-retreat-begins",
                "05-look-back", "06-final-step", "07-curtain-stop" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2BackwardFarewell(stages[i]);
                string path = Path.Combine(directory, names[i] + ".png");
                SaveFrame(camera, target, capture, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(20000));
            }
            for (int i = 0; i <= 72; i++)
            {
                presenter.PreviewV2BackwardFarewell(i / 72f);
                SaveFrame(camera, target, capture, Path.Combine(frames, $"frame-{i:000}.png"));
            }
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
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
