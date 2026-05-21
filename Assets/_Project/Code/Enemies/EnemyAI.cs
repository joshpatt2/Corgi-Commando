using System;
using CorgiCommando.Core;
using CorgiCommando.Data;
using UnityEngine;

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
            Faction = CorgiCommando.Core.Faction.Enemy;
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
                AggroSlotManager.ReleaseSlotFromAllManagers(this);
                HasAggroSlot = false;
            }

            OnStateChanged?.Invoke(oldState, newState);
            OnStateTransitioned(oldState, newState);
            return true;
        }

        /// <summary>
        /// Called each frame. Runs the FSM logic.
        /// </summary>
        public virtual void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || CurrentState == EnemyState.Dead)
            {
                return;
            }

            if (CurrentState == EnemyState.Stunned)
            {
                TransitionTo(EnemyState.Recover);
                return;
            }

            if (CurrentState == EnemyState.Recover)
            {
                TransitionTo(EnemyState.Chase);
                return;
            }

            if (CurrentState == EnemyState.Attack)
            {
                TransitionTo(EnemyState.Recover);
                return;
            }

            CurrentTarget = FindClosestPlayerTarget();
            if (CurrentTarget == null || Data == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, CurrentTarget.transform.position);
            if (CurrentState == EnemyState.Idle && distance <= Data.aggroRange)
            {
                TransitionTo(EnemyState.Chase);
                return;
            }

            if (CurrentState == EnemyState.Chase && distance <= Data.attackRange)
            {
                if (TryAcquireAggroSlot())
                {
                    TransitionTo(EnemyState.Attack);
                }
                else
                {
                    CircleTarget(deltaTime);
                }
            }
        }

        /// <summary>
        /// Called when this enemy takes a hit. May transition to Stunned.
        /// </summary>
        public void OnHit()
        {
            TransitionTo(EnemyState.Stunned);
        }

        internal void SetAggroSlotStatus(bool hasSlot)
        {
            HasAggroSlot = hasSlot;
        }

        protected virtual void OnStateTransitioned(EnemyState oldState, EnemyState newState)
        {
        }

        private bool TryAcquireAggroSlot()
        {
            if (HasAggroSlot)
            {
                return true;
            }

            if (CurrentTarget == null)
            {
                return false;
            }

            if (AggroSlotManager.TryReserveAny(this, CurrentTarget))
            {
                return true;
            }

            if (!AggroSlotManager.HasActiveManager())
            {
                HasAggroSlot = true;
                return true;
            }

            return false;
        }

        private Entity FindClosestPlayerTarget()
        {
            var entities = UnityEngine.Object.FindObjectsOfType<Entity>();
            Entity closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < entities.Length; i++)
            {
                var candidate = entities[i];
                if (candidate == null || candidate == this || !candidate.IsAlive || candidate is EnemyAI)
                {
                    continue;
                }

                if (candidate.Faction != Faction.Player)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        private void CircleTarget(float deltaTime)
        {
            if (CurrentTarget == null || Data == null)
            {
                return;
            }

            Vector3 toTarget = CurrentTarget.transform.position - transform.position;
            if (toTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 lateral = new Vector3(-toTarget.y, toTarget.x, 0f).normalized;
            transform.position += lateral * Data.moveSpeed * deltaTime;
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
