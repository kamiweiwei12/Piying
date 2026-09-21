using NUnit.Framework;
using UnityEngine;
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
}
