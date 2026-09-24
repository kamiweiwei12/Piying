using System;
using System.Collections.Generic;
using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>四项判定的透明墨笔图集；只负责 UI 字形，不参与判定或计分。</summary>
    public sealed class JudgmentCalligraphyAtlas : IDisposable
    {
        public const string ResourcePath = "YingYun/UI/JudgmentCalligraphy";
        public const double DisplaySeconds = 0.32d;
        public static readonly Color InkColor = new Color32(7, 27, 31, 255);

        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(4);

        private JudgmentCalligraphyAtlas(Texture2D texture)
        {
            Texture = texture != null ? texture : throw new ArgumentNullException(nameof(texture));
            int cellWidth = texture.width / 2;
            int cellHeight = texture.height / 2;
            if (cellWidth <= 0 || cellHeight <= 0)
            {
                throw new InvalidOperationException("Judgment calligraphy atlas must contain a 2 x 2 grid.");
            }

            Add("契合", new Rect(0f, cellHeight, cellWidth, cellHeight));
            Add("协律", new Rect(cellWidth, cellHeight, cellWidth, cellHeight));
            Add("应拍", new Rect(0f, 0f, cellWidth, cellHeight));
            Add("空引", new Rect(cellWidth, 0f, cellWidth, cellHeight));
        }

        public Texture2D Texture { get; }

        public static JudgmentCalligraphyAtlas Load()
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Judgment calligraphy texture was not found at Resources/{ResourcePath}.");
            }

            return new JudgmentCalligraphyAtlas(texture);
        }

        public bool TryGet(string label, out Sprite sprite) => _sprites.TryGetValue(label, out sprite);

        public Sprite Get(string label)
        {
            if (!_sprites.TryGetValue(label, out Sprite sprite))
            {
                throw new ArgumentException("Only the four grade labels have calligraphy sprites.", nameof(label));
            }

            return sprite;
        }

        public void Dispose()
        {
            foreach (Sprite sprite in _sprites.Values)
            {
                if (sprite != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(sprite);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(sprite);
                    }
                }
            }

            _sprites.Clear();
        }

        private void Add(string label, Rect rect)
        {
            Sprite sprite = Sprite.Create(Texture, rect, new Vector2(0.5f, 0.5f), 256f);
            sprite.name = $"判定墨笔-{label}";
            _sprites.Add(label, sprite);
        }
    }
}
