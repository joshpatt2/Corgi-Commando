using System;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Combat
{
    /// <summary>
    /// Interface for the centralized combat system.
    /// All hit resolution goes through this — prevents per-entity inconsistencies.
    /// </summary>
    public interface ICombatSystem
    {
        /// <summary>Fired when a hit connects. Subscribers handle VFX, SFX, scoring.</summary>
        event Action<HitResult> OnHitConnected;

        /// <summary>Fired when hitstop begins.</summary>
        event Action<int> OnHitstopStarted;

        /// <summary>Fired when hitstop ends.</summary>
        event Action OnHitstopEnded;

        /// <summary>Whether the game is currently in hitstop.</summary>
        bool IsInHitstop { get; }

        /// <summary>Current combo counter for the given attacker.</summary>
        int GetComboCount(Entity attacker);

        /// <summary>Current special meter value for the given entity.</summary>
        float GetSpecialMeter(Entity entity);

        /// <summary>
        /// Attempts to resolve an attack. Checks Z-band, hurtbox state, faction.
        /// Returns the result of the hit attempt.
        /// </summary>
        /// <param name="attacker">Entity performing the attack.</param>
        /// <param name="attackData">Frame/damage data for the attack.</param>
        /// <param name="targets">Potential targets in range.</param>
        HitResult ResolveAttack(Entity attacker, AttackData attackData, Entity[] targets);

        /// <summary>
        /// Resets the combo counter for the given attacker.
        /// Called on timeout or on taking a hit.
        /// </summary>
        void ResetCombo(Entity attacker);

        /// <summary>
        /// Adds special meter to the given entity.
        /// </summary>
        void AddSpecialMeter(Entity entity, float amount);

        /// <summary>
        /// Consumes the full special meter for the given entity.
        /// Returns false if meter is not full.
        /// </summary>
        bool ConsumeSpecialMeter(Entity entity, float cost);

        /// <summary>
        /// Ticks the combat system (combo timeout, hitstop countdown, meter decay).
        /// </summary>
        void Tick(float deltaTime);
    }
}
