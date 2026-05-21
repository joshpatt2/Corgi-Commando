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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Picks up this weapon. Called by PickupHandler when player presses Special near it.
        /// </summary>
        public void Pickup(Entity holder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Swings the weapon (light attack). Decrements uses.
        /// </summary>
        /// <returns>The attack data for the swing, or null if broken.</returns>
        public AttackData Swing()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Throws the weapon (heavy attack). Consumes the weapon immediately.
        /// </summary>
        /// <returns>The attack data for the throw.</returns>
        public AttackData Throw()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Drops the weapon without consuming it.
        /// </summary>
        public void Drop()
        {
            throw new NotImplementedException();
        }
    }
}
