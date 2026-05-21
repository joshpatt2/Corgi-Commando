using System;
using System.Collections.Generic;
using CorgiCommando.Core;

namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Manages aggro slots per player target. Prevents dogpiling by limiting
    /// the number of enemies that can actively attack a single player.
    /// Enemies that can't get a slot circle at range instead.
    /// </summary>
    public class AggroSlotManager : IDisposable
    {
        private static readonly List<AggroSlotManager> ActiveManagers = new List<AggroSlotManager>();
        private static readonly Dictionary<EnemyAI, AggroSlotManager> ReservationOwner = new Dictionary<EnemyAI, AggroSlotManager>();
        private readonly Dictionary<Entity, List<EnemyAI>> _slotsByTarget = new Dictionary<Entity, List<EnemyAI>>();
        private readonly Dictionary<EnemyAI, Entity> _targetByEnemy = new Dictionary<EnemyAI, Entity>();

        public AggroSlotManager()
        {
            ActiveManagers.Add(this);
        }

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
            else if (ReservationOwner.TryGetValue(enemy, out var owner))
            {
                if (!HasAvailableSlot(target))
                {
                    return false;
                }

                owner.ReleaseSlot(enemy);
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
            ReservationOwner[enemy] = this;
            enemy.SetAggroSlotStatus(true);
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
                ReservationOwner.Remove(enemy);
                enemy.SetAggroSlotStatus(false);
                return;
            }

            _targetByEnemy.Remove(enemy);
            ReservationOwner.Remove(enemy);
            if (!_slotsByTarget.TryGetValue(target, out var attackers))
            {
                enemy.SetAggroSlotStatus(false);
                return;
            }

            attackers.Remove(enemy);
            if (attackers.Count == 0)
            {
                _slotsByTarget.Remove(target);
            }

            enemy.SetAggroSlotStatus(false);
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
            var reservedEnemies = new List<EnemyAI>(_targetByEnemy.Keys);
            foreach (var enemy in reservedEnemies)
            {
                ReservationOwner.Remove(enemy);
                enemy.SetAggroSlotStatus(false);
            }

            _slotsByTarget.Clear();
            _targetByEnemy.Clear();
        }

        public void Dispose()
        {
            ClearAll();
            ActiveManagers.Remove(this);
        }

        internal static void ReleaseSlotForEnemy(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (ReservationOwner.TryGetValue(enemy, out var owner))
            {
                owner.ReleaseSlot(enemy);
            }
        }

        internal static bool TryReserveAny(EnemyAI enemy, Entity target)
        {
            if (enemy == null || target == null)
            {
                return false;
            }

            if (ReservationOwner.TryGetValue(enemy, out var owner))
            {
                return owner.TryReserveSlot(enemy, target);
            }

            for (int i = 0; i < ActiveManagers.Count; i++)
            {
                if (ActiveManagers[i].TryReserveSlot(enemy, target))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasActiveManager()
        {
            return ActiveManagers.Count > 0;
        }
    }
}
