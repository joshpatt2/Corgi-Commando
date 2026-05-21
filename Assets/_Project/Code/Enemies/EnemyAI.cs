using System;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Enemies
{
    /// <summary>
    /// Base enemy AI with lightweight FSM. Concrete subclasses (FeralCatAI,
    /// RaccoonBanditAI, SprinklerTurretAI) override behavior per state.
    /// </summary>
    public class EnemyAI : Entity
    {
        /// <summary>Current FSM state.</summary>
        public EnemyState CurrentState { get; protected set; }

        /// <summary>Enemy configuration data.</summary>
        public EnemyData Data { get; private set; }

        /// <summary>The player this enemy is currently targeting.</summary>
        public Entity CurrentTarget { get; protected set; }

        /// <summary>Whether this enemy holds an aggro slot on its target.</summary>
        public bool HasAggroSlot { get; protected set; }

        /// <summary>Fired on state transitions.</summary>
        public event Action<EnemyState, EnemyState> OnStateChanged;

        /// <summary>
        /// Initializes the enemy with its data asset.
        /// </summary>
        public void Initialize(EnemyData data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transitions to a new FSM state.
        /// </summary>
        public bool TransitionTo(EnemyState newState)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called each frame. Runs the FSM logic.
        /// </summary>
        public virtual void Tick(float deltaTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when this enemy takes a hit. May transition to Stunned.
        /// </summary>
        public void OnHit()
        {
            throw new NotImplementedException();
        }
    }
}
