using System;
using System.Collections.Generic;
using System.IO;
using CorgiCommando.Core;
using UnityEngine;

namespace CorgiCommando.Testing
{
    /// <summary>
    /// Lightweight combat/session instrumentation collector used by playtest automation.
    /// JSON report shape:
    /// {
    ///   "hitstops":[{"startTime":0.5,"endTime":0.5666667,"durationFrames":4.0}],
    ///   "knockbacks":[{"impulseMagnitude":3.2,"targetId":"Enemy_12"}],
    ///   "screenShakes":[{"amplitude":0.15,"source":"Heavy"}],
    ///   "stateTransitions":[{"componentId":"FeralCatAI:2384","oldState":"Idle","newState":"Chase"}],
    ///   "frameTimes":[{"deltaTime":0.0166667}]
    /// }
    /// </summary>
    public static class PlaytestMetrics
    {
        public const float BaseFramerate = 60f;
        private const int InitialCapacity = 1024;

        public static bool IsRecording { get; set; }

        private static readonly List<HitstopEntry> _hitstops = new List<HitstopEntry>(InitialCapacity);
        private static readonly List<KnockbackEntry> _knockbacks = new List<KnockbackEntry>(InitialCapacity);
        private static readonly List<ScreenShakeEntry> _screenShakes = new List<ScreenShakeEntry>(InitialCapacity);
        private static readonly List<StateTransitionEntry> _stateTransitions = new List<StateTransitionEntry>(InitialCapacity);
        private static readonly List<FrameTimeEntry> _frameTimes = new List<FrameTimeEntry>(InitialCapacity);
        private static readonly List<InputRecordedEntry> _inputRecorded = new List<InputRecordedEntry>(InitialCapacity);
        private static readonly List<InputConsumedEntry> _inputConsumed = new List<InputConsumedEntry>(InitialCapacity);

        [Serializable]
        public sealed class PlaytestReport
        {
            public List<HitstopEntry> hitstops;
            public List<KnockbackEntry> knockbacks;
            public List<ScreenShakeEntry> screenShakes;
            public List<StateTransitionEntry> stateTransitions;
            public List<FrameTimeEntry> frameTimes;
            public List<InputRecordedEntry> inputRecorded;
            public List<InputConsumedEntry> inputConsumed;
        }

        [Serializable]
        public struct HitstopEntry
        {
            public float startTime;
            public float endTime;
            public float durationFrames;
        }

        [Serializable]
        public struct KnockbackEntry
        {
            public float impulseMagnitude;
            public string targetId;
        }

        [Serializable]
        public struct ScreenShakeEntry
        {
            public float amplitude;
            public string source;
        }

        [Serializable]
        public struct StateTransitionEntry
        {
            public string componentId;
            public string oldState;
            public string newState;
        }

        [Serializable]
        public struct FrameTimeEntry
        {
            public float deltaTime;
        }

        [Serializable]
        public struct InputRecordedEntry
        {
            public InputAction action;
            public Vector2 axisValue;
            public float timestamp;
        }

        [Serializable]
        public struct InputConsumedEntry
        {
            public InputAction action;
            public string consumerComponentId;
            public float timestamp;
        }

        public static void LogHitstop(float startTime, float endTime)
        {
            if (!IsRecording)
            {
                return;
            }

            float durationSeconds = Mathf.Max(0f, endTime - startTime);
            _hitstops.Add(new HitstopEntry
            {
                startTime = startTime,
                endTime = endTime,
                durationFrames = durationSeconds * BaseFramerate
            });
        }

        public static void LogKnockback(float impulseMagnitude, string targetId)
        {
            if (!IsRecording)
            {
                return;
            }

            _knockbacks.Add(new KnockbackEntry
            {
                impulseMagnitude = impulseMagnitude,
                targetId = targetId ?? string.Empty
            });
        }

        public static void LogScreenShake(float amplitude, string source)
        {
            if (!IsRecording)
            {
                return;
            }

            _screenShakes.Add(new ScreenShakeEntry
            {
                amplitude = amplitude,
                source = source ?? string.Empty
            });
        }

        public static void LogStateTransition(string componentId, string oldState, string newState)
        {
            if (!IsRecording)
            {
                return;
            }

            _stateTransitions.Add(new StateTransitionEntry
            {
                componentId = componentId ?? string.Empty,
                oldState = oldState ?? string.Empty,
                newState = newState ?? string.Empty
            });
        }

        public static void LogFrameTime(float deltaTime)
        {
            if (!IsRecording)
            {
                return;
            }

            _frameTimes.Add(new FrameTimeEntry { deltaTime = deltaTime });
        }

        public static void LogInputRecorded(InputAction action, Vector2 axisValue, float timestamp)
        {
            if (!IsRecording)
            {
                return;
            }

            _inputRecorded.Add(new InputRecordedEntry
            {
                action = action,
                axisValue = axisValue,
                timestamp = timestamp
            });
        }

        public static void LogInputConsumed(InputAction action, string consumerComponentId, float timestamp)
        {
            if (!IsRecording)
            {
                return;
            }

            _inputConsumed.Add(new InputConsumedEntry
            {
                action = action,
                consumerComponentId = consumerComponentId ?? string.Empty,
                timestamp = timestamp
            });
        }

        public static void Reset()
        {
            _hitstops.Clear();
            _knockbacks.Clear();
            _screenShakes.Clear();
            _stateTransitions.Clear();
            _frameTimes.Clear();
            _inputRecorded.Clear();
            _inputConsumed.Clear();
        }

        public static void WriteReport(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Report path must not be null or empty.", nameof(path));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var report = new PlaytestReport
            {
                hitstops = new List<HitstopEntry>(_hitstops),
                knockbacks = new List<KnockbackEntry>(_knockbacks),
                screenShakes = new List<ScreenShakeEntry>(_screenShakes),
                stateTransitions = new List<StateTransitionEntry>(_stateTransitions),
                frameTimes = new List<FrameTimeEntry>(_frameTimes),
                inputRecorded = new List<InputRecordedEntry>(_inputRecorded),
                inputConsumed = new List<InputConsumedEntry>(_inputConsumed)
            };

            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json);
        }
    }
}
