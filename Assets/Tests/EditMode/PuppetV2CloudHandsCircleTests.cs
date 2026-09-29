using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2CloudHandsCircleTests
    {
        [Test]
        public void P5_04_DrawsStaggeredUpperAndLowerArcsBeforeOpenFinish()
        {
            Assert.That(CloudHandsCircleChoreography.Name, Is.EqualTo("云手展圆"));
            Assert.That(CloudHandsCircleChoreography.Beats, Is.EqualTo(12));
            PuppetV2Pose start = CloudHandsCircleChoreography.Evaluate(0d);
            PuppetV2Pose upperOutside = CloudHandsCircleChoreography.Evaluate(0.46d);
            PuppetV2Pose circlePeak = CloudHandsCircleChoreography.Evaluate(0.72d);
            PuppetV2Pose finish = CloudHandsCircleChoreography.Evaluate(1d);

            Assert.That(upperOutside.LeftHandX, Is.LessThan(-1.1d));
            Assert.That(upperOutside.LeftHandY, Is.GreaterThan(1d));
            Assert.That(circlePeak.LeftHandY, Is.GreaterThan(1.68d));
            Assert.That(circlePeak.RightHandY, Is.LessThan(-0.58d));
            Assert.That(circlePeak.LeftWrist, Is.LessThan(-30d));
            Assert.That(circlePeak.LeftHandShape, Is.EqualTo(PuppetHandShape.SupportPalm));
            Assert.That(finish.LeftHandY - finish.RightHandY, Is.GreaterThan(0.75d));
            Assert.That(finish.LeftHandX, Is.LessThan(-1d));
            Assert.That(finish.RightHandY, Is.GreaterThan(start.RightHandY + 0.30d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_04_ContinuousCircularTargetsKeepBothFeetLocked()
        {
            DestroyNamed("V2 P5-04 Continuity Test");
            var root = new GameObject("V2 P5-04 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2CloudHandsCircle(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftTarget = presenter.V2LeftHandTarget;
            Vector3 previousRightTarget = presenter.V2RightHandTarget;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            for (int i = 1; i <= 720; i++)
            {
                float progress = i / 720f;
                presenter.PreviewV2CloudHandsCircle(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.001f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.001f), $"right wrist at {progress:F4}");
                // 横向移重心由腿部 IK 吸收；容许亚像素求解残差，不允许形成可见脚滑。
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.0041f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.0041f));
                Assert.That(Mathf.Abs(presenter.PelvisPosition.x), Is.LessThanOrEqualTo(0.041f));
                Assert.That(Vector3.Distance(presenter.V2LeftHandTarget, previousLeftTarget), Is.LessThan(0.040f));
                Assert.That(Vector3.Distance(presenter.V2RightHandTarget, previousRightTarget), Is.LessThan(0.040f));
                Assert.That(Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow), Is.LessThan(0.12f),
                    $"left elbow branch jump at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightElbowPosition, previousRightElbow), Is.LessThan(0.12f),
                    $"right elbow branch jump at {progress:F4}");
                previousLeftTarget = presenter.V2LeftHandTarget;
                previousRightTarget = presenter.V2RightHandTarget;
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.EqualTo(0f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_04_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-04 Visual Test");
            DestroyNamed("V2 P5-04 Camera");
            DestroyNamed("V2 P5-04 Continuity Test");
            var root = new GameObject("V2 P5-04 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-04-CloudHandsCircle");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-04 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.20f, 0.46f, 0.60f, 0.72f, 0.84f, 1f };
            string[] names = { "01-half-close", "02-gather", "03-upper-outer-arc", "04-lower-arc", "05-circle-peak", "06-connect-circle", "07-open-finish" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2CloudHandsCircle(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 96; i++)
            {
                presenter.PreviewV2CloudHandsCircle(i / 96f);
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
