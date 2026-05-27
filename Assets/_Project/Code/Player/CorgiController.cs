using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Combat;
using CorgiCommando.Data;
using CorgiCommando.Testing;

namespace CorgiCommando.Player
{
    /// <summary>
    /// Player controller for a corgi character. Extends Entity with state machine,
    /// input reading from InputBuffer, combo chains, and special meter consumption.
    /// One instance per active player.
    /// </summary>
    public class CorgiController : Entity
    {
        private const float FramesPerSecond = 60f;
        [SerializeField] private CorgiData _characterData;
        [SerializeField] private int _playerIndex;

        private IInputBuffer _inputBuffer;
        private KinematicMovementController _movementController;
        private float _comboWindowRemainingFrames;
        private ICombatSystem _combatSystem;
        private Coroutine _attackResolveCoroutine;
        private bool _hasResolvedThisAttack;

        /// <summary>Current state in the player state machine.</summary>
        public CorgiState CurrentState { get; private set; }

        /// <summary>Character data (stats, combo chain, special move).</summary>
        public CorgiData CharacterData { get; private set; }

        /// <summary>Player index (0 = P1, 1 = P2).</summary>
        public int PlayerIndex { get; private set; }

        /// <summary>Current special meter value (0 to maxSpecialMeter).</summary>
        public float SpecialMeter { get; private set; }

        /// <summary>Whether the special meter is full and ready to use.</summary>
        public bool IsSpecialReady { get; private set; }

        /// <summary>Current position in the combo chain (0 = not attacking).</summary>
        public int ComboStep { get; private set; }

        /// <summary>Whether the player is currently holding an environmental weapon.</summary>
        public bool IsHoldingWeapon { get; private set; }

        /// <summary>Facing direction: 1 = right, -1 = left. Updated from move axis sign.</summary>
        public int Facing { get; private set; } = 1;

        /// <summary>Fired when state changes.</summary>
        public event Action<CorgiState, CorgiState> OnStateChanged;

        /// <summary>Fired when an attack lands a hit. Use for VFX/audio hookups.</summary>
        public event Action<HitResult> OnHitLanded;

        private void Awake()
        {
            TryAutoInitialize();
        }

