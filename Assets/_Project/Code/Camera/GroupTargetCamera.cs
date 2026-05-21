using System;
using UnityEngine;

namespace CorgiCommando.Camera
{
    /// <summary>
    /// Manages Cinemachine Target Group to frame both players.
    /// Soft horizontal distance cap prevents players from separating too far.
    /// Integrates with ArenaCameraLock for wave/boss encounters.
    /// </summary>
    public class GroupTargetCamera : MonoBehaviour
    {
        /// <summary>Maximum horizontal distance between players before soft pull-in (units).</summary>
        public float MaxPlayerDistance { get; set; } = 10f;

        /// <summary>Whether the camera is currently arena-locked.</summary>
        public bool IsArenaLocked { get; private set; }

        /// <summary>Fired when the trailing player exceeds the distance cap.</summary>
        public event Action OnDistanceCapReached;

        /// <summary>Fired when arena lock is engaged.</summary>
        public event Action OnArenaLocked;

        /// <summary>Fired when arena lock is released.</summary>
        public event Action OnArenaUnlocked;

        /// <summary>
        /// Adds a player transform to the target group.
        /// </summary>
        public void AddTarget(Transform target, float weight = 1f)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes a player transform from the target group (drop-out).
        /// </summary>
        public void RemoveTarget(Transform target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Engages arena lock — clamps camera X bounds to the given range.
        /// Used during wave encounters and boss fights.
        /// </summary>
        public void LockToArena(float minX, float maxX)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases arena lock. Camera returns to following target group.
        /// </summary>
        public void UnlockArena()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the current horizontal distance between the two furthest targets.
        /// </summary>
        public float GetPlayerDistance()
        {
            throw new NotImplementedException();
        }
    }
}
