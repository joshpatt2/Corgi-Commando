using System;
using CorgiCommando.Core;

namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Manages aggro slots per player target. Prevents dogpiling by limiting
    /// the number of enemies that can actively attack a single player.
    /// Enemies that can't get a slot circle at range instead.
    /// </summary>
    public class AggroSlotManager
    {
        /// <summary>Maximum number of attackers per player target (default 2).</summary>
        public int MaxSlotsPerTarget { get; set; } = 2;

        /// <summary>
        /// Attempts to reserve an aggro slot for the given enemy against the target.
        /// Returns true if a slot was available and reserved.
        /// </summary>
        public bool TryReserveSlot(EnemyAI enemy, Entity target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases the slot held by the given enemy (on stun, death, or state change).
        /// </summary>
        public void ReleaseSlot(EnemyAI enemy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the number of currently occupied slots for the given target.
        /// </summary>
        public int GetOccupiedSlots(Entity target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns true if a slot is available for the given target.
        /// </summary>
        public bool HasAvailableSlot(Entity target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears all slots (e.g., on wave clear).
        /// </summary>
        public void ClearAll()
        {
            throw new NotImplementedException();
        }
    }
}
