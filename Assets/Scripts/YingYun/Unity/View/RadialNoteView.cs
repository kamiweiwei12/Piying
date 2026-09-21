using UnityEngine;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.View
{
    /// <summary>單顆邏輯音符的畫面表示；可預建六部位標記，但不包含判定邏輯。</summary>
    public sealed class RadialNoteView : MonoBehaviour
    {
        private const int LaneCount = 6;

        public static readonly Color TapColor = new Color(0.90f, 0.24f, 0.12f, 1f);
        public static readonly Color HoldColor = new Color(0.96f, 0.68f, 0.12f, 1f);
        public static readonly Color ChordColor = new Color(0.28f, 0.78f, 0.72f, 1f);

        private readonly SpriteRenderer[] _markers = new SpriteRenderer[LaneCount];
        private readonly Vector2[] _spawnPositions = new Vector2[LaneCount];
        private readonly Vector2[] _receptorPositions = new Vector2[LaneCount];
        private readonly Color[] _laneColors = new Color[LaneCount];
        private LineRenderer _chordLine;
        private LineRenderer _holdTrail;
        private SpriteRenderer _holdHead;
        private float _baseScale;
        private float _bodyWidth;
        private float _bodyLength;
        private double _durationSec;
        private bool _isHold;
        private bool _isHolding;

        public int NoteId { get; private set; }
        public double NoteTimeSec { get; private set; }
        public int Lane { get; private set; }
        public int RequiredLanesMask { get; private set; }
        public bool IsResolved { get; private set; }
        public double ReleaseSongTimeSec { get; private set; }
        public bool IsChordVisualActive => _chordLine != null && _chordLine.gameObject.activeSelf;
        public bool IsHoldVisualActive => _holdTrail != null && _holdTrail.gameObject.activeSelf;
        public bool IsHolding => _isHolding;
        public bool IsHoldNote => _isHold;
        public bool IsHoldHeadActive => _holdHead != null && _holdHead.gameObject.activeSelf;
        public Vector3 NoteMarkerScale => _markers[Lane] == null ? Vector3.zero : _markers[Lane].transform.localScale;
        public Color NoteMarkerColor => _markers[Lane] == null ? Color.clear : _markers[Lane].color;
        public Vector3 NoteMarkerUp => _markers[Lane] == null ? Vector3.up : _markers[Lane].transform.up;
        public int ActiveMarkerCount
        {
            get
            {
                int count = 0;
                for (int lane = 0; lane < LaneCount; lane++)
                {
                    if (_markers[lane] != null && _markers[lane].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Initialize(Sprite sprite, float scale, Material lineMaterial)
        {
            _baseScale = scale;
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            for (int lane = 0; lane < LaneCount; lane++)
            {
                var markerObject = new GameObject($"Lane {lane + 1} Marker");
                markerObject.transform.SetParent(transform, false);
                SpriteRenderer marker = markerObject.AddComponent<SpriteRenderer>();
                marker.sprite = sprite;
                marker.sortingOrder = 20;
                markerObject.SetActive(false);
                _markers[lane] = marker;
            }

            _chordLine = CreateLine("Chord Link", lineMaterial, 19, 0.08f);
            _holdTrail = CreateLine("Hold Trail", lineMaterial, 18, 0.16f);

            // 長按音符的頭端圓帽：長條橢圓的起點，提示「從這裡按下去」。
            var headObject = new GameObject("Hold Head Cap");
            headObject.transform.SetParent(transform, false);
            _holdHead = headObject.AddComponent<SpriteRenderer>();
            _holdHead.sprite = sprite;
            _holdHead.sortingOrder = 21;
            headObject.SetActive(false);

            gameObject.SetActive(false);
        }

        public void Bind(
            NoteData note,
            Vector2[] spawnPositions,
            Vector2[] receptorPositions,
            Color[] laneColors)
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
            Color noteColor = note.IsChord ? ChordColor : (_isHold ? HoldColor : TapColor);

            for (int lane = 0; lane < LaneCount; lane++)
            {
                _spawnPositions[lane] = spawnPositions[lane];
                _receptorPositions[lane] = receptorPositions[lane];
                _laneColors[lane] = noteColor;
                bool active = (RequiredLanesMask & (1 << lane)) != 0;
                _markers[lane].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                _markers[lane].color = noteColor;
                _markers[lane].transform.localScale = Vector3.one * _baseScale;
                _markers[lane].transform.localPosition = spawnPositions[lane];
                _markers[lane].transform.localRotation = Quaternion.identity;
            }

            _chordLine.gameObject.SetActive(note.IsChord);
            if (note.IsChord)
            {
                _chordLine.positionCount = CountRequiredLanes();
                SetLineColor(_chordLine, ChordColor);
            }

            _holdTrail.gameObject.SetActive(note.Kind == NoteKind.Hold);
            _holdHead.gameObject.SetActive(_isHold);
            if (_isHold)
            {
                _holdHead.color = HoldColor;
            }

            gameObject.SetActive(true);
        }

        public void UpdateVisual(double songTimeSec, double visibleLeadSec)
        {
            int chordPoint = 0;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((RequiredLanesMask & (1 << lane)) == 0)
                {
                    continue;
                }

                float progress = RadialNoteGeometry.Progress(songTimeSec, NoteTimeSec, visibleLeadSec);
                Vector2 position = RadialNoteGeometry.Position(
                    _spawnPositions[lane],
                    _receptorPositions[lane],
                    songTimeSec,
                    NoteTimeSec,
                    visibleLeadSec);
                _markers[lane].transform.localPosition = position;
                float holdingPulse = _isHolding ? 0.16f : 0f;
                _markers[lane].transform.localScale = Vector3.one * (_baseScale * (1f + (0.12f * progress) + holdingPulse));

                if (_chordLine.gameObject.activeSelf)
                {
                    _chordLine.SetPosition(chordPoint++, position);
                }
            }

            if (_holdTrail.gameObject.activeSelf)
            {
                Vector2 head = RadialNoteGeometry.Position(
                    _spawnPositions[Lane],
                    _receptorPositions[Lane],
                    songTimeSec,
                    NoteTimeSec,
                    visibleLeadSec);
                Vector2 tail = RadialNoteGeometry.Position(
                    _spawnPositions[Lane],
                    _receptorPositions[Lane],
                    songTimeSec,
                    NoteTimeSec + _durationSec,
                    visibleLeadSec);

                Color color = HoldColor;

                // 點按是圓形；長按是沿軌道伸長的長條橢圓（類似太鼓達人的長音符）。
                // 長度直接等於音符在軌道上的長度，所以尾巴一定在「放開時間」抵達判定點。
                float holdProgress = RadialNoteGeometry.Progress(songTimeSec, NoteTimeSec, visibleLeadSec);
                _bodyWidth = _baseScale * (1f + (0.12f * holdProgress) + (_isHolding ? 0.16f : 0f));
                Vector2 direction = tail - head;
                _bodyLength = Mathf.Max(direction.magnitude, _bodyWidth);

                SpriteRenderer body = _markers[Lane];
                body.color = color;
                body.transform.localPosition = (head + tail) * 0.5f;
                // 貼圖是圓形：拉的長軸在 local Y，因此旋轉量要把 local Y 對到軌道方向（方向角 − 90°）。
                body.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f);
                body.transform.localScale = new Vector3(_bodyWidth, _bodyLength, 1f);

                _holdHead.color = color;
                _holdHead.transform.localPosition = head;
                _holdHead.transform.localRotation = Quaternion.identity;
                _holdHead.transform.localScale = Vector3.one * (_bodyWidth * 1.15f);

                _holdTrail.SetPosition(0, head);
                _holdTrail.SetPosition(1, tail);
                _holdTrail.startColor = color;
                _holdTrail.endColor = new Color(color.r, color.g, color.b, 0.35f);
            }
        }

        public void BeginHold()
        {
            _isHolding = true;
        }

        public void Resolve(Color resultColor, double releaseSongTimeSec)
        {
            IsResolved = true;
            ReleaseSongTimeSec = releaseSongTimeSec;
            _isHolding = false;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((RequiredLanesMask & (1 << lane)) == 0)
                {
                    continue;
                }

                _markers[lane].color = resultColor;
                _markers[lane].transform.localScale = _isHold
                    ? new Vector3(_bodyWidth * 1.35f, _bodyLength * 1.35f, 1f)
                    : Vector3.one * (_baseScale * 1.35f);
            }

            if (_holdHead.gameObject.activeSelf)
            {
                _holdHead.color = resultColor;
            }

            SetLineColor(_chordLine, resultColor);
            SetLineColor(_holdTrail, resultColor);
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
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                _markers[lane].gameObject.SetActive(false);
                _markers[lane].transform.localRotation = Quaternion.identity;
            }

            _chordLine.gameObject.SetActive(false);
            _holdTrail.gameObject.SetActive(false);
            _holdHead.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private LineRenderer CreateLine(string objectName, Material material, int sortingOrder, float width)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = material;
            line.sortingOrder = sortingOrder;
            lineObject.SetActive(false);
            return line;
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        private int CountRequiredLanes()
        {
            int count = 0;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                if ((RequiredLanesMask & (1 << lane)) != 0)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
