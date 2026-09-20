using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>單顆音符的畫面表示；不包含任何判定邏輯。</summary>
    public sealed class RadialNoteView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private float _baseScale;

        public int NoteId { get; private set; }
        public double NoteTimeSec { get; private set; }
        public int Lane { get; private set; }
        public Vector2 SpawnPosition { get; private set; }
        public Vector2 ReceptorPosition { get; private set; }
        public bool IsResolved { get; private set; }
        public double ReleaseSongTimeSec { get; private set; }

        public void Initialize(Sprite sprite, float scale)
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = sprite;
            _renderer.sortingOrder = 20;
            _baseScale = scale;
            gameObject.SetActive(false);
        }

        public void Bind(
            int noteId,
            int lane,
            double noteTimeSec,
            Vector2 spawnPosition,
            Vector2 receptorPosition,
            Color color)
        {
            NoteId = noteId;
            Lane = lane;
            NoteTimeSec = noteTimeSec;
            SpawnPosition = spawnPosition;
            ReceptorPosition = receptorPosition;
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;
            _baseColor = color;
            _renderer.color = color;
            transform.localScale = Vector3.one * _baseScale;
            transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
            gameObject.SetActive(true);
        }

        public void SetProgress(float progress, Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            float pulse = 1f + (0.12f * progress);
            transform.localScale = Vector3.one * (_baseScale * pulse);
        }

        public void Resolve(Color resultColor, double releaseSongTimeSec)
        {
            IsResolved = true;
            ReleaseSongTimeSec = releaseSongTimeSec;
            _renderer.color = resultColor;
            transform.localScale = Vector3.one * (_baseScale * 1.35f);
        }

        public void ResetForPool()
        {
            NoteId = 0;
            Lane = 0;
            NoteTimeSec = 0d;
            SpawnPosition = Vector2.zero;
            ReceptorPosition = Vector2.zero;
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;
            _renderer.color = _baseColor;
            gameObject.SetActive(false);
        }
    }
}
