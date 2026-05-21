using System;
using System.Collections.Generic;
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

        private readonly List<Transform> _targets = new List<Transform>();
        // TODO: wire _arenaMinX/_arenaMaxX into CinemachineConfiner2D bounds when Cinemachine is integrated
        private float _arenaMinX;
        private float _arenaMaxX;

        /// <summary>
        /// Adds a player transform to the target group.
        /// </summary>
        public void AddTarget(Transform target, float weight = 1f)
        {
            if (target != null && !_targets.Contains(target))
                _targets.Add(target);
        }

        /// <summary>
        /// Removes a player transform from the target group (drop-out).
        /// </summary>
        public void RemoveTarget(Transform target)
        {
            _targets.Remove(target);
        }

        /// <summary>
        /// Engages arena lock — clamps camera X bounds to the given range.
        /// Used during wave encounters and boss fights.
        /// </summary>
        public void LockToArena(float minX, float maxX)
        {
            _arenaMinX = minX;
            _arenaMaxX = maxX;
            IsArenaLocked = true;
            OnArenaLocked?.Invoke();
        }

        /// <summary>
        /// Releases arena lock. Camera returns to following target group.
        /// </summary>
        public void UnlockArena()
        {
            IsArenaLocked = false;
            OnArenaUnlocked?.Invoke();
        }

        /// <summary>
        /// Returns the current horizontal distance between the two furthest targets.
        /// </summary>
        public float GetPlayerDistance()
        {
            if (_targets.Count < 2) return 0f;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            foreach (var t in _targets)
            {
                if (t == null) continue;
                if (t.position.x < minX) minX = t.position.x;
                if (t.position.x > maxX) maxX = t.position.x;
            }
            return maxX - minX;
        }

        private void Update()
        {
            if (_targets.Count >= 2 && GetPlayerDistance() > MaxPlayerDistance)
                OnDistanceCapReached?.Invoke();
        }
    }
}
