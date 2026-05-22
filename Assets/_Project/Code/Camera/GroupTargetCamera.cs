using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using CorgiCommando.Combat;
using CorgiCommando.Core;
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

        /// <summary>
        /// Listens for combat hit results and emits Cinemachine impulses for screen shake.
        /// Intended to live on the camera virtual target or camera rig object.
        /// </summary>
        public class ScreenShakeHandler : MonoBehaviour
        {
            private const float HitstopReferenceFramerate = 60f;

            [SerializeField] private CinemachineImpulseSource _impulseSource;
            [SerializeField, Min(0f)] private float _minimumShakeIntensity = 0.05f;
            [SerializeField, Min(0f)] private float _impulseScale = 1f;
            [SerializeField] private Vector3 _impulseDirection = Vector3.right;

            private ICombatSystem _combatSystem;
            private bool _subscribed;

            public float MinimumShakeIntensity
            {
                get => _minimumShakeIntensity;
                set => _minimumShakeIntensity = Mathf.Max(0f, value);
            }

            public float ImpulseScale
            {
                get => _impulseScale;
                set => _impulseScale = Mathf.Max(0f, value);
            }

            private void OnEnable()
            {
                TrySubscribe();
            }

            private void Start()
            {
                if (_combatSystem != null)
                {
                    return;
                }

                var sceneBootstrap = FindObjectOfType<SceneBootstrap>();
                if (sceneBootstrap?.CombatSystem != null)
                {
                    SetCombatSystem(sceneBootstrap.CombatSystem);
                }
            }

            private void OnDisable()
            {
                StopAllCoroutines();
                Unsubscribe();
            }

            public void SetCombatSystem(ICombatSystem combatSystem)
            {
                if (ReferenceEquals(_combatSystem, combatSystem))
                {
                    TrySubscribe();
                    return;
                }

                Unsubscribe();
                _combatSystem = combatSystem;
                TrySubscribe();
            }

            private void TrySubscribe()
            {
                if (_subscribed || _combatSystem == null || !isActiveAndEnabled)
                {
                    return;
                }

                _combatSystem.OnHitResolved += HandleHitResolved;
                _subscribed = true;
            }

            private void Unsubscribe()
            {
                if (!_subscribed || _combatSystem == null)
                {
                    return;
                }

                _combatSystem.OnHitResolved -= HandleHitResolved;
                _subscribed = false;
            }

            private void HandleHitResolved(HitResult hitResult)
            {
                if (!hitResult.DidHit || hitResult.ScreenShakeIntensity <= _minimumShakeIntensity)
                {
                    return;
                }

                float magnitude = hitResult.ScreenShakeIntensity * _impulseScale;
                if (magnitude <= 0f)
                {
                    return;
                }

                float delaySeconds = Mathf.Max(0f, hitResult.HitstopFrames) / HitstopReferenceFramerate;
                if (delaySeconds <= 0f)
                {
                    EmitImpulse(magnitude);
                    return;
                }

                StartCoroutine(EmitImpulseAfterDelay(delaySeconds, magnitude));
            }

            private IEnumerator EmitImpulseAfterDelay(float delaySeconds, float magnitude)
            {
                yield return new WaitForSeconds(delaySeconds);
                EmitImpulse(magnitude);
            }

            protected virtual void EmitImpulse(float magnitude)
            {
                if (_impulseSource == null)
                {
                    return;
                }

                Vector3 direction = _impulseDirection.sqrMagnitude > 0f ? _impulseDirection.normalized : Vector3.right;
                _impulseSource.GenerateImpulseWithVelocity(direction * magnitude);
            }
        }
    }
}
