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

        private readonly List<BufferedInput> _bufferedInputs = new List<BufferedInput>();
        private float _latestTimestamp = float.NegativeInfinity;
        private Vector2 _latestMoveAxis;

        public void RecordInput(InputAction action, float timestamp, Vector2 axisValue = default)
        {
            _bufferedInputs.Add(new BufferedInput(action, timestamp, axisValue));

            if (timestamp > _latestTimestamp)
            {
                _latestTimestamp = timestamp;
            }

            if (IsMoveAction(action))
            {
                _latestMoveAxis = axisValue;
            }
        }

        public BufferedInput? ConsumeInput(InputAction action)
        {
            for (int i = _bufferedInputs.Count - 1; i >= 0; i--)
            {
                BufferedInput bufferedInput = _bufferedInputs[i];
                if (bufferedInput.Consumed || bufferedInput.Action != action)
                {
                    continue;
                }

                bufferedInput.Consumed = true;
                _bufferedInputs[i] = bufferedInput;
                return bufferedInput;
            }

            return null;
        }

        public Vector2 GetMoveAxis()
        {
            return _latestMoveAxis;
        }

        public bool HasBufferedInput(InputAction action, float windowSeconds)
        {
            if (windowSeconds < 0f)
            {
                windowSeconds = 0f;
            }

            if (_bufferedInputs.Count == 0 || float.IsNegativeInfinity(_latestTimestamp))
            {
                return false;
            }

            float minTimestamp = _latestTimestamp - windowSeconds;
            for (int i = _bufferedInputs.Count - 1; i >= 0; i--)
            {
                BufferedInput bufferedInput = _bufferedInputs[i];
                if (bufferedInput.Consumed || bufferedInput.Action != action)
                {
                    continue;
                }

                if (bufferedInput.Timestamp >= minTimestamp)
                {
                    return true;
                }
            }

            return false;
        }

        public void PurgeStaleInputs(float currentTime, float maxAge)
        {
            float minTimestamp = currentTime - maxAge;
            _bufferedInputs.RemoveAll(input => input.Timestamp < minTimestamp);
            RecalculateDerivedState();
        }

        public void Clear()
        {
            _bufferedInputs.Clear();
            _latestTimestamp = float.NegativeInfinity;
            _latestMoveAxis = Vector2.zero;
        }

        public IReadOnlyList<BufferedInput> GetAllBuffered()
        {
            var unconsumed = new List<BufferedInput>(_bufferedInputs.Count);
            for (int i = 0; i < _bufferedInputs.Count; i++)
            {
                BufferedInput bufferedInput = _bufferedInputs[i];
                if (!bufferedInput.Consumed)
                {
                    unconsumed.Add(bufferedInput);
                }
            }

            return unconsumed;
        }

        private static bool IsMoveAction(InputAction action)
        {
            return action == InputAction.MoveLeft ||
                   action == InputAction.MoveRight ||
                   action == InputAction.MoveUp ||
                   action == InputAction.MoveDown;
        }

        private void RecalculateDerivedState()
        {
            _latestTimestamp = float.NegativeInfinity;
            _latestMoveAxis = Vector2.zero;

            for (int i = 0; i < _bufferedInputs.Count; i++)
            {
                BufferedInput bufferedInput = _bufferedInputs[i];
                if (bufferedInput.Timestamp >= _latestTimestamp)
                {
                    _latestTimestamp = bufferedInput.Timestamp;
                }

                if (IsMoveAction(bufferedInput.Action))
                {
                    _latestMoveAxis = bufferedInput.AxisValue;
                }
            }
        }
    }
}
