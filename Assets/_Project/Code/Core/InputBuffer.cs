using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Concrete input buffer. Stores timestamped input events for consumption
    /// by combat and movement systems. Purges stale inputs each frame.
    /// </summary>
    public class InputBuffer : IInputBuffer
    {
        /// <summary>Default buffer window in seconds. Inputs older than this are purged.</summary>
        public const float DefaultBufferWindow = 0.2f;

        public void RecordInput(InputAction action, float timestamp, Vector2 axisValue = default)
        {
            throw new NotImplementedException();
        }

        public BufferedInput? ConsumeInput(InputAction action)
        {
            throw new NotImplementedException();
        }

        public Vector2 GetMoveAxis()
        {
            throw new NotImplementedException();
        }

        public bool HasBufferedInput(InputAction action, float windowSeconds)
        {
            throw new NotImplementedException();
        }

        public void PurgeStaleInputs(float currentTime, float maxAge)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<BufferedInput> GetAllBuffered()
        {
            throw new NotImplementedException();
        }
    }
}
