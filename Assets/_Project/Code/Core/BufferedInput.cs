using System;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// A single input event with its action type, timestamp, and raw analog value.
    /// Stored in the InputBuffer for consumption by combat and movement systems.
    /// </summary>
    public struct BufferedInput
    {
        /// <summary>The action that was performed.</summary>
        public InputAction Action;

        /// <summary>
        /// Timestamp in seconds (Time.time or injected clock) when the input was recorded.
        /// Used for buffer expiry and simultaneous-input resolution.
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// Raw analog value for analog inputs (stick axes). 0 or 1 for digital buttons.
        /// </summary>
        public Vector2 AxisValue;

        /// <summary>Whether this input has been consumed by a system.</summary>
        public bool Consumed;

        public BufferedInput(InputAction action, float timestamp, Vector2 axisValue = default)
        {
            Action = action;
            Timestamp = timestamp;
            AxisValue = axisValue;
            Consumed = false;
        }
    }
}
