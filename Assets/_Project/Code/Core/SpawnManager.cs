using System;
using UnityEngine;
using CorgiCommando.Data;
using CorgiCommando.Enemies;

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
        private static Sprite _placeholderSprite;
        private WaveData _waveData;

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

                    int spawnCount = Math.Max(0, spawnGroup.count);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = spawnGroup.spawnPosition + new Vector3(i * SpawnOffsetX, 0f, 0f);
                        EnemyAI spawnedEnemy = CreateEnemy(spawnGroup.enemyData, spawnPosition);
                        if (spawnedEnemy == null)
                        {
                            continue;
                        }

                        AliveEnemyCount++;
                        NotifyEnemySpawned(spawnedEnemy);
                    }
                }
            }

            OnWaveStarted?.Invoke(CurrentWaveIndex);

            if (AliveEnemyCount == 0)
            {
                ClearCurrentWave();
            }
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

            var texture = Texture2D.whiteTexture;
            _placeholderSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            return _placeholderSprite;
        }

        /// <summary>
        /// Called when an enemy dies. Decrements alive count, checks wave clear.
        /// </summary>
        public void OnEnemyDied()
        {
            HandleEnemyDiedCount();
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

            HandleEnemyDiedCount();
            OnEnemyDeath?.Invoke(enemy);
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
        }

        private void ResetWaveState()
        {
            AliveEnemyCount = 0;
            IsWaveCleared = false;
        }
    }
}
