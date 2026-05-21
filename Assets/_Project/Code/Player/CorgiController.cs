using System;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Combat;
using CorgiCommando.Data;

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
        [SerializeField, Range(0, 1)] private int _playerIndex;

        private IInputBuffer _inputBuffer;
        private KinematicMovementController _movementController;
        private float _comboWindowRemainingFrames;

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

        /// <summary>Fired when state changes.</summary>
        public event Action<CorgiState, CorgiState> OnStateChanged;

        private void Awake()
        {
            TryAutoInitialize();
        }

        /// <summary>
        /// Initializes the controller with character data, input buffer, and player index.
        /// </summary>
        public void Initialize(CorgiData data, IInputBuffer inputBuffer, int playerIndex)
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
            RefreshSpecialReady();
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
            switch (state)
            {
                case CorgiState.Attack1:
                    ComboStep = 1;
                    OpenComboWindowForCurrentAttack();
                    return;
                case CorgiState.Attack2:
                    ComboStep = 2;
                    OpenComboWindowForCurrentAttack();
                    return;
                case CorgiState.Attack3:
                    ComboStep = 3;
                    OpenComboWindowForCurrentAttack();
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
