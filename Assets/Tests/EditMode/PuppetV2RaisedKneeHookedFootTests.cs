using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2RaisedKneeHookedFootTests
    {
        [Test]
        public void P2_FullPoseMovesWeightBeforeLiftAndPlantsBeforeRecentering()
        {
            Assert.That(RaisedKneeHookedFootChoreography.Name, Is.EqualTo("提膝挂脚"));
            Assert.That(RaisedKneeHookedFootChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose shifted = RaisedKneeHookedFootChoreography.Evaluate(0.25d);
            PuppetV2Pose peak = RaisedKneeHookedFootChoreography.Evaluate(0.65d);
            PuppetV2Pose landed = RaisedKneeHookedFootChoreography.Evaluate(0.85d);
            PuppetV2Pose exit = RaisedKneeHookedFootChoreography.Evaluate(1d);
            Assert.That(shifted.RootX, Is.EqualTo(-0.18d).Within(0.0001d));
            Assert.That(shifted.RightFootY, Is.EqualTo(-2.08d).Within(0.0001d));
            Assert.That(peak.RightFootY, Is.GreaterThan(-1.7d));
            Assert.That(peak.RightShoe, Is.EqualTo(-10d).Within(0.01d));
            Assert.That(peak.LeftFootPlanted, Is.True);
            Assert.That(peak.RightFootPlanted, Is.False);
            Assert.That(landed.RightFootPlanted, Is.True);
            Assert.That(landed.RootX, Is.EqualTo(-0.18d).Within(0.0001d));
            Assert.That(exit.RootX, Is.Zero.Within(0.0001d));
        }

        [Test]
        public void P2_SupportSoleStaysLockedAndRaisedFootLandsBeforePelvisReturns()
        {
            var root = new GameObject("V2 P2 Support Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2RaisedKneeHookedFoot(0f);
            Vector3 support = presenter.LeftAnklePosition;
            float previousRightY = presenter.RightAnklePosition.y;
            for (int i = 0; i <= 240; i++)
            {
                float progress = i / 240f;
                PuppetV2Pose pose = RaisedKneeHookedFootChoreography.Evaluate(progress);
                presenter.PreviewV2RaisedKneeHookedFoot(progress);
                Assert.That(Vector3.Distance(support, presenter.LeftAnklePosition), Is.LessThan(0.002f));
                if (!pose.RightFootPlanted)
                    Assert.That(presenter.PelvisPosition.x, Is.LessThan(-0.17f));
                if (progress >= 0.85f && progress < 1f)
                    Assert.That(presenter.RightAnklePosition.y, Is.EqualTo(-2.08f).Within(0.003f));
                Assert.That(Mathf.Abs(presenter.RightAnklePosition.y - previousRightY), Is.LessThan(0.025f));
                previousRightY = presenter.RightAnklePosition.y;
            }
            presenter.PreviewV2RaisedKneeHookedFoot(0.65f);
            Assert.That(presenter.RightAnklePosition.y, Is.GreaterThan(-1.7f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.RightAnkleRotation), Is.EqualTo(-10f).Within(0.1f));
            presenter.PreviewV2RaisedKneeHookedFoot(1f);
            Assert.That(presenter.RightAnklePosition.y, Is.EqualTo(-2.08f).Within(0.003f));
            Assert.That(presenter.PelvisPosition.x, Is.Zero.Within(0.001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P2_RendersAcceptanceStagesAndAnimationFrames()
        {
            var root = new GameObject("V2 P2 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P2-RaisedKneeHookedFoot");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P2 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.45f, 0.65f, 0.85f, 1f };
            string[] names = { "01-start", "02-weight-shift", "03-knee-rise", "04-peak", "05-landed", "06-exit" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2RaisedKneeHookedFoot(stages[i]);
                string path = Path.Combine(directory, names[i] + ".png");
                SaveFrame(camera, target, capture, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(20000));
            }
            for (int i = 0; i <= 48; i++)
            {
                presenter.PreviewV2RaisedKneeHookedFoot(i / 48f);
                SaveFrame(camera, target, capture, Path.Combine(frames, $"frame-{i:000}.png"));
            }
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
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
