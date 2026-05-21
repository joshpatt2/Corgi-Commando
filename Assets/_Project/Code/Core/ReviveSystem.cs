using System;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Handles partner revive mechanic. Downed players are revived
    /// when their partner stands nearby for a few seconds.
    /// </summary>
    public class ReviveSystem
    {
        /// <summary>Time in seconds required to revive a downed partner.</summary>
        public float ReviveTime { get; set; } = 3.0f;

        /// <summary>Maximum distance between players for revive to work.</summary>
        public float ReviveRange { get; set; } = 2.0f;

        /// <summary>Current revive progress (0 to ReviveTime).</summary>
        public float ReviveProgress { get; private set; }

        /// <summary>Whether a revive is currently in progress.</summary>
        public bool IsReviving { get; private set; }

        /// <summary>Fired when revive completes.</summary>
        public event Action<int> OnReviveComplete;

        /// <summary>
        /// Ticks the revive system. Checks proximity, advances timer.
        /// </summary>
        /// <param name="alivePlayerPosition">Position of the alive player.</param>
        /// <param name="downedPlayerPosition">Position of the downed player.</param>
        /// <param name="deltaTime">Time step.</param>
        public void Tick(Vector3 alivePlayerPosition, Vector3 downedPlayerPosition, float deltaTime)
        {
            if (Vector3.Distance(alivePlayerPosition, downedPlayerPosition) > ReviveRange)
            {
                IsReviving = false;
                return;
            }

            IsReviving = true;
            ReviveProgress += Mathf.Max(0f, deltaTime);

            if (ReviveProgress >= ReviveTime)
            {
                OnReviveComplete?.Invoke(0);
                Reset();
            }
        }

        /// <summary>
        /// Resets the revive state.
        /// </summary>
        public void Reset()
        {
            ReviveProgress = 0f;
            IsReviving = false;
        }
    }
}
