using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2PointingFarGazeTests
    {
        [Test]
        public void P5_09_HeadLeadsBeforeArmAndPointingShapeHoldsForTheGaze()
        {
            Assert.That(PointingFarGazeChoreography.Name, Is.EqualTo("指路远眺"));
            Assert.That(PointingFarGazeChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = PointingFarGazeChoreography.Evaluate(0d);
            PuppetV2Pose headLead = PointingFarGazeChoreography.Evaluate(0.25d);
            PuppetV2Pose shapeEntry = PointingFarGazeChoreography.Evaluate(0.50d);
            PuppetV2Pose gaze = PointingFarGazeChoreography.Evaluate(0.70d);
            PuppetV2Pose recovery = PointingFarGazeChoreography.Evaluate(0.875d);
            PuppetV2Pose finish = PointingFarGazeChoreography.Evaluate(1d);

            Assert.That(headLead.Head - start.Head, Is.GreaterThan(5d));
            Assert.That(headLead.RightHandX - start.RightHandX, Is.LessThan(0.05d));
            Assert.That(shapeEntry.RightHandShape, Is.EqualTo(PuppetHandShape.DirectionPalm));
            Assert.That(gaze.RightHandShape, Is.EqualTo(PuppetHandShape.DirectionPalm));
            Assert.That(gaze.RightHandX, Is.GreaterThan(1.70d));
            Assert.That(gaze.RightHandY, Is.GreaterThan(1.00d));
            Assert.That(gaze.RightHandX - gaze.LeftHandX, Is.GreaterThan(1.74d));
            Assert.That(recovery.RightHandShape, Is.EqualTo(PuppetHandShape.NaturalPalm));
            Assert.That(finish.RightHandX, Is.EqualTo(start.RightHandX).Within(0.001d));
            Assert.That(finish.RootX, Is.Zero.Within(0.001d));
            Assert.That(finish.Facing, Is.EqualTo(1d));
            Assert.That(finish.LeftFootPlanted && finish.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_09_PointingLineStaysOutsideFaceWhileFeetAndPelvisRemainLocked()
        {
            DestroyNamed("V2 P5-09 Continuity Test");
            var root = new GameObject("V2 P5-09 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2PointingFarGaze(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            float maximumLeftElbowStep = 0f;
            float maximumRightElbowStep = 0f;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2PointingFarGaze(progress);
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
            presenter.PreviewV2PointingFarGaze(0.70f);
            Assert.That(presenter.RightHandSpriteName, Is.EqualTo("puppet_hand_point_v2"));
            Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(1.70f));
            Assert.That(presenter.RightWristPosition.y, Is.LessThan(1.25f));
            Assert.That(Vector3.Distance(presenter.RightShoulderPosition, presenter.RightWristPosition),
                Is.GreaterThan(1.25f));
            presenter.PreviewV2PointingFarGaze(0.90f);
            Assert.That(presenter.RightHandSpriteName, Is.EqualTo("puppet_hand_natural_v2"));
            presenter.PreviewV2PointingFarGaze(1f);
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.Zero.Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_09_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-09 Visual Test");
            DestroyNamed("V2 P5-09 Camera");
            var root = new GameObject("V2 P5-09 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-09-PointingFarGaze");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-09 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.50f, 0.625f, 0.75f, 0.875f, 1f };
            string[] names = { "01-half-close", "02-head-leads", "03-point-shape", "04-far-point", "05-gaze-hold", "06-return-palm", "07-half-return" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2PointingFarGaze(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2PointingFarGaze(i / 80f);
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
