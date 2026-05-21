using System;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Receives knockback impulses from the combat system and applies them
    /// as velocity to the entity's movement controller.
    /// </summary>
    public class KnockbackReceiver : IEntityComponent
    {
        public Entity Owner { get; private set; }

        /// <summary>Current knockback velocity being applied.</summary>
        public Vector3 KnockbackVelocity { get; private set; }

        /// <summary>Whether knockback is currently active.</summary>
        public bool IsInKnockback { get; private set; }

        /// <summary>Fired when a knockback impulse is applied.</summary>
        public event Action<Vector3> OnKnockbackApplied;

        public void OnAttach(Entity owner)
        {
            Owner = owner;
        }

        public void OnDetach()
        {
            Owner = null;
            ClearKnockback();
        }

        /// <summary>
        /// Applies a knockback impulse. Direction and magnitude from AttackData.
        /// </summary>
        /// <param name="impulse">Velocity impulse vector (X, Y, Z).</param>
        public void ApplyKnockback(Vector3 impulse)
        {
            KnockbackVelocity = impulse;
            IsInKnockback = true;
            OnKnockbackApplied?.Invoke(impulse);
        }

        /// <summary>
        /// Clears knockback state (called when knockback animation completes).
        /// </summary>
        public void ClearKnockback()
        {
            KnockbackVelocity = Vector3.zero;
            IsInKnockback = false;
        }
    }
}
