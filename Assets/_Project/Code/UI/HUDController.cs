using System;
using UnityEngine;
using CorgiCommando.Core;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Main HUD controller. Manages health bars, special meter, wave indicator.
    /// Anchored to safe areas for iOS notch/home-indicator compatibility.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        /// <summary>Whether the game is currently paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Fired when pause state changes.</summary>
        public event Action<bool> OnPauseStateChanged;

        /// <summary>
        /// Updates the health bar display for the given player.
        /// </summary>
        public void UpdateHealthBar(int playerIndex, int currentHP, int maxHP)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the special meter display for the given player.
        /// </summary>
        public void UpdateSpecialMeter(int playerIndex, float currentMeter, float maxMeter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Toggles pause. Either player can pause. Halts Time.timeScale.
        /// </summary>
        public void TogglePause()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shows the pause menu UI.
        /// </summary>
        public void ShowPauseMenu()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hides the pause menu UI.
        /// </summary>
        public void HidePauseMenu()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Applies safe area insets to the HUD RectTransform.
        /// Ensures UI is not clipped by iOS notch or home indicator.
        /// </summary>
        public void ApplySafeArea()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns whether the current safe area is being respected.
        /// </summary>
        public bool IsSafeAreaApplied()
        {
            throw new NotImplementedException();
        }
    }
}
