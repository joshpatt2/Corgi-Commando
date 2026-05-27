using System;
using System.Collections.Generic;
using UnityEngine;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Testing;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Drives wave-based encounters from WaveData ScriptableObjects.
    /// Spawns runtime enemy GameObjects for each spawn group and tracks wave/encounter state.
    ///
    /// Contract: OnEncounterComplete fires exactly once, from the moment the final enemy of the
    /// final wave dies (via ClearCurrentWave). AdvanceToNextWave never completes the encounter;
    /// it only moves between cleared waves.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        private const float SpawnOffsetX = 1.5f;
        private static Texture2D _placeholderTexture;
        private static Sprite _placeholderSprite;
        private WaveData _waveData;
        private readonly List<PendingSpawnGroup> _pendingLowHpSpawnGroups = new List<PendingSpawnGroup>();
        private readonly List<EnemyAI> _currentWaveEnemies = new List<EnemyAI>();
        private readonly HashSet<EnemyAI> _aliveWaveEnemies = new HashSet<EnemyAI>();
        private readonly HashSet<EnemyAI> _lowHpTriggerCohort = new HashSet<EnemyAI>();

        private struct PendingSpawnGroup
        {
            public SpawnGroup Group;
            public int StartingHp;
        }

        /// <summary>Current wave index (0-based).</summary>
        public int CurrentWaveIndex { get; private set; }

        /// <summary>Total number of waves in the current encounter.</summary>
        public int TotalWaves { get; private set; }

        /// <summary>Number of alive enemies in the current wave.</summary>
        public int AliveEnemyCount { get; private set; }

        /// <summary>Whether the current wave is cleared (all enemies dead).</summary>
        public bool IsWaveCleared { get; private set; }

        /// <summary>Whether all waves in the encounter are complete.</summary>
        public bool IsEncounterComplete { get; private set; }

        /// <summary>Fired when a wave is cleared.</summary>
        public event Action<int> OnWaveCleared;

        /// <summary>Fired when the entire encounter is complete.</summary>
        public event Action OnEncounterComplete;

        /// <summary>Fired when a new wave begins spawning.</summary>
        public event Action<int> OnWaveStarted;

        /// <summary>Fired when an enemy instance is spawned.</summary>
        public event Action<EnemyAI> OnEnemySpawned;

        /// <summary>Fired when an enemy instance dies.</summary>
        public event Action<EnemyAI> OnEnemyDeath;

        /// <summary>
        /// Begins an encounter using the given wave data.
        /// </summary>
        public void StartEncounter(WaveData waveData)
        {
            if (waveData == null)
            {
                throw new ArgumentNullException(nameof(waveData));
            }

            _waveData = waveData;
            CurrentWaveIndex = 0;
            TotalWaves = _waveData.waves?.Length ?? 0;
            IsEncounterComplete = false;
            ResetWaveState();

            if (TotalWaves == 0)
            {
                CompleteEncounter();
            }
        }

        /// <summary>
        /// Spawns the current wave's enemies at their designated positions.
        /// </summary>
        public void SpawnCurrentWave()
        {
            if (_waveData == null || IsEncounterComplete || CurrentWaveIndex < 0 || CurrentWaveIndex >= TotalWaves)
            {
                return;
            }

            var wave = _waveData.waves[CurrentWaveIndex];
            ResetWaveState();

            if (wave?.spawnGroups != null)
            {
                foreach (var spawnGroup in wave.spawnGroups)
                {
                    if (spawnGroup == null || spawnGroup.enemyData == null)
                    {
                        continue;
                    }

                    if (spawnGroup.spawnTrigger == SpawnTrigger.OnLowHP)
                    {
                        _pendingLowHpSpawnGroups.Add(new PendingSpawnGroup
                        {
                            Group = spawnGroup,
                            StartingHp = 0
                        });
                        continue;
                    }

                    SpawnGroupEnemies(spawnGroup, includeInLowHpTriggerCohort: true);
                }
            }

            int startingCohortHp = CalculateLowHpTriggerCohortHp();
            for (int i = 0; i < _pendingLowHpSpawnGroups.Count; i++)
            {
                var pending = _pendingLowHpSpawnGroups[i];
                pending.StartingHp = startingCohortHp;
                _pendingLowHpSpawnGroups[i] = pending;
            }

            if (startingCohortHp <= 0 && _pendingLowHpSpawnGroups.Count > 0)
            {
                for (int i = 0; i < _pendingLowHpSpawnGroups.Count; i++)
                {
                    SpawnGroupEnemies(_pendingLowHpSpawnGroups[i].Group, includeInLowHpTriggerCohort: false);
                }
                _pendingLowHpSpawnGroups.Clear();
            }

            OnWaveStarted?.Invoke(CurrentWaveIndex);
            if (PlaytestMetrics.IsRecording)
            {
                PlaytestMetrics.LogPositionSnapshot(
                    $"wave-{CurrentWaveIndex + 1}-start",
                    PlaytestMetrics.ResolvePrimaryActorPosition(),
                    PlaytestMetrics.CaptureNamedPositions());
            }
            EvaluateLowHpSpawnGroups();

            if (AliveEnemyCount == 0)
            {
                ClearCurrentWave();
            }
        }

        private void SpawnGroupEnemies(SpawnGroup spawnGroup, bool includeInLowHpTriggerCohort)
        {
            int spawnCount = Math.Max(0, spawnGroup.count);
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPosition = spawnGroup.spawnPosition + new Vector3(i * SpawnOffsetX, 0f, 0f);
                EnemyAI spawnedEnemy = CreateEnemy(spawnGroup.enemyData, spawnPosition);
                if (spawnedEnemy == null)
                {
                    continue;
                }

                spawnedEnemy.OnDeath += HandleSpawnedEnemyDeath;
                _currentWaveEnemies.Add(spawnedEnemy);
                _aliveWaveEnemies.Add(spawnedEnemy);
                if (includeInLowHpTriggerCohort)
                {
                    _lowHpTriggerCohort.Add(spawnedEnemy);
                    var health = spawnedEnemy.GetEntityComponent<IHealthComponent>();
                    if (health != null)
                    {
                        health.OnDamaged += HandleCohortEnemyDamaged;
                    }
                }

                AliveEnemyCount++;
                NotifyEnemySpawned(spawnedEnemy);
            }
        }

        private void EvaluateLowHpSpawnGroups()
        {
            if (IsEncounterComplete || IsWaveCleared || _pendingLowHpSpawnGroups.Count == 0)
            {
                return;
            }

            int currentCohortHp = CalculateLowHpTriggerCohortHp();
            for (int i = _pendingLowHpSpawnGroups.Count - 1; i >= 0; i--)
            {
                var pending = _pendingLowHpSpawnGroups[i];
                if (pending.StartingHp <= 0)
                {
                    continue;
                }

                int thresholdHp = Mathf.CeilToInt(pending.StartingHp * Mathf.Clamp01(pending.Group.lowHpThresholdNormalized));
                bool shouldSpawn = currentCohortHp < thresholdHp;
                if (!shouldSpawn)
                {
                    continue;
                }

                SpawnGroupEnemies(pending.Group, includeInLowHpTriggerCohort: false);
                _pendingLowHpSpawnGroups.RemoveAt(i);
            }
        }

        private void HandleCohortEnemyDamaged(int _)
        {
            EvaluateLowHpSpawnGroups();
        }

        private int CalculateLowHpTriggerCohortHp()
        {
            int totalHp = 0;
            foreach (var enemy in _lowHpTriggerCohort)
            {
                if (enemy == null || !_aliveWaveEnemies.Contains(enemy))
                {
                    continue;
                }

                var health = enemy.GetEntityComponent<IHealthComponent>();
                if (health != null)
                {
                    totalHp += Math.Max(0, health.CurrentHP);
                }
            }

            return totalHp;
        }

        private static EnemyAI CreateEnemy(EnemyData enemyData, Vector3 spawnPosition)
        {
            string enemyName = string.IsNullOrWhiteSpace(enemyData.enemyName) ? "Enemy" : enemyData.enemyName;
            var enemyGameObject = new GameObject(enemyName);
            enemyGameObject.transform.position = spawnPosition;

            enemyGameObject.AddComponent<KinematicMovementController>();

            var spriteRenderer = enemyGameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetPlaceholderSprite();
            spriteRenderer.color = enemyData.placeholderColor;

            Type enemyType = ResolveEnemyType(enemyData.behaviorPreset);
            var enemy = enemyGameObject.AddComponent(enemyType) as EnemyAI;
            if (enemy == null)
            {
                Destroy(enemyGameObject);
                return null;
            }

            enemy.AddEntityComponent(new HurtboxComponent
            {
                Bounds = new Rect(-0.5f, -0.5f, 1f, 1f)
            });
            enemy.Initialize(enemyData);
            return enemy;
        }

        private static Type ResolveEnemyType(EnemyBehaviorPreset behaviorPreset)
        {
            return behaviorPreset switch
            {
                EnemyBehaviorPreset.FeralCat => typeof(FeralCatAI),
                EnemyBehaviorPreset.RaccoonBandit => typeof(RaccoonBanditAI),
                EnemyBehaviorPreset.SprinklerTurret => typeof(SprinklerTurretAI),
                _ => typeof(EnemyAI)
            };
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null)
            {
                return _placeholderSprite;
            }

            if (_placeholderTexture == null)
            {
                _placeholderTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _placeholderTexture.SetPixel(0, 0, Color.white);
                _placeholderTexture.Apply();
            }

            _placeholderSprite = Sprite.Create(
                _placeholderTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            return _placeholderSprite;
        }

        /// <summary>
        /// Called when an enemy dies. Decrements alive count, checks wave clear.
        /// </summary>
        public void OnEnemyDied()
        {
            HandleEnemyDiedCount();
            EvaluateLowHpSpawnGroups();
        }

        private void HandleEnemyDiedCount()
        {
            if (_waveData == null || IsEncounterComplete || IsWaveCleared || AliveEnemyCount <= 0)
            {
                return;
            }

            AliveEnemyCount--;
            if (AliveEnemyCount == 0)
            {
                ClearCurrentWave();
            }
        }

        /// <summary>
        /// Called by runtime spawn code when an enemy instance is created.
        /// </summary>
        public void NotifyEnemySpawned(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            OnEnemySpawned?.Invoke(enemy);
        }

        /// <summary>
        /// Called by runtime code when an enemy instance dies.
        /// </summary>
        public void NotifyEnemyDied(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!TryRegisterEnemyDeath(enemy))
            {
                return;
            }

            OnEnemyDeath?.Invoke(enemy);
            EvaluateLowHpSpawnGroups();
        }

        /// <summary>
        /// Advances to the next wave after the delay specified in WaveData.
        /// </summary>
        public void AdvanceToNextWave()
        {
            if (_waveData == null || IsEncounterComplete || !IsWaveCleared)
            {
                return;
            }

            CurrentWaveIndex++;
            ResetWaveState();
        }

        private void ClearCurrentWave()
        {
            IsWaveCleared = true;
            OnWaveCleared?.Invoke(CurrentWaveIndex);
            if (PlaytestMetrics.IsRecording)
            {
                PlaytestMetrics.LogPositionSnapshot(
                    $"wave-{CurrentWaveIndex + 1}-clear",
                    PlaytestMetrics.ResolvePrimaryActorPosition(),
                    PlaytestMetrics.CaptureNamedPositions());
            }

            if (CurrentWaveIndex >= TotalWaves - 1)
            {
                CompleteEncounter();
            }
        }

        private void CompleteEncounter()
        {
            if (IsEncounterComplete)
            {
                return;
            }

            IsEncounterComplete = true;
            OnEncounterComplete?.Invoke();
            if (PlaytestMetrics.IsRecording)
            {
                PlaytestMetrics.LogPositionSnapshot(
                    "victory",
                    PlaytestMetrics.ResolvePrimaryActorPosition(),
                    PlaytestMetrics.CaptureNamedPositions());
            }
        }

        private void ResetWaveState()
        {
            foreach (var enemy in _currentWaveEnemies)
            {
                if (enemy != null)
                {
                    enemy.OnDeath -= HandleSpawnedEnemyDeath;
                }
            }

            foreach (var enemy in _lowHpTriggerCohort)
            {
                if (enemy != null)
                {
                    var health = enemy.GetEntityComponent<IHealthComponent>();
                    if (health != null)
                    {
                        health.OnDamaged -= HandleCohortEnemyDamaged;
                    }
                }
            }

            _pendingLowHpSpawnGroups.Clear();
            _currentWaveEnemies.Clear();
            _aliveWaveEnemies.Clear();
            _lowHpTriggerCohort.Clear();
            AliveEnemyCount = 0;
            IsWaveCleared = false;
        }

        private void HandleSpawnedEnemyDeath(Entity entity)
        {
            var enemy = entity as EnemyAI;
            if (enemy == null || !TryRegisterEnemyDeath(enemy))
            {
                return;
            }

            OnEnemyDeath?.Invoke(enemy);
            EvaluateLowHpSpawnGroups();
        }

        private bool TryRegisterEnemyDeath(EnemyAI enemy)
        {
            if (!_aliveWaveEnemies.Remove(enemy))
            {
                return false;
            }

            if (_lowHpTriggerCohort.Remove(enemy))
            {
                var health = enemy.GetEntityComponent<IHealthComponent>();
                if (health != null)
                {
                    health.OnDamaged -= HandleCohortEnemyDamaged;
                }
            }

            // Keep legacy death-count behavior centralized here for both NotifyEnemyDied and OnDeath callbacks.
            HandleEnemyDiedCount();
            return true;
        }
    }
}
