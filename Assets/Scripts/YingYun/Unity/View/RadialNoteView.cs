using UnityEngine;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.View
{
    /// <summary>單顆邏輯音符的畫面表示；所有位置都由歌曲時間推導，不參與判定。</summary>
    public sealed class RadialNoteView : MonoBehaviour
    {
        private const int LaneCount = 6;

        public static readonly Color TapColor = new Color(0.90f, 0.24f, 0.12f, 1f);
        public static readonly Color HoldColor = new Color(0.96f, 0.68f, 0.12f, 1f);
        public static readonly Color ChordColor = new Color(0.28f, 0.78f, 0.72f, 1f);

        private readonly SpriteRenderer[] _markers = new SpriteRenderer[LaneCount];
        private readonly Vector2[] _spawnPositions = new Vector2[LaneCount];
        private readonly Vector2[] _receptorPositions = new Vector2[LaneCount];
        private LineRenderer _chordBridgeOutline;
        private LineRenderer _chordBridge;
        private SpriteRenderer _holdHeadVisual;
        private SpriteRenderer _holdBodyVisual;
        private SpriteRenderer _holdTailVisual;
        private Sprite _tapSprite;
        private Sprite _holdHeadSprite;
        private Sprite _holdBodySprite;
        private Sprite _holdTailSprite;
        private float _baseScale;
        private float _bodyWidth;
        private float _bodyLength;
        private double _durationSec;
        private bool _isHold;
        private bool _isHolding;
        private Color _logicalColor;

        public int NoteId { get; private set; }
        public double NoteTimeSec { get; private set; }
        public int Lane { get; private set; }
        public int RequiredLanesMask { get; private set; }
        public bool IsResolved { get; private set; }
        public double ReleaseSongTimeSec { get; private set; }
        public bool IsChordVisualActive => _chordBridge != null && _chordBridge.gameObject.activeSelf;
        public bool IsHoldVisualActive => _holdTailVisual != null && _holdTailVisual.gameObject.activeSelf;
        public bool IsHolding => _isHolding;
        public bool IsHoldNote => _isHold;
        public bool IsHoldHeadActive => _holdHeadVisual != null && _holdHeadVisual.gameObject.activeSelf;
        public Vector3 NoteMarkerScale => _isHold && _holdBodyVisual != null
            ? new Vector3(_bodyWidth, _bodyLength, 1f)
            : FirstActiveMarkerScale();
        public Color NoteMarkerColor => _logicalColor;
        public Vector3 NoteMarkerUp => _isHold && _holdBodyVisual != null
            ? _holdBodyVisual.transform.up
            : FirstActiveMarkerUp();
        public Vector2 HoldHeadPosition { get; private set; }
        public Vector2 HoldTailPosition { get; private set; }
        public int ActiveMarkerCount => IsChordVisualActive ? CountRequiredLanes() : IsHoldVisualActive ? 1 : CountActiveTapMarkers();

        public void Initialize(Sprite sprite, float scale, Material lineMaterial)
        {
            Initialize(sprite, sprite, sprite, sprite, scale, lineMaterial);
        }

        public void Initialize(Sprite tapSprite, Sprite holdSprite, Sprite chordSprite, float scale, Material lineMaterial)
        {
            Initialize(tapSprite, holdSprite, holdSprite, holdSprite, scale, lineMaterial);
        }

        public void Initialize(
            Sprite tapSprite,
            Sprite holdHeadSprite,
            Sprite holdBodySprite,
            Sprite holdTailSprite,
            float scale,
            Material lineMaterial)
        {
            _baseScale = scale;
            _tapSprite = tapSprite;
            _holdHeadSprite = holdHeadSprite;
            _holdBodySprite = holdBodySprite;
            _holdTailSprite = holdTailSprite;
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            for (int lane = 0; lane < LaneCount; lane++)
            {
                var markerObject = new GameObject($"Lane {lane + 1} Marker");
                markerObject.transform.SetParent(transform, false);
                SpriteRenderer marker = markerObject.AddComponent<SpriteRenderer>();
                marker.sprite = _tapSprite;
                marker.sortingOrder = 20;
                markerObject.SetActive(false);
                _markers[lane] = marker;
            }

            _chordBridgeOutline = CreateLine("Chord Bridge Outline", lineMaterial, 18, 0.20f, Color.white);
            _chordBridge = CreateLine("Chord Bridge", lineMaterial, 19, 0.10f, new Color(0.52f, 0.95f, 0.86f, 1f));
            _holdBodyVisual = CreateArtRenderer("Hold Body", _holdBodySprite, 20);
            _holdHeadVisual = CreateArtRenderer("Hold Head", _holdHeadSprite, 21);
            _holdTailVisual = CreateArtRenderer("Hold Tail", _holdTailSprite, 22);
            gameObject.SetActive(false);
        }

        public void Bind(NoteData note, Vector2[] spawnPositions, Vector2[] receptorPositions, Color[] laneColors)
        {
            NoteId = note.Id;
            Lane = note.Lane;
            NoteTimeSec = note.TimeSec;
            RequiredLanesMask = note.RequiredLanesMask;
            _durationSec = note.DurationSec;
            _isHold = note.Kind == NoteKind.Hold;
            _bodyWidth = _baseScale;
            _bodyLength = _baseScale;
            _isHolding = false;
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;
            _logicalColor = note.IsChord ? ChordColor : (_isHold ? HoldColor : TapColor);

            for (int lane = 0; lane < LaneCount; lane++)
            {
                _spawnPositions[lane] = spawnPositions[lane];
                _receptorPositions[lane] = receptorPositions[lane];
                bool activeTapMarker = !_isHold && (RequiredLanesMask & (1 << lane)) != 0;
                _markers[lane].gameObject.SetActive(activeTapMarker);
                if (activeTapMarker)
                {
                    _markers[lane].sprite = _tapSprite;
                    _markers[lane].color = Color.white;
                    _markers[lane].transform.localPosition = spawnPositions[lane];
                    _markers[lane].transform.localRotation = Quaternion.identity;
                }
            }

            _chordBridgeOutline.gameObject.SetActive(note.IsChord);
            _chordBridge.gameObject.SetActive(note.IsChord);
            _holdHeadVisual.gameObject.SetActive(_isHold);
            _holdBodyVisual.gameObject.SetActive(_isHold);
            _holdTailVisual.gameObject.SetActive(_isHold);
            HoldHeadPosition = spawnPositions[Lane];
            HoldTailPosition = spawnPositions[Lane];
            gameObject.SetActive(true);
        }

        public void UpdateVisual(double songTimeSec, double visibleLeadSec)
        {
            if (IsChordVisualActive)
            {
                UpdateChord(songTimeSec, visibleLeadSec);
                return;
            }

            if (_isHold)
            {
                UpdateHold(songTimeSec, visibleLeadSec);
                return;
            }

            float progress = RadialNoteGeometry.Progress(songTimeSec, NoteTimeSec, visibleLeadSec);
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (!_markers[lane].gameObject.activeSelf) continue;
                _markers[lane].transform.localPosition = RadialNoteGeometry.Position(
                    _spawnPositions[lane], _receptorPositions[lane], songTimeSec, NoteTimeSec, visibleLeadSec);
                float targetHeight = _baseScale * (1.75f + (0.18f * progress));
                _markers[lane].transform.localScale = UniformScaleForHeight(_tapSprite, targetHeight);
            }
        }

        public void BeginHold()
        {
            _isHolding = true;
            if (_holdHeadVisual != null) _holdHeadVisual.gameObject.SetActive(false);
        }

        public void Resolve(Color resultColor, double releaseSongTimeSec)
        {
            IsResolved = true;
            ReleaseSongTimeSec = releaseSongTimeSec;
            _isHolding = false;
            if (!_isHold)
            {
                for (int lane = 0; lane < LaneCount; lane++)
                {
                    if (_markers[lane].gameObject.activeSelf) _markers[lane].transform.localScale *= 1.18f;
                }
            }
        }

        public void ResetForPool()
        {
            NoteId = 0;
            Lane = 0;
            NoteTimeSec = 0d;
            RequiredLanesMask = 0;
            _durationSec = 0d;
            _isHold = false;
            _bodyWidth = 0f;
            _bodyLength = 0f;
            _isHolding = false;
            _logicalColor = Color.clear;
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;
            HoldHeadPosition = Vector2.zero;
            HoldTailPosition = Vector2.zero;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                _markers[lane].gameObject.SetActive(false);
                _markers[lane].transform.localRotation = Quaternion.identity;
            }

            _chordBridgeOutline.gameObject.SetActive(false);
            _chordBridge.gameObject.SetActive(false);
            _holdHeadVisual.gameObject.SetActive(false);
            _holdBodyVisual.gameObject.SetActive(false);
            _holdTailVisual.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void UpdateHold(double songTimeSec, double visibleLeadSec)
        {
            Vector2 head = RadialNoteGeometry.Position(
                _spawnPositions[Lane], _receptorPositions[Lane], songTimeSec, NoteTimeSec, visibleLeadSec);
            Vector2 tail = RadialNoteGeometry.Position(
                _spawnPositions[Lane], _receptorPositions[Lane], songTimeSec, NoteTimeSec + _durationSec, visibleLeadSec);
            HoldHeadPosition = head;
            HoldTailPosition = tail;
            float holdProgress = RadialNoteGeometry.Progress(songTimeSec, NoteTimeSec, visibleLeadSec);
            _bodyWidth = _baseScale * (1f + (0.12f * holdProgress));
            Vector2 direction = tail - head;
            _bodyLength = direction.magnitude;
            Vector2 axis = _bodyLength > 0.001f
                ? direction / _bodyLength
                : (_spawnPositions[Lane] - _receptorPositions[Lane]).normalized;
            Quaternion rotation = Quaternion.Euler(
                0f, 0f, (Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg) - 90f);

            _holdHeadVisual.gameObject.SetActive(!_isHolding);
            _holdHeadVisual.transform.localPosition = head;
            _holdHeadVisual.transform.localRotation = rotation;
            _holdHeadVisual.transform.localScale = UniformScaleForWidth(_holdHeadSprite, _bodyWidth);

            bool bodyVisible = _bodyLength > 0.02f;
            _holdBodyVisual.gameObject.SetActive(bodyVisible);
            if (bodyVisible)
            {
                _holdBodyVisual.transform.localPosition = (head + tail) * 0.5f;
                _holdBodyVisual.transform.localRotation = rotation;
                ApplyHoldBodyScale();
            }

            _holdTailVisual.gameObject.SetActive(true);
            _holdTailVisual.transform.localPosition = tail;
            _holdTailVisual.transform.localRotation = rotation;
            _holdTailVisual.transform.localScale = UniformScaleForWidth(_holdTailSprite, _bodyWidth);
        }

        private void ApplyHoldBodyScale()
        {
            Vector2 spriteSize = _holdBodySprite == null ? Vector2.one : _holdBodySprite.bounds.size;
            _holdBodyVisual.transform.localScale = new Vector3(
                _bodyWidth / Mathf.Max(0.001f, spriteSize.x),
                _bodyLength / Mathf.Max(0.001f, spriteSize.y),
                1f);
        }

        private void UpdateChord(double songTimeSec, double visibleLeadSec)
        {
            int firstLane = -1;
            int secondLane = -1;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((RequiredLanesMask & (1 << lane)) == 0) continue;
                if (firstLane < 0) firstLane = lane;
                else if (secondLane < 0) secondLane = lane;
            }

            if (firstLane < 0 || secondLane < 0) return;
            Vector2 first = RadialNoteGeometry.Position(
                _spawnPositions[firstLane], _receptorPositions[firstLane], songTimeSec, NoteTimeSec, visibleLeadSec);
            Vector2 second = RadialNoteGeometry.Position(
                _spawnPositions[secondLane], _receptorPositions[secondLane], songTimeSec, NoteTimeSec, visibleLeadSec);
            float progress = RadialNoteGeometry.Progress(songTimeSec, NoteTimeSec, visibleLeadSec);
            float targetHeight = _baseScale * (1.75f + (0.18f * progress));
            Vector3 endpointScale = UniformScaleForHeight(_tapSprite, targetHeight);
            _markers[firstLane].transform.localPosition = first;
            _markers[firstLane].transform.localScale = endpointScale;
            _markers[secondLane].transform.localPosition = second;
            _markers[secondLane].transform.localScale = endpointScale;
            _chordBridgeOutline.SetPosition(0, first);
            _chordBridgeOutline.SetPosition(1, second);
            _chordBridge.SetPosition(0, first);
            _chordBridge.SetPosition(1, second);
        }

        private SpriteRenderer CreateArtRenderer(string objectName, Sprite sprite, int sortingOrder)
        {
            var artObject = new GameObject(objectName);
            artObject.transform.SetParent(transform, false);
            var renderer = artObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            artObject.SetActive(false);
            return renderer;
        }

        private LineRenderer CreateLine(string objectName, Material material, int sortingOrder, float width, Color color)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = sortingOrder;
            lineObject.SetActive(false);
            return line;
        }

        private static Vector3 UniformScaleForHeight(Sprite sprite, float targetHeight)
        {
            float height = sprite == null ? 1f : Mathf.Max(0.001f, sprite.bounds.size.y);
            float scale = targetHeight / height;
            return Vector3.one * scale;
        }

        private static Vector3 UniformScaleForWidth(Sprite sprite, float targetWidth)
        {
            float width = sprite == null ? 1f : Mathf.Max(0.001f, sprite.bounds.size.x);
            return Vector3.one * (targetWidth / width);
        }

        private int CountRequiredLanes()
        {
            int count = 0;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((RequiredLanesMask & (1 << lane)) != 0) count++;
            }

            return count;
        }

        private int CountActiveTapMarkers()
        {
            int count = 0;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (_markers[lane] != null && _markers[lane].gameObject.activeSelf) count++;
            }

            return count;
        }

        private Vector3 FirstActiveMarkerScale()
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (_markers[lane] != null && _markers[lane].gameObject.activeSelf)
                {
                    return _markers[lane].transform.localScale;
                }
            }

            return Vector3.zero;
        }

        private Vector3 FirstActiveMarkerUp()
        {
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (_markers[lane] != null && _markers[lane].gameObject.activeSelf)
                {
                    return _markers[lane].transform.up;
                }
            }

            return Vector3.up;
        }
    }
}
