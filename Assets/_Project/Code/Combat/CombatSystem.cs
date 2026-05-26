using System;
using System.Collections.Generic;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Testing;

namespace CorgiCommando.Combat
{
    /// <summary>
    /// Singleton combat system. Centralizes all hit resolution, hitstop, knockback,
    /// combo tracking, and special meter management.
    /// Same-Z-band hit rule: attacker and target must be within ±0.5 Z units.
    ///
    /// Caller contract: the game loop is responsible for not calling ResolveAttack
    /// while IsInHitstop is true (check IsInHitstop before ticking entities).
    /// </summary>
    public class CombatSystem : ICombatSystem
    {
        /// <summary>Z-band tolerance for hit detection. Attacks whiff outside this range.</summary>
        public const float ZBandTolerance = 0.5f;

        /// <summary>Time in seconds before combo counter resets after last hit.</summary>
        public const float ComboTimeoutSeconds = 2.0f;

        /// <summary>Default special meter gained per hit when no CorgiData is available.</summary>
        private const float DefaultSpecialGainPerHit = 10f;

        /// <summary>
        /// Reference frame rate used to convert hitstop frames to seconds.
        /// Hitstop duration in seconds = hitstopFrames / TargetFrameRate.
        /// </summary>
        private const float TargetFrameRate = 60f;

        public event Action<HitResult> OnHitConnected;
        public event Action<int> OnHitstopStarted;
        public event Action OnHitstopEnded;

        public bool IsInHitstop { get; private set; }

        private readonly Dictionary<Entity, int> _comboCounts = new Dictionary<Entity, int>();
        private readonly Dictionary<Entity, float> _comboTimers = new Dictionary<Entity, float>();
        private readonly Dictionary<Entity, float> _specialMeters = new Dictionary<Entity, float>();

        /// <summary>
        /// Entities whose death should purge them from combat dictionaries.
        /// Prevents unbounded growth as enemies are destroyed.
        /// </summary>
        private readonly HashSet<Entity> _trackedEntities = new HashSet<Entity>();

        /// <summary>Remaining hitstop time in seconds (time-based, not frame-based).</summary>
        private float _hitstopSecondsRemaining;

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
            var result = new HitResult { Attacker = attacker, DidHit = false };
            bool firstHit = true;

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
                {
                    knockbackReceiver.ApplyKnockback(attackData.knockbackForce);
                    if (PlaytestMetrics.IsRecording)
                    {
                        PlaytestMetrics.LogKnockback(attackData.knockbackForce.magnitude, target != null ? target.name : string.Empty);
                    }
                }

                // Build per-target result
                var targetResult = new HitResult
                {
                    DidHit = true,
                    Attacker = attacker,
                    Target = target,
                    DamageDealt = attackData.damage,
                    KnockbackApplied = attackData.knockbackForce,
                    HitstopFrames = attackData.hitstopFrames,
                    HitType = attackData.hitType,
                    ScreenShakeIntensity = attackData.screenShakeIntensity,
                    HitPosition = target.transform.position
                };

                // Track attacker for lifetime cleanup (idempotent; subscription happens once)
                TrackEntity(attacker);

                if (firstHit)
                {
                    // Return the first hit's data (single-target API back-compat)
                    result = targetResult;
                    firstHit = false;

                    // Start hitstop once per attack (time-based using TargetFrameRate)
                    if (attackData.hitstopFrames > 0)
                    {
                        IsInHitstop = true;
                        _hitstopSecondsRemaining = attackData.hitstopFrames / TargetFrameRate;
                        OnHitstopStarted?.Invoke(attackData.hitstopFrames);

                        if (PlaytestMetrics.IsRecording)
                        {
                            float startTime = Time.time;
                            PlaytestMetrics.LogHitstop(startTime, startTime + _hitstopSecondsRemaining);
                        }
                    }

                    // Increment combo once per attack call, not per enemy hit
                    if (!_comboCounts.ContainsKey(attacker))
                        _comboCounts[attacker] = 0;
                    _comboCounts[attacker]++;
                    _comboTimers[attacker] = ComboTimeoutSeconds;
                }

                // Special meter and event fire once per valid target hit
                AddSpecialMeter(attacker, DefaultSpecialGainPerHit);
                OnHitConnected?.Invoke(targetResult);
            }

            return result;
        }

        public void ResetCombo(Entity attacker)
        {
            _comboCounts.Remove(attacker);
            _comboTimers.Remove(attacker);
        }

        public void AddSpecialMeter(Entity entity, float amount)
        {
            TrackEntity(entity);
            if (!_specialMeters.ContainsKey(entity))
                _specialMeters[entity] = 0f;
            _specialMeters[entity] += amount;
        }

        public bool ConsumeSpecialMeter(Entity entity, float cost)
        {
            if (cost < 0f)
                return false;
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

            // Tick hitstop countdown (time-based: decrements by deltaTime, not by frame count)
            if (IsInHitstop)
            {
                _hitstopSecondsRemaining -= deltaTime;
                if (_hitstopSecondsRemaining <= 0f)
                {
                    IsInHitstop = false;
                    OnHitstopEnded?.Invoke();
                }
            }
        }

        /// <summary>
        /// Registers an entity for lifetime tracking so its dictionary entries are
        /// purged when it dies, preventing unbounded growth from enemy churn.
        /// </summary>
        private void TrackEntity(Entity entity)
        {
            if (_trackedEntities.Add(entity))
                entity.OnDeath += OnEntityDeath;
        }

        private void OnEntityDeath(Entity entity)
        {
            _comboCounts.Remove(entity);
            _comboTimers.Remove(entity);
            _specialMeters.Remove(entity);
            _trackedEntities.Remove(entity);
            entity.OnDeath -= OnEntityDeath;
        }
    }
}
