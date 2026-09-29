using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2SlowEntranceTests
    {
        [Test]
        public void P5_01_HasNamedTwelveBeatEightStepTrackAndSettles()
        {
            Assert.That(SlowEntranceChoreography.Name, Is.EqualTo("缓步入场"));
            Assert.That(SlowEntranceChoreography.Beats, Is.EqualTo(12));
            PuppetV2Pose start = SlowEntranceChoreography.Evaluate(0d);
            PuppetV2Pose look = SlowEntranceChoreography.Evaluate(2d / 12d);
            PuppetV2Pose end = SlowEntranceChoreography.Evaluate(1d);
            Assert.That(end.RootX - start.RootX, Is.EqualTo(1.25d).Within(0.0001d));
            Assert.That(start.Head, Is.EqualTo(6d).Within(0.0001d));
            Assert.That(look.Head, Is.EqualTo(0d).Within(0.0001d));
            Assert.That(end.LeftFootPlanted, Is.True);
            Assert.That(end.RightFootPlanted, Is.True);
            Assert.That(end.LeftFootY, Is.EqualTo(SlowEntranceChoreography.GroundY).Within(0.0001d));
            Assert.That(end.RightFootY, Is.EqualTo(SlowEntranceChoreography.GroundY).Within(0.0001d));
            Assert.That((end.LeftFootX + end.RightFootX) * 0.5d,
                Is.EqualTo(SlowEntranceChoreography.EndX).Within(0.0001d));
            Assert.That(end.LeftWrist, Is.EqualTo(0d).Within(0.0001d));
            Assert.That(end.RightWrist, Is.EqualTo(0d).Within(0.0001d));
        }

        [Test]
        public void P5_01_SupportFeetStayLockedAndBodyStopsWithoutSliding()
        {
            var root = new GameObject("V2 P5-01 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            float previousRootX = float.NegativeInfinity;
            Vector3 previousLeft = Vector3.zero;
            Vector3 previousRight = Vector3.zero;
            PuppetV2Pose previousPose = SlowEntranceChoreography.Evaluate(0d);
            for (int i = 0; i <= 480; i++)
            {
                float progress = i / 480f;
                PuppetV2Pose pose = SlowEntranceChoreography.Evaluate(progress);
                presenter.PreviewV2SlowEntrance(progress);
                Assert.That(presenter.PelvisPosition.x, Is.GreaterThanOrEqualTo(previousRootX - 0.0001f));
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.003f), $"left wrist at progress {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.003f), $"right wrist at progress {progress:F4}");
                if (i > 0 && pose.LeftFootPlanted && previousPose.LeftFootPlanted)
                    Assert.That(Vector3.Distance(presenter.LeftAnklePosition, previousLeft), Is.LessThan(0.0015f));
                if (i > 0 && pose.RightFootPlanted && previousPose.RightFootPlanted)
                    Assert.That(Vector3.Distance(presenter.RightAnklePosition, previousRight), Is.LessThan(0.0015f));
                Assert.That(presenter.GetRodDrive(3),
                    Is.EqualTo(pose.LeftFootPlanted ? 0f : 1f).Within(0.0001f));
                Assert.That(presenter.GetRodDrive(5),
                    Is.EqualTo(pose.RightFootPlanted ? 0f : 1f).Within(0.0001f));
                previousRootX = presenter.PelvisPosition.x;
                previousLeft = presenter.LeftAnklePosition;
                previousRight = presenter.RightAnklePosition;
                previousPose = pose;
            }
            presenter.PreviewV2SlowEntrance(10f / 12f);
            Vector3 settledPelvis = presenter.PelvisPosition;
            Vector3 settledLeft = presenter.LeftAnklePosition;
            Vector3 settledRight = presenter.RightAnklePosition;
            presenter.PreviewV2SlowEntrance(1f);
            Assert.That(Vector3.Distance(presenter.PelvisPosition, settledPelvis), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(presenter.LeftAnklePosition, settledLeft), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(presenter.RightAnklePosition, settledRight), Is.LessThan(0.0001f));
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.EqualTo(0f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_01_RendersFiveAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-01 Visual Test");
            DestroyNamed("V2 P5-01 Camera");
            var root = new GameObject("V2 P5-01 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-01-SlowEntrance");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-01 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 2f / 12f, 0.54f, 0.79f, 1f };
            string[] names = { "01-curtain", "02-look", "03-walk", "04-slow", "05-settle" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2SlowEntrance(stages[i]);
                string path = Path.Combine(directory, names[i] + ".png");
                SaveFrame(camera, target, capture, path);
            }
            for (int i = 0; i <= 72; i++)
            {
                presenter.PreviewV2SlowEntrance(i / 72f);
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
