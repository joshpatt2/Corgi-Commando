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
        private readonly Collider[] _overlapBuffer = new Collider[16];

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
            int count = Physics.OverlapSphereNonAlloc(transform.position, PickupRange, _overlapBuffer);
            EnvironmentalWeaponEntity nearest = null;
            float nearestSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var weapon = _overlapBuffer[i].GetComponent<EnvironmentalWeaponEntity>();
                if (weapon != null && weapon.IsPickupable)
                {
                    float sqrDist = (transform.position - _overlapBuffer[i].transform.position).sqrMagnitude;
                    if (sqrDist < nearestSqrDist)
                    {
                        nearestSqrDist = sqrDist;
                        nearest = weapon;
                    }
                }
            }

            if (nearest == null)
            {
                return false;
            }

            nearest.Pickup(holder);
            HeldWeapon = nearest;
            return true;
        }

        /// <summary>
        /// Uses the held weapon with a light attack (swing).
        /// </summary>
        public void UseSwing()
        {
            HeldWeapon?.Swing();
            if (HeldWeapon != null && !HeldWeapon.IsHeld)
            {
                HeldWeapon = null;
            }
        }

        /// <summary>
        /// Uses the held weapon with a heavy attack (throw, consumes weapon).
        /// </summary>
        public void UseThrow()
        {
            HeldWeapon?.Throw();
            HeldWeapon = null;
        }

        /// <summary>
        /// Drops the currently held weapon.
        /// </summary>
        public void DropWeapon()
        {
            HeldWeapon?.Drop();
            HeldWeapon = null;
        }
    }
}
