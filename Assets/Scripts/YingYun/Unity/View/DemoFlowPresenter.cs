using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.View
{
    public readonly struct SongMenuEntry
    {
        public SongMenuEntry(string songId, string title, string artist)
        {
            SongId = songId;
            Title = title;
            Artist = artist;
        }

        public string SongId { get; }
        public string Title { get; }
        public string Artist { get; }
    }

    /// <summary>M7 的選曲、難度與延遲校準入口；只發出意圖，不參與節奏判定。</summary>
    public sealed class DemoFlowPresenter : MonoBehaviour
    {
        private GameObject _canvasObject;
        private GameObject _openingPanel;
        private GameObject _menuPanel;
        private GameObject _pausePanel;
        private GameObject _diagnosticsPanel;
        private CanvasGroup _menuGroup;
        private CanvasGroup _openingGroup;
        private UnityEngine.UI.Text _calibrationText;
        private UnityEngine.UI.Text _selectedSongText;
        private UnityEngine.UI.Text _songImportStatusText;
        private UnityEngine.UI.Text _songPageText;
        private UnityEngine.UI.Text _diagnosticsText;
        private UnityEngine.UI.Text _diagnosticsStatusText;
        private UnityEngine.UI.Text _openingPrompt;
        private UnityEngine.UI.Button _openingButton;
        private CanvasGroup _openingLaughGroup;
        private CanvasGroup _openingFinalGroup;
        private RectTransform _openingLaughRect;
        private RectTransform _openingFinalRect;
        private Coroutine _openingRoutine;
        private bool _openingCanDismiss;
        private Font _runtimeFont;
        private string _selectedSongId = string.Empty;
        private readonly List<SongButton> _songButtons = new List<SongButton>(3);
        private readonly List<SongMenuEntry> _songEntries = new List<SongMenuEntry>();
        private int _songPage;
        private const int SongsPerPage = 3;

        public event Action<string, PlayDifficulty> PlayRequested;
        public event Action<double, double> CalibrationAdjusted;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action ReturnRequested;
        public event Action CustomSongsRefreshRequested;
        public event Action CustomSongsFolderRequested;
        public event Action DiagnosticsRequested;
        public event Action DiagnosticsCopyRequested;
        public event Action DiagnosticsFolderRequested;
        public event Action OpeningDismissed;

        public bool IsOpeningVisible => _openingPanel != null && _openingPanel.activeSelf;
        public bool IsMenuVisible => _menuPanel != null && _menuPanel.activeSelf;
        public bool IsPauseVisible => _pausePanel != null && _pausePanel.activeSelf;
        public bool IsDiagnosticsVisible => _diagnosticsPanel != null && _diagnosticsPanel.activeSelf;
        public string SelectedSongId => _selectedSongId;
        public bool OpeningCanDismiss => _openingCanDismiss;

        private sealed class SongButton
        {
            public string SongId;
            public string DisplayText;
            public UnityEngine.UI.Button Button;
        }

        private void Awake()
        {
            Build();
        }

        public void ShowOpening()
        {
            _menuPanel.SetActive(false);
            _openingPanel.SetActive(true);
            _openingGroup.alpha = 1f;
            _menuGroup.alpha = 1f;
            _openingCanDismiss = false;
            _openingButton.interactable = false;
            SetTextAlpha(_openingPrompt, 0f);
            SetOpeningSlide(_openingLaughGroup, _openingLaughRect, 1f, 1.03f, new Vector2(-22f, 4f));
            SetOpeningSlide(_openingFinalGroup, _openingFinalRect, 0f, 1.12f, new Vector2(76f, -8f));
            if (_openingRoutine != null) StopCoroutine(_openingRoutine);
            _openingRoutine = StartCoroutine(PlayOpening());
        }

        public void ShowMenu(double audioOffsetMs, double inputOffsetMs)
        {
            _menuPanel.SetActive(true);
            _menuGroup.alpha = IsOpeningVisible ? 0f : 1f;
            RefreshCalibration(audioOffsetMs, inputOffsetMs);
        }

        public void HideMenu() => _menuPanel.SetActive(false);

        public void ShowPause() => _pausePanel.SetActive(true);

        public void HidePause() => _pausePanel.SetActive(false);

        public void ShowDiagnostics(string content, string path)
        {
            _diagnosticsText.text = content ?? string.Empty;
            _diagnosticsStatusText.text = $"日志文件：{path}";
            _diagnosticsPanel.SetActive(true);
        }

        public void HideDiagnostics() => _diagnosticsPanel.SetActive(false);

        public void SetDiagnosticsStatus(string status, string content)
        {
            _diagnosticsStatusText.text = status ?? string.Empty;
            _diagnosticsText.text = content ?? string.Empty;
        }

        public void RefreshCalibration(double audioOffsetMs, double inputOffsetMs)
        {
            if (_calibrationText != null)
            {
                _calibrationText.text = $"延迟校准　音频 {audioOffsetMs:+0;-0;0} ms　输入 {inputOffsetMs:+0;-0;0} ms";
            }
        }

        public void ConfigureSongs(IReadOnlyList<SongMenuEntry> songs, string initialSongId)
        {
            if (songs == null || songs.Count == 0)
            {
                throw new ArgumentException("At least one playable song is required.", nameof(songs));
            }

            int previousSongCount = _songEntries.Count;
            _songEntries.Clear();
            for (int i = 0; i < songs.Count; i++) _songEntries.Add(songs[i]);

            bool selectionExists = false;
            for (int i = 0; i < songs.Count; i++)
            {
                if (songs[i].SongId == _selectedSongId) selectionExists = true;
            }

            if (!selectionExists)
            {
                _selectedSongId = string.IsNullOrWhiteSpace(initialSongId) ? songs[0].SongId : initialSongId;
            }

            int selectedIndex = 0;
            for (int i = 0; i < songs.Count; i++)
            {
                if (songs[i].SongId == _selectedSongId) selectedIndex = i;
            }
            _songPage = DetermineSongPage(previousSongCount, songs.Count, selectedIndex);
            RebuildSongPage();
            RefreshSongSelection(songs);
        }

        public static int DetermineSongPage(int previousSongCount, int currentSongCount, int selectedIndex)
        {
            if (currentSongCount <= 0) throw new ArgumentOutOfRangeException(nameof(currentSongCount));
            bool songsWereAdded = currentSongCount > previousSongCount;
            return songsWereAdded ? (currentSongCount - 1) / SongsPerPage : selectedIndex / SongsPerPage;
        }

        public void SetSongImportStatus(string status)
        {
            if (_songImportStatusText != null) _songImportStatusText.text = status ?? string.Empty;
        }

        private void RebuildSongPage()
        {
            for (int i = 0; i < _songButtons.Count; i++)
            {
                if (_songButtons[i].Button != null) Destroy(_songButtons[i].Button.gameObject);
            }
            _songButtons.Clear();

            RectTransform panel = (RectTransform)_menuPanel.transform;
            float spacing = 500f;
            int first = _songPage * SongsPerPage;
            int visibleCount = Math.Min(SongsPerPage, _songEntries.Count - first);
            float startX = -((visibleCount - 1) * spacing) * 0.5f;
            for (int i = 0; i < visibleCount; i++)
            {
                SongMenuEntry entry = _songEntries[first + i];
                UnityEngine.UI.Button button = CreateButton(
                    panel,
                    entry.Title,
                    27,
                    new Vector2(startX + (i * spacing), 220f),
                    new Vector2(450f, 78f));
                string songId = entry.SongId;
                button.onClick.AddListener(() => SelectSong(songId));
                _songButtons.Add(new SongButton
                {
                    SongId = songId,
                    DisplayText = $"当前：{entry.Title}　{entry.Artist}",
                    Button = button,
                });
            }

            int pageCount = Math.Max(1, (_songEntries.Count + SongsPerPage - 1) / SongsPerPage);
            if (_songPageText != null) _songPageText.text = $"{_songPage + 1} / {pageCount}";
            RefreshSongButtonColors();
        }

        private void ChangeSongPage(int delta)
        {
            int pageCount = Math.Max(1, (_songEntries.Count + SongsPerPage - 1) / SongsPerPage);
            _songPage = (_songPage + delta + pageCount) % pageCount;
            RebuildSongPage();
        }

        public void SelectSong(string songId)
        {
            for (int i = 0; i < _songButtons.Count; i++)
            {
                if (_songButtons[i].SongId == songId)
                {
                    _selectedSongId = songId;
                    RefreshSongButtonColors();
                    _selectedSongText.text = _songButtons[i].DisplayText;
                    return;
                }
            }
        }

        private void Build()
        {
            _runtimeFont = ChineseFontProvider.Load();

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

            _menuPanel = new GameObject("选曲与校准", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            _menuPanel.transform.SetParent(_canvasObject.transform, false);
            RectTransform panel = (RectTransform)_menuPanel.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            _menuPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.075f, 0.018f, 0.012f, 0.97f);
            _menuGroup = _menuPanel.GetComponent<CanvasGroup>();

            AddText(panel, "影　韵", 86, new Vector2(0f, 405f), new Vector2(900f, 110f), new Color(1f, 0.76f, 0.23f));
            AddText(panel, "数字皮影操演 · 选择曲目与难度", 30, new Vector2(0f, 330f), new Vector2(900f, 60f), new Color(1f, 0.90f, 0.68f));
            AddSmallButton(panel, "打开歌曲文件夹", new Vector2(-130f, 280f), () => CustomSongsFolderRequested?.Invoke());
            AddSmallButton(panel, "刷新歌曲", new Vector2(130f, 280f), () => CustomSongsRefreshRequested?.Invoke());
            AddSmallButton(panel, "运行日志", new Vector2(390f, 280f), () => DiagnosticsRequested?.Invoke());
            UnityEngine.UI.Button previousPage = CreateButton(panel, "‹", 34, new Vector2(-780f, 220f), new Vector2(72f, 72f));
            UnityEngine.UI.Button nextPage = CreateButton(panel, "›", 34, new Vector2(780f, 220f), new Vector2(72f, 72f));
            previousPage.onClick.AddListener(() => ChangeSongPage(-1));
            nextPage.onClick.AddListener(() => ChangeSongPage(1));
            _songPageText = AddText(panel, string.Empty, 20, new Vector2(0f, 275f), new Vector2(100f, 40f), new Color(0.72f, 0.59f, 0.43f));
            _selectedSongText = AddText(panel, string.Empty, 25, new Vector2(0f, 150f), new Vector2(1200f, 50f), new Color(0.93f, 0.80f, 0.58f));
            _songImportStatusText = AddText(panel, string.Empty, 19, new Vector2(0f, 112f), new Vector2(1300f, 38f), new Color(0.72f, 0.59f, 0.43f));

            AddDifficultyButton(panel, PlayDifficulty.Easy, 42f);
            AddDifficultyButton(panel, PlayDifficulty.Normal, -74f);
            AddDifficultyButton(panel, PlayDifficulty.Hard, -190f);

            _calibrationText = AddText(panel, string.Empty, 25, new Vector2(0f, -270f), new Vector2(900f, 55f), new Color(0.93f, 0.80f, 0.58f));
            AddSmallButton(panel, "音频 -5", new Vector2(-315f, -340f), () => CalibrationAdjusted?.Invoke(-5d, 0d));
            AddSmallButton(panel, "音频 +5", new Vector2(-105f, -340f), () => CalibrationAdjusted?.Invoke(5d, 0d));
            AddSmallButton(panel, "输入 -5", new Vector2(105f, -340f), () => CalibrationAdjusted?.Invoke(0d, -5d));
            AddSmallButton(panel, "输入 +5", new Vector2(315f, -340f), () => CalibrationAdjusted?.Invoke(0d, 5d));
            AddText(panel, "快捷键：1 / 2 / 3 选择难度", 21, new Vector2(0f, -430f), new Vector2(1100f, 50f), new Color(0.72f, 0.59f, 0.43f));
            _menuPanel.SetActive(false);
            BuildOpeningPanel();
            BuildPausePanel();
            BuildDiagnosticsPanel();
        }

        private void BuildOpeningPanel()
        {
            _openingPanel = new GameObject("开场", typeof(RectTransform), typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button), typeof(CanvasGroup));
            _openingPanel.transform.SetParent(_canvasObject.transform, false);
            RectTransform panel = (RectTransform)_openingPanel.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            UnityEngine.UI.Image hitArea = _openingPanel.GetComponent<UnityEngine.UI.Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);
            hitArea.raycastTarget = true;
            _openingGroup = _openingPanel.GetComponent<CanvasGroup>();
            _openingButton = _openingPanel.GetComponent<UnityEngine.UI.Button>();
            _openingButton.transition = UnityEngine.UI.Selectable.Transition.None;
            _openingButton.interactable = false;
            _openingButton.onClick.AddListener(DismissOpening);

            _openingLaughGroup = CreateOpeningSlide(panel, "掩嘴笑", "YingYun/UI/Opening/OpeningLaugh", out _openingLaughRect);
            _openingFinalGroup = CreateOpeningSlide(panel, "落手定格", "YingYun/UI/Opening/OpeningFinal", out _openingFinalRect);

            var promptMask = new GameObject("开场提示底", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            promptMask.transform.SetParent(panel, false);
            RectTransform promptMaskRect = (RectTransform)promptMask.transform;
            promptMaskRect.anchorMin = promptMaskRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptMaskRect.anchoredPosition = new Vector2(0f, -438f);
            promptMaskRect.sizeDelta = new Vector2(980f, 96f);
            UnityEngine.UI.Image promptMaskImage = promptMask.GetComponent<UnityEngine.UI.Image>();
            promptMaskImage.color = new Color(0.93f, 0.895f, 0.81f, 0.97f);
            promptMaskImage.raycastTarget = false;
            _openingPrompt = AddText(panel, "点击任意处开始游戏", 34, new Vector2(0f, -438f), new Vector2(900f, 72f), new Color(0.56f, 0.29f, 0.08f, 0f));
            _openingPanel.SetActive(false);
        }

        private static CanvasGroup CreateOpeningSlide(
            RectTransform parent,
            string name,
            string resourcePath,
            out RectTransform rect)
        {
            var slideObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage),
                typeof(UnityEngine.UI.AspectRatioFitter), typeof(CanvasGroup));
            slideObject.transform.SetParent(parent, false);
            rect = (RectTransform)slideObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            UnityEngine.UI.RawImage image = slideObject.GetComponent<UnityEngine.UI.RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            UnityEngine.UI.AspectRatioFitter fitter = slideObject.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture == null ? 16f / 9f : (float)texture.width / texture.height;
            return slideObject.GetComponent<CanvasGroup>();
        }

        private IEnumerator PlayOpening()
        {
            yield return new WaitForSecondsRealtime(0.70f);
            float elapsed = 0f;
            const float laughDuration = 1.60f;
            while (elapsed < laughDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / laughDuration));
                SetOpeningSlide(_openingLaughGroup, _openingLaughRect, 1f,
                    Mathf.Lerp(1.03f, 1.11f, t), Vector2.Lerp(new Vector2(-22f, 4f), new Vector2(24f, 14f), t));
                yield return null;
            }

            elapsed = 0f;
            const float transitionDuration = 1.35f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                SetOpeningSlide(_openingLaughGroup, _openingLaughRect, 1f - t,
                    Mathf.Lerp(1.11f, 1.16f, t), Vector2.Lerp(new Vector2(24f, 14f), new Vector2(-92f, 5f), t));
                SetOpeningSlide(_openingFinalGroup, _openingFinalRect, t,
                    Mathf.Lerp(1.12f, 1f, t), Vector2.Lerp(new Vector2(76f, -8f), Vector2.zero, t));
                yield return null;
            }

            SetOpeningSlide(_openingLaughGroup, _openingLaughRect, 0f, 1.16f, new Vector2(-92f, 5f));
            SetOpeningSlide(_openingFinalGroup, _openingFinalRect, 1f, 1f, Vector2.zero);
            yield return new WaitForSecondsRealtime(0.45f);
            float fade = 0f;
            while (fade < 1f)
            {
                fade += Time.unscaledDeltaTime / 0.80f;
                SetTextAlpha(_openingPrompt, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fade)));
                yield return null;
            }

            SetTextAlpha(_openingPrompt, 1f);
            _openingCanDismiss = true;
            _openingButton.interactable = true;
            _openingRoutine = null;
        }

        private static void SetOpeningSlide(CanvasGroup group, RectTransform rect, float alpha, float scale, Vector2 position)
        {
            if (group != null) group.alpha = alpha;
            if (rect == null) return;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = position;
        }

        private void DismissOpening()
        {
            if (!_openingCanDismiss) return;
            _openingCanDismiss = false;
            _openingButton.interactable = false;
            if (_openingRoutine != null) StopCoroutine(_openingRoutine);
            _openingRoutine = StartCoroutine(TransitionToMenu());
        }

        private IEnumerator TransitionToMenu()
        {
            OpeningDismissed?.Invoke();
            float elapsed = 0f;
            const float duration = 0.75f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _openingGroup.alpha = 1f - t;
                _menuGroup.alpha = t;
                float scale = Mathf.Lerp(1f, 1.035f, t);
                _openingFinalRect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            _openingGroup.alpha = 0f;
            _menuGroup.alpha = 1f;
            _openingPanel.SetActive(false);
            _openingGroup.alpha = 1f;
            _openingRoutine = null;
        }

        private static void SetTextAlpha(UnityEngine.UI.Text text, float alpha)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }

        private void BuildDiagnosticsPanel()
        {
            _diagnosticsPanel = new GameObject("运行日志", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _diagnosticsPanel.transform.SetParent(_canvasObject.transform, false);
            RectTransform panel = (RectTransform)_diagnosticsPanel.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            _diagnosticsPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.01f, 0.008f, 0.98f);

            AddText(panel, "运行诊断", 58, new Vector2(0f, 450f), new Vector2(700f, 80f), new Color(1f, 0.76f, 0.23f));
            _diagnosticsText = AddText(panel, string.Empty, 18, new Vector2(0f, 30f), new Vector2(1640f, 720f), new Color(0.93f, 0.86f, 0.70f));
            _diagnosticsText.alignment = TextAnchor.UpperLeft;
            _diagnosticsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _diagnosticsText.verticalOverflow = VerticalWrapMode.Truncate;
            _diagnosticsStatusText = AddText(panel, string.Empty, 18, new Vector2(0f, -375f), new Vector2(1600f, 44f), new Color(0.72f, 0.59f, 0.43f));
            AddSmallButton(panel, "刷新", new Vector2(-310f, -445f), () => DiagnosticsRequested?.Invoke());
            AddSmallButton(panel, "复制完整日志", new Vector2(-95f, -445f), () => DiagnosticsCopyRequested?.Invoke());
            AddSmallButton(panel, "打开日志文件夹", new Vector2(135f, -445f), () => DiagnosticsFolderRequested?.Invoke());
            AddSmallButton(panel, "关闭", new Vector2(350f, -445f), HideDiagnostics);
            _diagnosticsPanel.SetActive(false);
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
            button.onClick.AddListener(() => PlayRequested?.Invoke(_selectedSongId, difficulty));
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

        private void RefreshSongSelection(IReadOnlyList<SongMenuEntry> songs)
        {
            for (int i = 0; i < songs.Count; i++)
            {
                if (songs[i].SongId == _selectedSongId)
                {
                    _selectedSongText.text = $"当前：{songs[i].Title}　{songs[i].Artist}";
                    break;
                }
            }

            RefreshSongButtonColors();
        }

        private void RefreshSongButtonColors()
        {
            for (int i = 0; i < _songButtons.Count; i++)
            {
                bool selected = _songButtons[i].SongId == _selectedSongId;
                _songButtons[i].Button.GetComponent<UnityEngine.UI.Image>().color = selected
                    ? new Color(0.68f, 0.22f, 0.07f, 1f)
                    : new Color(0.34f, 0.085f, 0.045f, 0.96f);
            }
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
            if (_openingRoutine != null) StopCoroutine(_openingRoutine);
            if (_runtimeFont != null)
            {
                ChineseFontProvider.Release(_runtimeFont);
            }
        }
    }
}