        /// <summary>
        /// Initializes the controller with character data, input buffer, and player index.
        /// </summary>
        public void Initialize(CorgiData data, IInputBuffer inputBuffer, int playerIndex)
        {
            string componentId = $"{gameObject.name} ({GetType().Name})";
            try
            {
                CharacterData = data ?? throw new ArgumentNullException(nameof(data));
                _inputBuffer = inputBuffer ?? throw new ArgumentNullException(nameof(inputBuffer));
                if (playerIndex < 0 || playerIndex > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(playerIndex), "Player index must be 0 (P1) or 1 (P2).");
                }

                PlayerIndex = playerIndex;
                _movementController = GetComponent<KinematicMovementController>();
                if (_movementController != null)
                {
                    _movementController.WalkSpeed = CharacterData.walkSpeed;
                    _movementController.DepthSpeed = CharacterData.depthSpeed;
                    _movementController.JumpForce = CharacterData.jumpForce;
                }

                CurrentState = CorgiState.Idle;
                ComboStep = 0;
                IsHoldingWeapon = false;
                SpecialMeter = 0f;
                _comboWindowRemainingFrames = 0f;
                _hasResolvedThisAttack = false;
                _attackResolveCoroutine = null;
                RefreshSpecialReady();
                PlaytestMetrics.LogInitialize(componentId, true, string.Empty);
            }
            catch (Exception ex)
            {
                PlaytestMetrics.LogInitialize(componentId, false, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Injects the combat system used for attack resolution.
        /// Call this from the scene bootstrap after initialization.
        /// </summary>
        public void SetCombatSystem(ICombatSystem combatSystem)
        {
            _combatSystem = combatSystem;
        }

        /// <summary>
        /// Transitions to a new state. Validates the transition is legal.
        /// </summary>
        /// <param name="newState">Target state.</param>
        /// <returns>True if the transition was valid and applied.</returns>
        public bool TransitionTo(CorgiState newState)
        {
            if (newState == CurrentState || !IsValidTransition(CurrentState, newState))
            {
                return false;
            }

            CorgiState previousState = CurrentState;
            CurrentState = newState;
            OnEnterState(newState);
            OnStateChanged?.Invoke(previousState, newState);
            return true;
        }

        /// <summary>
        /// Returns the AttackData for the current combo step.
        /// </summary>
        public AttackData GetCurrentAttackData()
        {
            if (CharacterData == null || CharacterData.comboChain == null || ComboStep <= 0)
            {
                return null;
            }

            int comboIndex = ComboStep - 1;
            if (comboIndex >= CharacterData.comboChain.Length)
            {
                return null;
            }

            return CharacterData.comboChain[comboIndex];
        }

        /// <summary>
        /// Attempts to use the special move. Requires full meter.
        /// Returns false if meter is not full.
        /// </summary>
        public bool UseSpecial()
        {
            if (CharacterData == null)
            {
                return false;
            }

            RefreshSpecialReady();
            if (!IsSpecialReady || SpecialMeter < CharacterData.specialCost)
            {
                return false;
            }

            if (!IsValidTransition(CurrentState, CorgiState.Special))
            {
                return false;
            }

            SpecialMeter = Mathf.Max(0f, SpecialMeter - CharacterData.specialCost);
            RefreshSpecialReady();
            return TransitionTo(CorgiState.Special);
        }

        /// <summary>
        /// Adds special meter from gameplay events (for example, landing hits).
        /// Value is clamped to [0, maxSpecialMeter].
        /// </summary>
        public void AddSpecialMeter(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Special meter amount cannot be negative.");
            }

            if (CharacterData == null || amount == 0f)
            {
                return;
            }

            SpecialMeter = Mathf.Clamp(SpecialMeter + amount, 0f, CharacterData.maxSpecialMeter);
            RefreshSpecialReady();
        }

        /// <summary>
        /// Called each frame. Reads input buffer, updates state machine.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_inputBuffer == null)
            {
                return;
            }

            if (_comboWindowRemainingFrames > 0f)
            {
                _comboWindowRemainingFrames = Mathf.Max(0f, _comboWindowRemainingFrames - (deltaTime * FramesPerSecond));
            }

            Vector2 axis = _inputBuffer.GetMoveAxis();
            _movementController?.SetMoveInput(axis);

            if (Mathf.Abs(axis.x) > 0.1f)
            {
                Facing = axis.x > 0f ? 1 : -1;
            }

            // Priority rule: when both are buffered this frame, Special is consumed before combo attacks.
            if (_inputBuffer.ConsumeInput(InputAction.Special).HasValue)
            {
                UseSpecial();
            }
            else
            {
                switch (CurrentState)
                {
                    case CorgiState.Attack1:
                        if (_inputBuffer.ConsumeInput(InputAction.Punch).HasValue)
                        {
                            TransitionTo(CorgiState.Attack2);
                        }
                        break;
                    case CorgiState.Attack2:
                        if (_inputBuffer.ConsumeInput(InputAction.Kick).HasValue || _inputBuffer.ConsumeInput(InputAction.Punch).HasValue)
                        {
                            TransitionTo(CorgiState.Attack3);
                        }
                        break;
                    default:
                        if (_inputBuffer.ConsumeInput(InputAction.Punch).HasValue)
                        {
                            TransitionTo(CorgiState.Attack1);
                        }
                        break;
                }

                bool hasMoveInput = axis.sqrMagnitude > 0f;
                if (hasMoveInput && CurrentState == CorgiState.Idle)
                {
                    TransitionTo(CorgiState.Walk);
                }
                else if (!hasMoveInput && CurrentState == CorgiState.Walk)
                {
                    TransitionTo(CorgiState.Idle);
                }
            }

            if (ShouldDecaySpecialMeter())
            {
                SpecialMeter = Mathf.Max(0f, SpecialMeter - (CharacterData.specialDecayRate * deltaTime));
                RefreshSpecialReady();
            }

            _movementController?.Tick(deltaTime);
        }

