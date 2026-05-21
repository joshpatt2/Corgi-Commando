using System;
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

        public event Action<HitResult> OnHitConnected;
        public event Action<int> OnHitstopStarted;
        public event Action OnHitstopEnded;

        public bool IsInHitstop { get; private set; }

        public int GetComboCount(Entity attacker)
        {
            throw new NotImplementedException();
        }

        public float GetSpecialMeter(Entity entity)
        {
            throw new NotImplementedException();
        }

        public HitResult ResolveAttack(Entity attacker, AttackData attackData, Entity[] targets)
        {
            throw new NotImplementedException();
        }

        public void ResetCombo(Entity attacker)
        {
            throw new NotImplementedException();
        }

        public void AddSpecialMeter(Entity entity, float amount)
        {
            throw new NotImplementedException();
        }

        public bool ConsumeSpecialMeter(Entity entity, float cost)
        {
            throw new NotImplementedException();
        }

        public void Tick(float deltaTime)
        {
            throw new NotImplementedException();
        }
    }
}
