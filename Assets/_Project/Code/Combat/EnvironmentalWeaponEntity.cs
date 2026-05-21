using System;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Combat
{
    /// <summary>
    /// Environmental weapon that can be picked up, swung, or thrown.
    /// Light attack = swing (multi-use). Heavy attack = throw (consumes).
    /// Breaks after N uses. Holding slows movement.
    /// </summary>
    public class EnvironmentalWeaponEntity : Entity
    {
        /// <summary>Whether this weapon can be picked up (is on the ground, flagged).</summary>
        public bool IsPickupable { get; private set; }

        /// <summary>Whether this weapon is currently held by a player.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>The entity currently holding this weapon (null if not held).</summary>
        public Entity Holder { get; private set; }

        /// <summary>Remaining uses before the weapon breaks.</summary>
        public int RemainingUses { get; private set; }

        /// <summary>Attack data used when swinging this weapon.</summary>
        public AttackData SwingAttackData { get; private set; }

        /// <summary>Attack data used when throwing this weapon.</summary>
        public AttackData ThrowAttackData { get; private set; }

        /// <summary>Movement speed multiplier while held (< 1.0 = slower).</summary>
        public float HeldSpeedMultiplier { get; set; } = 0.7f;

        /// <summary>Fired when the weapon breaks.</summary>
        public event Action<EnvironmentalWeaponEntity> OnWeaponBroken;

        /// <summary>
        /// Initializes the weapon with attack data and use count.
        /// </summary>
        public void Initialize(AttackData swingData, AttackData throwData, int maxUses)
        {
            SwingAttackData = swingData;
            ThrowAttackData = throwData;
            RemainingUses = maxUses;
            IsPickupable = true;
            IsHeld = false;
            Holder = null;
        }

        /// <summary>
        /// Picks up this weapon. Called by PickupHandler when player presses Special near it.
        /// </summary>
        public void Pickup(Entity holder)
        {
            if (holder == null)
            {
                throw new ArgumentNullException(nameof(holder));
            }

            IsPickupable = false;
            IsHeld = true;
            Holder = holder;

            var mover = holder.GetComponent<KinematicMovementController>();
            if (mover != null)
            {
                mover.SpeedMultiplier = HeldSpeedMultiplier;
            }
        }

        /// <summary>
        /// Swings the weapon (light attack). Decrements uses.
        /// </summary>
        /// <returns>The attack data for the swing, or null if broken.</returns>
        public AttackData Swing()
        {
            if (RemainingUses <= 0)
            {
                return null;
            }

            RemainingUses--;

            if (RemainingUses == 0)
            {
                Break();
            }

            return SwingAttackData;
        }

        /// <summary>
        /// Throws the weapon (heavy attack). Consumes the weapon immediately.
        /// Returns null if the weapon is already broken (RemainingUses == 0).
        /// </summary>
        /// <returns>The attack data for the throw, or null if already broken.</returns>
        public AttackData Throw()
        {
            if (RemainingUses <= 0)
            {
                return null;
            }

            var data = ThrowAttackData;
            ResetHolderSpeed();
            RemainingUses = 0;
            IsHeld = false;
            Holder = null;
            return data;
        }

        /// <summary>
        /// Drops the weapon without consuming it.
        /// </summary>
        public void Drop()
        {
            ResetHolderSpeed();
            IsHeld = false;
            IsPickupable = true;
            Holder = null;
        }

        private void Break()
        {
            ResetHolderSpeed();
            Holder = null;
            IsHeld = false;
            OnWeaponBroken?.Invoke(this);
        }

        private void ResetHolderSpeed()
        {
            if (Holder != null)
            {
                var mover = Holder.GetComponent<KinematicMovementController>();
                if (mover != null)
                {
                    mover.SpeedMultiplier = 1f;
                }
            }
        }
    }
}