        /// <summary>
        /// Called when this entity takes a hit. Transitions to Hit or Knockdown state.
        /// </summary>
        public void OnHit(HitResult hitResult)
        {
            if (!hitResult.DidHit)
            {
                return;
            }

            TransitionTo(CorgiState.Hit);
        }

        private bool IsValidTransition(CorgiState from, CorgiState to)
        {
            switch (from)
            {
                case CorgiState.Idle:
                    return to == CorgiState.Walk ||
                           to == CorgiState.Attack1 ||
                           to == CorgiState.Special ||
                           to == CorgiState.Hit;
                case CorgiState.Walk:
                    return to == CorgiState.Idle ||
                           to == CorgiState.Attack1 ||
                           to == CorgiState.Special ||
                           to == CorgiState.Hit;
                case CorgiState.Attack1:
                    return (to == CorgiState.Attack2 && CanChainToNextAttack()) ||
                           to == CorgiState.Idle ||
                           to == CorgiState.Hit;
                case CorgiState.Attack2:
                    return (to == CorgiState.Attack3 && CanChainToNextAttack()) ||
                           to == CorgiState.Idle ||
                           to == CorgiState.Hit;
                case CorgiState.Attack3:
                    return to == CorgiState.Idle ||
                           to == CorgiState.Hit;
                case CorgiState.Hit:
                    return to == CorgiState.Knockdown ||
                           to == CorgiState.Idle;
                case CorgiState.Knockdown:
                    return to == CorgiState.GetUp;
                case CorgiState.GetUp:
                    return to == CorgiState.Idle;
                case CorgiState.Special:
                    return to == CorgiState.Idle ||
                           to == CorgiState.Hit;
                case CorgiState.PickupHold:
                    return to == CorgiState.Idle ||
                           to == CorgiState.Walk ||
                           to == CorgiState.Attack1 ||
                           to == CorgiState.Special ||
                           to == CorgiState.Hit;
                case CorgiState.Dead:
                    return false;
                default:
                    return false;
            }
        }

        private void OnEnterState(CorgiState state)
        {
            _hasResolvedThisAttack = false;

            if (_attackResolveCoroutine != null)
            {
                StopCoroutine(_attackResolveCoroutine);
                _attackResolveCoroutine = null;
            }

            switch (state)
            {
                case CorgiState.Attack1:
                    ComboStep = 1;
                    OpenComboWindowForCurrentAttack();
                    ScheduleAttackResolve(GetCurrentAttackData());
                    return;
                case CorgiState.Attack2:
                    ComboStep = 2;
                    OpenComboWindowForCurrentAttack();
                    ScheduleAttackResolve(GetCurrentAttackData());
                    return;
                case CorgiState.Attack3:
                    ComboStep = 3;
                    OpenComboWindowForCurrentAttack();
                    ScheduleAttackResolve(GetCurrentAttackData());
                    return;
                case CorgiState.Special:
                    ScheduleAttackResolve(CharacterData?.specialAttack);
                    ComboStep = 0;
                    _comboWindowRemainingFrames = 0f;
                    return;
                default:
                    ComboStep = 0;
                    _comboWindowRemainingFrames = 0f;
                    return;
            }
        }

        private bool CanChainToNextAttack()
        {
            AttackData currentAttack = GetCurrentAttackData();
            return currentAttack != null &&
                   currentAttack.comboWindowFrames > 0 &&
                   _comboWindowRemainingFrames > 0f;
        }

        private void OpenComboWindowForCurrentAttack()
        {
            AttackData currentAttack = GetCurrentAttackData();
            if (currentAttack == null)
            {
                _comboWindowRemainingFrames = 0f;
                return;
            }

            // Design choice: follow-up can be buffered during recovery and through comboWindowFrames.
            // This intentionally uses a forgiving chain timing (recovery + window), even though
            // comboWindowFrames alone could also be interpreted as a strict post-recovery window.
            // Example: recovery=10 and comboWindow=5 gives 15 total frames to enter the next attack.
            _comboWindowRemainingFrames = Mathf.Max(0f, currentAttack.recoveryFrames + currentAttack.comboWindowFrames);
        }

