using System;
using UnityEngine;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.View
{
    /// <summary>M7 的選曲、難度與延遲校準入口；只發出意圖，不參與節奏判定。</summary>
    public sealed class DemoFlowPresenter : MonoBehaviour
    {
        private GameObject _canvasObject;
        private GameObject _menuPanel;
        private GameObject _pausePanel;
        private UnityEngine.UI.Text _calibrationText;
        private Font _runtimeFont;

        public event Action<PlayDifficulty> PlayRequested;
        public event Action<double, double> CalibrationAdjusted;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action ReturnRequested;

        public bool IsMenuVisible => _menuPanel != null && _menuPanel.activeSelf;
        public bool IsPauseVisible => _pausePanel != null && _pausePanel.activeSelf;

        private void Awake()
        {
            Build();
        }

        public void ShowMenu(double audioOffsetMs, double inputOffsetMs)
        {
            _menuPanel.SetActive(true);
            RefreshCalibration(audioOffsetMs, inputOffsetMs);
        }

        public void HideMenu() => _menuPanel.SetActive(false);

        public void ShowPause() => _pausePanel.SetActive(true);

        public void HidePause() => _pausePanel.SetActive(false);

        public void RefreshCalibration(double audioOffsetMs, double inputOffsetMs)
        {
            if (_calibrationText != null)
            {
                _calibrationText.text = $"延迟校准　音频 {audioOffsetMs:+0;-0;0} ms　输入 {inputOffsetMs:+0;-0;0} ms";
            }
        }

        private void Build()
        {
            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS" }, 64);

            EnsureEventSystem();
            _canvasObject = new GameObject("M7 可玩Demo", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = _canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _menuPanel = new GameObject("选曲与校准", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _menuPanel.transform.SetParent(_canvasObject.transform, false);
            RectTransform panel = (RectTransform)_menuPanel.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            _menuPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.075f, 0.018f, 0.012f, 0.97f);

            AddText(panel, "影　韵", 94, new Vector2(0f, 390f), new Vector2(900f, 120f), new Color(1f, 0.76f, 0.23f));
            AddText(panel, "数字皮影操演 · 第一折《试灯》", 32, new Vector2(0f, 310f), new Vector2(900f, 70f), new Color(1f, 0.90f, 0.68f));

            AddDifficultyButton(panel, PlayDifficulty.Easy, 180f);
            AddDifficultyButton(panel, PlayDifficulty.Normal, 35f);
            AddDifficultyButton(panel, PlayDifficulty.Hard, -110f);

            _calibrationText = AddText(panel, string.Empty, 28, new Vector2(0f, -245f), new Vector2(900f, 60f), new Color(0.93f, 0.80f, 0.58f));
            AddSmallButton(panel, "音频 -5", new Vector2(-315f, -320f), () => CalibrationAdjusted?.Invoke(-5d, 0d));
            AddSmallButton(panel, "音频 +5", new Vector2(-105f, -320f), () => CalibrationAdjusted?.Invoke(5d, 0d));
            AddSmallButton(panel, "输入 -5", new Vector2(105f, -320f), () => CalibrationAdjusted?.Invoke(0d, -5d));
            AddSmallButton(panel, "输入 +5", new Vector2(315f, -320f), () => CalibrationAdjusted?.Invoke(0d, 5d));
            AddText(panel, "快捷键：1 / 2 / 3 选择难度　[ ] 调音频　- = 调输入", 23, new Vector2(0f, -410f), new Vector2(1100f, 55f), new Color(0.72f, 0.59f, 0.43f));
            BuildPausePanel();
        }

        private void BuildPausePanel()
        {
            _pausePanel = new GameObject("暂停菜单", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _pausePanel.transform.SetParent(_canvasObject.transform, false);
            RectTransform panel = (RectTransform)_pausePanel.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            _pausePanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.045f, 0.01f, 0.008f, 0.88f);

            AddText(panel, "演出暂停", 78, new Vector2(0f, 235f), new Vector2(800f, 110f), new Color(1f, 0.76f, 0.23f));
            UnityEngine.UI.Button resume = CreateButton(panel, "继续演出", 34, new Vector2(0f, 80f), new Vector2(560f, 92f));
            UnityEngine.UI.Button restart = CreateButton(panel, "重新开始", 34, new Vector2(0f, -40f), new Vector2(560f, 92f));
            UnityEngine.UI.Button back = CreateButton(panel, "返回选曲", 34, new Vector2(0f, -160f), new Vector2(560f, 92f));
            resume.onClick.AddListener(() => ResumeRequested?.Invoke());
            restart.onClick.AddListener(() => RestartRequested?.Invoke());
            back.onClick.AddListener(() => ReturnRequested?.Invoke());
            AddText(panel, "P / Esc 继续", 23, new Vector2(0f, -285f), new Vector2(600f, 50f), new Color(0.72f, 0.59f, 0.43f));
            _pausePanel.SetActive(false);
        }

        private void AddDifficultyButton(RectTransform parent, PlayDifficulty difficulty, float y)
        {
            string label = $"{PlayDifficultyInfo.DisplayName(difficulty)}　{PlayDifficultyInfo.Description(difficulty)}";
            UnityEngine.UI.Button button = CreateButton(parent, label, 30, new Vector2(0f, y), new Vector2(900f, 108f));
            button.onClick.AddListener(() => PlayRequested?.Invoke(difficulty));
        }

        private void AddSmallButton(RectTransform parent, string label, Vector2 position, Action action)
        {
            UnityEngine.UI.Button button = CreateButton(parent, label, 23, position, new Vector2(185f, 64f));
            button.onClick.AddListener(() => action());
        }

        private UnityEngine.UI.Button CreateButton(RectTransform parent, string label, int size, Vector2 position, Vector2 dimensions)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            buttonObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0.34f, 0.085f, 0.045f, 0.96f);
            UnityEngine.UI.Text text = AddText(rect, label, size, Vector2.zero, dimensions - new Vector2(28f, 12f), new Color(1f, 0.89f, 0.62f));
            text.raycastTarget = false;
            return buttonObject.GetComponent<UnityEngine.UI.Button>();
        }

        private UnityEngine.UI.Text AddText(RectTransform parent, string value, int size, Vector2 position, Vector2 dimensions, Color color)
        {
            var textObject = new GameObject("文字", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = textObject.GetComponent<UnityEngine.UI.Text>();
            text.font = _runtimeFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                return;
            }

            new GameObject(
                "EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
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
