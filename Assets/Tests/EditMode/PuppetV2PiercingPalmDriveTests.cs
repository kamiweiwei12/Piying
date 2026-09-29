using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2PiercingPalmDriveTests
    {
        [Test]
        public void P5_08_FootLeadsBeforeBodyAndPalmDrive()
        {
            Assert.That(PiercingPalmDriveChoreography.Name, Is.EqualTo("穿掌送势"));
            Assert.That(PiercingPalmDriveChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = PiercingPalmDriveChoreography.Evaluate(0d);
            PuppetV2Pose lifted = PiercingPalmDriveChoreography.Evaluate(0.12d);
            PuppetV2Pose planted = PiercingPalmDriveChoreography.Evaluate(0.24d);
            PuppetV2Pose peak = PiercingPalmDriveChoreography.Evaluate(0.68d);
            PuppetV2Pose finish = PiercingPalmDriveChoreography.Evaluate(1d);

            Assert.That(lifted.RootX, Is.Zero.Within(0.001d));
            Assert.That(lifted.RightFootX, Is.GreaterThan(start.RightFootX + 0.25d));
            Assert.That(lifted.RightFootY, Is.GreaterThan(start.RightFootY + 0.05d));
            Assert.That(lifted.RightFootPlanted, Is.False);
            Assert.That(planted.RootX, Is.Zero.Within(0.001d));
            Assert.That(planted.RightFootY, Is.EqualTo(start.RightFootY).Within(0.001d));
            Assert.That(planted.RightFootPlanted, Is.True);
            Assert.That(planted.RightFootX - planted.LeftFootX, Is.GreaterThan(0.92d));
            Assert.That(peak.RootX, Is.EqualTo(0.16d).Within(0.001d));
            Assert.That(peak.RightHandX, Is.GreaterThan(1.87d));
            Assert.That(peak.RightHandY, Is.EqualTo(0.67d).Within(0.001d));
            Assert.That(peak.LeftHandY, Is.LessThan(0d));
            Assert.That(planted.RightWrist, Is.EqualTo(40d).Within(0.001d));
            Assert.That(peak.RightWrist, Is.EqualTo(65d).Within(0.001d));
            Assert.That(finish.RightHandX, Is.EqualTo(1.48d).Within(0.001d));
            Assert.That(finish.RightWrist, Is.EqualTo(32d).Within(0.001d));
            Assert.That(peak.RightHandX - finish.RightHandX, Is.InRange(0.39d, 0.48d));
            Assert.That(finish.RightFootX, Is.EqualTo(planted.RightFootX).Within(0.001d));
            Assert.That(finish.Facing, Is.EqualTo(1d));
        }

        [Test]
        public void P5_08_StraightDriveKeepsSupportFootAndLandedFrontFootStable()
        {
            DestroyNamed("V2 P5-08 Continuity Test");
            var root = new GameObject("V2 P5-08 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Transform rightHandArt = root.GetComponentsInChildren<Transform>(true)
                .First(transform => transform.name == "Art Right Hand");
            presenter.PreviewV2PiercingPalmDrive(0f);
            Vector3 supportFoot = presenter.LeftAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            Vector3 plantedFrontFoot = Vector3.zero;
            float previousDriveX = float.NegativeInfinity;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2PiercingPalmDrive(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, supportFoot), Is.LessThan(0.0001f));
                if (i == 116) plantedFrontFoot = presenter.RightAnklePosition;
                if (i >= 116)
                    Assert.That(Vector3.Distance(presenter.RightAnklePosition, plantedFrontFoot),
                        Is.LessThan(0.0001f), $"planted front foot at {progress:F4}");
                if (progress < 0.24f)
                    Assert.That(presenter.PelvisPosition.x, Is.Zero.Within(0.0001f));
                if (progress >= 0.24f)
                {
                    float uprightAngle = Mathf.DeltaAngle(0f, rightHandArt.eulerAngles.z);
                    Assert.That(uprightAngle, Is.InRange(65f, 82f),
                        $"upright palm at {progress:F4}");
                }
                if (progress >= 0.24f && progress <= 0.68f)
                {
                    Assert.That(presenter.RightWristPosition.x + 0.0001f, Is.GreaterThanOrEqualTo(previousDriveX),
                        $"straight drive must not reverse at {progress:F4}");
                    previousDriveX = presenter.RightWristPosition.x;
                }
                maximumLeftElbowStep = Mathf.Max(maximumLeftElbowStep,
                    Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow));
                maximumRightElbowStep = Mathf.Max(maximumRightElbowStep,
                    Vector3.Distance(presenter.RightElbowPosition, previousRightElbow));
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            Assert.That(maximumLeftElbowStep, Is.LessThan(0.08f));
            Assert.That(maximumRightElbowStep, Is.LessThan(0.08f));
            presenter.PreviewV2PiercingPalmDrive(0.68f);
            Assert.That(Vector3.Distance(presenter.RightShoulderPosition, presenter.RightWristPosition),
                Is.GreaterThan(1.50f));
            Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(1.87f));
            Assert.That(presenter.LeftWristPosition.y, Is.LessThan(0f));
            presenter.PreviewV2PiercingPalmDrive(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_08_RendersSixAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-08 Visual Test");
            DestroyNamed("V2 P5-08 Camera");
            var root = new GameObject("V2 P5-08 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-08-PiercingPalmDrive");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-08 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.12f, 0.24f, 0.48f, 0.68f, 1f };
            string[] names = { "01-chest-gather", "02-front-foot-lead", "03-foot-planted", "04-palm-drive", "05-full-extension", "06-one-third-return" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2PiercingPalmDrive(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2PiercingPalmDrive(i / 80f);
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