        private void RefreshSpecialReady()
        {
            if (CharacterData == null)
            {
                IsSpecialReady = false;
                return;
            }

            // Per player-controller contract, specials require a full meter and the configured move cost.
            IsSpecialReady = SpecialMeter >= CharacterData.maxSpecialMeter &&
                             SpecialMeter >= CharacterData.specialCost;
        }

        private static bool IsInAttackState(CorgiState state)
        {
            return state == CorgiState.Attack1 ||
                   state == CorgiState.Attack2 ||
                   state == CorgiState.Attack3;
        }

        private bool ShouldDecaySpecialMeter()
        {
            return !IsInAttackState(CurrentState) &&
                   CharacterData != null &&
                   CharacterData.specialDecayRate > 0f &&
                   SpecialMeter > 0f;
        }

        private void ScheduleAttackResolve(AttackData attackData)
        {
            if (_combatSystem == null || attackData == null)
            {
                return;
            }

            _attackResolveCoroutine = StartCoroutine(ResolveAfterStartupFrames(attackData));
        }

        private IEnumerator ResolveAfterStartupFrames(AttackData attackData)
        {
            // Wait for startup frames before the active window opens
            if (attackData.startupFrames > 0)
            {
                yield return new WaitForSeconds(attackData.startupFrames / FramesPerSecond);
            }
            else
            {
                yield return null; // Yield one frame so state is fully entered
            }

            if (_hasResolvedThisAttack)
            {
                yield break;
            }

            _hasResolvedThisAttack = true;

            // Gather enemy targets in the scene
            var targets = new List<Entity>();
            Entity[] allEntities = FindObjectsOfType<Entity>();
            for (int i = 0; i < allEntities.Length; i++)
            {
                Entity candidate = allEntities[i];
                if (candidate != null && candidate != this && candidate.IsAlive && candidate.Faction != Faction)
                {
                    targets.Add(candidate);
                }
            }

            if (targets.Count == 0)
            {
                _attackResolveCoroutine = null;
                yield break;
            }

            HitResult result = _combatSystem.ResolveAttack(this, attackData, targets.ToArray());

            if (result.DidHit)
            {
                // Reapply knockback with facing correction so hits push in the right direction.
                // CombatSystem.ResolveAttack already applied the raw knockback from AttackData;
                // this call intentionally overwrites it with the facing-corrected impulse because
                // KnockbackReceiver.ApplyKnockback is a simple setter (KnockbackVelocity = impulse).
                // The X component is signed by Facing so enemies fly away from the attacker.
                var knockbackReceiver = result.Target?.GetEntityComponent<KnockbackReceiver>();
                if (knockbackReceiver != null)
                {
                    knockbackReceiver.ApplyKnockback(new Vector3(
                        attackData.knockbackForce.x * Facing,
                        attackData.knockbackForce.y,
                        attackData.knockbackForce.z));
                }

                // Fill attacker's special meter for landing a hit
                if (CharacterData != null)
                {
                    AddSpecialMeter(CharacterData.specialGainPerHit);
                }

                OnHitLanded?.Invoke(result);
            }

            _attackResolveCoroutine = null;
        }

        private void TryAutoInitialize()
        {
            if (CharacterData != null || _characterData == null)
            {
                return;
            }

            int clampedPlayerIndex = Mathf.Clamp(_playerIndex, 0, 1);
            IInputBuffer inputBuffer = null;
            PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                inputBuffer = inputHandler.Buffer;
            }

            if (inputBuffer == null)
            {
                inputBuffer = new InputBuffer();
                inputHandler?.Initialize(inputBuffer, clampedPlayerIndex);
            }

            Initialize(_characterData, inputBuffer, clampedPlayerIndex);
        }
    }
}
