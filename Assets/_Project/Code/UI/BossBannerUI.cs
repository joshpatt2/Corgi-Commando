using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Boss health bar banner. Shows boss name and health bar at top of screen.
    /// Flashes on phase changes.
    /// </summary>
    public class BossBannerUI : MonoBehaviour
    {
        private const float PhaseFlashSeconds = 0.12f;
        private const string DefaultFontName = "Arial.ttf";

        private Text _bossNameText;
        private Image _healthFillImage;
        private Image _flashOverlayImage;
        private CanvasGroup _canvasGroup;
        private Coroutine _flashCoroutine;
        private static Font _defaultFont;

        /// <summary>Whether the boss banner is currently displayed.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Currently displayed boss name.</summary>
        public string BossName { get; private set; }

        /// <summary>Current boss HP value.</summary>
        public int CurrentHP { get; private set; }

        /// <summary>Current boss max HP value.</summary>
        public int MaxHP { get; private set; }

        /// <summary>
        /// Shows the boss banner with name and initial HP.
        /// </summary>
        public void Show(string bossName, int currentHP, int maxHP)
        {
            EnsureVisualElements();
            BossName = bossName ?? string.Empty;
            IsVisible = true;
            _canvasGroup.alpha = 1f;

            if (_bossNameText != null)
            {
                _bossNameText.text = BossName;
            }

            UpdateHP(currentHP, maxHP);
        }

        /// <summary>
        /// Updates the boss health bar.
        /// </summary>
        public void UpdateHP(int currentHP, int maxHP)
        {
            EnsureVisualElements();

            MaxHP = Mathf.Max(1, maxHP);
            CurrentHP = Mathf.Clamp(currentHP, 0, MaxHP);

            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = CurrentHP / (float)MaxHP;
            }
        }

        /// <summary>
        /// Triggers the phase-change flash effect.
        /// </summary>
        public void FlashPhaseChange()
        {
            if (!IsVisible)
            {
                return;
            }

            EnsureVisualElements();

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                if (_flashCoroutine != null)
                {
                    StopCoroutine(_flashCoroutine);
                }

                _flashCoroutine = StartCoroutine(PhaseFlashRoutine());
                return;
            }

            _flashOverlayImage.enabled = false;
        }

        /// <summary>
        /// Hides the boss banner.
        /// </summary>
        public void Hide()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            IsVisible = false;
            BossName = string.Empty;
            CurrentHP = 0;
            MaxHP = 0;

            if (_bossNameText != null)
            {
                _bossNameText.text = string.Empty;
            }

            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = 0f;
            }

            if (_flashOverlayImage != null)
            {
                _flashOverlayImage.enabled = false;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void Awake()
        {
            EnsureVisualElements();
            Hide();
        }

        private void EnsureVisualElements()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            var rootBackground = gameObject.GetComponent<Image>();
            if (rootBackground == null)
            {
                rootBackground = gameObject.AddComponent<Image>();
            }

            rootBackground.color = new Color(0f, 0f, 0f, 0.6f);

            if (_bossNameText == null)
            {
                var nameGO = new GameObject("BossNameText", typeof(RectTransform), typeof(Text));
                nameGO.transform.SetParent(transform, false);
                var nameRect = nameGO.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.5f, 1f);
                nameRect.anchorMax = new Vector2(0.5f, 1f);
                nameRect.pivot = new Vector2(0.5f, 1f);
                nameRect.sizeDelta = new Vector2(400f, 28f);
                nameRect.anchoredPosition = new Vector2(0f, -6f);

                _bossNameText = nameGO.GetComponent<Text>();
                _bossNameText.font = GetDefaultFont();
                _bossNameText.color = Color.white;
                _bossNameText.alignment = TextAnchor.MiddleCenter;
                _bossNameText.fontSize = 24;
            }

            if (_healthFillImage == null)
            {
                var healthBG = new GameObject("BossHealthBG", typeof(RectTransform), typeof(Image));
                healthBG.transform.SetParent(transform, false);
                var healthBGRect = healthBG.GetComponent<RectTransform>();
                healthBGRect.anchorMin = new Vector2(0.5f, 1f);
                healthBGRect.anchorMax = new Vector2(0.5f, 1f);
                healthBGRect.pivot = new Vector2(0.5f, 1f);
                healthBGRect.sizeDelta = new Vector2(460f, 22f);
                healthBGRect.anchoredPosition = new Vector2(0f, -40f);
                healthBG.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

                var fillGO = new GameObject("BossHealthFill", typeof(RectTransform), typeof(Image));
                fillGO.transform.SetParent(healthBG.transform, false);
                var fillRect = fillGO.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                _healthFillImage = fillGO.GetComponent<Image>();
                _healthFillImage.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                _healthFillImage.type = Image.Type.Filled;
                _healthFillImage.fillMethod = Image.FillMethod.Horizontal;
                _healthFillImage.fillOrigin = 0;

                var flashGO = new GameObject("PhaseFlash", typeof(RectTransform), typeof(Image));
                flashGO.transform.SetParent(healthBG.transform, false);
                var flashRect = flashGO.GetComponent<RectTransform>();
                flashRect.anchorMin = Vector2.zero;
                flashRect.anchorMax = Vector2.one;
                flashRect.offsetMin = Vector2.zero;
                flashRect.offsetMax = Vector2.zero;

                _flashOverlayImage = flashGO.GetComponent<Image>();
                _flashOverlayImage.color = new Color(1f, 1f, 1f, 0.85f);
                _flashOverlayImage.enabled = false;
            }
        }

        private IEnumerator PhaseFlashRoutine()
        {
            _flashOverlayImage.enabled = true;
            yield return new WaitForSeconds(PhaseFlashSeconds);
            _flashOverlayImage.enabled = false;
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
