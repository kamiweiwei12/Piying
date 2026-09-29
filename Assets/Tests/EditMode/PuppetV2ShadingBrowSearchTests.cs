using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2ShadingBrowSearchTests
    {
        [Test]
        public void P5_10_HandReachesBrowBeforeBodySearchesAndThenLowers()
        {
            Assert.That(ShadingBrowSearchChoreography.Name, Is.EqualTo("遮额探看"));
            Assert.That(ShadingBrowSearchChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = ShadingBrowSearchChoreography.Evaluate(0d);
            PuppetV2Pose brow = ShadingBrowSearchChoreography.Evaluate(0.375d);
            PuppetV2Pose lean = ShadingBrowSearchChoreography.Evaluate(0.625d);
            PuppetV2Pose hold = ShadingBrowSearchChoreography.Evaluate(0.75d);
            PuppetV2Pose finish = ShadingBrowSearchChoreography.Evaluate(1d);

            Assert.That(brow.RightHandY, Is.GreaterThan(1.20d));
            Assert.That(brow.Torso, Is.LessThan(2d));
            Assert.That(lean.RightHandY, Is.GreaterThan(1.40d));
            Assert.That(lean.Torso, Is.EqualTo(-12d).Within(0.001d));
            Assert.That(lean.RootX, Is.GreaterThan(0.06d));
            Assert.That(lean.RightWrist, Is.LessThanOrEqualTo(-135d));
            Assert.That(hold.RightHandY, Is.EqualTo(lean.RightHandY).Within(0.001d));
            Assert.That(hold.Head, Is.EqualTo(lean.Head).Within(0.001d));
            Assert.That(finish.RightHandY, Is.LessThan(0.50d));
            Assert.That(finish.Torso, Is.EqualTo(-3d).Within(0.001d));
            Assert.That(finish.LeftHandShape, Is.EqualTo(PuppetHandShape.NaturalPalm));
            Assert.That(finish.RightHandShape, Is.EqualTo(PuppetHandShape.NaturalPalm));
            Assert.That(start.LeftFootPlanted && start.RightFootPlanted &&
                finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_10_BrowHandStaysOutsideCrownWhileFeetStayLocked()
        {
            DestroyNamed("V2 P5-10 Continuity Test");
            var root = new GameObject("V2 P5-10 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2ShadingBrowSearch(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2ShadingBrowSearch(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.0001f));
                maximumLeftElbowStep = Mathf.Max(maximumLeftElbowStep,
                    Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow));
                maximumRightElbowStep = Mathf.Max(maximumRightElbowStep,
                    Vector3.Distance(presenter.RightElbowPosition, previousRightElbow));
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            Assert.That(maximumLeftElbowStep, Is.LessThan(0.08f));
            Assert.That(maximumRightElbowStep, Is.LessThan(0.08f));
            presenter.PreviewV2ShadingBrowSearch(0.70f);
            Assert.That(presenter.RightWristPosition.x, Is.InRange(0.90f, 0.97f));
            Assert.That(presenter.RightWristPosition.y, Is.InRange(1.39f, 1.46f));
            Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(0.72f));
            Assert.That(presenter.PelvisPosition.x, Is.GreaterThan(0.06f));
            presenter.PreviewV2ShadingBrowSearch(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_10_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-10 Visual Test");
            DestroyNamed("V2 P5-10 Camera");
            var root = new GameObject("V2 P5-10 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-10-ShadingBrowSearch");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-10 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.1875f, 0.375f, 0.50f, 0.625f, 0.75f, 1f };
            string[] names = { "01-half-close", "02-hand-rises", "03-brow-arrival", "04-body-follows", "05-forward-search", "06-gaze-hold", "07-return" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2ShadingBrowSearch(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2ShadingBrowSearch(i / 80f);
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
