using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class RadialNoteViewTests
    {
        private static readonly Vector2[] Spawn =
        {
            new Vector2(-6f, 0f), new Vector2(0f, 5f), new Vector2(6f, 0f),
            new Vector2(-5f, -4f), new Vector2(0f, -5f), new Vector2(5f, -4f)
        };

        private static readonly Vector2[] Receptor =
        {
            new Vector2(-3f, 0f), new Vector2(0f, 2f), new Vector2(3f, 0f),
            new Vector2(-2f, -2f), new Vector2(0f, 0f), new Vector2(2f, -2f)
        };

        private static readonly Color[] Colors =
        {
            Color.cyan, Color.yellow, Color.red, Color.green, Color.magenta, Color.white
        };

        [Test]
        public void BindChord_ActivatesAllRequiredMarkersAndConnector()
        {
            Fixture fixture = CreateFixture();
            try
            {
                int bothHands = (1 << 0) | (1 << 2);
                fixture.View.Bind(
                    new NoteData(1, "chord", 0, 1d, requiredLanesMask: bothHands),
                    Spawn,
                    Receptor,
                    Colors);
                fixture.View.UpdateVisual(0.5d, 1.5d);

                Assert.That(fixture.View.ActiveMarkerCount, Is.EqualTo(2));
                Assert.That(fixture.View.IsChordVisualActive, Is.True);
                Assert.That(fixture.View.IsHoldVisualActive, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void BindHold_ActivatesTrailAndHoldingState()
        {
            Fixture fixture = CreateFixture();
            try
            {
                fixture.View.Bind(new NoteData(1, "hold", 4, 1d, 1d), Spawn, Receptor, Colors);
                fixture.View.BeginHold();
                fixture.View.UpdateVisual(1.5d, 1.5d);

                Assert.That(fixture.View.ActiveMarkerCount, Is.EqualTo(1));
                Assert.That(fixture.View.IsHoldVisualActive, Is.True);
                Assert.That(fixture.View.IsChordVisualActive, Is.False);
                Assert.That(fixture.View.IsHolding, Is.True);

                fixture.View.Resolve(Color.green, 2.2d);
                Assert.That(fixture.View.IsHolding, Is.False);
                Assert.That(fixture.View.IsResolved, Is.True);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void BindHold_RendersAnElongatedEllipseWithAHeadCap()
        {
            Fixture fixture = CreateFixture();
            try
            {
                fixture.View.Bind(new NoteData(1, "hold", 4, 1d, 1d), Spawn, Receptor, Colors);
                fixture.View.UpdateVisual(1d, 1.5d);

                // 頭端在判定點 (0,0)，尾巴在 1 秒後的位置 → 長條長度 = 5 × (1 − 1.0/1.5) ≈ 3.333
                Vector3 scale = fixture.View.NoteMarkerScale;
                Assert.That(fixture.View.IsHoldNote, Is.True);
                Assert.That(fixture.View.IsHoldHeadActive, Is.True);
                Assert.That(scale.x, Is.EqualTo(0.4f).Within(0.05f));
                Assert.That(scale.y, Is.EqualTo(3.333f).Within(0.05f));
                Assert.That(scale.y, Is.GreaterThan(scale.x * 2f));

                // 長軸必須沿著軌道（lane 4 是垂直軌道），不能橫躺。
                Vector3 laneAxis = (Receptor[4] - Spawn[4]).normalized;
                Assert.That(Mathf.Abs(Vector3.Dot(fixture.View.NoteMarkerUp, laneAxis)), Is.GreaterThan(0.99f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void BindTap_RendersACircleWithoutAHoldVisual()
        {
            Fixture fixture = CreateFixture();
            try
            {
                fixture.View.Bind(new NoteData(1, "tap", 4, 1d), Spawn, Receptor, Colors);
                fixture.View.UpdateVisual(1d, 1.5d);

                Vector3 scale = fixture.View.NoteMarkerScale;
                Assert.That(fixture.View.IsHoldNote, Is.False);
                Assert.That(fixture.View.IsHoldHeadActive, Is.False);
                Assert.That(fixture.View.IsHoldVisualActive, Is.False);
                Assert.That(scale.x, Is.EqualTo(scale.y).Within(0.0001f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void BeginHold_TurnsTheEllipseGoldAndStaysElongated()
        {
            Fixture fixture = CreateFixture();
            try
            {
                fixture.View.Bind(new NoteData(1, "hold", 4, 1d, 1d), Spawn, Receptor, Colors);
                fixture.View.UpdateVisual(1d, 1.5d);
                Color before = fixture.View.NoteMarkerColor;
                float widthBefore = fixture.View.NoteMarkerScale.x;

                fixture.View.BeginHold();
                fixture.View.UpdateVisual(1d, 1.5d);

                Color during = fixture.View.NoteMarkerColor;
                Assert.That(during, Is.Not.EqualTo(before));
                Assert.That(during.g, Is.GreaterThan(before.g));
                Assert.That(fixture.View.NoteMarkerScale.x, Is.GreaterThan(widthBefore));
                Assert.That(fixture.View.NoteMarkerScale.y, Is.GreaterThan(fixture.View.NoteMarkerScale.x * 2f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void TapAndHoldNotes_RenderShapeComparisonImage()
        {
            var root = new GameObject("Note Shape Test");
            var texture = BuildCircleTexture(64);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
            var material = new Material(Shader.Find("Sprites/Default"));

            var tapRoot = new GameObject("Tap Note");
            tapRoot.transform.SetParent(root.transform, false);
            tapRoot.transform.localPosition = new Vector3(-3.2f, 0f, 0f);
            var tapView = tapRoot.AddComponent<RadialNoteView>();
            tapView.Initialize(sprite, 0.42f, material);
            tapView.Bind(new NoteData(1, "tap", 4, 2d), Spawn, Receptor, Colors);
            tapView.UpdateVisual(2d, 2d);

            var holdRoot = new GameObject("Hold Note");
            holdRoot.transform.SetParent(root.transform, false);
            holdRoot.transform.localPosition = new Vector3(3.2f, 0f, 0f);
            var holdView = holdRoot.AddComponent<RadialNoteView>();
            holdView.Initialize(sprite, 0.42f, material);
            holdView.Bind(new NoteData(2, "hold", 4, 2d, 1d), Spawn, Receptor, Colors);
            holdView.UpdateVisual(2d, 2d);

            var cameraObject = new GameObject("Validation Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);

            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            capture.Apply();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Logs", "M6-2-note-shapes.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, capture.EncodeToPNG());

            Assert.That(new FileInfo(output).Length, Is.GreaterThan(5000));

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(material);
        }

        private static Texture2D BuildCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    pixels[(y * size) + x] = new Color32(255, 255, 255, distance <= radius ? (byte)255 : (byte)0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Fixture CreateFixture()
        {
            var root = new GameObject("RadialNoteView Test");
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            var material = new Material(Shader.Find("Sprites/Default"));
            var view = root.AddComponent<RadialNoteView>();
            view.Initialize(sprite, 0.4f, material);
            return new Fixture(root, texture, sprite, material, view);
        }

        private sealed class Fixture
        {
            public Fixture(GameObject root, Texture2D texture, Sprite sprite, Material material, RadialNoteView view)
            {
                Root = root;
                Texture = texture;
                Sprite = sprite;
                Material = material;
                View = view;
            }

            public GameObject Root { get; }
            public Texture2D Texture { get; }
            public Sprite Sprite { get; }
            public Material Material { get; }
            public RadialNoteView View { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(Sprite);
                Object.DestroyImmediate(Texture);
                Object.DestroyImmediate(Material);
            }
        }
    }
}
