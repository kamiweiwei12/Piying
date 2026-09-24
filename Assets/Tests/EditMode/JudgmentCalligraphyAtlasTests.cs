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

        [Test]
        public void PerfectJudgment_UsesCalligraphySpriteWhileHoldPromptKeepsText()
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
                    JudgmentGrade.Perfect,
                    0d,
                    1,
                    1000,
                    1d));

                SpriteRenderer brush = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(renderer => renderer.gameObject.name == "Judgment Calligraphy");
                Assert.That(brush.enabled, Is.True);
                Assert.That(brush.sprite.name, Does.Contain("契合"));

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
    }
}
