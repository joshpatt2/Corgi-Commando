using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Displays the combo counter during fights. Shows current chain (x2, x3, etc.)
    /// and fades out when the combo breaks.
    /// </summary>
    public class ComboCounterUI : MonoBehaviour
    {
        private const float FadeDurationSeconds = 1f;
        private const string DefaultFontName = "LegacyRuntime.ttf";

        private Text _comboText;
        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;
        private static Font _defaultFont;

        /// <summary>Current displayed combo count.</summary>
        public int DisplayedComboCount { get; private set; }

        /// <summary>Whether the combo counter is currently visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Updates the displayed combo count. Shows the counter if hidden.
        /// </summary>
        public void SetComboCount(int count)
        {
            EnsureVisualElements();
            DisplayedComboCount = Mathf.Max(0, count);
            IsVisible = DisplayedComboCount > 0;

            if (_comboText != null)
            {
                _comboText.text = $"x{DisplayedComboCount}";
                _comboText.enabled = IsVisible;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = IsVisible ? 1f : 0f;
            }
        }

        /// <summary>
        /// Triggers the fade-out animation when the combo breaks.
        /// </summary>
        public void FadeOut()
        {
            if (!IsVisible)
            {
                return;
            }

            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                if (_fadeCoroutine != null)
                {
                    return;
                }

                _fadeCoroutine = StartCoroutine(FadeOutRoutine());
                return;
            }

            Hide();
        }

        /// <summary>
        /// Immediately hides the counter (no animation).
        /// </summary>
        public void Hide()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            IsVisible = false;

            if (_comboText != null)
            {
                _comboText.enabled = false;
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

            if (_comboText != null)
            {
                return;
            }

            var textGO = new GameObject("ComboText", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(240f, 60f);
            textRect.anchoredPosition = Vector2.zero;

            _comboText = textGO.GetComponent<Text>();
            _comboText.font = GetDefaultFont();
            _comboText.alignment = TextAnchor.MiddleCenter;
            _comboText.fontSize = 44;
            _comboText.color = Color.white;
            _comboText.text = "x0";
        }

        private IEnumerator FadeOutRoutine()
        {
            EnsureVisualElements();

            float elapsed = 0f;
            while (elapsed < FadeDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FadeDurationSeconds);
                _canvasGroup.alpha = 1f - t;
                yield return null;
            }

            _fadeCoroutine = null;
            Hide();
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
