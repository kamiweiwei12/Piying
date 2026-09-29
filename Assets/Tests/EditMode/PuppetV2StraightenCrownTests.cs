using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2StraightenCrownTests
    {
        [Test]
        public void P5_02_HasEightBeatSideApproachAndOneWristAdjustment()
        {
            Assert.That(StraightenCrownChoreography.Name, Is.EqualTo("整冠肃立"));
            Assert.That(StraightenCrownChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = StraightenCrownChoreography.Evaluate(0d);
            PuppetV2Pose elbows = StraightenCrownChoreography.Evaluate(0.25d);
            PuppetV2Pose contact = StraightenCrownChoreography.Evaluate(0.50d);
            PuppetV2Pose adjust = StraightenCrownChoreography.Evaluate(0.625d);
            PuppetV2Pose end = StraightenCrownChoreography.Evaluate(1d);
            Assert.That(elbows.LeftHandX, Is.LessThan(start.LeftHandX));
            Assert.That(elbows.RightHandX, Is.GreaterThan(start.RightHandX));
            Assert.That(contact.LeftHandX, Is.LessThanOrEqualTo(-0.47d));
            Assert.That(contact.RightHandX, Is.GreaterThanOrEqualTo(0.47d));
            Assert.That(contact.LeftHandY, Is.GreaterThan(1.60d));
            Assert.That(contact.RightHandY, Is.GreaterThan(1.60d));
            Assert.That(adjust.LeftWrist, Is.GreaterThan(contact.LeftWrist));
            Assert.That(adjust.RightWrist, Is.LessThan(contact.RightWrist));
            Assert.That(start.Head, Is.EqualTo(8d).Within(0.0001d));
            Assert.That(end.Head, Is.EqualTo(2d).Within(0.0001d));
            Assert.That(end.RootX, Is.EqualTo(start.RootX).Within(0.0001d));
            Assert.That(end.LeftFootPlanted && end.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_02_HandsStayOutsideFaceAndFeetRemainLocked()
        {
            DestroyNamed("V2 P5-02 Continuity Test");
            var root = new GameObject("V2 P5-02 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2StraightenCrown(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftTarget = presenter.V2LeftHandTarget;
            Vector3 previousRightTarget = presenter.V2RightHandTarget;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2StraightenCrown(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.003f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.003f), $"right wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.0015f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.0015f));
                Assert.That(presenter.V2LeftHandTarget.x, Is.LessThan(-0.46f));
                Assert.That(presenter.V2RightHandTarget.x, Is.GreaterThan(0.46f));
                Assert.That(Vector3.Distance(presenter.V2LeftHandTarget, previousLeftTarget), Is.LessThan(0.025f));
                Assert.That(Vector3.Distance(presenter.V2RightHandTarget, previousRightTarget), Is.LessThan(0.025f));
                previousLeftTarget = presenter.V2LeftHandTarget;
                previousRightTarget = presenter.V2RightHandTarget;
            }
            Assert.That(presenter.PelvisPosition.x, Is.EqualTo(0f).Within(0.0001f));
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.EqualTo(0f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_02_RendersFiveAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-02 Visual Test");
            DestroyNamed("V2 P5-02 Camera");
            DestroyNamed("V2 P5-02 Continuity Test");
            var root = new GameObject("V2 P5-02 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-02-StraightenCrown");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-02 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.50f, 0.625f, 1f };
            string[] names = { "01-stand", "02-elbows-rise", "03-crown-contact", "04-wrist-adjust", "05-settle" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2StraightenCrown(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 64; i++)
            {
                presenter.PreviewV2StraightenCrown(i / 64f);
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
