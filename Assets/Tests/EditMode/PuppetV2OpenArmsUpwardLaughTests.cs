using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2OpenArmsUpwardLaughTests
    {
        [Test]
        public void P5_12_GathersThenOpensChestAndRaisesHead()
        {
            Assert.That(OpenArmsUpwardLaughChoreography.Name, Is.EqualTo("展臂仰笑"));
            Assert.That(OpenArmsUpwardLaughChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = OpenArmsUpwardLaughChoreography.Evaluate(0d);
            PuppetV2Pose gather = OpenArmsUpwardLaughChoreography.Evaluate(0.25d);
            PuppetV2Pose opening = OpenArmsUpwardLaughChoreography.Evaluate(0.50d);
            PuppetV2Pose laugh = OpenArmsUpwardLaughChoreography.Evaluate(0.75d);
            PuppetV2Pose finish = OpenArmsUpwardLaughChoreography.Evaluate(1d);

            Assert.That(gather.RightHandX - gather.LeftHandX, Is.LessThan(start.RightHandX - start.LeftHandX));
            Assert.That(opening.LeftHandX, Is.LessThan(-0.80d));
            Assert.That(opening.RightHandX, Is.GreaterThan(1.10d));
            Assert.That(laugh.LeftHandY, Is.GreaterThan(1.20d));
            Assert.That(laugh.RightHandY, Is.GreaterThan(1.20d));
            Assert.That(laugh.Head, Is.LessThan(-14d));
            Assert.That(laugh.Torso, Is.GreaterThan(9d));
            Assert.That(laugh.RootY, Is.GreaterThan(gather.RootY + 0.05d));
            Assert.That(finish.LeftHandY, Is.GreaterThan(1.10d));
            Assert.That(finish.RightHandY, Is.GreaterThan(1.10d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_12_ArmsOpenDiagonallyWithoutElbowJumpAndFeetStayLocked()
        {
            DestroyNamed("V2 P5-12 Continuity Test");
            var root = new GameObject("V2 P5-12 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2OpenArmsUpwardLaugh(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2OpenArmsUpwardLaugh(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                Assert.That(presenter.LeftWristPosition.x, Is.LessThan(presenter.RightWristPosition.x - 0.60f));
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.0001f));
                maximumLeftElbowStep = Mathf.Max(maximumLeftElbowStep,
                    Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow));
                maximumRightElbowStep = Mathf.Max(maximumRightElbowStep,
                    Vector3.Distance(presenter.RightElbowPosition, previousRightElbow));
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            Assert.That(maximumLeftElbowStep, Is.LessThan(0.035f));
            Assert.That(maximumRightElbowStep, Is.LessThan(0.035f));
            presenter.PreviewV2OpenArmsUpwardLaugh(0.75f);
            Assert.That(presenter.LeftWristPosition.x, Is.LessThan(-1.35f));
            Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(1.48f));
            Assert.That(presenter.LeftWristPosition.y, Is.GreaterThan(1.20f));
            Assert.That(presenter.RightWristPosition.y, Is.GreaterThan(1.20f));
            Assert.That(presenter.LeftElbowPosition.x, Is.LessThan(presenter.LeftShoulderPosition.x));
            Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(presenter.RightShoulderPosition.x));
            presenter.PreviewV2OpenArmsUpwardLaugh(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_12_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-12 Visual Test");
            DestroyNamed("V2 P5-12 Camera");
            var root = new GameObject("V2 P5-12 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-12-OpenArmsUpwardLaugh");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-12 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 1f };
            string[] names = { "01-lament-release", "02-chest-gather", "03-arms-break-open", "04-diagonal-opening", "05-head-lifts", "06-upward-laugh", "07-open-finish" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2OpenArmsUpwardLaugh(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2OpenArmsUpwardLaugh(i / 80f);
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
