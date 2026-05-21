using System;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Interface for health management on entities.
    /// Fires events on damage and death for combat system integration.
    /// </summary>
    public interface IHealthComponent : IEntityComponent
    {
        /// <summary>Current HP.</summary>
        int CurrentHP { get; }

        /// <summary>Maximum HP.</summary>
        int MaxHP { get; }

        /// <summary>Whether this entity is dead (CurrentHP &lt;= 0).</summary>
        bool IsDead { get; }

        /// <summary>Fired when damage is taken. Arg = damage amount.</summary>
        event Action<int> OnDamaged;

        /// <summary>Fired when HP reaches zero.</summary>
        event Action OnDied;

        /// <summary>
        /// Applies damage. Clamps to zero. Fires OnDamaged and OnDied as appropriate.
        /// </summary>
        /// <param name="amount">Positive damage amount.</param>
        void TakeDamage(int amount);

        /// <summary>
        /// Heals by the given amount, clamped to MaxHP.
        /// </summary>
        void Heal(int amount);

        /// <summary>
        /// Sets HP to max. Used on spawn/revive.
        /// </summary>
        void ResetToMax();
    }
}
