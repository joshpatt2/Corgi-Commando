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

        // Targets with their Cinemachine weights (weight will be forwarded to TargetGroup when Cinemachine is integrated).
        // Callers must call RemoveTarget when a player GameObject is destroyed; stale refs are skipped but
        // not purged automatically, so _targets.Count may be misleading if ownership is violated.
        private readonly List<(Transform transform, float weight)> _targets = new List<(Transform, float)>();
        private bool _wasOverCap;
        [SerializeField] private bool _useManualTick;
        // TODO: wire _arenaMinX/_arenaMaxX into CinemachineConfiner2D bounds when Cinemachine is integrated
        private float _arenaMinX;
        private float _arenaMaxX;

        /// <summary>
        /// When true, this component does not self-tick in Update and must be ticked manually.
        /// </summary>
        public bool UseManualTick
        {
            get => _useManualTick;
            set => _useManualTick = value;
        }

        /// <summary>
        /// Adds a player transform to the target group.
        /// <paramref name="weight"/> is stored and will be forwarded to CinemachineTargetGroup once Cinemachine is integrated.
        /// </summary>
        public void AddTarget(Transform target, float weight = 1f)
        {
            if (target == null) return;
            foreach (var entry in _targets)
                if (entry.transform == target) return;
            _targets.Add((target, weight));
        }

        /// <summary>
        /// Removes a player transform from the target group (drop-out).
        /// </summary>
        public void RemoveTarget(Transform target)
        {
            _targets.RemoveAll(e => e.transform == target);
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
        /// Returns the current horizontal span (max X − min X) across all tracked targets.
        /// Returns 0 if fewer than two targets are tracked.
        /// </summary>
        public float GetPlayerDistance()
        {
            if (_targets.Count < 2) return 0f;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            foreach (var e in _targets)
            {
                if (e.transform == null) continue;
                if (e.transform.position.x < minX) minX = e.transform.position.x;
                if (e.transform.position.x > maxX) maxX = e.transform.position.x;
            }
            return maxX - minX;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime == 0f && _targets.Count < 2)
            {
                _wasOverCap = false;
                return;
            }

            bool isOverCap = _targets.Count >= 2 && GetPlayerDistance() > MaxPlayerDistance;
            if (isOverCap && !_wasOverCap)
                OnDistanceCapReached?.Invoke();
            _wasOverCap = isOverCap;
        }

        private void Update()
        {
            if (_useManualTick)
            {
                return;
            }

            Tick(Time.deltaTime);
        }
    }
}
