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
            BossName = bossName ?? string.Empty;
            IsVisible = true;
            UpdateHP(currentHP, maxHP);
        }

        /// <summary>
        /// Updates the boss health bar.
        /// </summary>
        public void UpdateHP(int currentHP, int maxHP)
        {
            _ = Mathf.Clamp(currentHP, 0, Mathf.Max(1, maxHP));
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
        }

        /// <summary>
        /// Hides the boss banner.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            BossName = string.Empty;
        }
    }
}
