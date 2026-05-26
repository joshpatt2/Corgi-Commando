using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Interface for the input buffer that sits between raw Unity Input System events
    /// and gameplay systems (combat, movement). All gameplay reads from this buffer,
    /// never from raw input — enables replay and AI-driven players later.
    /// </summary>
    public interface IInputBuffer
    {
        /// <summary>
        /// Records an input action at the given timestamp.
        /// </summary>
        /// <param name="action">The action performed.</param>
        /// <param name="timestamp">Time the input occurred (seconds).</param>
        /// <param name="axisValue">
        /// Analog axis value if applicable. For MoveLeft/MoveRight/MoveUp/MoveDown,
        /// this value drives GetMoveAxis(). If omitted (Vector2.zero), the buffer
        /// falls back to the action's unit direction.
        /// </param>
        void RecordInput(InputAction action, float timestamp, Vector2 axisValue = default);

        /// <summary>
        /// Returns the most recent unconsumed input matching the given action
        /// within the buffer window, or null if none exists.
        /// </summary>
        BufferedInput? ConsumeInput(InputAction action);

        /// <summary>
        /// Returns the current movement axis value (most recent MoveLeft/Right/Up/Down composite).
        /// </summary>
        Vector2 GetMoveAxis();

        /// <summary>
        /// Checks whether the given action was pressed within the last <paramref name="windowSeconds"/>
        /// seconds and has not yet been consumed.
        /// </summary>
        bool HasBufferedInput(InputAction action, float windowSeconds);

        /// <summary>
        /// Removes all buffered inputs older than <paramref name="maxAge"/> seconds
        /// relative to <paramref name="currentTime"/>.
        /// </summary>
        void PurgeStaleInputs(float currentTime, float maxAge);

        /// <summary>
        /// Clears all buffered inputs immediately.
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns all currently buffered (unconsumed) inputs, ordered oldest to newest.
        /// </summary>
        IReadOnlyList<BufferedInput> GetAllBuffered();
    }
}
