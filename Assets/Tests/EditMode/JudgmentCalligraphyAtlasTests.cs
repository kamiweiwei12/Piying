using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class JudgmentCalligraphyAtlasTests
    {
        [Test]
        public void Atlas_ContainsFourDistinctGradeSprites()
        {
            JudgmentCalligraphyAtlas atlas = JudgmentCalligraphyAtlas.Load();
            try
            {
                Assert.That(atlas.Texture.width % 2, Is.Zero);
                Assert.That(atlas.Texture.height % 2, Is.Zero);
                Sprite[] sprites = { atlas.Get("契合"), atlas.Get("协律"), atlas.Get("应拍"), atlas.Get("空引") };
                Assert.That(sprites.Select(sprite => sprite.name).Distinct().Count(), Is.EqualTo(4));
                Assert.That(sprites.All(sprite => sprite.texture == atlas.Texture), Is.True);
                Assert.That(sprites.All(sprite => sprite.rect.width == atlas.Texture.width / 2f), Is.True);
                Assert.That(sprites.All(sprite => sprite.rect.height == atlas.Texture.height / 2f), Is.True);
                Assert.That(atlas.TryGet(JudgmentLabels.HoldReleasedEarly, out _), Is.False);
            }
            finally
            {
                atlas.Dispose();
            }
        }

        [TestCase(JudgmentGrade.Perfect, "契合")]
        [TestCase(JudgmentGrade.Great, "协律")]
        [TestCase(JudgmentGrade.Good, "应拍")]
        [TestCase(JudgmentGrade.Miss, "空引")]
        public void NoteJudgment_UsesFixedInkCalligraphy(JudgmentGrade grade, string expectedLabel)
        {
            var root = new GameObject("Judgment Calligraphy Presenter Test");
            try
            {
                RadialNotePresenter presenter = root.AddComponent<RadialNotePresenter>();
                typeof(RadialNotePresenter)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, null);
                var note = new NoteData(1, "tap", 0, 1d);
                presenter.Begin(new[] { note });
                presenter.Tick(1d);
                presenter.OnJudged(new JudgmentResult(
                    JudgmentEventKind.NoteJudged,
                    1,
                    0,
                    grade,
                    0d,
                    1,
                    1000,
                    1d));

                SpriteRenderer brush = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "Judgment Calligraphy");
                Assert.That(brush.enabled, Is.True);
                Assert.That(brush.sprite.name, Does.Contain(expectedLabel));
                Assert.That(brush.color, Is.EqualTo(JudgmentCalligraphyAtlas.InkColor));
                Assert.That(brush.transform.localScale, Is.EqualTo(Vector3.one));

                presenter.OnJudged(new JudgmentResult(
                    JudgmentEventKind.SegmentInterrupted,
                    1,
                    0,
                    JudgmentGrade.Miss,
                    0d,
                    1,
                    1000,
                    1d));
                presenter.OnJudged(new JudgmentResult(
                    JudgmentEventKind.SegmentCompleted,
                    1,
                    0,
                    JudgmentGrade.None,
                    0d,
                    1,
                    1000,
                    1d));
                Assert.That(brush.enabled, Is.True);
                Assert.That(brush.sprite.name, Does.Contain(expectedLabel));

                presenter.Tick(1.319d);
                Assert.That(brush.enabled, Is.True);
                presenter.Tick(1.321d);
                Assert.That(brush.enabled, Is.False);

                presenter.OnJudged(new JudgmentResult(
                    JudgmentEventKind.HoldStarted,
                    1,
                    0,
                    JudgmentGrade.None,
                    0d,
                    1,
                    1000,
                    1d));
                Assert.That(brush.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResultPanel_UsesFixedInkForAllCalligraphyLabels()
        {
            var root = new GameObject("Result Calligraphy Presenter Test");
            try
            {
                GameplayHudPresenter presenter = root.AddComponent<GameplayHudPresenter>();
                typeof(GameplayHudPresenter)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, null);

                string[] labels = { "契合", "协律", "应拍", "空引" };
                UnityEngine.UI.Image[] images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (string label in labels)
                {
                    UnityEngine.UI.Image image = images.Single(candidate => candidate.gameObject.name == label);
                    UnityEngine.UI.Image background = images.Single(candidate => candidate.gameObject.name == $"{label}统计");
                    UnityEngine.UI.Text count = background.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    Assert.That(image.sprite.name, Does.Contain(label));
                    Assert.That(image.color, Is.EqualTo(JudgmentCalligraphyAtlas.InkColor));
                    Assert.That(image.preserveAspect, Is.True);
                    Assert.That(background.color, Is.EqualTo((Color)new Color32(238, 222, 184, 235)));
                    Assert.That(count.color, Is.EqualTo(JudgmentCalligraphyAtlas.InkColor));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HoldHitRing_RemainsActiveUntilFinalJudgment()
        {
            var root = new GameObject("Sustained Hold Hit Ring Test");
            try
            {
                RadialNotePresenter presenter = root.AddComponent<RadialNotePresenter>();
                typeof(RadialNotePresenter)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, null);

                const int laneMask = 1;
                presenter.Begin(new[] { new NoteData(1, "hold", 0, 1d, 2d) });
                presenter.Tick(1d);
                presenter.OnJudged(new JudgmentResult(
                    JudgmentEventKind.HoldStarted,
                    1,
                    0,
                    JudgmentGrade.None,
                    0d,
                    1,
                    1000,
                    1d,
                    laneMask));

                Assert.That(presenter.ActiveHitEffectCount, Is.EqualTo(1));
                presenter.Tick(2.1d);
                Assert.That(presenter.ActiveHitEffectCount, Is.EqualTo(1),
                    "the white ring must keep cycling for the whole hold");

                presenter.OnJudged(new JudgmentResult(
                    JudgmentEventKind.NoteJudged,
                    1,
                    0,
                    JudgmentGrade.Miss,
                    0d,
                    1,
                    1000,
                    3d,
                    laneMask));

                Assert.That(presenter.ActiveHitEffectCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
