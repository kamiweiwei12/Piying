using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2BowingSaluteTests
    {
        [Test]
        public void P5_03_CombinesChestSaluteWithAFullBodyBow()
        {
            Assert.That(BowingSaluteChoreography.Name, Is.EqualTo("拱手致礼"));
            Assert.That(BowingSaluteChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = BowingSaluteChoreography.Evaluate(0d);
            PuppetV2Pose joined = BowingSaluteChoreography.Evaluate(0.48d);
            PuppetV2Pose bowed = BowingSaluteChoreography.Evaluate(0.75d);
            PuppetV2Pose end = BowingSaluteChoreography.Evaluate(1d);
            Assert.That(joined.RightHandX - joined.LeftHandX, Is.InRange(0.44d, 0.50d));
            Assert.That(joined.LeftHandX, Is.LessThanOrEqualTo(-0.20d));
            Assert.That(joined.RightHandX, Is.GreaterThanOrEqualTo(0.20d));
            Assert.That(joined.LeftHandY, Is.GreaterThan(0.30d));
            Assert.That(joined.LeftHandShape, Is.EqualTo(PuppetHandShape.SupportPalm));
            Assert.That(joined.RightHandShape, Is.EqualTo(PuppetHandShape.ClosedPalm));
            Assert.That(bowed.Torso, Is.LessThan(-12d));
            Assert.That(bowed.Head, Is.LessThan(joined.Head));
            Assert.That(bowed.RootY, Is.LessThan(joined.RootY));
            Assert.That(bowed.RightHandX - bowed.LeftHandX, Is.InRange(0.44d, 0.50d));
            Assert.That(end.LeftHandY, Is.GreaterThan(start.LeftHandY + 0.70d));
            Assert.That(end.Torso, Is.Zero.Within(0.0001d));
            Assert.That(end.RootX, Is.Zero.Within(0.0001d));
            Assert.That(end.LeftFootPlanted && end.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_03_LeftAndRightArmsKeepTheirOwnSidesDuringBowAndBothFeetRemainLocked()
        {
            DestroyNamed("V2 P5-03 Continuity Test");
            var root = new GameObject("V2 P5-03 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2BowingSalute(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftTarget = presenter.V2LeftHandTarget;
            Vector3 previousRightTarget = presenter.V2RightHandTarget;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2BowingSalute(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.003f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.003f), $"right wrist at {progress:F4}");
                Assert.That(presenter.LeftWristPosition.x, Is.LessThan(-0.16f), $"left wrist crossed at {progress:F4}");
                Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(0.16f), $"right wrist crossed at {progress:F4}");
                Assert.That(presenter.LeftElbowPosition.x, Is.LessThan(-0.34f), $"left elbow crossed at {progress:F4}");
                Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(0.30f), $"right elbow crossed at {progress:F4}");
                // 躬身下沉由腿部 IK 吸收；允许求解器的小数残差，但不允许形成可见脚滑。
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.002f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.002f));
                if (progress >= 0.48f && progress <= 0.88f)
                    Assert.That(presenter.V2RightHandTarget.x - presenter.V2LeftHandTarget.x,
                        Is.InRange(0.43f, 0.50f), $"joined hands at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.V2LeftHandTarget, previousLeftTarget), Is.LessThan(0.026f));
                Assert.That(Vector3.Distance(presenter.V2RightHandTarget, previousRightTarget), Is.LessThan(0.026f));
                previousLeftTarget = presenter.V2LeftHandTarget;
                previousRightTarget = presenter.V2RightHandTarget;
            }
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.EqualTo(0f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_03_RendersSixAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-03 Visual Test");
            DestroyNamed("V2 P5-03 Camera");
            DestroyNamed("V2 P5-03 Continuity Test");
            var root = new GameObject("V2 P5-03 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-03-BowingSalute");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-03 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.32f, 0.48f, 0.62f, 0.75f, 1f };
            string[] names = { "01-ready", "02-hands-approach", "03-salute-joined", "04-bow-descend", "05-bow-peak", "06-rise-half-close" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2BowingSalute(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2BowingSalute(i / 80f);
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
