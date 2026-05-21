using System;
using UnityEngine;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Displays the combo counter during fights. Shows current chain (x2, x3, etc.)
    /// and fades out when the combo breaks.
    /// </summary>
    public class ComboCounterUI : MonoBehaviour
    {
        /// <summary>Current displayed combo count.</summary>
        public int DisplayedComboCount { get; private set; }

        /// <summary>Whether the combo counter is currently visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Updates the displayed combo count. Shows the counter if hidden.
        /// </summary>
        public void SetComboCount(int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Triggers the fade-out animation when the combo breaks.
        /// </summary>
        public void FadeOut()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Immediately hides the counter (no animation).
        /// </summary>
        public void Hide()
        {
            throw new NotImplementedException();
        }
    }
}
