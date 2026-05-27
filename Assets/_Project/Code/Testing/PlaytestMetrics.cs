using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
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
    ///   "frameTimes":[{"deltaTime":0.0166667}],
    ///   "positionSnapshots":[{"label":"boss-phase-1-to-2","actorPosition":{"x":0,"y":0,"z":0},"namedPositions":{"player-1":{"x":0,"y":0,"z":0},"boss":{"x":0,"y":0,"z":0}},"frame":120}]
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
        private static readonly List<PositionSnapshotEntry> _positionSnapshots = new List<PositionSnapshotEntry>(InitialCapacity);
        private static readonly Dictionary<Type, string> _enemyTypeNameCache = new Dictionary<Type, string>();

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

        [Serializable]
        public struct NamedPositionEntry
        {
            public string key;
            public Vector3 position;
        }

        [Serializable]
        public struct PositionSnapshotEntry
        {
            public string label;
            public Vector3 actorPosition;
            public List<NamedPositionEntry> namedPositions;
            public int frame;
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

        public static void LogPositionSnapshot(string label, Vector3 actorPosition, Dictionary<string, Vector3> namedPositions)
        {
            if (!IsRecording)
            {
                return;
            }

            var entries = new List<NamedPositionEntry>(namedPositions?.Count ?? 0);
            if (namedPositions != null)
            {
                foreach (var pair in namedPositions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    entries.Add(new NamedPositionEntry
                    {
                        key = pair.Key ?? string.Empty,
                        position = pair.Value
                    });
                }
            }

            _positionSnapshots.Add(new PositionSnapshotEntry
            {
                label = label ?? string.Empty,
                actorPosition = actorPosition,
                namedPositions = entries,
                frame = Time.frameCount
            });
        }

        public static Dictionary<string, Vector3> CaptureNamedPositions()
        {
            var namedPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);

            CorgiController[] players = UnityEngine.Object.FindObjectsOfType<CorgiController>();
            Array.Sort(players, (left, right) =>
            {
                if (left == null && right == null)
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                return left.PlayerIndex.CompareTo(right.PlayerIndex);
            });

            for (int i = 0; i < players.Length; i++)
            {
                CorgiController player = players[i];
                if (player == null)
                {
                    continue;
                }

                string key = $"player-{player.PlayerIndex + 1}";
                namedPositions[key] = player.transform.position;
            }

            EnemyAI[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            Array.Sort(enemies, (left, right) =>
            {
                if (left == null && right == null)
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                int byName = string.CompareOrdinal(left.GetType().Name, right.GetType().Name);
                if (byName != 0)
                {
                    return byName;
                }

                return string.CompareOrdinal(left.name, right.name);
            });

            var enemyTypeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAI enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Type enemyClrType = enemy.GetType();
                if (!_enemyTypeNameCache.TryGetValue(enemyClrType, out string enemyType))
                {
                    enemyType = enemyClrType.Name.ToLowerInvariant();
                    _enemyTypeNameCache[enemyClrType] = enemyType;
                }

                enemyTypeCounts.TryGetValue(enemyType, out int typeCount);
                int nextCount = typeCount + 1;
                enemyTypeCounts[enemyType] = nextCount;
                namedPositions[$"enemy-{nextCount}-{enemyType}"] = enemy.transform.position;
            }

            WhiskerbotController boss = UnityEngine.Object.FindObjectOfType<WhiskerbotController>();
            if (boss != null)
            {
                namedPositions["boss"] = boss.transform.position;
            }

            return namedPositions;
        }

        public static Vector3 ResolvePrimaryActorPosition()
        {
            CorgiController[] players = UnityEngine.Object.FindObjectsOfType<CorgiController>();
            for (int i = 0; i < players.Length; i++)
            {
                CorgiController player = players[i];
                if (player != null && player.PlayerIndex == 0)
                {
                    return player.transform.position;
                }
            }

            for (int i = 0; i < players.Length; i++)
            {
                CorgiController player = players[i];
                if (player != null)
                {
                    return player.transform.position;
                }
            }

            WhiskerbotController boss = UnityEngine.Object.FindObjectOfType<WhiskerbotController>();
            if (boss != null)
            {
                return boss.transform.position;
            }

            return Vector3.zero;
        }

        public static Vector3 ResolvePrimaryActorPosition(Dictionary<string, Vector3> namedPositions)
        {
            if (namedPositions != null)
            {
                if (namedPositions.TryGetValue("player-1", out Vector3 playerOnePosition))
                {
                    return playerOnePosition;
                }

                foreach (var pair in namedPositions)
                {
                    if (pair.Key.StartsWith("player-", StringComparison.Ordinal))
                    {
                        return pair.Value;
                    }
                }

                if (namedPositions.TryGetValue("boss", out Vector3 bossPosition))
                {
                    return bossPosition;
                }
            }

            return ResolvePrimaryActorPosition();
        }

        public static void Reset()
        {
            _hitstops.Clear();
            _knockbacks.Clear();
            _screenShakes.Clear();
            _stateTransitions.Clear();
            _frameTimes.Clear();
            _positionSnapshots.Clear();
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

            string json = BuildReportJson(report);
            File.WriteAllText(path, json);
        }

        private static string BuildReportJson(PlaytestReport report)
        {
            string baseJson = JsonUtility.ToJson(report, true);
            int insertIndex = baseJson.LastIndexOf('}');
            if (insertIndex < 0)
            {
                return baseJson;
            }

            var builder = new StringBuilder(baseJson.Length + 512);
            builder.Append(baseJson, 0, insertIndex);
            builder.AppendLine(",");
            builder.AppendLine("  \"positionSnapshots\": [");
            for (int i = 0; i < _positionSnapshots.Count; i++)
            {
                PositionSnapshotEntry snapshot = _positionSnapshots[i];
                builder.AppendLine("    {");
                builder.Append("      \"label\": \"").Append(EscapeJson(snapshot.label)).AppendLine("\",");
                builder.Append("      \"actorPosition\": ").Append(FormatVector3(snapshot.actorPosition)).AppendLine(",");
                builder.AppendLine("      \"namedPositions\": {");

                List<NamedPositionEntry> namedEntries = snapshot.namedPositions;
                for (int j = 0; namedEntries != null && j < namedEntries.Count; j++)
                {
                    NamedPositionEntry entry = namedEntries[j];
                    builder
                        .Append("        \"")
                        .Append(EscapeJson(entry.key))
                        .Append("\": ")
                        .Append(FormatVector3(entry.position));
                    builder.AppendLine(j < namedEntries.Count - 1 ? "," : string.Empty);
                }

                builder.AppendLine("      },");
                builder.Append("      \"frame\": ").Append(snapshot.frame).AppendLine();
                builder.Append("    }");
                builder.AppendLine(i < _positionSnapshots.Count - 1 ? "," : string.Empty);
            }
            builder.AppendLine("  ]");
            builder.Append('}');
            return builder.ToString();
        }

        private static string FormatVector3(Vector3 vector)
        {
            return string.Concat(
                "{\"x\":",
                vector.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ",\"y\":",
                vector.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ",\"z\":",
                vector.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                "}");
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
