using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2HandShapeTests
    {
        private static readonly PuppetHandShape[] Shapes =
        {
            PuppetHandShape.NaturalPalm,
            PuppetHandShape.SupportPalm,
            PuppetHandShape.DirectionPalm,
            PuppetHandShape.ClosedPalm
        };

        private static readonly string[] SpriteNames =
        {
            "puppet_hand_natural_v2",
            "puppet_hand_support_v2",
            "puppet_hand_point_v2",
            "puppet_hand_fist_v2"
        };

        [Test]
        public void P4_AtlasContainsFourNamedSpritesWithRivetPivots()
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("YingYun/Art/Puppet/V2/puppet_hand_shapes_v2");
            Assert.That(sprites.Select(sprite => sprite.name), Is.EquivalentTo(SpriteNames));
            Vector2[] expectedPivots =
            {
                new Vector2(521.97f, 165.33f),
                new Vector2(446.36f, 101.55f),
                new Vector2(533.23f, 144.01f),
                new Vector2(337.82f, 135.19f)
            };
            for (int i = 0; i < SpriteNames.Length; i++)
            {
                Sprite sprite = sprites.Single(candidate => candidate.name == SpriteNames[i]);
                Assert.That(Vector2.Distance(sprite.pivot, expectedPivots[i]), Is.LessThan(1.1f));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f));
            }
        }

        [Test]
        public void P4_ShapeSwapKeepsBothWristJointsFixedAndUsesMirroredScale()
        {
            var root = new GameObject("V2 P4 Hand Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Assert.That(presenter.HasV2HandShapes, Is.True);
            Vector3 leftWrist = default;
            Vector3 rightWrist = default;
            for (int i = 0; i < Shapes.Length; i++)
            {
                presenter.PreviewV2HandShape(Shapes[i]);
                Assert.That(presenter.LeftHandSpriteName, Is.EqualTo(SpriteNames[i]));
                Assert.That(presenter.RightHandSpriteName, Is.EqualTo(SpriteNames[i]));
                if (i == 0)
                {
                    leftWrist = presenter.LeftWristPosition;
                    rightWrist = presenter.RightWristPosition;
                }
                else
                {
                    Assert.That(Vector3.Distance(leftWrist, presenter.LeftWristPosition), Is.LessThan(0.001f));
                    Assert.That(Vector3.Distance(rightWrist, presenter.RightWristPosition), Is.LessThan(0.001f));
                }
            }

            Transform stage = root.transform.Find("M6 Shadow Puppet Stage");
            Transform leftArt = stage.Find("Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow/Joint Left Wrist/Art Left Hand");
            Transform rightArt = stage.Find("Joint Pelvis/Joint Waist/Joint Right Shoulder/Joint Right Elbow/Joint Right Wrist/Art Right Hand");
            Assert.That(leftArt.localScale.x, Is.EqualTo(0.08f).Within(0.0001f));
            Assert.That(rightArt.localScale.x, Is.EqualTo(-0.08f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P4_RendersFourHandShapesAtGameScale()
        {
            var root = new GameObject("V2 P4 Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P4-HandShapes");
            Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("V2 P4 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            string[] filenames = { "01-natural", "02-support", "03-point", "04-fist" };
            for (int i = 0; i < Shapes.Length; i++)
            {
                presenter.PreviewV2HandShape(Shapes[i]);
                string path = Path.Combine(directory, filenames[i] + ".png");
                SaveFrame(camera, target, capture, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(20000));
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
