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
