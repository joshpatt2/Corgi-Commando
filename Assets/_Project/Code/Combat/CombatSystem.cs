using System;
using System.Collections.Generic;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Combat
{
    /// <summary>
    /// Singleton combat system. Centralizes all hit resolution, hitstop, knockback,
    /// combo tracking, and special meter management.
    /// Same-Z-band hit rule: attacker and target must be within ±0.5 Z units.
    /// </summary>
    public class CombatSystem : ICombatSystem
    {
        /// <summary>Z-band tolerance for hit detection. Attacks whiff outside this range.</summary>
        public const float ZBandTolerance = 0.5f;

        /// <summary>Time in seconds before combo counter resets after last hit.</summary>
        public const float ComboTimeoutSeconds = 2.0f;

        /// <summary>Default special meter gained per hit when no CorgiData is available.</summary>
        private const float DefaultSpecialGainPerHit = 10f;

        public event Action<HitResult> OnHitConnected;
        public event Action<int> OnHitstopStarted;
        public event Action OnHitstopEnded;

        public bool IsInHitstop { get; private set; }

        private readonly Dictionary<Entity, int> _comboCounts = new Dictionary<Entity, int>();
        private readonly Dictionary<Entity, float> _comboTimers = new Dictionary<Entity, float>();
        private readonly Dictionary<Entity, float> _specialMeters = new Dictionary<Entity, float>();
        private int _hitstopFramesRemaining;

        // Reusable scratch lists to avoid per-Tick GC allocations
        private readonly List<Entity> _tickExpiredEntities = new List<Entity>();
        private readonly List<Entity> _tickTimerKeys = new List<Entity>();

        public int GetComboCount(Entity attacker)
        {
            return _comboCounts.TryGetValue(attacker, out var count) ? count : 0;
        }

        public float GetSpecialMeter(Entity entity)
        {
            return _specialMeters.TryGetValue(entity, out var meter) ? meter : 0f;
        }

        public HitResult ResolveAttack(Entity attacker, AttackData attackData, Entity[] targets)
        {
            var result = new HitResult
            {
                Attacker = attacker,
                DidHit = false
            };

            foreach (var target in targets)
            {
                // Check Z-band: attacker and target must be within ±0.5 Z units
                float zDiff = Mathf.Abs(attacker.transform.position.z - target.transform.position.z);
                if (zDiff > ZBandTolerance)
                    continue;

                // Check hurtbox is enabled
                var hurtbox = target.GetEntityComponent<HurtboxComponent>();
                if (hurtbox == null || !hurtbox.IsEnabled)
                    continue;

                // No friendly fire
                if (attacker.Faction == target.Faction)
                    continue;

                // Apply damage
                var health = target.GetEntityComponent<IHealthComponent>();
                if (health != null)
                    health.TakeDamage(attackData.damage);

                // Apply knockback
                var knockbackReceiver = target.GetEntityComponent<KnockbackReceiver>();
                if (knockbackReceiver != null)
                    knockbackReceiver.ApplyKnockback(attackData.knockbackForce);

                // Build result
                result.DidHit = true;
                result.Target = target;
                result.DamageDealt = attackData.damage;
                result.KnockbackApplied = attackData.knockbackForce;
                result.HitstopFrames = attackData.hitstopFrames;
                result.HitType = attackData.hitType;
                result.ScreenShakeIntensity = attackData.screenShakeIntensity;
                result.HitPosition = target.transform.position;

                // Start hitstop
                if (attackData.hitstopFrames > 0)
                {
                    IsInHitstop = true;
                    _hitstopFramesRemaining = attackData.hitstopFrames;
                    OnHitstopStarted?.Invoke(attackData.hitstopFrames);
                }

                // Increment combo and reset timer
                if (!_comboCounts.ContainsKey(attacker))
                    _comboCounts[attacker] = 0;
                _comboCounts[attacker]++;
                _comboTimers[attacker] = ComboTimeoutSeconds;

                // Add special meter for the attacker
                AddSpecialMeter(attacker, DefaultSpecialGainPerHit);

                // Fire event
                OnHitConnected?.Invoke(result);

                // Only process the first valid target
                break;
            }

            return result;
        }

        public void ResetCombo(Entity attacker)
        {
            _comboCounts[attacker] = 0;
            _comboTimers.Remove(attacker);
        }

        public void AddSpecialMeter(Entity entity, float amount)
        {
            if (!_specialMeters.ContainsKey(entity))
                _specialMeters[entity] = 0f;
            _specialMeters[entity] += amount;
        }

        public bool ConsumeSpecialMeter(Entity entity, float cost)
        {
            if (!_specialMeters.TryGetValue(entity, out var meter) || meter < cost)
                return false;
            _specialMeters[entity] -= cost;
            return true;
        }

        public void Tick(float deltaTime)
        {
            // Tick combo timers using reusable scratch lists to avoid per-call GC allocations
            _tickExpiredEntities.Clear();
            _tickTimerKeys.Clear();
            _tickTimerKeys.AddRange(_comboTimers.Keys);

            foreach (var key in _tickTimerKeys)
            {
                float remaining = _comboTimers[key] - deltaTime;
                if (remaining <= 0f)
                    _tickExpiredEntities.Add(key);
                else
                    _comboTimers[key] = remaining;
            }

            foreach (var attacker in _tickExpiredEntities)
                ResetCombo(attacker);

            // Tick hitstop countdown (frame-based; game loop calls Tick once per frame)
            if (IsInHitstop)
            {
                _hitstopFramesRemaining--;
                if (_hitstopFramesRemaining <= 0)
                {
                    IsInHitstop = false;
                    OnHitstopEnded?.Invoke();
                }
            }
        }
    }
}
