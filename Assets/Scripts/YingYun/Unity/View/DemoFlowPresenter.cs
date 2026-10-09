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
        private CanvasGroup _pauseGroup;
        private UnityEngine.UI.Text _calibrationText;
        private UnityEngine.UI.Text _selectedSongText;
        private UnityEngine.UI.Text _songImportStatusText;
        private UnityEngine.UI.Text _songPageText;
        private UnityEngine.UI.Text _diagnosticsText;
        private UnityEngine.UI.Text _diagnosticsStatusText;
        private UnityEngine.UI.Button _openingButton;
        private CanvasGroup _openingFinalGroup;
        private RectTransform _openingFinalRect;
        private RectTransform _pausePlaqueRect;
        private Coroutine _openingRoutine;
        private Coroutine _pauseRoutine;
        private Coroutine _songCarouselRoutine;
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

        /// <summary>本機檔案與資料夾入口只在具備桌面檔案系統的版本顯示。</summary>
        public static bool SupportsLocalFileFeatures(RuntimePlatform platform)
        {
            return platform != RuntimePlatform.WebGLPlayer;
        }

        private sealed class SongButton
        {
            public string SongId;
            public string DisplayText;
            public UnityEngine.UI.Button Button;
            public RectTransform Rect;
            public UnityEngine.UI.Image CardFace;
            public UnityEngine.UI.Text Title;
            public UnityEngine.UI.Text Artist;
        }

        private static readonly Color InkBrown = new Color(0.16f, 0.075f, 0.052f, 1f);
        private static readonly Color LacquerRed = new Color(0.31f, 0.075f, 0.052f, 1f);
        private static readonly Color Cinnabar = new Color(0.49f, 0.14f, 0.085f, 1f);
        private static readonly Color AntiqueGold = new Color(0.68f, 0.49f, 0.22f, 1f);
        private static readonly Color WarmGold = new Color(0.82f, 0.64f, 0.33f, 1f);
        private static readonly Color RicePaper = new Color(0.91f, 0.855f, 0.72f, 1f);
        private static readonly Color LightRicePaper = new Color(0.965f, 0.925f, 0.82f, 1f);
        private static readonly Color MutedJade = new Color(0.15f, 0.27f, 0.23f, 1f);

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
            if (_openingRoutine != null) StopCoroutine(_openingRoutine);
            _openingRoutine = null;
            SetOpeningSlide(_openingFinalGroup, _openingFinalRect, 1f, 1f, Vector2.zero);
            _openingCanDismiss = true;
            _openingButton.interactable = true;
        }

        public void ShowMenu(double audioOffsetMs, double inputOffsetMs)
        {
            _menuPanel.SetActive(true);
            _menuGroup.alpha = IsOpeningVisible ? 0f : 1f;
            RefreshCalibration(audioOffsetMs, inputOffsetMs);
        }

        public void HideMenu() => _menuPanel.SetActive(false);

        public void ShowPause()
        {
            _pausePanel.SetActive(true);
            if (_pauseRoutine != null) StopCoroutine(_pauseRoutine);
            if (!Application.isPlaying)
            {
                SetPauseVisual(1f, 1f, Vector2.zero);
                return;
            }

            _pauseRoutine = StartCoroutine(PlayPauseReveal());
        }

        public void HidePause()
        {
            if (_pauseRoutine != null)
            {
                StopCoroutine(_pauseRoutine);
                _pauseRoutine = null;
            }
            _pausePanel.SetActive(false);
            SetPauseVisual(1f, 1f, Vector2.zero);
        }

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
            if (_songCarouselRoutine != null)
            {
                StopCoroutine(_songCarouselRoutine);
                _songCarouselRoutine = null;
            }
            for (int i = 0; i < _songButtons.Count; i++)
            {
                if (_songButtons[i].Button != null) Destroy(_songButtons[i].Button.gameObject);
            }
            _songButtons.Clear();

            RectTransform panel = (RectTransform)_menuPanel.transform;
            int first = _songPage * SongsPerPage;
            int visibleCount = Math.Min(SongsPerPage, _songEntries.Count - first);
            for (int i = 0; i < visibleCount; i++)
            {
                SongMenuEntry entry = _songEntries[first + i];
                SongButton songButton = CreateSongCard(panel, entry, i, visibleCount);
                string songId = entry.SongId;
                songButton.Button.onClick.AddListener(() => SelectSong(songId));
                _songButtons.Add(songButton);
            }

            int pageCount = Math.Max(1, (_songEntries.Count + SongsPerPage - 1) / SongsPerPage);
            if (_songPageText != null) _songPageText.text = $"{_songPage + 1} / {pageCount}";
            RefreshSongButtonColors(false);
        }

        private void SelectAdjacentSong(int delta)
        {
            if (_songEntries.Count == 0) return;

            int selectedIndex = 0;
            for (int i = 0; i < _songEntries.Count; i++)
            {
                if (_songEntries[i].SongId == _selectedSongId)
                {
                    selectedIndex = i;
                    break;
                }
            }

            int nextIndex = (selectedIndex + delta + _songEntries.Count) % _songEntries.Count;
            SongMenuEntry next = _songEntries[nextIndex];
            _selectedSongId = next.SongId;
            int nextPage = nextIndex / SongsPerPage;
            if (nextPage != _songPage)
            {
                _songPage = nextPage;
                RebuildSongPage();
            }
            else
            {
                RefreshSongButtonColors(true);
            }

            _selectedSongText.text = $"当前：{next.Title}　{next.Artist}";
        }

        public void SelectSong(string songId)
        {
            for (int i = 0; i < _songButtons.Count; i++)
            {
                if (_songButtons[i].SongId == songId)
                {
                    _selectedSongId = songId;
                    RefreshSongButtonColors(true);
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
            _menuPanel.GetComponent<UnityEngine.UI.Image>().color = InkBrown;
            _menuGroup = _menuPanel.GetComponent<CanvasGroup>();

            BuildStageFrame(panel);
            AddText(panel, "皮影声律", 64, new Vector2(0f, 446f), new Vector2(700f, 82f), LacquerRed);
            AddText(panel, "皮影随乐 · 择曲开演", 24, new Vector2(0f, 394f), new Vector2(760f, 42f), new Color(0.38f, 0.24f, 0.14f, 1f));
            if (SupportsLocalFileFeatures(Application.platform))
            {
                AddSmallButton(panel, "歌曲文件夹", new Vector2(-245f, 342f), () => CustomSongsFolderRequested?.Invoke());
                AddSmallButton(panel, "刷新曲目", new Vector2(0f, 342f), () => CustomSongsRefreshRequested?.Invoke());
                AddSmallButton(panel, "运行日志", new Vector2(245f, 342f), () => DiagnosticsRequested?.Invoke());
            }
            else
            {
                AddSmallButton(panel, "运行日志", new Vector2(0f, 342f), () => DiagnosticsRequested?.Invoke());
            }
            UnityEngine.UI.Button previousPage = CreateButton(panel, "‹", 34, new Vector2(-800f, 207f), new Vector2(66f, 92f));
            UnityEngine.UI.Button nextPage = CreateButton(panel, "›", 34, new Vector2(800f, 207f), new Vector2(66f, 92f));
            previousPage.onClick.AddListener(() => SelectAdjacentSong(-1));
            nextPage.onClick.AddListener(() => SelectAdjacentSong(1));
            _songPageText = AddText(panel, string.Empty, 18, new Vector2(0f, 303f), new Vector2(120f, 34f), AntiqueGold);
            _selectedSongText = AddText(panel, string.Empty, 25, new Vector2(0f, 92f), new Vector2(1250f, 46f), LacquerRed);
            _songImportStatusText = AddText(panel, string.Empty, 17, new Vector2(0f, 62f), new Vector2(1300f, 32f), new Color(0.42f, 0.31f, 0.20f, 1f));

            AddText(panel, "择　难　度", 22, new Vector2(0f, 22f), new Vector2(300f, 38f), AntiqueGold);
            AddDifficultyButton(panel, PlayDifficulty.Easy, new Vector2(-390f, -48f));
            AddDifficultyButton(panel, PlayDifficulty.Normal, new Vector2(0f, -48f));
            AddDifficultyButton(panel, PlayDifficulty.Hard, new Vector2(390f, -48f));

            _calibrationText = AddText(panel, string.Empty, 22, new Vector2(0f, -145f), new Vector2(900f, 46f), new Color(0.34f, 0.23f, 0.14f, 1f));
            AddSmallButton(panel, "音频 -5", new Vector2(-315f, -205f), () => CalibrationAdjusted?.Invoke(-5d, 0d));
            AddSmallButton(panel, "音频 +5", new Vector2(-105f, -205f), () => CalibrationAdjusted?.Invoke(5d, 0d));
            AddSmallButton(panel, "输入 -5", new Vector2(105f, -205f), () => CalibrationAdjusted?.Invoke(0d, -5d));
            AddSmallButton(panel, "输入 +5", new Vector2(315f, -205f), () => CalibrationAdjusted?.Invoke(0d, 5d));
            AddText(panel, "按 1 / 2 / 3 选择难度并开演", 19, new Vector2(0f, -275f), new Vector2(1100f, 42f), new Color(0.43f, 0.32f, 0.22f, 1f));
            _menuPanel.SetActive(false);
            BuildOpeningPanel();
            BuildPausePanel();
            BuildDiagnosticsPanel();
        }

        private void BuildStageFrame(RectTransform parent)
        {
            AddPanel(parent, "宣纸幕", Vector2.zero, new Vector2(1740f, 1000f), RicePaper);
            AddPanel(parent, "内层宣纸", new Vector2(0f, -4f), new Vector2(1650f, 944f), LightRicePaper);
            AddPanel(parent, "上檐", new Vector2(0f, 505f), new Vector2(1920f, 70f), LacquerRed);
            AddPanel(parent, "下檐", new Vector2(0f, -505f), new Vector2(1920f, 70f), LacquerRed);
            AddPanel(parent, "左戏台边", new Vector2(-905f, 0f), new Vector2(110f, 1080f), LacquerRed);
            AddPanel(parent, "右戏台边", new Vector2(905f, 0f), new Vector2(110f, 1080f), LacquerRed);
            AddPanel(parent, "上金线", new Vector2(0f, 472f), new Vector2(1710f, 4f), WarmGold);
            AddPanel(parent, "下金线", new Vector2(0f, -472f), new Vector2(1710f, 4f), AntiqueGold);
            AddPanel(parent, "左金线", new Vector2(-850f, 0f), new Vector2(4f, 944f), AntiqueGold);
            AddPanel(parent, "右金线", new Vector2(850f, 0f), new Vector2(4f, 944f), AntiqueGold);

            AddCornerOrnament(parent, new Vector2(-806f, 428f), 1f);
            AddCornerOrnament(parent, new Vector2(806f, 428f), -1f);
            AddCornerOrnament(parent, new Vector2(-806f, -428f), 1f);
            AddCornerOrnament(parent, new Vector2(806f, -428f), -1f);
        }

        private SongButton CreateSongCard(RectTransform parent, SongMenuEntry entry, int index, int visibleCount)
        {
            float startX = -((visibleCount - 1) * 500f) * 0.5f;
            var cardObject = new GameObject($"曲目卡片 {entry.Title}", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(CanvasGroup));
            cardObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)cardObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(startX + (index * 500f), 200f);
            rect.sizeDelta = new Vector2(450f, 176f);

            UnityEngine.UI.Image border = cardObject.GetComponent<UnityEngine.UI.Image>();
            border.color = AntiqueGold;
            UnityEngine.UI.Button button = cardObject.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = border;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.78f, 1f);
            colors.pressedColor = new Color(0.78f, 0.60f, 0.35f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            RectTransform faceRect = AddPanel(rect, "宣纸卡面", Vector2.zero, new Vector2(438f, 164f), RicePaper);
            UnityEngine.UI.Image face = faceRect.GetComponent<UnityEngine.UI.Image>();
            AddPanel(faceRect, "上饰线", new Vector2(0f, 65f), new Vector2(370f, 2f), AntiqueGold);
            AddPanel(faceRect, "下饰线", new Vector2(0f, -65f), new Vector2(370f, 2f), AntiqueGold);
            AddPanel(faceRect, "左印", new Vector2(-182f, 0f), new Vector2(8f, 88f), Cinnabar);
            RectTransform knot = AddPanel(faceRect, "曲目菱印", new Vector2(181f, 0f), new Vector2(16f, 16f), Cinnabar);
            knot.localRotation = Quaternion.Euler(0f, 0f, 45f);
            UnityEngine.UI.Text title = AddText(faceRect, entry.Title, 30, new Vector2(8f, 24f), new Vector2(380f, 56f), LacquerRed);
            UnityEngine.UI.Text artist = AddText(faceRect,
                string.IsNullOrWhiteSpace(entry.Artist) ? "佚名" : entry.Artist,
                19,
                new Vector2(8f, -30f),
                new Vector2(380f, 40f),
                new Color(0.34f, 0.25f, 0.17f, 1f));
            title.raycastTarget = false;
            artist.raycastTarget = false;

            return new SongButton
            {
                SongId = entry.SongId,
                DisplayText = $"当前：{entry.Title}　{entry.Artist}",
                Button = button,
                Rect = rect,
                CardFace = face,
                Title = title,
                Artist = artist,
            };
        }

        private RectTransform AddPanel(RectTransform parent, string name, Vector2 position, Vector2 dimensions, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)panelObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            UnityEngine.UI.Image image = panelObject.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private void AddCornerOrnament(RectTransform parent, Vector2 position, float direction)
        {
            RectTransform horizontal = AddPanel(parent, "回纹横饰", position, new Vector2(76f, 6f), AntiqueGold);
            horizontal.localScale = new Vector3(direction, 1f, 1f);
            AddPanel(horizontal, "回纹短线", new Vector2(31f, -18f), new Vector2(6f, 42f), AntiqueGold);
            RectTransform diamond = AddPanel(horizontal, "菱花", new Vector2(7f, 0f), new Vector2(20f, 20f), Cinnabar);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
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

            _openingFinalGroup = CreateOpeningSlide(panel, "落手定格", "YingYun/UI/Opening/OpeningFinal", out _openingFinalRect);
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
                yield return null;
            }

            _openingGroup.alpha = 0f;
            _menuGroup.alpha = 1f;
            _openingPanel.SetActive(false);
            _openingGroup.alpha = 1f;
            _openingRoutine = null;
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
            if (SupportsLocalFileFeatures(Application.platform))
            {
                AddSmallButton(panel, "刷新", new Vector2(-310f, -445f), () => DiagnosticsRequested?.Invoke());
                AddSmallButton(panel, "复制完整日志", new Vector2(-95f, -445f), () => DiagnosticsCopyRequested?.Invoke());
                AddSmallButton(panel, "打开日志文件夹", new Vector2(135f, -445f), () => DiagnosticsFolderRequested?.Invoke());
                AddSmallButton(panel, "关闭", new Vector2(350f, -445f), HideDiagnostics);
            }
            else
            {
                AddSmallButton(panel, "刷新", new Vector2(-105f, -445f), () => DiagnosticsRequested?.Invoke());
                AddSmallButton(panel, "关闭", new Vector2(105f, -445f), HideDiagnostics);
            }
            _diagnosticsPanel.SetActive(false);
        }

        private void BuildPausePanel()
        {
            _pausePanel = new GameObject("暂停菜单", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            _pausePanel.transform.SetParent(_canvasObject.transform, false);
            RectTransform panel = (RectTransform)_pausePanel.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            _pausePanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.028f, 0.022f, 0.82f);
            _pauseGroup = _pausePanel.GetComponent<CanvasGroup>();

            RectTransform plaqueBorder = AddPanel(panel, "暂停牌匾金边", new Vector2(0f, 0f), new Vector2(680f, 760f), AntiqueGold);
            _pausePlaqueRect = plaqueBorder;
            RectTransform plaque = AddPanel(plaqueBorder, "暂停牌匾宣纸", Vector2.zero, new Vector2(664f, 744f), new Color(0.91f, 0.85f, 0.70f, 0.98f));
            AddPanel(plaque, "牌匾顶饰", new Vector2(0f, 324f), new Vector2(360f, 3f), AntiqueGold);
            RectTransform seal = AddPanel(plaque, "暂停印章", new Vector2(0f, 268f), new Vector2(42f, 42f), Cinnabar);
            seal.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddText(panel, "演出暂停", 62, new Vector2(0f, 220f), new Vector2(580f, 92f), LacquerRed);
            AddText(panel, "幕间稍歇", 21, new Vector2(0f, 164f), new Vector2(360f, 40f), new Color(0.38f, 0.27f, 0.17f, 1f));
            UnityEngine.UI.Button resume = CreateButton(panel, "继续演出", 30, new Vector2(0f, 65f), new Vector2(470f, 78f));
            UnityEngine.UI.Button restart = CreateButton(panel, "重新开始", 30, new Vector2(0f, -42f), new Vector2(470f, 78f));
            UnityEngine.UI.Button back = CreateButton(panel, "返回选曲", 30, new Vector2(0f, -149f), new Vector2(470f, 78f));
            resume.onClick.AddListener(() => ResumeRequested?.Invoke());
            restart.onClick.AddListener(() => RestartRequested?.Invoke());
            back.onClick.AddListener(() => ReturnRequested?.Invoke());
            AddText(panel, "P / Esc 继续", 20, new Vector2(0f, -276f), new Vector2(500f, 44f), new Color(0.38f, 0.27f, 0.17f, 1f));
            _pausePanel.SetActive(false);
        }

        private void AddDifficultyButton(RectTransform parent, PlayDifficulty difficulty, Vector2 position)
        {
            string label = $"{PlayDifficultyInfo.DisplayName(difficulty)}　{PlayDifficultyInfo.Description(difficulty)}";
            UnityEngine.UI.Button button = CreateButton(parent, label, 24, position, new Vector2(350f, 76f));
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
            UnityEngine.UI.Image border = buttonObject.GetComponent<UnityEngine.UI.Image>();
            border.color = AntiqueGold;
            RectTransform face = AddPanel(rect, "按钮宣纸", Vector2.zero, dimensions - new Vector2(8f, 8f), new Color(0.37f, 0.095f, 0.06f, 0.98f));
            UnityEngine.UI.Text text = AddText(face, label, size, Vector2.zero, dimensions - new Vector2(30f, 14f), new Color(0.96f, 0.85f, 0.58f, 1f));
            text.raycastTarget = false;
            UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = border;
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.90f, 0.62f, 1f);
            colors.pressedColor = new Color(0.72f, 0.48f, 0.23f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.10f;
            button.colors = colors;
            return button;
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

            RefreshSongButtonColors(false);
        }

        private void RefreshSongButtonColors(bool animate)
        {
            int selectedIndex = 0;
            for (int i = 0; i < _songButtons.Count; i++)
            {
                if (_songButtons[i].SongId == _selectedSongId) selectedIndex = i;
            }

            var targetPositions = new Vector2[_songButtons.Count];
            var targetScales = new Vector3[_songButtons.Count];
            var targetAlphas = new float[_songButtons.Count];
            for (int i = 0; i < _songButtons.Count; i++)
            {
                bool selected = _songButtons[i].SongId == _selectedSongId;
                int visualOffset = i - selectedIndex;
                int half = _songButtons.Count / 2;
                if (visualOffset > half) visualOffset -= _songButtons.Count;
                if (visualOffset < -half) visualOffset += _songButtons.Count;
                targetPositions[i] = new Vector2(visualOffset * 500f, 200f);
                targetScales[i] = selected ? Vector3.one * 1.06f : Vector3.one * 0.90f;
                targetAlphas[i] = selected ? 1f : 0.72f;
                _songButtons[i].Button.GetComponent<UnityEngine.UI.Image>().color = selected ? WarmGold : AntiqueGold;
                _songButtons[i].CardFace.color = selected ? LightRicePaper : RicePaper;
                _songButtons[i].Title.color = selected ? LacquerRed : new Color(0.25f, 0.18f, 0.13f, 1f);
                _songButtons[i].Artist.color = selected
                    ? MutedJade
                    : new Color(0.36f, 0.30f, 0.23f, 1f);
            }

            if (_songCarouselRoutine != null)
            {
                StopCoroutine(_songCarouselRoutine);
                _songCarouselRoutine = null;
            }

            if (animate && Application.isPlaying && isActiveAndEnabled)
            {
                _songCarouselRoutine = StartCoroutine(AnimateSongCards(targetPositions, targetScales, targetAlphas));
                return;
            }

            ApplySongCardTargets(targetPositions, targetScales, targetAlphas);
        }

        private IEnumerator AnimateSongCards(Vector2[] targetPositions, Vector3[] targetScales, float[] targetAlphas)
        {
            int count = _songButtons.Count;
            var startPositions = new Vector2[count];
            var startScales = new Vector3[count];
            var startAlphas = new float[count];
            for (int i = 0; i < count; i++)
            {
                startPositions[i] = _songButtons[i].Rect.anchoredPosition;
                startScales[i] = _songButtons[i].Rect.localScale;
                startAlphas[i] = _songButtons[i].Button.GetComponent<CanvasGroup>().alpha;
            }

            float elapsed = 0f;
            const float duration = 0.24f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                for (int i = 0; i < count; i++)
                {
                    _songButtons[i].Rect.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], t);
                    _songButtons[i].Rect.localScale = Vector3.LerpUnclamped(startScales[i], targetScales[i], t);
                    _songButtons[i].Button.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(startAlphas[i], targetAlphas[i], t);
                }
                yield return null;
            }

            ApplySongCardTargets(targetPositions, targetScales, targetAlphas);
            _songCarouselRoutine = null;
        }

        private void ApplySongCardTargets(Vector2[] targetPositions, Vector3[] targetScales, float[] targetAlphas)
        {
            for (int i = 0; i < _songButtons.Count; i++)
            {
                _songButtons[i].Rect.anchoredPosition = targetPositions[i];
                _songButtons[i].Rect.localScale = targetScales[i];
                _songButtons[i].Button.GetComponent<CanvasGroup>().alpha = targetAlphas[i];
            }
        }

        private IEnumerator PlayPauseReveal()
        {
            SetPauseVisual(0f, 0.94f, new Vector2(0f, -12f));
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetPauseVisual(t, Mathf.Lerp(0.94f, 1f, t), Vector2.Lerp(new Vector2(0f, -12f), Vector2.zero, t));
                yield return null;
            }

            SetPauseVisual(1f, 1f, Vector2.zero);
            _pauseRoutine = null;
        }

        private void SetPauseVisual(float alpha, float scale, Vector2 position)
        {
            if (_pauseGroup != null) _pauseGroup.alpha = alpha;
            if (_pausePlaqueRect == null) return;
            _pausePlaqueRect.localScale = new Vector3(scale, scale, 1f);
            _pausePlaqueRect.anchoredPosition = position;
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
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            BrushTypography.Apply(text, size, color);
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
            if (_pauseRoutine != null) StopCoroutine(_pauseRoutine);
            if (_songCarouselRoutine != null) StopCoroutine(_songCarouselRoutine);
            if (_runtimeFont != null)
            {
                ChineseFontProvider.Release(_runtimeFont);
            }
        }
    }
}
