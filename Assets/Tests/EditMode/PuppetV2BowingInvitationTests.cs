using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2BowingInvitationTests
    {
        [Test]
        public void P5_13_ExtendsOnePalmLowWhileOtherHandGuardsChestAndBodyBows()
        {
            Assert.That(BowingInvitationChoreography.Name, Is.EqualTo("躬身请势"));
            Assert.That(BowingInvitationChoreography.Beats, Is.EqualTo(8));
            PuppetV2Pose start = BowingInvitationChoreography.Evaluate(0d);
            PuppetV2Pose palmTurns = BowingInvitationChoreography.Evaluate(0.375d);
            PuppetV2Pose peak = BowingInvitationChoreography.Evaluate(0.75d);
            PuppetV2Pose end = BowingInvitationChoreography.Evaluate(1d);
            Assert.That(palmTurns.RightWrist, Is.LessThan(-29d));
            Assert.That(peak.RightHandX, Is.GreaterThan(1.42d));
            Assert.That(peak.RightHandY, Is.LessThan(-0.82d));
            Assert.That(peak.LeftHandX, Is.InRange(-0.24d, -0.12d));
            Assert.That(peak.LeftHandY, Is.InRange(0.16d, 0.25d));
            Assert.That(peak.Torso, Is.LessThan(-19d));
            Assert.That(peak.Head, Is.LessThan(-11d));
            Assert.That(peak.RootY, Is.LessThan(start.RootY - 0.07d));
            Assert.That(peak.RootX, Is.GreaterThan(start.RootX + 0.03d));
            Assert.That(peak.RightHandShape, Is.EqualTo(PuppetHandShape.NaturalPalm));
            Assert.That(peak.LeftHandShape, Is.EqualTo(PuppetHandShape.SupportPalm));
            Assert.That(end.RightHandX, Is.GreaterThan(start.RightHandX + 0.40d));
            Assert.That(end.Torso, Is.LessThan(-4d));
            Assert.That(end.LeftFootPlanted && end.RightFootPlanted, Is.True);
        }

        [Test]
        public void P5_13_ArmTargetsStayContinuousAndFeetRemainLockedThroughoutInvitation()
        {
            DestroyNamed("V2 P5-13 Continuity Test");
            var root = new GameObject("V2 P5-13 Continuity Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewV2BowingInvitation(0f);
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            Vector3 previousLeftElbow = presenter.LeftElbowPosition;
            Vector3 previousRightElbow = presenter.RightElbowPosition;
            for (int i = 1; i <= 480; i++)
            {
                float progress = i / 480f;
                presenter.PreviewV2BowingInvitation(progress);
                Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.V2LeftHandTarget),
                    Is.LessThan(0.003f), $"left wrist at {progress:F4}");
                Assert.That(Vector3.Distance(presenter.RightWristPosition, presenter.V2RightHandTarget),
                    Is.LessThan(0.003f), $"right wrist at {progress:F4}");
                Assert.That(presenter.LeftWristPosition.x, Is.LessThan(-0.11f), $"guard hand crossed at {progress:F4}");
                Assert.That(presenter.RightWristPosition.x, Is.GreaterThan(0.45f), $"inviting hand crossed at {progress:F4}");
                if (progress >= 0.35f)
                {
                    Assert.That(presenter.LeftElbowPosition.x,
                        Is.LessThan(presenter.LeftWristPosition.x - 0.12f),
                        $"guard elbow must remain outside the palm at {progress:F4}");
                    Assert.That(presenter.LeftElbowPosition.x, Is.LessThan(-0.30f),
                        $"guard elbow entered the torso at {progress:F4}");
                }
                Assert.That(Vector3.Distance(presenter.LeftAnklePosition, leftFoot), Is.LessThan(0.002f));
                Assert.That(Vector3.Distance(presenter.RightAnklePosition, rightFoot), Is.LessThan(0.002f));
                Assert.That(Vector3.Distance(presenter.LeftElbowPosition, previousLeftElbow), Is.LessThan(0.025f));
                Assert.That(Vector3.Distance(presenter.RightElbowPosition, previousRightElbow), Is.LessThan(0.025f));
                previousLeftElbow = presenter.LeftElbowPosition;
                previousRightElbow = presenter.RightElbowPosition;
            }
            for (int lane = 0; lane < presenter.RodCount; lane++)
                Assert.That(presenter.GetRodDrive(lane), Is.EqualTo(0f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P5_13_RendersSevenAcceptanceStagesAndAnimationFrames()
        {
            DestroyNamed("V2 P5-13 Visual Test");
            DestroyNamed("V2 P5-13 Camera");
            DestroyNamed("V2 P5-13 Continuity Test");
            var root = new GameObject("V2 P5-13 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P5-13-BowingInvitation");
            string frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            var cameraObject = new GameObject("V2 P5-13 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            float[] stages = { 0f, 0.25f, 0.375f, 0.625f, 0.75f, 0.875f, 1f };
            string[] names = { "01-ready", "02-palm-turns", "03-invitation-starts", "04-bow-descends", "05-bow-peak", "06-rise", "07-half-close" };
            for (int i = 0; i < stages.Length; i++)
            {
                presenter.PreviewV2BowingInvitation(stages[i]);
                SaveFrame(camera, target, capture, Path.Combine(directory, names[i] + ".png"));
            }
            for (int i = 0; i <= 80; i++)
            {
                presenter.PreviewV2BowingInvitation(i / 80f);
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
