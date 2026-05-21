using System;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Combat;

namespace CorgiCommando.Player
{
    /// <summary>
    /// Handles environmental weapon pickup for a player entity.
    /// Checks proximity to flagged objects when Special is pressed.
    /// </summary>
    public class PickupHandler : MonoBehaviour
    {
        /// <summary>Maximum distance to pick up a weapon.</summary>
        public float PickupRange { get; set; } = 1.5f;

        /// <summary>The weapon currently held (null if empty-handed).</summary>
        public EnvironmentalWeaponEntity HeldWeapon { get; private set; }

        /// <summary>Whether the player is currently holding a weapon.</summary>
        public bool IsHoldingWeapon => HeldWeapon != null;

        /// <summary>
        /// Attempts to pick up the nearest weapon within range.
        /// Returns true if a weapon was picked up.
        /// </summary>
        public bool TryPickup(Entity holder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Uses the held weapon with a light attack (swing).
        /// </summary>
        public void UseSwing()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Uses the held weapon with a heavy attack (throw, consumes weapon).
        /// </summary>
        public void UseThrow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Drops the currently held weapon.
        /// </summary>
        public void DropWeapon()
        {
            throw new NotImplementedException();
        }
    }
}
