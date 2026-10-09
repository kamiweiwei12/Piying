using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2TurnBackSetPoseTests
    {
        [Test]
        public void P3_FullPoseClosesBeforeTwelveBeatTurnAndReopensReversed()
        {
            Assert.That(TurnBackSetPoseChoreography.Name, Is.EqualTo("回身定相"));
            Assert.That(TurnBackSetPoseChoreography.Beats, Is.EqualTo(12));
            PuppetV2Pose start = TurnBackSetPoseChoreography.Evaluate(0d);
            PuppetV2Pose edge = TurnBackSetPoseChoreography.Evaluate(0.5d);
            PuppetV2Pose exit = TurnBackSetPoseChoreography.Evaluate(1d);
            Assert.That(start.Facing, Is.EqualTo(1d).Within(0.0001d));
            Assert.That(edge.Facing, Is.Zero.Within(0.0001d));
            Assert.That(exit.Facing, Is.EqualTo(-1d).Within(0.0001d));
            Assert.That(System.Math.Abs(edge.LeftHandX), Is.LessThan(System.Math.Abs(start.LeftHandX) * 0.4d));
            Assert.That(System.Math.Abs(exit.LeftHandX), Is.EqualTo(System.Math.Abs(start.LeftHandX)).Within(0.0001d));
        }

        [Test]
        public void P3_TurnKeepsFeetLockedUsesShortEdgeWindowAndSwapsLayering()
        {
            var root = new GameObject("V2 P3 Turn Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2TurnBackSetPose(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 bodyStringStart = presenter.GetRodGripPosition(4);
            Transform stage = root.transform.Find("M6 Shadow Puppet Stage");
            SpriteRenderer leftArm = stage.Find("Joint Pelvis/Joint Waist/Joint Left Shoulder/Art Left Upper Arm")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer rightArm = stage.Find("Joint Pelvis/Joint Waist/Joint Right Shoulder/Art Right Upper Arm")
                .GetComponent<SpriteRenderer>();
            Assert.That(leftArm.sortingOrder, Is.GreaterThan(rightArm.sortingOrder));

            int narrowSamples = 0;
            float previousFacing = presenter.FacingScale;
            for (int i = 0; i <= 240; i++)
            {
                presenter.PreviewV2TurnBackSetPose(i / 240f);
                Assert.That(Vector3.Distance(leftFoot, presenter.LeftAnklePosition), Is.LessThan(0.002f));
                Assert.That(Vector3.Distance(rightFoot, presenter.RightAnklePosition), Is.LessThan(0.002f));
                Assert.That(Mathf.Abs(presenter.V2VisibleFacing), Is.GreaterThanOrEqualTo(0.099f));
                Assert.That(presenter.FacingScale, Is.LessThanOrEqualTo(previousFacing + 0.0001f));
                if (Mathf.Abs(presenter.V2VisibleFacing) < 0.5f) narrowSamples++;
                previousFacing = presenter.FacingScale;
            }
            Assert.That(narrowSamples, Is.LessThan(45));
            Assert.That(presenter.FacingScale, Is.LessThan(-0.99f));
            Assert.That(leftArm.sortingOrder, Is.LessThan(rightArm.sortingOrder));
            Vector3 bodyStringEnd = presenter.GetRodGripPosition(4);
            Assert.That(Vector3.Distance(bodyStringStart, bodyStringEnd), Is.LessThan(0.25f));
            presenter.PreviewV2TurnBackSetPose(0.5f);
            Assert.That(presenter.GetRodGripPosition(4).y, Is.GreaterThan(3f));
            Assert.That(presenter.GetRodSortingOrder(4), Is.GreaterThan(6));
            presenter.PreviewV2TurnBackSetPose(0f);
            Assert.That(presenter.GetRodSortingOrder(4), Is.GreaterThan(6));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P3_RendersTurnBoundaryFramesAndAnimation()
        {
            var root = new GameObject("V2 P3 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P3-TurnBackSetPose");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P3 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.42f, 0.46f, 0.50f, 0.54f, 0.58f, 0.75f, 1f };
            string[] names = { "01-start", "02-close", "03-turn-entry", "04-edge-before", "05-edge", "06-edge-after", "07-turn-exit", "08-reopen", "09-reversed" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2TurnBackSetPose(stages[i]);
                string path = Path.Combine(directory, names[i] + ".png");
                SaveFrame(camera, target, capture, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(20000));
            }
            for (int i = 0; i <= 60; i++)
            {
                presenter.PreviewV2TurnBackSetPose(i / 60f);
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
