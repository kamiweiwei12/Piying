using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.View
{
    /// <summary>事件驅動的中文 HUD 與結算顯示，不參與判定或計分。</summary>
    public sealed class GameplayHudPresenter : MonoBehaviour
    {
        private readonly GameplayStatistics _statistics = new GameplayStatistics();
        private RectTransform _safeArea;
        private UnityEngine.UI.Text _comboText;
        private UnityEngine.UI.Text _scoreText;
        private UnityEngine.UI.Text _accuracyText;
        private UnityEngine.UI.Text _danceNameText;
        private UnityEngine.UI.Text _danceStateText;
        private UnityEngine.UI.Text _danceDetailText;
        private UnityEngine.UI.Text _resultTitle;
        private UnityEngine.UI.Text _resultDetails;
        private UnityEngine.UI.Text _resultFooter;
        private readonly UnityEngine.UI.Text[] _resultGradeCounts = new UnityEngine.UI.Text[4];
        private JudgmentCalligraphyAtlas _calligraphyAtlas;
        private GameObject _resultPanel;
        private GameObject _countdownPanel;
        private UnityEngine.UI.Text _countdownText;
        private Font _runtimeFont;
        private int _totalNotes;
        private Rect _lastSafeArea;
        private DifficultyConfig _difficulty;
        private bool _wasCountingDown;

        public bool IsResultVisible => _resultPanel != null && _resultPanel.activeSelf;
        public GameplayStatistics Statistics => _statistics;

        public void SetVisible(bool visible)
        {
            if (_safeArea != null)
            {
                _safeArea.gameObject.SetActive(visible);
            }
        }

        private void Awake()
        {
            BuildHud();
        }

        public void Begin(int totalNotes, DifficultyConfig difficulty)
        {
            if (_safeArea == null) BuildHud();
            _totalNotes = totalNotes;
            _difficulty = difficulty ?? throw new System.ArgumentNullException(nameof(difficulty));
            _statistics.Reset();
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }

            RefreshHud(0, 0, 0d);
            ShowDanceStatus(new DancePerformanceStatus(DancePerformanceKind.Waiting, null));
            _wasCountingDown = true;
            if (_countdownPanel != null)
            {
                _countdownPanel.SetActive(true);
            }
        }

        public void TickSongTime(double songTime)
        {
            if (_countdownPanel == null)
            {
                return;
            }

            if (songTime < 0d)
            {
                int count = Mathf.Clamp(Mathf.CeilToInt((float)-songTime), 1, 3);
                _countdownText.text = count.ToString();
                _countdownPanel.SetActive(true);
                _wasCountingDown = true;
                return;
            }

            if (_wasCountingDown && songTime < 0.35d)
            {
                _countdownText.text = "开演";
                _countdownPanel.SetActive(true);
                return;
            }

            _wasCountingDown = false;
            _countdownPanel.SetActive(false);
        }

        public void OnJudged(JudgmentResult result)
        {
            if (result.EventKind != JudgmentEventKind.NoteJudged)
            {
                return;
            }

            _statistics.Apply(result);
            RefreshHud(result.ComboAfter, result.ScoreAfter, result.AccuracyAfter);
        }

        public void ShowDanceStatus(DancePerformanceStatus status)
        {
            if (_danceNameText == null) return;
            DancePhrase performed = status.Performed;
            switch (status.Kind)
            {
                case DancePerformanceKind.Pending:
                    _danceNameText.text = performed == null ? "尚未起势" : $"保持：{performed.Name}";
                    _danceStateText.text = $"已命中 · 待拍：{status.Cue?.Name}";
                    _danceDetailText.text = "到达拍点后才开始演出";
                    break;
                case DancePerformanceKind.Performing:
                case DancePerformanceKind.Closing:
                    _danceNameText.text = performed.Name;
                    _danceStateText.text = status.Kind == DancePerformanceKind.Closing ? "正在收势" : "正在演出";
                    _danceDetailText.text =
                        $"第 {performed.StartBeat} 拍 · 持续 {performed.DurationBeats} 拍\n控制：{performed.JointDisplay}";
                    break;
                case DancePerformanceKind.Holding:
                    _danceNameText.text = $"保持：{performed.Name}";
                    _danceStateText.text = "本式结束";
                    _danceDetailText.text = "等待下一式，不自动起舞";
                    break;
                case DancePerformanceKind.Interrupted:
                    _danceNameText.text = performed == null ? "尚未起势" : $"保持：{performed.Name}";
                    _danceStateText.text = $"漏击：{status.Cue?.Name} 未演";
                    _danceDetailText.text = "皮影停势，等待下一次成功命中";
                    break;
                default:
                    _danceNameText.text = "尚未起势";
                    _danceStateText.text = "等待开演";
                    _danceDetailText.text = "命中对应音符后才演出动作";
                    break;
            }
        }

        public void ShowResult()
        {
            ResultRating rating = ResultGradeCalculator.Calculate(_statistics.Accuracy, _difficulty);
            _resultTitle.text = RatingText(rating);
            _resultDetails.text =
                $"总得分　{_statistics.Score:N0}\n" +
                $"准确率　{_statistics.Accuracy:P2}\n" +
                $"最高连击　{_statistics.MaxCombo}";
            _resultGradeCounts[0].text = _statistics.PerfectCount.ToString();
            _resultGradeCounts[1].text = _statistics.GreatCount.ToString();
            _resultGradeCounts[2].text = _statistics.GoodCount.ToString();
            _resultGradeCounts[3].text = _statistics.MissCount.ToString();
            _resultFooter.text =
                $"完成　{_statistics.JudgedCount}/{_totalNotes}\n" +
                "按 Enter 返回选曲　·　按 R 再奏";
            _resultPanel.SetActive(true);
        }

        private void BuildHud()
        {
            _runtimeFont = CreateChineseFont();
            _calligraphyAtlas = JudgmentCalligraphyAtlas.Load();

            var canvasObject = new GameObject("M4 中文界面", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _safeArea = CreateRect("安全区域", canvasObject.transform);
            ApplySafeArea();

            _comboText = CreateText("连击", _safeArea, "连击　0", 42, TextAnchor.UpperLeft);
            Anchor(_comboText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -28f), new Vector2(420f, 90f), new Vector2(0f, 1f));

            _scoreText = CreateText("得分", _safeArea, "得分　0", 42, TextAnchor.UpperRight);
            Anchor(_scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f), new Vector2(520f, 90f), new Vector2(1f, 1f));

            _accuracyText = CreateText("准确", _safeArea, "准确　0.00%", 38, TextAnchor.UpperCenter);
            Anchor(_accuracyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(460f, 80f), new Vector2(0.5f, 1f));

            BuildDancePanel();
            BuildResultPanel();
            BuildCountdownPanel();
        }

        private void BuildDancePanel()
        {
            var panelObject = new GameObject("演出解说", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panelObject.transform.SetParent(_safeArea, false);
            var panel = (RectTransform)panelObject.transform;
            Anchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -140f), new Vector2(420f, 250f), new Vector2(0f, 1f));
            var background = panelObject.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.10f, 0.025f, 0.018f, 0.80f);
            background.raycastTarget = false;

            var heading = CreateText("解说标题", panel, "影戏身段", 26, TextAnchor.UpperLeft);
            Anchor(heading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -16f), new Vector2(380f, 35f), new Vector2(0f, 1f));
            heading.color = new Color(1f, 0.76f, 0.23f);

            _danceNameText = CreateText("当前动作", panel, "尚未起势", 40, TextAnchor.UpperLeft);
            Anchor(_danceNameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -55f), new Vector2(380f, 55f), new Vector2(0f, 1f));
            _danceStateText = CreateText("演出状态", panel, "等待开演", 27, TextAnchor.UpperLeft);
            Anchor(_danceStateText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -120f), new Vector2(380f, 40f), new Vector2(0f, 1f));
            _danceDetailText = CreateText("拍数与控制", panel, "命中对应音符后才演出动作", 24, TextAnchor.UpperLeft);
            Anchor(_danceDetailText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -165f), new Vector2(380f, 75f), new Vector2(0f, 1f));
        }

        private void BuildCountdownPanel()
        {
            _countdownPanel = new GameObject("开演倒数", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _countdownPanel.transform.SetParent(_safeArea, false);
            RectTransform rect = (RectTransform)_countdownPanel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(360f, 260f);
            UnityEngine.UI.Image image = _countdownPanel.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.10f, 0.022f, 0.014f, 0.88f);
            image.raycastTarget = false;
            _countdownText = CreateText("倒数", rect, "3", 118, TextAnchor.MiddleCenter);
            Anchor(_countdownText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            _countdownText.color = new Color(1f, 0.76f, 0.23f);
            _countdownPanel.SetActive(false);
        }

        private void BuildResultPanel()
        {
            _resultPanel = new GameObject("结算", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _resultPanel.transform.SetParent(_safeArea, false);
            var rect = (RectTransform)_resultPanel.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 690f);
            rect.anchoredPosition = Vector2.zero;
            var image = _resultPanel.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.12f, 0.025f, 0.018f, 0.94f);
            image.raycastTarget = false;

            _resultTitle = CreateText("总评", rect, string.Empty, 92, TextAnchor.MiddleCenter);
            Anchor(_resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(620f, 130f), new Vector2(0.5f, 1f));
            _resultTitle.color = new Color(1f, 0.78f, 0.24f);

            _resultDetails = CreateText("明细", rect, string.Empty, 34, TextAnchor.MiddleCenter);
            Anchor(_resultDetails.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(640f, 155f), new Vector2(0.5f, 1f));

            CreateResultGradeCell(rect, "契合", 0, new Vector2(-175f, -370f));
            CreateResultGradeCell(rect, "协律", 1, new Vector2(175f, -370f));
            CreateResultGradeCell(rect, "应拍", 2, new Vector2(-175f, -485f));
            CreateResultGradeCell(rect, "空引", 3, new Vector2(175f, -485f));

            _resultFooter = CreateText("结算操作", rect, string.Empty, 25, TextAnchor.MiddleCenter);
            Anchor(_resultFooter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(640f, 82f), new Vector2(0.5f, 0f));
            _resultPanel.SetActive(false);
        }

        private void CreateResultGradeCell(
            RectTransform parent,
            string label,
            int index,
            Vector2 position)
        {
            var cellObject = new GameObject($"{label}统计", typeof(RectTransform));
            cellObject.transform.SetParent(parent, false);
            RectTransform cell = (RectTransform)cellObject.transform;
            Anchor(cell, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(300f, 100f), new Vector2(0.5f, 1f));

            var imageObject = new GameObject(label, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(cell, false);
            RectTransform imageRect = (RectTransform)imageObject.transform;
            Anchor(imageRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(190f, 92f), new Vector2(0f, 0.5f));
            UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
            image.sprite = _calligraphyAtlas.Get(label);
            image.color = JudgmentCalligraphyAtlas.InkColor;
            image.preserveAspect = true;
            image.raycastTarget = false;

            _resultGradeCounts[index] = CreateText("次数", cell, "0", 38, TextAnchor.MiddleRight);
            Anchor(_resultGradeCounts[index].rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(85f, 70f), new Vector2(1f, 0.5f));
        }

        private UnityEngine.UI.Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<UnityEngine.UI.Text>();
            text.font = _runtimeFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = new Color(1f, 0.89f, 0.62f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void RefreshHud(int combo, int score, double accuracy)
        {
            _comboText.text = $"连击　{combo}";
            _scoreText.text = $"得分　{score:N0}";
            _accuracyText.text = $"准确　{accuracy:P2}";
        }

        private void ApplySafeArea()
        {
            if (_safeArea == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            _lastSafeArea = safe;
            _safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Font CreateChineseFont()
        {
            return ChineseFontProvider.Load();
        }

        private static string RatingText(ResultRating rating)
        {
            switch (rating)
            {
                case ResultRating.TianCheng: return "天成";
                case ResultRating.ChuanShen: return "传神";
                case ResultRating.RuYun: return "入韵";
                case ResultRating.ChuCheng: return "初成";
                default: return "未成";
            }
        }

        private void OnDestroy()
        {
            if (_runtimeFont != null)
            {
                ChineseFontProvider.Release(_runtimeFont);
            }

            _calligraphyAtlas?.Dispose();
        }
    }
}
