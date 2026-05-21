using System;
using UnityEngine;

namespace CorgiCommando.UI
{
    /// <summary>
    /// Boss health bar banner. Shows boss name and health bar at top of screen.
    /// Flashes on phase changes.
    /// </summary>
    public class BossBannerUI : MonoBehaviour
    {
        /// <summary>Whether the boss banner is currently displayed.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Currently displayed boss name.</summary>
        public string BossName { get; private set; }

        /// <summary>
        /// Shows the boss banner with name and initial HP.
        /// </summary>
        public void Show(string bossName, int currentHP, int maxHP)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the boss health bar.
        /// </summary>
        public void UpdateHP(int currentHP, int maxHP)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Triggers the phase-change flash effect.
        /// </summary>
        public void FlashPhaseChange()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hides the boss banner.
        /// </summary>
        public void Hide()
        {
            throw new NotImplementedException();
        }
    }
}
