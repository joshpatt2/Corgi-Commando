using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CorgiCommando.Core
{
    /// <summary>
    /// MonoBehaviour that bridges Unity's PlayerInput component to the game's IInputBuffer.
    /// One instance per player. Handles controller connect/disconnect events.
    /// Platform parity: identical contract on macOS and iOS. Gamepad canonical on both;
    /// keyboard is macOS-only fallback.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        /// <summary>The input buffer this handler writes to.</summary>
        public IInputBuffer Buffer { get; private set; }

        /// <summary>Player index (0 = P1, 1 = P2). Assigned by PlayerInputManager join order.</summary>
        public int PlayerIndex { get; private set; }

        /// <summary>Whether a gamepad is currently connected for this player.</summary>
        public bool IsGamepadConnected { get; private set; }

        /// <summary>Fired when this player's controller disconnects.</summary>
        public event Action<int> OnControllerDisconnected;

        /// <summary>Fired when a controller reconnects for this player.</summary>
        public event Action<int> OnControllerReconnected;

        /// <summary>
        /// Initializes the handler with an input buffer and player index.
        /// Called by the co-op join flow.
        /// </summary>
        public void Initialize(IInputBuffer buffer, int playerIndex)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            PlayerIndex = playerIndex;
            IsGamepadConnected = GetConnectedGamepadCount() > 0;
        }

        /// <summary>
        /// Called by Unity Input System message when a device is lost.
        /// </summary>
        public void OnDeviceLost()
        {
            IsGamepadConnected = false;
            OnControllerDisconnected?.Invoke(PlayerIndex);
        }

        /// <summary>
        /// Called by Unity Input System message when a device is regained.
        /// </summary>
        public void OnDeviceRegained()
        {
            IsGamepadConnected = true;
            OnControllerReconnected?.Invoke(PlayerIndex);
        }

        /// <summary>
        /// Returns the total number of gamepads currently connected to the device.
        /// Used to gate P2 join flow on iOS (requires second Bluetooth controller).
        /// </summary>
        public static int GetConnectedGamepadCount()
        {
            int connectedCount = 0;
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad gamepad = Gamepad.all[i];
                if (gamepad != null && gamepad.added)
                {
                    connectedCount++;
                }
            }

            return connectedCount;
        }

        private void OnMove(InputValue value)
        {
            if (Buffer == null || value == null)
            {
                return;
            }

            Vector2 axis = value.Get<Vector2>();
            Buffer.RecordInput(GetMoveAction(axis), Time.time, axis);
        }

        private void OnPunch()
        {
            RecordButtonInput(InputAction.Punch);
        }

        private void OnPunch(InputValue value)
        {
            RecordButtonInput(InputAction.Punch, value);
        }

        private void OnKick()
        {
            RecordButtonInput(InputAction.Kick);
        }

        private void OnKick(InputValue value)
        {
            RecordButtonInput(InputAction.Kick, value);
        }

        private void OnJump()
        {
            RecordButtonInput(InputAction.Jump);
        }

        private void OnJump(InputValue value)
        {
            RecordButtonInput(InputAction.Jump, value);
        }

        private void OnSpecial()
        {
            RecordButtonInput(InputAction.Special);
        }

        private void OnSpecial(InputValue value)
        {
            RecordButtonInput(InputAction.Special, value);
        }

        private void OnPause()
        {
            RecordButtonInput(InputAction.Pause);
        }

        private void OnPause(InputValue value)
        {
            RecordButtonInput(InputAction.Pause, value);
        }

        private void RecordButtonInput(InputAction action, InputValue value = null)
        {
            if (Buffer == null)
            {
                return;
            }

            if (value != null && !value.isPressed)
            {
                return;
            }

            Buffer.RecordInput(action, Time.time);
        }

        private static InputAction GetMoveAction(Vector2 axis)
        {
            if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y))
            {
                return axis.x < 0f ? InputAction.MoveLeft : InputAction.MoveRight;
            }

            return axis.y < 0f ? InputAction.MoveDown : InputAction.MoveUp;
        }
    }
}
