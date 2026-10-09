using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>優先載入隨遊戲出包的中文字型，僅在資產缺失時退回系統字型。</summary>
    public static class ChineseFontProvider
    {
        public const string BundledFontResourcePath = "YingYun/Fonts/MaShanZheng-Regular";
        public const string BundledFallbackFontResourcePath = "YingYun/Fonts/NotoSansSC-VF";

        private static readonly string[] SystemFallbackNames =
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS"
        };

        public static Font Load()
        {
            Font bundledFont = Resources.Load<Font>(BundledFontResourcePath);
            if (bundledFont != null)
            {
                return bundledFont;
            }

            Font bundledFallback = Resources.Load<Font>(BundledFallbackFontResourcePath);
            return bundledFallback != null
                ? bundledFallback
                : Font.CreateDynamicFontFromOSFont(SystemFallbackNames, 64);
        }

        public static void Release(Font font)
        {
            if (font == null ||
                font == Resources.Load<Font>(BundledFontResourcePath) ||
                font == Resources.Load<Font>(BundledFallbackFontResourcePath))
            {
                return;
            }

            Object.Destroy(font);
        }
    }

    /// <summary>統一毛筆字的字重與描邊；只處理視覺，不改變文字內容或佈局。</summary>
    internal static class BrushTypography
    {
        private static readonly Color DarkOutline = new Color(0.10f, 0.035f, 0.02f, 0.78f);
        private static readonly Color LightOutline = new Color(0.96f, 0.78f, 0.42f, 0.38f);

        public static void Apply(UnityEngine.UI.Text text, int size, Color foreground)
        {
            text.fontStyle = FontStyle.Normal;
            UnityEngine.UI.Outline outline = text.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<UnityEngine.UI.Outline>();
            }

            bool brightText = foreground.grayscale >= 0.52f;
            outline.effectColor = brightText ? DarkOutline : LightOutline;
            float distance = size >= 72 ? 2.2f : size >= 36 ? 1.5f : 1f;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        public static void Apply(TextMesh text)
        {
            text.fontStyle = FontStyle.Normal;
        }
    }
}
