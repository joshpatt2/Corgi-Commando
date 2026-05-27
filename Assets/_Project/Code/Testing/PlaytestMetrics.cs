using System;
using System.Collections.Generic;
using System.IO;
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
        public static event Action<StateTransitionEntry> StateTransitionLogged;

        private static readonly List<HitstopEntry> _hitstops = new List<HitstopEntry>(InitialCapacity);
        private static readonly List<KnockbackEntry> _knockbacks = new List<KnockbackEntry>(InitialCapacity);
        private static readonly List<ScreenShakeEntry> _screenShakes = new List<ScreenShakeEntry>(InitialCapacity);
        private static readonly List<StateTransitionEntry> _stateTransitions = new List<StateTransitionEntry>(InitialCapacity);
        private static readonly List<FrameTimeEntry> _frameTimes = new List<FrameTimeEntry>(InitialCapacity);

        [Serializable]
        public sealed class PlaytestReport
        {
            public List<HitstopEntry> hitstops;
            public List<KnockbackEntry> knockbacks;
            public List<ScreenShakeEntry> screenShakes;
            public List<StateTransitionEntry> stateTransitions;
            public List<FrameTimeEntry> frameTimes;
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

            var entry = new StateTransitionEntry
            {
                componentId = componentId ?? string.Empty,
                oldState = oldState ?? string.Empty,
                newState = newState ?? string.Empty
            };

            _stateTransitions.Add(entry);
            StateTransitionLogged?.Invoke(entry);
        }

        public static void LogFrameTime(float deltaTime)
        {
            if (!IsRecording)
            {
                return;
            }

            _frameTimes.Add(new FrameTimeEntry { deltaTime = deltaTime });
        }

        public static void Reset()
        {
            _hitstops.Clear();
            _knockbacks.Clear();
            _screenShakes.Clear();
            _stateTransitions.Clear();
            _frameTimes.Clear();
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
                frameTimes = new List<FrameTimeEntry>(_frameTimes)
            };

            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json);
        }
    }
}
