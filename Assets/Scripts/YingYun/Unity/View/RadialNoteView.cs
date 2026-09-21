using UnityEngine;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.View
{
    /// <summary>單顆邏輯音符的畫面表示；可預建六部位標記，但不包含判定邏輯。</summary>
    public sealed class RadialNoteView : MonoBehaviour
    {
        private const int LaneCount = 6;

        private readonly SpriteRenderer[] _markers = new SpriteRenderer[LaneCount];
        private readonly Vector2[] _spawnPositions = new Vector2[LaneCount];
        private readonly Vector2[] _receptorPositions = new Vector2[LaneCount];
        private readonly Color[] _laneColors = new Color[LaneCount];
        private LineRenderer _chordLine;
        private LineRenderer _holdTrail;
        private float _baseScale;
        private double _durationSec;
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
            _isHolding = false;
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;

            for (int lane = 0; lane < LaneCount; lane++)
            {
                _spawnPositions[lane] = spawnPositions[lane];
                _receptorPositions[lane] = receptorPositions[lane];
                _laneColors[lane] = laneColors[lane];
                bool active = (RequiredLanesMask & (1 << lane)) != 0;
                _markers[lane].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                _markers[lane].color = laneColors[lane];
                _markers[lane].transform.localScale = Vector3.one * _baseScale;
                _markers[lane].transform.localPosition = spawnPositions[lane];
            }

            _chordLine.gameObject.SetActive(note.IsChord);
            if (note.IsChord)
            {
                _chordLine.positionCount = CountRequiredLanes();
                SetLineColor(_chordLine, new Color(1f, 0.82f, 0.28f));
            }

            _holdTrail.gameObject.SetActive(note.Kind == NoteKind.Hold);
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
                _holdTrail.SetPosition(0, head);
                _holdTrail.SetPosition(1, tail);
                Color color = _laneColors[Lane];
                if (_isHolding)
                {
                    color = Color.Lerp(color, new Color(1f, 0.88f, 0.28f), 0.55f);
                }

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
                _markers[lane].transform.localScale = Vector3.one * (_baseScale * 1.35f);
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
            _isHolding = false;
            IsResolved = false;
            ReleaseSongTimeSec = double.PositiveInfinity;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                _markers[lane].gameObject.SetActive(false);
            }

            _chordLine.gameObject.SetActive(false);
            _holdTrail.gameObject.SetActive(false);
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
