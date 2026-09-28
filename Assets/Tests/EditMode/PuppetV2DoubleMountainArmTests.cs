using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2DoubleMountainArmTests
    {
        [Test]
        public void P1_FullPoseHasNamedEightBeatFiveStageTrack()
        {
            Assert.That(DoubleMountainArmChoreography.Name, Is.EqualTo("双展山膀"));
            Assert.That(DoubleMountainArmChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = DoubleMountainArmChoreography.Evaluate(0d);
            PuppetV2Pose peak = DoubleMountainArmChoreography.Evaluate(0.75d);
            PuppetV2Pose exit = DoubleMountainArmChoreography.Evaluate(1d);
            Assert.That(start.LeftHandX, Is.EqualTo(-0.34d).Within(0.0001d));
            Assert.That(peak.LeftHandX, Is.LessThan(-1.6d));
            Assert.That(peak.RightHandX, Is.GreaterThan(1.6d));
            Assert.That(exit.LeftHandX, Is.LessThan(-1.4d));
            Assert.That(exit.LeftHandShape, Is.EqualTo(PuppetHandShape.NaturalPalm));
            Assert.That(exit.RightHandShape, Is.EqualTo(PuppetHandShape.NaturalPalm));
            Assert.That(exit.LeftFootY, Is.EqualTo(-2.08d));
            Assert.That(exit.RightFootY, Is.EqualTo(-2.08d));
        }

        [Test]
        public void P1_TwoBoneSolverReachesBothTargetsWithoutStretchingOrCrossingSides()
        {
            var root = new GameObject("V2 P1 Solver Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            float previousLeftShoulder = presenter.LeftUpperArmRotation;
            float previousRightShoulder = presenter.RightUpperArmRotation;
            for (int i = 0; i <= 240; i++)
            {
                presenter.PreviewV2DoubleMountainArm(i / 240f);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget), Is.LessThan(0.003f));
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget), Is.LessThan(0.003f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousLeftShoulder, presenter.LeftUpperArmRotation)), Is.LessThan(4f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousRightShoulder, presenter.RightUpperArmRotation)), Is.LessThan(4f));
                Assert.That(presenter.LeftWristPosition.x, Is.LessThan(0f));
                Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(0f));
                previousLeftShoulder = presenter.LeftUpperArmRotation;
                previousRightShoulder = presenter.RightUpperArmRotation;
            }
            presenter.PreviewV2DoubleMountainArm(0.75f);
            Assert.That(presenter.LeftWristPosition.x, Is.LessThan(-1.6f));
            Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(1.6f));
            Assert.That(Mathf.Abs(presenter.LeftWristPosition.y - presenter.RightWristPosition.y), Is.LessThan(0.01f));
            Assert.That(presenter.LeftAnklePosition.y, Is.EqualTo(-2.08f).Within(0.025f));
            Assert.That(presenter.RightAnklePosition.y, Is.EqualTo(-2.08f).Within(0.025f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftThighRotation), Is.LessThan(0f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.RightThighRotation), Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.LeftThighRotation)), Is.LessThan(25f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.RightThighRotation)), Is.LessThan(25f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P1_RendersFiveAcceptanceStagesAndAnimationFrames()
        {
            var root = new GameObject("V2 P1 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P1-DoubleMountainArm");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P1 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.625f, 0.75f, 1f };
            string[] names = { "01-start", "02-gather", "03-expand", "04-peak", "05-exit" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2DoubleMountainArm(stages[i]);
                string path = Path.Combine(directory, names[i] + ".png");
                SaveFrame(camera, target, capture, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(20000));
            }
            for (int i = 0; i <= 48; i++)
            {
                presenter.PreviewV2DoubleMountainArm(i / 48f);
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
