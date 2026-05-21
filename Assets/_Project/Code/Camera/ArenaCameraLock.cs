using System;
using UnityEngine;

namespace CorgiCommando.Camera
{
    /// <summary>
    /// Trigger-based arena lock. When players enter an arena zone,
    /// locks the camera and spawns gate colliders. Unlocks on wave clear.
    /// Placed as a trigger collider in the level.
    /// </summary>
    public class ArenaCameraLock : MonoBehaviour
    {
        /// <summary>Left boundary of the arena in world X.</summary>
        public float ArenaMinX { get; set; }

        /// <summary>Right boundary of the arena in world X.</summary>
        public float ArenaMaxX { get; set; }

        /// <summary>Whether the arena is currently active (locked).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Fired when the arena activates.</summary>
        public event Action OnArenaActivated;

        /// <summary>Fired when the arena is cleared and deactivates.</summary>
        public event Action OnArenaCleared;

        /// <summary>
        /// Activates the arena lock. Called when trigger enters.
        /// </summary>
        public void Activate(GroupTargetCamera camera)
        {
            IsActive = true;
            camera?.LockToArena(ArenaMinX, ArenaMaxX);
            OnArenaActivated?.Invoke();
        }

        /// <summary>
        /// Deactivates the arena lock. Called on wave clear.
        /// </summary>
        public void Deactivate(GroupTargetCamera camera)
        {
            IsActive = false;
            camera?.UnlockArena();
            OnArenaCleared?.Invoke();
        }
    }
}
