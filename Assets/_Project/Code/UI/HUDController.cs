using System;
using UnityEngine;
using UnityEngine.UI;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Main HUD controller. Manages health bars, special meter, wave indicator.
    /// Anchored to safe areas for iOS notch/home-indicator compatibility.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private const int MaxPlayers = 2;
        private const int DefaultMaxHealth = 100;
        private const int DefaultCurrentHealth = 100;
        private const float DefaultMaxSpecial = 100f;
        private const float DefaultCurrentSpecial = 0f;
        private const float SpecialMeterFullThreshold = 0.999f;
        private const string DefaultFontName = "LegacyRuntime.ttf";
        private static readonly Color PlayerPanelBackgroundColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color PlayerOnePortraitColor = new Color(1f, 0.55f, 0.15f, 1f);
        private static readonly Color PlayerTwoPortraitColor = new Color(0.55f, 0.8f, 1f, 1f);
        private static readonly Color SpecialMeterFullColor = new Color(1f, 0.9f, 0.35f, 1f);
        private static readonly Color SpecialMeterNormalColor = new Color(0.3f, 0.85f, 1f, 1f);

        private bool _isSafeAreaApplied;
        private float _timeScaleBeforePause = 1f;
        private RectTransform _cachedRectTransform;
        private RectTransform _safeAreaRectTransform;
        private bool _visualsBuilt;

        private readonly int[] _currentHealth = new int[MaxPlayers];
        private readonly int[] _maxHealth = new int[MaxPlayers];
        private readonly float[] _currentSpecial = new float[MaxPlayers];
        private readonly float[] _maxSpecial = new float[MaxPlayers];

        private readonly Image[] _healthFillImages = new Image[MaxPlayers];
        private readonly Image[] _specialFillImages = new Image[MaxPlayers];
        private readonly GameObject[] _playerPanels = new GameObject[MaxPlayers];

        private GameObject _pauseMenuPanel;
        private static Font _defaultFont;

        /// <summary>Whether the game is currently paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Fired when pause state changes.</summary>
        public event Action<bool> OnPauseStateChanged;

        private void Awake()
        {
            _cachedRectTransform = GetComponent<RectTransform>();
            InitializePlayerDefaults();
            EnsureVisualHierarchy();
            SetPlayerStripVisible(0, true);
            SetPlayerStripVisible(1, false);
        }

        /// <summary>
        /// Updates the health bar display for the given player.
        /// </summary>
        public void UpdateHealthBar(int playerIndex, int currentHP, int maxHP)
        {
            EnsureVisualHierarchy();

            int slot = ResolvePlayerSlot(playerIndex);
            int safeMaxHP = Mathf.Max(1, maxHP);
            int safeCurrentHP = Mathf.Clamp(currentHP, 0, safeMaxHP);

            _maxHealth[slot] = safeMaxHP;
            _currentHealth[slot] = safeCurrentHP;

            var fillImage = _healthFillImages[slot];
            if (fillImage != null)
            {
                fillImage.fillAmount = safeCurrentHP / (float)safeMaxHP;
            }
        }

        /// <summary>
        /// Updates the special meter display for the given player.
        /// </summary>
        public void UpdateSpecialMeter(int playerIndex, float currentMeter, float maxMeter)
        {
            EnsureVisualHierarchy();

            int slot = ResolvePlayerSlot(playerIndex);
            float safeMaxMeter = Mathf.Max(0.01f, maxMeter);
            float safeCurrentMeter = Mathf.Clamp(currentMeter, 0f, safeMaxMeter);
            float ratio = safeCurrentMeter / safeMaxMeter;

            _maxSpecial[slot] = safeMaxMeter;
            _currentSpecial[slot] = safeCurrentMeter;

            var fillImage = _specialFillImages[slot];
            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
                fillImage.color = ratio >= SpecialMeterFullThreshold
                    ? SpecialMeterFullColor
                    : SpecialMeterNormalColor;
            }
        }

        /// <summary>
        /// Toggles pause. Either player can pause. Halts Time.timeScale.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = _timeScaleBeforePause;
                HidePauseMenu();
            }
            else
            {
                IsPaused = true;
                _timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
                ShowPauseMenu();
            }

            OnPauseStateChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// Shows the pause menu UI.
        /// </summary>
        public void ShowPauseMenu()
        {
            EnsureVisualHierarchy();
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the pause menu UI.
        /// </summary>
        public void HidePauseMenu()
        {
            EnsureVisualHierarchy();
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Applies safe area insets to the HUD RectTransform.
        /// Ensures UI is not clipped by iOS notch or home indicator.
        /// </summary>
        public void ApplySafeArea()
        {
            EnsureVisualHierarchy();
            var rectTransform = _safeAreaRectTransform;
            if (rectTransform != null && Screen.width > 0 && Screen.height > 0)
            {
                Rect safeArea = Screen.safeArea;
                Vector2 anchorMin = safeArea.position;
                Vector2 anchorMax = safeArea.position + safeArea.size;
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                _isSafeAreaApplied = true;
            }
            else
            {
                _isSafeAreaApplied = false;
            }
        }

        /// <summary>
        /// Returns whether the current safe area is being respected.
        /// </summary>
        public bool IsSafeAreaApplied()
        {
            return _isSafeAreaApplied;
        }

        public int GetCurrentHealth(int playerIndex)
        {
            return _currentHealth[ResolvePlayerSlot(playerIndex)];
        }

        public int GetMaxHealth(int playerIndex)
        {
            return _maxHealth[ResolvePlayerSlot(playerIndex)];
        }

        public float GetCurrentSpecialMeter(int playerIndex)
        {
            return _currentSpecial[ResolvePlayerSlot(playerIndex)];
        }

        public float GetMaxSpecialMeter(int playerIndex)
        {
            return _maxSpecial[ResolvePlayerSlot(playerIndex)];
        }

        public Image GetHealthFillImage(int playerIndex)
        {
            return _healthFillImages[ResolvePlayerSlot(playerIndex)];
        }

        public Image GetSpecialMeterFillImage(int playerIndex)
        {
            return _specialFillImages[ResolvePlayerSlot(playerIndex)];
        }

        public bool HasVisualHierarchy()
        {
            return _visualsBuilt;
        }

        public void SetPlayerStripVisible(int playerIndex, bool isVisible)
        {
            EnsureVisualHierarchy();
            int slot = ResolvePlayerSlot(playerIndex);
            if (_playerPanels[slot] != null)
            {
                _playerPanels[slot].SetActive(isVisible);
            }
        }

        public bool IsPlayerStripVisible(int playerIndex)
        {
            int slot = ResolvePlayerSlot(playerIndex);
            return _playerPanels[slot] != null && _playerPanels[slot].activeSelf;
        }

        private void OnDestroy()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = _timeScaleBeforePause;
                OnPauseStateChanged?.Invoke(false);
            }
        }

        private void InitializePlayerDefaults()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                _maxHealth[i] = DefaultMaxHealth;
                _currentHealth[i] = DefaultCurrentHealth;
                _maxSpecial[i] = DefaultMaxSpecial;
                _currentSpecial[i] = DefaultCurrentSpecial;
            }
        }

        private int ResolvePlayerSlot(int playerIndex)
        {
            return Mathf.Clamp(playerIndex, 0, MaxPlayers - 1);
        }

        private void EnsureVisualHierarchy()
        {
            if (_visualsBuilt)
            {
                return;
            }

            _safeAreaRectTransform = _cachedRectTransform;
            if (_safeAreaRectTransform == null)
            {
                var canvasGO = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGO.transform.SetParent(transform, false);
                var canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                _safeAreaRectTransform = canvasGO.GetComponent<RectTransform>();
                _safeAreaRectTransform.anchorMin = Vector2.zero;
                _safeAreaRectTransform.anchorMax = Vector2.one;
                _safeAreaRectTransform.offsetMin = Vector2.zero;
                _safeAreaRectTransform.offsetMax = Vector2.zero;
            }

            BuildPlayerPanel(0, true);
            BuildPlayerPanel(1, false);
            BuildComboCounter();
            BuildBossBanner();
            BuildPauseMenu();

            _visualsBuilt = true;
        }

        private void BuildPlayerPanel(int slot, bool isLeftSide)
        {
            string panelName = slot == 0 ? "P1HUD" : "P2HUD";
            var panelGO = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(_safeAreaRectTransform, false);
            _playerPanels[slot] = panelGO;

            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = isLeftSide ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            panelRect.anchorMax = panelRect.anchorMin;
            panelRect.pivot = isLeftSide ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(240f, 72f);
            panelRect.anchoredPosition = isLeftSide ? new Vector2(24f, -24f) : new Vector2(-24f, -24f);

            panelGO.GetComponent<Image>().color = PlayerPanelBackgroundColor;

            var portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGO.transform.SetParent(panelGO.transform, false);
            var portraitRect = portraitGO.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.sizeDelta = new Vector2(36f, 36f);
            portraitRect.anchoredPosition = new Vector2(8f, 0f);
            portraitGO.GetComponent<Image>().color = slot == 0
                ? PlayerOnePortraitColor
                : PlayerTwoPortraitColor;

            var healthBackground = CreateBarBackground("HealthBarBG", panelGO.transform, new Vector2(52f, -14f), new Vector2(176f, 16f));
            _healthFillImages[slot] = CreateFilledBar("HealthBarFill", healthBackground.transform, new Color(0.95f, 0.2f, 0.2f, 1f));
            _healthFillImages[slot].fillAmount = _maxHealth[slot] > 0
                ? _currentHealth[slot] / (float)_maxHealth[slot]
                : 0f;

            var specialBackground = CreateBarBackground("SpecialMeterBG", panelGO.transform, new Vector2(52f, -38f), new Vector2(176f, 14f));
            _specialFillImages[slot] = CreateFilledBar("SpecialMeterFill", specialBackground.transform, new Color(0.3f, 0.85f, 1f, 1f));
            _specialFillImages[slot].fillAmount = _maxSpecial[slot] > 0f
                ? _currentSpecial[slot] / _maxSpecial[slot]
                : 0f;
        }

        private void BuildComboCounter()
        {
            var comboGO = new GameObject("ComboCounter", typeof(RectTransform), typeof(ComboCounterUI));
            comboGO.transform.SetParent(_safeAreaRectTransform, false);

            var comboRect = comboGO.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 1f);
            comboRect.anchorMax = new Vector2(0.5f, 1f);
            comboRect.pivot = new Vector2(0.5f, 1f);
            comboRect.sizeDelta = new Vector2(240f, 60f);
            comboRect.anchoredPosition = new Vector2(0f, -16f);
        }

        private void BuildBossBanner()
        {
            var bossGO = new GameObject("BossBanner", typeof(RectTransform), typeof(BossBannerUI));
            bossGO.transform.SetParent(_safeAreaRectTransform, false);

            var bossRect = bossGO.GetComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0.5f, 1f);
            bossRect.anchorMax = new Vector2(0.5f, 1f);
            bossRect.pivot = new Vector2(0.5f, 1f);
            bossRect.sizeDelta = new Vector2(520f, 72f);
            bossRect.anchoredPosition = new Vector2(0f, -80f);
        }

        private void BuildPauseMenu()
        {
            _pauseMenuPanel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(Image));
            _pauseMenuPanel.transform.SetParent(_safeAreaRectTransform, false);

            var panelRect = _pauseMenuPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            _pauseMenuPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var pauseTextGO = new GameObject("PauseText", typeof(RectTransform), typeof(Text));
            pauseTextGO.transform.SetParent(_pauseMenuPanel.transform, false);
            var pauseTextRect = pauseTextGO.GetComponent<RectTransform>();
            pauseTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            pauseTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            pauseTextRect.pivot = new Vector2(0.5f, 0.5f);
            pauseTextRect.sizeDelta = new Vector2(260f, 64f);
            pauseTextRect.anchoredPosition = Vector2.zero;

            var pauseText = pauseTextGO.GetComponent<Text>();
            pauseText.text = "PAUSED";
            pauseText.color = Color.white;
            pauseText.alignment = TextAnchor.MiddleCenter;
            pauseText.fontSize = 42;
            pauseText.font = GetDefaultFont();

            _pauseMenuPanel.SetActive(false);
        }

        private GameObject CreateBarBackground(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var barBG = new GameObject(name, typeof(RectTransform), typeof(Image));
            barBG.transform.SetParent(parent, false);
            var barRect = barBG.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = anchoredPosition;
            barRect.sizeDelta = size;
            barBG.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            return barBG;
        }

        private Image CreateFilledBar(string name, Transform parent, Color fillColor)
        {
            var barFill = new GameObject(name, typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(parent, false);

            var fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = barFill.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;

            return fillImage;
        }

        private static Font GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>(DefaultFontName);
            }

            return _defaultFont;
        }
    }
}
