using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2MoonOfferingGazeTests
    {
        [Test]
        public void P5_05_RaisesOneSupportingPalmThenLooksUp()
        {
            Assert.That(MoonOfferingGazeChoreography.Name, Is.EqualTo("托月仰望"));
            Assert.That(MoonOfferingGazeChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = MoonOfferingGazeChoreography.Evaluate(0d);
            PuppetV2Pose raised = MoonOfferingGazeChoreography.Evaluate(0.375d);
            PuppetV2Pose palm = MoonOfferingGazeChoreography.Evaluate(0.50d);
            PuppetV2Pose gaze = MoonOfferingGazeChoreography.Evaluate(0.72d);
            PuppetV2Pose finish = MoonOfferingGazeChoreography.Evaluate(1d);

            Assert.That(raised.RightHandY, Is.GreaterThan(1.25d));
            Assert.That(raised.RightHandY, Is.GreaterThan(raised.LeftHandY + 1.35d));
            Assert.That(palm.RightWrist, Is.EqualTo(45d).Within(0.001d));
            Assert.That(palm.RightHandShape, Is.EqualTo(PuppetHandShape.SupportPalm));
            Assert.That(gaze.Head, Is.EqualTo(24d).Within(0.001d));
            Assert.That(gaze.RightHandY, Is.GreaterThan(1.70d));
            Assert.That(gaze.LeftHandY, Is.LessThan(-0.25d));
            Assert.That(finish.RightHandY, Is.InRange(1.50d, 1.60d));
            Assert.That(finish.RightHandY, Is.GreaterThan(start.RightHandY + 1d));
            Assert.That(finish.LeftHandY, Is.LessThan(0d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_05_ContinuousLiftKeepsFeetAndPelvisLocked()
        {
            DestroyNamed("V2 P5-05 Continuity Test");
            var root = new GameObject("V2 P5-05 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2MoonOfferingGaze(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftTarget = presenter.V2LeftHandTarget;
            Vector3 previousRightTarget = presenter.V2RightHandTarget;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2MoonOfferingGaze(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.0001f));
                Assert.That(presenter.PelvisPosition.x, Is.Zero.Within(0.0001f));
                Assert.That(Vector3.Distance(presenter.V2LeftHandTarget, previousLeftTarget), Is.LessThan(0.025f));
                Assert.That(Vector3.Distance(presenter.V2RightHandTarget, previousRightTarget), Is.LessThan(0.025f));
                maximumLeftElbowStep = Mathf.Max(maximumLeftElbowStep,
                    Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow));
                maximumRightElbowStep = Mathf.Max(maximumRightElbowStep,
                    Vector3.Distance(presenter.RightElbowPosition, previousRightElbow));
                previousLeftTarget = presenter.V2LeftHandTarget;
                previousRightTarget = presenter.V2RightHandTarget;
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            Assert.That(maximumLeftElbowStep, Is.LessThan(0.080f),
                $"maximum left elbow step {maximumLeftElbowStep:F6}");
            Assert.That(maximumRightElbowStep, Is.LessThan(0.080f),
                $"maximum right elbow step {maximumRightElbowStep:F6}");
            presenter.PreviewV2MoonOfferingGaze(0.72f);
            Assert.That(presenter.HeadRotation, Is.EqualTo(24f).Within(0.01f));
            Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(0.60f));
            Assert.That(presenter.RightElbowPosition.y, Is.GreaterThan(1.35f));
            Assert.That(presenter.RightWristPosition.x - presenter.RightElbowPosition.x,
                Is.GreaterThan(0.50f));
            presenter.PreviewV2MoonOfferingGaze(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.EqualTo(0f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_05_RendersSixAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-05 Visual Test");
            DestroyNamed("V2 P5-05 Camera");
            DestroyNamed("V2 P5-05 Continuity Test");
            var root = new GameObject("V2 P5-05 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-05-MoonOfferingGaze");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-05 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.18f, 0.375f, 0.50f, 0.72f, 1f };
            string[] names = { "01-half-close", "02-diagonal-lift", "03-eye-line", "04-supporting-palm", "05-upward-gaze", "06-shoulder-high-finish" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2MoonOfferingGaze(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2MoonOfferingGaze(i / 80f);
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
