using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2VeiledFaceLamentTests
    {
        [Test]
        public void P5_11_GathersHandsBeforeLoweringHeadAndUsesOneShoulderSigh()
        {
            Assert.That(VeiledFaceLamentChoreography.Name, Is.EqualTo("掩面低诉"));
            Assert.That(VeiledFaceLamentChoreography.Beats, Is.EqualTo(12));
            PuppetV2Pose start = VeiledFaceLamentChoreography.Evaluate(0d);
            PuppetV2Pose handsNear = VeiledFaceLamentChoreography.Evaluate(0.4167d);
            PuppetV2Pose closed = VeiledFaceLamentChoreography.Evaluate(0.6667d);
            PuppetV2Pose sigh = VeiledFaceLamentChoreography.Evaluate(0.75d);
            PuppetV2Pose recovery = VeiledFaceLamentChoreography.Evaluate(0.8333d);
            PuppetV2Pose finish = VeiledFaceLamentChoreography.Evaluate(1d);

            Assert.That(handsNear.LeftHandY, Is.GreaterThan(0.75d));
            Assert.That(handsNear.RightHandY, Is.GreaterThan(0.84d));
            Assert.That(handsNear.Head, Is.GreaterThan(closed.Head));
            Assert.That(closed.Torso, Is.EqualTo(-11d).Within(0.01d));
            Assert.That(closed.RootY, Is.LessThan(start.RootY - 0.06d));
            Assert.That(sigh.RootY, Is.LessThan(closed.RootY));
            Assert.That(recovery.RootY, Is.GreaterThan(sigh.RootY));
            Assert.That(finish.LeftHandY, Is.InRange(0.35d, 0.41d));
            Assert.That(finish.RightHandY, Is.InRange(0.41d, 0.47d));
            Assert.That(finish.Head, Is.LessThan(0d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_11_HandsFrameLowerFaceWithoutCrossingAndFeetStayLocked()
        {
            DestroyNamed("V2 P5-11 Continuity Test");
            var root = new GameObject("V2 P5-11 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2VeiledFaceLament(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 600; i++)
            {
                float progress = i / 600f;
                presenter.PreviewV2VeiledFaceLament(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                Assert.That(presenter.V2LeftHandTarget.x, Is.LessThan(presenter.V2RightHandTarget.x - 0.45f));
                Assert.That(presenter.RightElbowPosition.y, Is.LessThan(presenter.RightWristPosition.y),
                    $"right elbow must stay below the hand at {progress:F4}");
                Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(presenter.RightShoulderPosition.x),
                    $"right elbow must stay outside the shoulder at {progress:F4}");
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
            presenter.PreviewV2VeiledFaceLament(0.6667f);
            Assert.That(presenter.LeftWristPosition.y, Is.InRange(0.88f, 0.94f));
            Assert.That(presenter.RightWristPosition.y, Is.InRange(0.93f, 0.99f));
            Assert.That(presenter.LeftElbowPosition.x, Is.GreaterThan(0.55f));
            Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(presenter.RightShoulderPosition.x + 0.70f));
            Assert.That(presenter.LeftElbowPosition.y, Is.LessThan(0.72f));
            Assert.That(presenter.RightElbowPosition.y, Is.LessThan(presenter.RightWristPosition.y - 0.25f));
            presenter.PreviewV2VeiledFaceLament(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_11_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-11 Visual Test");
            DestroyNamed("V2 P5-11 Camera");
            var root = new GameObject("V2 P5-11 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-11-VeiledFaceLament");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-11 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.4167f, 0.6667f, 0.75f, 0.8333f, 1f };
            string[] names = { "01-half-close", "02-hands-rise", "03-lower-face-frame", "04-head-chest-close", "05-single-shoulder-sigh", "06-sigh-release", "07-chest-finish" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2VeiledFaceLament(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2VeiledFaceLament(i / 80f);
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
