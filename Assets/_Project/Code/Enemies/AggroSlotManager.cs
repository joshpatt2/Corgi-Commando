using System.Collections.Generic;
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
        private readonly Dictionary<Entity, List<EnemyAI>> _slotsByTarget = new Dictionary<Entity, List<EnemyAI>>();
        private readonly Dictionary<EnemyAI, Entity> _targetByEnemy = new Dictionary<EnemyAI, Entity>();

        /// <summary>Maximum number of attackers per player target (default 2).</summary>
        public int MaxSlotsPerTarget { get; set; } = 2;

        /// <summary>
        /// Attempts to reserve an aggro slot for the given enemy against the target.
        /// Returns true if a slot was available and reserved.
        /// </summary>
        public bool TryReserveSlot(EnemyAI enemy, Entity target)
        {
            if (enemy == null || target == null)
            {
                return false;
            }

            if (_targetByEnemy.TryGetValue(enemy, out var existingTarget))
            {
                if (existingTarget == target)
                {
                    return true;
                }

                ReleaseSlot(enemy);
            }

            if (!_slotsByTarget.TryGetValue(target, out var attackers))
            {
                attackers = new List<EnemyAI>();
                _slotsByTarget[target] = attackers;
            }

            if (attackers.Count >= MaxSlotsPerTarget)
            {
                return false;
            }

            attackers.Add(enemy);
            _targetByEnemy[enemy] = target;
            return true;
        }

        /// <summary>
        /// Releases the slot held by the given enemy (on stun, death, or state change).
        /// </summary>
        public void ReleaseSlot(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!_targetByEnemy.TryGetValue(enemy, out var target))
            {
                return;
            }

            _targetByEnemy.Remove(enemy);
            if (!_slotsByTarget.TryGetValue(target, out var attackers))
            {
                return;
            }

            attackers.Remove(enemy);
            if (attackers.Count == 0)
            {
                _slotsByTarget.Remove(target);
            }
        }

        /// <summary>
        /// Returns the number of currently occupied slots for the given target.
        /// </summary>
        public int GetOccupiedSlots(Entity target)
        {
            if (target == null)
            {
                return 0;
            }

            return _slotsByTarget.TryGetValue(target, out var attackers) ? attackers.Count : 0;
        }

        /// <summary>
        /// Returns true if a slot is available for the given target.
        /// </summary>
        public bool HasAvailableSlot(Entity target)
        {
            return GetOccupiedSlots(target) < MaxSlotsPerTarget;
        }

        /// <summary>
        /// Clears all slots (e.g., on wave clear).
        /// </summary>
        public void ClearAll()
        {
            _slotsByTarget.Clear();
            _targetByEnemy.Clear();
        }
    }
}
