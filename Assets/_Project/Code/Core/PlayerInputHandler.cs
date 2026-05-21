using System;
using UnityEngine;

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by Unity Input System message when a device is lost.
        /// </summary>
        public void OnDeviceLost()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by Unity Input System message when a device is regained.
        /// </summary>
        public void OnDeviceRegained()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the total number of gamepads currently connected to the device.
        /// Used to gate P2 join flow on iOS (requires second Bluetooth controller).
        /// </summary>
        public static int GetConnectedGamepadCount()
        {
            throw new NotImplementedException();
        }
    }
}
