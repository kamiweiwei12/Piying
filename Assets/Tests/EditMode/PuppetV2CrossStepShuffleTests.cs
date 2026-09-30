using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2CrossStepShuffleTests
    {
        [Test]
        public void P5_17_UsesThreeAlternatingSmallStepsAndSettlesForward()
        {
            Assert.That(CrossStepShuffleChoreography.Name, Is.EqualTo("蹉步移位"));
            Assert.That(CrossStepShuffleChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = CrossStepShuffleChoreography.Evaluate(0d);
            PuppetV2Pose first = CrossStepShuffleChoreography.Evaluate(0.32d);
            PuppetV2Pose second = CrossStepShuffleChoreography.Evaluate(0.49d);
            PuppetV2Pose third = CrossStepShuffleChoreography.Evaluate(0.66d);
            PuppetV2Pose end = CrossStepShuffleChoreography.Evaluate(1d);
            Assert.That(first.RightFootPlanted, Is.False);
            Assert.That(second.LeftFootPlanted, Is.False);
            Assert.That(third.RightFootPlanted, Is.False);
            Assert.That(end.RootX - start.RootX, Is.EqualTo(CrossStepShuffleChoreography.Travel).Within(0.0001d));
            Assert.That(end.LeftFootPlanted && end.RightFootPlanted, Is.True);
            Assert.That(end.LeftFootY, Is.EqualTo(CrossStepShuffleChoreography.GroundY).Within(0.0001d));
            Assert.That(end.RightFootY, Is.EqualTo(CrossStepShuffleChoreography.GroundY).Within(0.0001d));
            Assert.That((end.LeftFootX + end.RightFootX) * 0.5d,
                Is.EqualTo(end.RootX).Within(0.0001d));
        }

        [Test]
        public void P5_17_PlantedFootLocksWhilePelvisAdvancesWithLowBounce()
        {
            DestroyNamed("V2 P5-17 Continuity Test");
            var root = new GameObject("V2 P5-17 Continuity Test");
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
            PuppetV2Pose previousPose = CrossStepShuffleChoreography.Evaluate(0d);
            float minimumPelvisY = float.PositiveInfinity;
            float maximumPelvisY = float.NegativeInfinity;
            float previousRootX = float.NegativeInfinity;
            for (int i = 0; i <= 480; i++)
            {
                float progress = i / 480f;
                PuppetV2Pose pose = CrossStepShuffleChoreography.Evaluate(progress);
                presenter.PreviewV2CrossStepShuffle(progress);
                Assert.That(presenter.PelvisPosition.x, Is.GreaterThanOrEqualTo(previousRootX - 0.0001f));
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                if (i > 0 && pose.LeftFootPlanted && previousPose.LeftFootPlanted)
                    Assert.That(Vector3.Distance(presenter.LeftAnklePosition, previousLeft), Is.LessThan(0.0015f));
                if (i > 0 && pose.RightFootPlanted && previousPose.RightFootPlanted)
                    Assert.That(Vector3.Distance(presenter.RightAnklePosition, previousRight), Is.LessThan(0.0015f));
                if (i > 0)
                {
                    Assert.That(Vector3.Distance(leftKnee.position, previousLeftKnee), Is.LessThan(0.05f));
                    Assert.That(Vector3.Distance(rightKnee.position, previousRightKnee), Is.LessThan(0.05f));
                }
                minimumPelvisY = Mathf.Min(minimumPelvisY, presenter.PelvisPosition.y);
                maximumPelvisY = Mathf.Max(maximumPelvisY, presenter.PelvisPosition.y);
                previousRootX = presenter.PelvisPosition.x;
                previousLeft = presenter.LeftAnklePosition;
                previousRight = presenter.RightAnklePosition;
                previousLeftKnee = leftKnee.position;
                previousRightKnee = rightKnee.position;
                previousPose = pose;
            }
            Assert.That(maximumPelvisY - minimumPelvisY, Is.LessThanOrEqualTo(0.0121f));
            presenter.PreviewV2CrossStepShuffle(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_17_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-17 Visual Test");
            DestroyNamed("V2 P5-17 Camera");
            var root = new GameObject("V2 P5-17 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-17-CrossStepShuffle");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-17 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.24f, 0.35f, 0.51f, 0.68f, 0.76f, 1f };
            string[] names = { "01-prepare", "02-ready", "03-right-step", "04-left-step",
                "05-final-step", "06-feet-settle", "07-stop" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2CrossStepShuffle(stages[i]);
                string path = Path.Combine(directory, names[i] + ".png");
                SaveFrame(camera, target, capture, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(20000));
            }
            for (int i = 0; i <= 64; i++)
            {
                presenter.PreviewV2CrossStepShuffle(i / 64f);
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
