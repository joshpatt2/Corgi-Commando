using System.Collections.Generic;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Concrete input buffer. Stores timestamped input events for consumption
    /// by combat and movement systems. Purges stale inputs each frame.
    /// Move axis is derived from the most recent MoveLeft/MoveRight/MoveUp/MoveDown
    /// input (axis payload when provided, otherwise the action's unit direction).
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
            Vector2 resolvedAxisValue = IsMoveAction(action)
                ? ResolveMoveAxis(action, axisValue)
                : axisValue;

            _bufferedInputs.Add(new BufferedInput(action, timestamp, resolvedAxisValue));

            if (timestamp > _latestTimestamp)
            {
                _latestTimestamp = timestamp;
            }

            if (IsMoveAction(action))
            {
                _latestMoveAxis = resolvedAxisValue;
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

        private static Vector2 ResolveMoveAxis(InputAction action, Vector2 axisValue)
        {
            if (axisValue != Vector2.zero)
            {
                return axisValue;
            }

            return action switch
            {
                InputAction.MoveLeft => Vector2.left,
                InputAction.MoveRight => Vector2.right,
                InputAction.MoveUp => Vector2.up,
                InputAction.MoveDown => Vector2.down,
                _ => axisValue
            };
        }

        private void RecalculateDerivedState()
        {
            _latestTimestamp = float.NegativeInfinity;
            _latestMoveAxis = Vector2.zero;
            float latestMoveTimestamp = float.NegativeInfinity;

            for (int i = 0; i < _bufferedInputs.Count; i++)
            {
                BufferedInput bufferedInput = _bufferedInputs[i];
                if (bufferedInput.Timestamp > _latestTimestamp)
                {
                    _latestTimestamp = bufferedInput.Timestamp;
                }

                if (IsMoveAction(bufferedInput.Action) && bufferedInput.Timestamp > latestMoveTimestamp)
                {
                    latestMoveTimestamp = bufferedInput.Timestamp;
                    _latestMoveAxis = bufferedInput.AxisValue;
                }
            }
        }
    }
}
