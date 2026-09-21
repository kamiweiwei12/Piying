using UnityEngine;
using YingYun.Rhythm.Judgment;
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
        private UnityEngine.UI.Text _resultTitle;
        private UnityEngine.UI.Text _resultDetails;
        private GameObject _resultPanel;
        private Font _runtimeFont;
        private int _totalNotes;
        private Rect _lastSafeArea;
        private DifficultyConfig _difficulty;

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
            _totalNotes = totalNotes;
            _difficulty = difficulty ?? throw new System.ArgumentNullException(nameof(difficulty));
            _statistics.Reset();
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }

            RefreshHud(0, 0, 0d);
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

        public void ShowResult()
        {
            ResultRating rating = ResultGradeCalculator.Calculate(_statistics.Accuracy, _difficulty);
            _resultTitle.text = RatingText(rating);
            _resultDetails.text =
                $"总得分　{_statistics.Score:N0}\n" +
                $"准确率　{_statistics.Accuracy:P2}\n" +
                $"最高连击　{_statistics.MaxCombo}\n\n" +
                $"契合　{_statistics.PerfectCount}　协律　{_statistics.GreatCount}\n" +
                $"应拍　{_statistics.GoodCount}　空引　{_statistics.MissCount}\n\n" +
                $"完成　{_statistics.JudgedCount}/{_totalNotes}\n" +
                "按 Enter 返回选曲　·　按 R 再奏";
            _resultPanel.SetActive(true);
        }

        private void BuildHud()
        {
            _runtimeFont = CreateChineseFont();

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

            BuildResultPanel();
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
            Anchor(_resultDetails.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(640f, 470f), new Vector2(0.5f, 0.5f));
            _resultPanel.SetActive(false);
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
            return Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS" },
                64);
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
                Destroy(_runtimeFont);
            }
        }
    }
}
