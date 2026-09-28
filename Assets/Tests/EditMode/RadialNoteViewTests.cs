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
        public void Presenter_CountdownBlocksEntryUntilZero()
        {
            var root = new GameObject("Countdown Entry Gate Test");
            try
            {
                RadialNotePresenter presenter = root.AddComponent<RadialNotePresenter>();
                presenter.Begin(new[] { new NoteData(1, "tap", 0, 0.86d) });

                presenter.Tick(-3d);
                presenter.Tick(-2d);
                presenter.Tick(-1d);
                presenter.Tick(-0.000001d);
                Assert.That(presenter.ActiveCount, Is.Zero,
                    "no note may enter while the three-second countdown is active");

                presenter.Tick(0d);
                Assert.That(presenter.ActiveCount, Is.EqualTo(1));

                double entryLead = RadialNoteGeometry.CountdownSafeLead(0.86d, 1.75d);
                Vector2 atEntry = RadialNoteGeometry.Position(
                    new Vector2(-7.5f, 0.65f),
                    RadialNotePresenter.ReceptorPositionForLane(0),
                    0d,
                    0.86d,
                    entryLead);
                Assert.That(atEntry.x, Is.EqualTo(-7.5f).Within(0.0001f));
                Assert.That(atEntry.y, Is.EqualTo(0.65f).Within(0.0001f));

                presenter.Tick(0.86d);
                Assert.That(presenter.ActiveCount, Is.EqualTo(1),
                    "the gate must not change or discard the note's original judgment time");
                Vector2 atJudgment = RadialNoteGeometry.Position(
                    new Vector2(-7.5f, 0.65f),
                    RadialNotePresenter.ReceptorPositionForLane(0),
                    0.86d,
                    0.86d,
                    entryLead);
                Assert.That(atJudgment.x, Is.EqualTo(-3.3f).Within(0.0001f));
                Assert.That(atJudgment.y, Is.EqualTo(0.35f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

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
                Assert.That(fixture.View.transform.Find("Chord Note Art"), Is.Null,
                    "a chord must be generated from two tap circles and a bridge, not a combined texture");
                Assert.That(fixture.View.GetComponentsInChildren<LineRenderer>(true), Is.Empty,
                    "the bridge must come from the supplied art rather than a procedural line");
                Assert.That(fixture.View.transform.Find("Chord Bridge Art"), Is.Not.Null);
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
                Assert.That(fixture.View.IsHoldHeadActive, Is.False,
                    "the entered head should disappear into the judgment circle");
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
        public void BindHold_RendersBodyFromHeadToTail()
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
        public void HoldHeadAndTail_FollowStartAndEndJudgmentTimes()
        {
            Fixture fixture = CreateFixture();
            try
            {
                const double startTime = 1d;
                const double duration = 1d;
                const double visibleLead = 1.5d;
                fixture.View.Bind(new NoteData(1, "hold", 4, startTime, duration), Spawn, Receptor, Colors);

                fixture.View.UpdateVisual(startTime, visibleLead);
                Assert.That(fixture.View.HoldHeadPosition.x, Is.EqualTo(Receptor[4].x).Within(0.0001f));
                Assert.That(fixture.View.HoldHeadPosition.y, Is.EqualTo(Receptor[4].y).Within(0.0001f));
                Vector2 expectedTailAtStart = RadialNoteGeometry.Position(
                    Spawn[4], Receptor[4], startTime, startTime + duration, visibleLead);
                Assert.That(fixture.View.HoldTailPosition.x, Is.EqualTo(expectedTailAtStart.x).Within(0.0001f));
                Assert.That(fixture.View.HoldTailPosition.y, Is.EqualTo(expectedTailAtStart.y).Within(0.0001f));
                Assert.That(Vector2.Dot(
                    (fixture.View.HoldTailPosition - fixture.View.HoldHeadPosition).normalized,
                    fixture.View.NoteMarkerUp), Is.GreaterThan(0.999f),
                    "the hold art's small tail cap must point at the release judgment position");

                fixture.View.UpdateVisual(startTime + duration, visibleLead);
                Assert.That(fixture.View.HoldHeadPosition.x, Is.EqualTo(Receptor[4].x).Within(0.0001f));
                Assert.That(fixture.View.HoldHeadPosition.y, Is.EqualTo(Receptor[4].y).Within(0.0001f));
                Assert.That(fixture.View.HoldTailPosition.x, Is.EqualTo(Receptor[4].x).Within(0.0001f));
                Assert.That(fixture.View.HoldTailPosition.y, Is.EqualTo(Receptor[4].y).Within(0.0001f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void NoteArtResources_LoadAsCroppedSprites()
        {
            Sprite tap = LoadImportedSprite("YingYun/Art/Notes/note_tap");
            Sprite chordLeft = LoadImportedSprite("YingYun/Art/Notes/note_chord", "note_chord_left");
            Sprite chordBridge = LoadImportedSprite("YingYun/Art/Notes/note_chord", "note_chord_bridge");
            Sprite chordRight = LoadImportedSprite("YingYun/Art/Notes/note_chord", "note_chord_right");
            Sprite holdHead = LoadImportedSprite("YingYun/Art/Notes/note_hold", "note_hold_head");
            Sprite holdBody = LoadImportedSprite("YingYun/Art/Notes/note_hold", "note_hold_body");
            Sprite holdTail = LoadImportedSprite("YingYun/Art/Notes/note_hold", "note_hold_tail");
            Sprite ring = LoadImportedSprite("YingYun/Art/Notes/hit_ring");

            Assert.That(tap, Is.Not.Null);
            Assert.That(chordLeft, Is.Not.Null);
            Assert.That(chordBridge, Is.Not.Null);
            Assert.That(chordRight, Is.Not.Null);
            Assert.That(chordLeft.rect.width, Is.EqualTo(chordLeft.rect.height));
            Assert.That(chordRight.rect.width, Is.EqualTo(chordRight.rect.height));
            Assert.That(chordBridge.rect.width, Is.LessThan(chordLeft.rect.width));
            Assert.That(holdHead, Is.Not.Null);
            Assert.That(holdBody, Is.Not.Null);
            Assert.That(holdTail, Is.Not.Null);
            Assert.That(ring, Is.Not.Null);
            Assert.That(holdBody.rect.height, Is.GreaterThan(holdHead.rect.height));
            Assert.That(holdBody.rect.height, Is.GreaterThan(holdTail.rect.height));
            Assert.That(holdBody.rect.width, Is.LessThan(holdHead.rect.width));
        }

        [Test]
        public void NoteArt_RendersImportedTapHoldAndChordPreview()
        {
            Sprite tap = LoadImportedSprite("YingYun/Art/Notes/note_tap");
            Sprite chordLeft = LoadImportedSprite("YingYun/Art/Notes/note_chord", "note_chord_left");
            Sprite chordBridge = LoadImportedSprite("YingYun/Art/Notes/note_chord", "note_chord_bridge");
            Sprite chordRight = LoadImportedSprite("YingYun/Art/Notes/note_chord", "note_chord_right");
            Sprite holdHead = LoadImportedSprite("YingYun/Art/Notes/note_hold", "note_hold_head");
            Sprite holdBody = LoadImportedSprite("YingYun/Art/Notes/note_hold", "note_hold_body");
            Sprite holdTail = LoadImportedSprite("YingYun/Art/Notes/note_hold", "note_hold_tail");
            var material = new Material(Shader.Find("Sprites/Default"));
            var root = new GameObject("Imported Note Art Preview");

            RadialNoteView tapView = CreatePreviewView(root.transform, "Tap", new Vector3(-4f, 1f), tap, chordLeft, chordBridge, chordRight, holdHead, holdBody, holdTail, material);
            tapView.Bind(new NoteData(1, "tap", 4, 2d), Spawn, Receptor, Colors);
            tapView.UpdateVisual(2d, 2d);

            RadialNoteView holdView = CreatePreviewView(root.transform, "Hold", new Vector3(0f, 2f), tap, chordLeft, chordBridge, chordRight, holdHead, holdBody, holdTail, material);
            holdView.Bind(new NoteData(2, "hold", 4, 2d, 1d), Spawn, Receptor, Colors);
            holdView.UpdateVisual(2d, 2d);
            SpriteRenderer holdRenderer = holdView.transform.Find("Hold Body").GetComponent<SpriteRenderer>();
            Assert.That(holdRenderer.bounds.size.x, Is.GreaterThan(0.1f));
            Assert.That(holdRenderer.bounds.size.y, Is.GreaterThan(0.1f));

            RadialNoteView chordView = CreatePreviewView(root.transform, "Chord", new Vector3(3.4f, 1f), tap, chordLeft, chordBridge, chordRight, holdHead, holdBody, holdTail, material);
            chordView.Bind(new NoteData(3, "chord", 4, 2d, requiredLanesMask: (1 << 4) | (1 << 5)), Spawn, Receptor, Colors);
            chordView.UpdateVisual(2d, 2d);
            Assert.That(chordView.transform.Find("Lane 5 Marker").GetComponent<SpriteRenderer>().sprite.name,
                Is.EqualTo("note_chord_left"));
            Assert.That(chordView.transform.Find("Lane 6 Marker").GetComponent<SpriteRenderer>().sprite.name,
                Is.EqualTo("note_chord_right"));
            Assert.That(chordView.transform.Find("Chord Bridge Art").GetComponent<SpriteRenderer>().sprite.name,
                Is.EqualTo("note_chord_bridge"));

            var cameraObject = new GameObject("Imported Art Validation Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4f;
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
            string output = Path.Combine(projectRoot, "Logs", "M11-note-art-preview-v3.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, capture.EncodeToPNG());

            Assert.That(new FileInfo(output).Length, Is.GreaterThan(10000));

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(material);
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
        public void BeginHold_HidesEnteredHeadWithoutChangingBodyWidth()
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
                Assert.That(before, Is.EqualTo(RadialNoteView.HoldColor));
                Assert.That(during, Is.EqualTo(RadialNoteView.HoldColor));
                Assert.That(fixture.View.IsHoldHeadActive, Is.False);
                Assert.That(fixture.View.NoteMarkerScale.x, Is.EqualTo(widthBefore).Within(0.0001f));
                Assert.That(fixture.View.NoteMarkerScale.y, Is.GreaterThan(fixture.View.NoteMarkerScale.x * 2f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void HoldDuringSustain_ShortensTowardJudgmentWithoutCompressingTail()
        {
            Fixture fixture = CreateFixture();
            try
            {
                fixture.View.Bind(new NoteData(1, "hold", 4, 1d, 1d), Spawn, Receptor, Colors);
                fixture.View.BeginHold();
                fixture.View.UpdateVisual(1.25d, 1.5d);

                float firstLength = fixture.View.NoteMarkerScale.y;
                Vector3 firstTailScale = fixture.View.transform.Find("Hold Tail").localScale;
                float firstTailDistance = Vector2.Distance(fixture.View.HoldHeadPosition, fixture.View.HoldTailPosition);

                fixture.View.UpdateVisual(1.75d, 1.5d);

                Vector3 secondTailScale = fixture.View.transform.Find("Hold Tail").localScale;
                float secondTailDistance = Vector2.Distance(fixture.View.HoldHeadPosition, fixture.View.HoldTailPosition);
                Assert.That(fixture.View.NoteMarkerScale.y, Is.LessThan(firstLength));
                Assert.That(secondTailDistance, Is.LessThan(firstTailDistance));
                Assert.That(firstTailScale.x, Is.EqualTo(firstTailScale.y).Within(0.0001f));
                Assert.That(secondTailScale.x, Is.EqualTo(secondTailScale.y).Within(0.0001f));
                Assert.That(secondTailScale.x, Is.EqualTo(firstTailScale.x).Within(0.05f),
                    "the tail cap stays uniform instead of being squashed with the body");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void NoteColors_AreAssignedByTapHoldAndChordRatherThanLane()
        {
            Fixture fixture = CreateFixture();
            try
            {
                fixture.View.Bind(new NoteData(1, "tap", 0, 1d), Spawn, Receptor, Colors);
                Assert.That(fixture.View.NoteMarkerColor, Is.EqualTo(RadialNoteView.TapColor));

                fixture.View.Bind(new NoteData(2, "hold", 5, 2d, 1d), Spawn, Receptor, Colors);
                Assert.That(fixture.View.NoteMarkerColor, Is.EqualTo(RadialNoteView.HoldColor));

                int chordMask = (1 << 0) | (1 << 2);
                fixture.View.Bind(new NoteData(3, "chord", 0, 3d, requiredLanesMask: chordMask), Spawn, Receptor, Colors);
                Assert.That(fixture.View.NoteMarkerColor, Is.EqualTo(RadialNoteView.ChordColor));
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

        private static RadialNoteView CreatePreviewView(
            Transform parent,
            string name,
            Vector3 position,
            Sprite tap,
            Sprite chordLeft,
            Sprite chordBridge,
            Sprite chordRight,
            Sprite holdHead,
            Sprite holdBody,
            Sprite holdTail,
            Material material)
        {
            var noteRoot = new GameObject(name);
            noteRoot.transform.SetParent(parent, false);
            noteRoot.transform.localPosition = position;
            var view = noteRoot.AddComponent<RadialNoteView>();
            view.Initialize(
                tap,
                chordLeft,
                chordBridge,
                chordRight,
                holdHead,
                holdBody,
                holdTail,
                0.62f,
                material);
            return view;
        }

        private static Sprite LoadImportedSprite(string resourcePath, string spriteName = null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (!string.IsNullOrEmpty(spriteName))
            {
                foreach (Sprite sprite in sprites)
                {
                    if (sprite.name == spriteName) return sprite;
                }

                return null;
            }

            return sprites.Length > 0 ? sprites[0] : Resources.Load<Sprite>(resourcePath);
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
