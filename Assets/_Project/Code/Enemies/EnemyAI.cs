using System;
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
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Data = data;
            CurrentTarget = null;
            HasAggroSlot = false;
            CurrentState = EnemyState.Idle;
        }

        /// <summary>
        /// Transitions to a new FSM state.
        /// </summary>
        public bool TransitionTo(EnemyState newState)
        {
            var oldState = CurrentState;
            if (oldState == newState)
            {
                return false;
            }

            if (!IsValidTransition(oldState, newState))
            {
                return false;
            }

            CurrentState = newState;
            if (newState == EnemyState.Stunned || newState == EnemyState.Dead)
            {
                HasAggroSlot = false;
            }

            OnStateChanged?.Invoke(oldState, newState);
            return true;
        }

        /// <summary>
        /// Called each frame. Runs the FSM logic.
        /// </summary>
        public virtual void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }
        }

        /// <summary>
        /// Called when this enemy takes a hit. May transition to Stunned.
        /// </summary>
        public void OnHit()
        {
            TransitionTo(EnemyState.Stunned);
        }

        private static bool IsValidTransition(EnemyState from, EnemyState to)
        {
            if (to == EnemyState.Dead)
            {
                return true;
            }

            return from switch
            {
                EnemyState.Idle => to == EnemyState.Chase || to == EnemyState.Attack || to == EnemyState.Stunned || to == EnemyState.Fleeing,
                EnemyState.Chase => to == EnemyState.Attack || to == EnemyState.Stunned || to == EnemyState.Idle || to == EnemyState.Fleeing,
                EnemyState.Attack => to == EnemyState.Stunned || to == EnemyState.Recover || to == EnemyState.Idle,
                EnemyState.Stunned => to == EnemyState.Recover || to == EnemyState.Dead,
                EnemyState.Recover => to == EnemyState.Chase || to == EnemyState.Attack || to == EnemyState.Idle || to == EnemyState.Fleeing,
                EnemyState.Fleeing => to == EnemyState.Stunned || to == EnemyState.Dead || to == EnemyState.Idle,
                EnemyState.Dead => false,
                _ => false
            };
        }
    }
}
