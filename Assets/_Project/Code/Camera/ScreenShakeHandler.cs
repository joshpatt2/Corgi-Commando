using System;
using System.Collections;
using Cinemachine;
using CorgiCommando.Combat;
using CorgiCommando.Core;
using UnityEngine;

namespace CorgiCommando.Camera
{
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

            _combatSystem.OnHitConnected += HandleHitResolved;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _combatSystem == null)
            {
                return;
            }

            _combatSystem.OnHitConnected -= HandleHitResolved;
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

            float delaySeconds = (float)hitResult.HitstopFrames / HitstopReferenceFramerate;
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
