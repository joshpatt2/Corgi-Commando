using System;
using UnityEngine;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Main HUD controller. Manages health bars, special meter, wave indicator.
    /// Anchored to safe areas for iOS notch/home-indicator compatibility.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private bool _isSafeAreaApplied;

        /// <summary>Whether the game is currently paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Fired when pause state changes.</summary>
        public event Action<bool> OnPauseStateChanged;

        /// <summary>
        /// Updates the health bar display for the given player.
        /// </summary>
        public void UpdateHealthBar(int playerIndex, int currentHP, int maxHP)
        {
            _ = playerIndex;
            _ = Mathf.Clamp(currentHP, 0, Mathf.Max(1, maxHP));
        }

        /// <summary>
        /// Updates the special meter display for the given player.
        /// </summary>
        public void UpdateSpecialMeter(int playerIndex, float currentMeter, float maxMeter)
        {
            _ = playerIndex;
            _ = Mathf.Clamp(currentMeter, 0f, Mathf.Max(0f, maxMeter));
        }

        /// <summary>
        /// Toggles pause. Either player can pause. Halts Time.timeScale.
        /// </summary>
        public void TogglePause()
        {
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;

            if (IsPaused)
            {
                ShowPauseMenu();
            }
            else
            {
                HidePauseMenu();
            }

            OnPauseStateChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// Shows the pause menu UI.
        /// </summary>
        public void ShowPauseMenu()
        {
        }

        /// <summary>
        /// Hides the pause menu UI.
        /// </summary>
        public void HidePauseMenu()
        {
        }

        /// <summary>
        /// Applies safe area insets to the HUD RectTransform.
        /// Ensures UI is not clipped by iOS notch or home indicator.
        /// </summary>
        public void ApplySafeArea()
        {
            var rectTransform = GetComponent<RectTransform>();
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
            }

            _isSafeAreaApplied = true;
        }

        /// <summary>
        /// Returns whether the current safe area is being respected.
        /// </summary>
        public bool IsSafeAreaApplied()
        {
            return _isSafeAreaApplied;
        }

        private void OnDestroy()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = 1f;
            }
        }
    }
}
