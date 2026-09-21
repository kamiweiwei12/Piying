using System;
using System.Collections.Generic;
using UnityEngine;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.View
{
    /// <summary>以 songTime 驅動六部位音符的生成、移動、回饋與回收。</summary>
    public sealed class RadialNotePresenter : MonoBehaviour
    {
        private const int LaneCount = 6;

        private static readonly Vector2[] ReceptorPositions =
        {
            new Vector2(-3.3f, 0.35f),
            new Vector2(0f, 2.35f),
            new Vector2(3.3f, 0.35f),
            new Vector2(-2.2f, -2.15f),
            new Vector2(0f, 0.15f),
            new Vector2(2.2f, -2.15f)
        };

        private static readonly Vector2[] SpawnPositions =
        {
            new Vector2(-7.5f, 0.65f),
            new Vector2(0f, 5f),
            new Vector2(7.5f, 0.65f),
            new Vector2(-5.6f, -4.25f),
            new Vector2(0f, -4.5f),
            new Vector2(5.6f, -4.25f)
        };

        private static readonly Color[] LaneColors =
        {
            new Color(0.20f, 0.75f, 1f),
            new Color(1f, 0.72f, 0.20f),
            new Color(1f, 0.35f, 0.28f),
            new Color(0.35f, 0.90f, 0.45f),
            new Color(0.75f, 0.38f, 1f),
            new Color(1f, 0.40f, 0.72f)
        };

        private static readonly string[] LaneLabels =
        {
            "Q  左手", "W  头部", "E  右手", "A  左脚", "S  身体", "D  右脚"
        };

        [SerializeField, Min(0.1f)] private float visibleLeadSeconds = 1.75f;
        [SerializeField, Min(0.01f)] private float releaseDelaySeconds = 0.18f;
        [SerializeField, Min(1)] private int initialPoolSize = 24;
        [SerializeField, Min(0.05f)] private float noteScale = 0.42f;

        private readonly Queue<RadialNoteView> _pool = new Queue<RadialNoteView>();
        private readonly List<RadialNoteView> _active = new List<RadialNoteView>(32);
        private readonly Dictionary<int, RadialNoteView> _byNoteId = new Dictionary<int, RadialNoteView>();
        private NoteData[] _notes = Array.Empty<NoteData>();
        private Transform _visualRoot;
        private TextMesh _judgmentText;
        private SpriteRenderer _stageRenderer;
        private Sprite _noteSprite;
        private Texture2D _noteTexture;
        private Material _lineMaterial;
        private Font _chineseFont;
        private int _nextNoteIndex;
        private double _currentSongTime;
        private double _judgmentTextClearSongTime = double.PositiveInfinity;
        private double _stageFeedbackClearSongTime = double.PositiveInfinity;

        public int ActiveCount => _active.Count;
        public int PooledCount => _pool.Count;
        public int CreatedViewCount { get; private set; }

        private void Awake()
        {
            BuildPlayfield();
        }

        public void Begin(NoteData[] notes)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            ReleaseAll();
            _notes = notes;
            _nextNoteIndex = 0;
            _currentSongTime = double.NegativeInfinity;
            if (_judgmentText != null)
            {
                _judgmentText.text = string.Empty;
            }

            _judgmentTextClearSongTime = double.PositiveInfinity;
            _stageFeedbackClearSongTime = double.PositiveInfinity;
            if (_stageRenderer != null)
            {
                _stageRenderer.color = new Color(0.28f, 0.06f, 0.04f, 0.16f);
                _stageRenderer.transform.localScale = new Vector3(2.8f, 4.0f, 1f);
            }
        }

        public void Tick(double songTimeSec)
        {
            _currentSongTime = songTimeSec;

            if (_judgmentText != null && songTimeSec >= _judgmentTextClearSongTime)
            {
                _judgmentText.text = string.Empty;
                _judgmentTextClearSongTime = double.PositiveInfinity;
            }

            while (_nextNoteIndex < _notes.Length &&
                   _notes[_nextNoteIndex].TimeSec - songTimeSec <= visibleLeadSeconds)
            {
                Spawn(_notes[_nextNoteIndex]);
                _nextNoteIndex++;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                RadialNoteView view = _active[i];
                if (view.IsResolved)
                {
                    if (songTimeSec >= view.ReleaseSongTimeSec)
                    {
                        ReleaseAt(i);
                    }

                    continue;
                }

                view.UpdateVisual(songTimeSec, visibleLeadSeconds);
            }

            UpdateStageFeedback(songTimeSec);
        }

        public void OnJudged(JudgmentResult result)
        {
            if (result.EventKind == JudgmentEventKind.HoldStarted)
            {
                if (_byNoteId.TryGetValue(result.NoteId, out RadialNoteView holdView))
                {
                    holdView.BeginHold();
                }

                return;
            }

            if (result.EventKind == JudgmentEventKind.SegmentCompleted)
            {
                ShowSegmentFeedback("合势", new Color(1f, 0.82f, 0.25f));
                return;
            }

            if (result.EventKind == JudgmentEventKind.SegmentInterrupted)
            {
                ShowSegmentFeedback("断势", new Color(1f, 0.22f, 0.18f));
                return;
            }

            if (result.EventKind != JudgmentEventKind.NoteJudged)
            {
                return;
            }

            Color resultColor = GradeColor(result.Grade);
            if (_byNoteId.TryGetValue(result.NoteId, out RadialNoteView view))
            {
                view.Resolve(resultColor, _currentSongTime + releaseDelaySeconds);
            }

            if (_judgmentText != null)
            {
                _judgmentText.text = GradeText(result.Grade);
                _judgmentText.color = resultColor;
                _judgmentTextClearSongTime = _currentSongTime + releaseDelaySeconds;
            }
        }

        private void BuildPlayfield()
        {
            var rootObject = new GameObject("M3 Radial Playfield");
            rootObject.transform.SetParent(transform, false);
            _visualRoot = rootObject.transform;

            _noteTexture = BuildCircleTexture(64);
            _noteSprite = Sprite.Create(
                _noteTexture,
                new Rect(0f, 0f, _noteTexture.width, _noteTexture.height),
                new Vector2(0.5f, 0.5f),
                64f);
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            _chineseFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS" },
                64);

            BuildCenterMarker();
            for (int lane = 0; lane < LaneCount; lane++)
            {
                BuildReceptor(lane);
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                _pool.Enqueue(CreateView());
            }

            var judgmentObject = new GameObject("Judgment Text");
            judgmentObject.transform.SetParent(_visualRoot, false);
            judgmentObject.transform.localPosition = new Vector3(0f, -3.4f, 0f);
            _judgmentText = judgmentObject.AddComponent<TextMesh>();
            _judgmentText.anchor = TextAnchor.MiddleCenter;
            _judgmentText.alignment = TextAlignment.Center;
            _judgmentText.characterSize = 0.16f;
            _judgmentText.fontSize = 64;
            _judgmentText.fontStyle = FontStyle.Bold;
            _judgmentText.text = string.Empty;
            ApplyChineseFont(_judgmentText);
            _judgmentText.GetComponent<MeshRenderer>().sortingOrder = 30;
        }

        private void BuildCenterMarker()
        {
            var center = new GameObject("Segment Feedback Glow");
            center.transform.SetParent(_visualRoot, false);
            _stageRenderer = center.AddComponent<SpriteRenderer>();
            _stageRenderer.sprite = _noteSprite;
            _stageRenderer.color = new Color(0.28f, 0.06f, 0.04f, 0.16f);
            _stageRenderer.sortingOrder = -5;
            center.transform.localScale = new Vector3(2.8f, 4.0f, 1f);
        }

        private void BuildReceptor(int lane)
        {
            var receptor = new GameObject($"Lane {lane + 1} Receptor");
            receptor.transform.SetParent(_visualRoot, false);
            receptor.transform.localPosition = ReceptorPositions[lane];

            var line = receptor.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.material = _lineMaterial;
            line.startColor = LaneColors[lane];
            line.endColor = LaneColors[lane];
            line.sortingOrder = 10;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.48f, Mathf.Sin(angle) * 0.48f, 0f));
            }

            var label = new GameObject("Label");
            label.transform.SetParent(receptor.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            var text = label.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.07f;
            text.fontSize = 48;
            text.fontStyle = FontStyle.Bold;
            text.color = LaneColors[lane];
            text.text = LaneLabels[lane];
            ApplyChineseFont(text);
            text.GetComponent<MeshRenderer>().sortingOrder = 15;
        }

        private void Spawn(NoteData note)
        {
            RadialNoteView view = _pool.Count > 0 ? _pool.Dequeue() : CreateView();
            view.Bind(note, SpawnPositions, ReceptorPositions, LaneColors);
            _active.Add(view);
            _byNoteId.Add(note.Id, view);
        }

        private RadialNoteView CreateView()
        {
            var noteObject = new GameObject($"Pooled Note {CreatedViewCount + 1}");
            noteObject.transform.SetParent(_visualRoot, false);
            var view = noteObject.AddComponent<RadialNoteView>();
            view.Initialize(_noteSprite, noteScale, _lineMaterial);
            CreatedViewCount++;
            return view;
        }

        private void ShowSegmentFeedback(string text, Color color)
        {
            _judgmentText.text = text;
            _judgmentText.color = color;
            _judgmentTextClearSongTime = _currentSongTime + 0.45d;
            _stageFeedbackClearSongTime = _currentSongTime + 0.45d;
            if (_stageRenderer != null)
            {
                _stageRenderer.color = color;
                _stageRenderer.transform.localScale = new Vector3(3.0f, 4.2f, 1f);
            }
        }

        private void UpdateStageFeedback(double songTimeSec)
        {
            if (_stageRenderer == null || songTimeSec < _stageFeedbackClearSongTime)
            {
                return;
            }

            _stageRenderer.color = new Color(0.28f, 0.06f, 0.04f, 0.16f);
            _stageRenderer.transform.localScale = new Vector3(2.8f, 4.0f, 1f);
            _stageFeedbackClearSongTime = double.PositiveInfinity;
        }

        private void ReleaseAt(int index)
        {
            RadialNoteView view = _active[index];
            _byNoteId.Remove(view.NoteId);
            _active.RemoveAt(index);
            view.ResetForPool();
            _pool.Enqueue(view);
        }

        private void ReleaseAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ReleaseAt(i);
            }
        }

        private static Color GradeColor(JudgmentGrade grade)
        {
            switch (grade)
            {
                case JudgmentGrade.Perfect:
                    return new Color(1f, 0.86f, 0.25f);
                case JudgmentGrade.Great:
                    return new Color(1f, 0.55f, 0.20f);
                case JudgmentGrade.Good:
                    return new Color(0.25f, 0.95f, 0.55f);
                case JudgmentGrade.Miss:
                    return new Color(1f, 0.25f, 0.25f);
                default:
                    return Color.white;
            }
        }

        private static string GradeText(JudgmentGrade grade)
        {
            switch (grade)
            {
                case JudgmentGrade.Perfect: return "契合";
                case JudgmentGrade.Great: return "协律";
                case JudgmentGrade.Good: return "应拍";
                case JudgmentGrade.Miss: return "空引";
                default: return string.Empty;
            }
        }

        private static Texture2D BuildCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "M3 Runtime Note Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    byte alpha = distance <= radius ? (byte)255 : (byte)0;
                    pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void ApplyChineseFont(TextMesh text)
        {
            if (_chineseFont == null)
            {
                return;
            }

            text.font = _chineseFont;
            text.GetComponent<MeshRenderer>().sharedMaterial = _chineseFont.material;
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }

            if (_noteSprite != null)
            {
                Destroy(_noteSprite);
            }

            if (_noteTexture != null)
            {
                Destroy(_noteTexture);
            }

            if (_chineseFont != null)
            {
                Destroy(_chineseFont);
            }
        }
    }
}
