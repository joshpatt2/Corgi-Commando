using System;
using UnityEngine;
using CorgiCommando.Core;

namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Base class for boss enemies. Extends Entity with phase tracking
    /// and HP-threshold-driven phase transitions.
    /// </summary>
    public class BossEntity : Entity
    {
        /// <summary>Current boss phase (1-indexed).</summary>
        public int CurrentPhase { get; protected set; } = 1;

        /// <summary>Total number of phases.</summary>
        public int TotalPhases { get; protected set; }

        /// <summary>Fired when the boss transitions to a new phase.</summary>
        public event Action<int, int> OnPhaseChanged;

        /// <summary>
        /// Checks HP thresholds and transitions phase if needed.
        /// Called after taking damage.
        /// </summary>
        public virtual void CheckPhaseTransition(int currentHP, int maxHP)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transitions to the specified phase.
        /// </summary>
        protected void TransitionToPhase(int newPhase)
        {
            throw new NotImplementedException();
        }
    }
}
