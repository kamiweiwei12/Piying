using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2WindFlagDiagonalTests
    {
        [Test]
        public void P5_07_BuildsAHighRightLowLeftDiagonalWithoutChangingFacing()
        {
            Assert.That(WindFlagDiagonalChoreography.Name, Is.EqualTo("斜展顺风旗"));
            Assert.That(WindFlagDiagonalChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = WindFlagDiagonalChoreography.Evaluate(0d);
            PuppetV2Pose peak = WindFlagDiagonalChoreography.Evaluate(0.85d);
            PuppetV2Pose finish = WindFlagDiagonalChoreography.Evaluate(1d);

            Assert.That(peak.LeftHandX, Is.LessThan(-1.60d));
            Assert.That(peak.RightHandX, Is.GreaterThan(1.15d));
            Assert.That(peak.RightHandY - peak.LeftHandY, Is.GreaterThan(1.20d));
            Assert.That(peak.Head, Is.EqualTo(8d).Within(0.001d));
            Assert.That(peak.Facing, Is.EqualTo(1d));
            Assert.That(finish.RightHandY, Is.GreaterThan(1.65d));
            Assert.That(finish.LeftHandX, Is.LessThan(-1.50d));
            Assert.That(finish.RightHandY - start.RightHandY, Is.GreaterThan(1.75d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_07_ContinuousDiagonalKeepsFeetLockedAndElbowsOnOuterBranches()
        {
            DestroyNamed("V2 P5-07 Continuity Test");
            var root = new GameObject("V2 P5-07 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2WindFlagDiagonal(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2WindFlagDiagonal(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.0031f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.0031f), $"right wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.0001f));
                Assert.That(presenter.PelvisPosition.x, Is.Zero.Within(0.0001f));
                maximumLeftElbowStep = Mathf.Max(maximumLeftElbowStep,
                    Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow));
                maximumRightElbowStep = Mathf.Max(maximumRightElbowStep,
                    Vector3.Distance(presenter.RightElbowPosition, previousRightElbow));
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            Assert.That(maximumLeftElbowStep, Is.LessThan(0.08f));
            Assert.That(maximumRightElbowStep, Is.LessThan(0.08f));
            presenter.PreviewV2WindFlagDiagonal(0.85f);
            Assert.That(presenter.LeftElbowPosition.x, Is.LessThan(-0.45f));
            Assert.That(presenter.RightElbowPosition.x, Is.GreaterThan(0.55f));
            Assert.That(presenter.RightWristPosition.y - presenter.LeftWristPosition.y, Is.GreaterThan(1.20f));
            float leftReach = Vector3.Distance(presenter.LeftShoulderPosition, presenter.LeftWristPosition);
            float rightReach = Vector3.Distance(presenter.RightShoulderPosition, presenter.RightWristPosition);
            Assert.That(leftReach, Is.GreaterThan(1.51f), $"left reach {leftReach:F6}");
            Assert.That(rightReach, Is.GreaterThan(1.51f), $"right reach {rightReach:F6}");
            Assert.That(PuppetRigV2Calibration.ArmMaximumReach - leftReach, Is.LessThan(0.03f));
            Assert.That(PuppetRigV2Calibration.ArmMaximumReach - rightReach, Is.LessThan(0.03f));
            presenter.PreviewV2WindFlagDiagonal(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_07_RendersSixAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-07 Visual Test");
            DestroyNamed("V2 P5-07 Camera");
            var root = new GameObject("V2 P5-07 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-07-WindFlagDiagonal");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-07 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.20f, 0.45f, 0.68f, 0.85f, 1f };
            string[] names = { "01-half-close", "02-open-sides", "03-diagonal-rise", "04-flag-formed", "05-peak", "06-exit" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2WindFlagDiagonal(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2WindFlagDiagonal(i / 80f);
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
