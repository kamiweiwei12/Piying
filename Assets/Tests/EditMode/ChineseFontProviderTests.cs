using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class ChineseFontProviderTests
    {
        [Test]
        public void Load_ReturnsBundledFontWithPrototypeChineseGlyphs()
        {
            Font font = ChineseFontProvider.Load();

            Assert.That(font, Is.Not.Null);
            Assert.That(font, Is.SameAs(Resources.Load<Font>(ChineseFontProvider.BundledFontResourcePath)));

            const string requiredCharacters =
                "影韵数字皮影操演第一折试灯简单普通困难延迟校准音频输入暂停继续重新开始返回选曲连击得分准确开演总评契合协律应拍空引完成再奏左右手头部脚身体断势";
            foreach (char character in requiredCharacters)
            {
                Assert.That(font.HasCharacter(character), Is.True, $"Bundled font is missing '{character}' (U+{(int)character:X4}).");
            }
        }
    }

    public sealed class DemoFlowPresenterUiTests
    {
        [Test]
        public void SongArrows_SelectAdjacentSongs_AndAllRuntimeUiUsesBrushFont()
        {
            var root = new GameObject("Demo Flow UI Test");
            try
            {
                var presenter = root.AddComponent<DemoFlowPresenter>();
                typeof(DemoFlowPresenter)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(presenter, null);
                presenter.ConfigureSongs(new[]
                {
                    new SongMenuEntry("song-a", "甲曲", "甲"),
                    new SongMenuEntry("song-b", "乙曲", "乙"),
                    new SongMenuEntry("song-c", "丙曲", "丙"),
                }, "song-b");

                Button previous = root.GetComponentsInChildren<Button>(true).Single(button => button.name == "‹");
                Button next = root.GetComponentsInChildren<Button>(true).Single(button => button.name == "›");
                previous.onClick.Invoke();
                Assert.That(presenter.SelectedSongId, Is.EqualTo("song-a"));
                next.onClick.Invoke();
                Assert.That(presenter.SelectedSongId, Is.EqualTo("song-b"));

                Font brushFont = Resources.Load<Font>(ChineseFontProvider.BundledFontResourcePath);
                Assert.That(brushFont, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Text>(true).All(text => text.font == brushFont), Is.True);
                Assert.That(root.transform.Find("M7 可玩Demo/开场/开场提示底"), Is.Null,
                    "The obsolete rectangular prompt cover must not obscure the opening artwork or prompt.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
