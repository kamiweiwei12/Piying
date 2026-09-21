using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>優先載入隨遊戲出包的中文字型，僅在資產缺失時退回系統字型。</summary>
    public static class ChineseFontProvider
    {
        public const string BundledFontResourcePath = "YingYun/Fonts/NotoSansSC-VF";

        private static readonly string[] SystemFallbackNames =
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS"
        };

        public static Font Load()
        {
            Font bundledFont = Resources.Load<Font>(BundledFontResourcePath);
            return bundledFont != null
                ? bundledFont
                : Font.CreateDynamicFontFromOSFont(SystemFallbackNames, 64);
        }

        public static void Release(Font font)
        {
            if (font == null || font == Resources.Load<Font>(BundledFontResourcePath))
            {
                return;
            }

            Object.Destroy(font);
        }
    }
}
