using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Enemies;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for SpawnManager wave management logic and enemy spawn wiring.
    /// </summary>
    [TestFixture]
    public class SpawnManagerTests
    {
        private GameObject _managerGo;
        private SpawnManager _manager;
        private WaveData _waveData;
        private EnemyData _catData;
        private EnemyData _raccoonData;
        private EnemyData _sprinklerData;

        [SetUp]
        public void SetUp()
        {
            _managerGo = new GameObject("SpawnManager");
            _manager = _managerGo.AddComponent<SpawnManager>();

            _catData = ScriptableObject.CreateInstance<EnemyData>();
            _catData.enemyName = "FeralCat";
            _catData.behaviorPreset = EnemyBehaviorPreset.FeralCat;
            _catData.maxHP = 30;

            _raccoonData = ScriptableObject.CreateInstance<EnemyData>();
            _raccoonData.enemyName = "Raccoon";
            _raccoonData.behaviorPreset = EnemyBehaviorPreset.RaccoonBandit;

            _sprinklerData = ScriptableObject.CreateInstance<EnemyData>();
            _sprinklerData.enemyName = "Sprinkler";
            _sprinklerData.behaviorPreset = EnemyBehaviorPreset.SprinklerTurret;

            _waveData = ScriptableObject.CreateInstance<WaveData>();
            _waveData.waves = new[]
            {
                new WaveEntry
                {
                    delayBeforeSpawn = 0f,
                    spawnGroups = new[]
                    {
                        new SpawnGroup { enemyData = _catData, count = 3, spawnPosition = Vector3.zero }
                    }
                },
                new WaveEntry
                {
                    delayBeforeSpawn = 1.0f,
                    spawnGroups = new[]
                    {
                        new SpawnGroup { enemyData = _catData, count = 2, spawnPosition = new Vector3(5f, 0f, 0f) }
                    }
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemies[i].gameObject);
                }
            }

            UnityEngine.Object.DestroyImmediate(_managerGo);
            UnityEngine.Object.DestroyImmediate(_catData);
            UnityEngine.Object.DestroyImmediate(_raccoonData);
            UnityEngine.Object.DestroyImmediate(_sprinklerData);
            UnityEngine.Object.DestroyImmediate(_waveData);
        }

        [Test]
        public void StartEncounter_InitializesWaveState()
        {
            // Act
            _manager.StartEncounter(_waveData);

            // Assert
            Assert.AreEqual(0, _manager.CurrentWaveIndex);
            Assert.AreEqual(2, _manager.TotalWaves);
            Assert.IsFalse(_manager.IsEncounterComplete);
        }

        [Test]
        public void SpawnCurrentWave_SetsAliveEnemyCount()
        {
            // Arrange
            _manager.StartEncounter(_waveData);

            // Act
            _manager.SpawnCurrentWave();

            // Assert — wave 0 has 3 cats
            Assert.AreEqual(3, _manager.AliveEnemyCount);
            Assert.IsFalse(_manager.IsWaveCleared);
        }

        [Test]
        public void SpawnManager_SpawnCurrentWave_FiresOnEnemySpawned()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            int spawnEventCount = 0;
            _manager.OnEnemySpawned += _ => spawnEventCount++;

            // Act
            _manager.SpawnCurrentWave();

            // Assert
            Assert.AreEqual(3, spawnEventCount);
        }

        [TestCase(EnemyBehaviorPreset.FeralCat, typeof(FeralCatAI))]
        [TestCase(EnemyBehaviorPreset.RaccoonBandit, typeof(RaccoonBanditAI))]
        [TestCase(EnemyBehaviorPreset.SprinklerTurret, typeof(SprinklerTurretAI))]
        [TestCase(EnemyBehaviorPreset.Roomba, typeof(EnemyAI))]
        [TestCase(EnemyBehaviorPreset.Boss, typeof(EnemyAI))]
        public void SpawnManager_SpawnCurrentWave_AttachesCorrectAIType(EnemyBehaviorPreset behaviorPreset, Type expectedType)
        {
            // Arrange
            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.enemyName = "TypedEnemy";
            enemyData.behaviorPreset = behaviorPreset;

            var singleWaveData = ScriptableObject.CreateInstance<WaveData>();
            singleWaveData.waves = new[]
            {
                new WaveEntry
                {
                    spawnGroups = new[]
                    {
                        new SpawnGroup { enemyData = enemyData, count = 1, spawnPosition = Vector3.zero }
                    }
                }
            };

            _manager.StartEncounter(singleWaveData);

            // Act
            _manager.SpawnCurrentWave();

            // Assert
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            Assert.AreEqual(1, enemies.Length);
            Assert.IsInstanceOf(expectedType, enemies[0]);

            UnityEngine.Object.DestroyImmediate(singleWaveData);
            UnityEngine.Object.DestroyImmediate(enemyData);
        }

        [Test]
        public void SpawnManager_SpawnCurrentWave_AttachesMovementAndHurtbox()
        {
            // Arrange
            _manager.StartEncounter(_waveData);

            // Act
            _manager.SpawnCurrentWave();

            // Assert
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            Assert.AreEqual(3, enemies.Length);

            foreach (var enemy in enemies)
            {
                Assert.NotNull(enemy.GetComponent<KinematicMovementController>());
                var spriteRenderer = enemy.GetComponent<SpriteRenderer>();
                Assert.NotNull(spriteRenderer);
                Assert.NotNull(enemy.GetEntityComponent<HurtboxComponent>());
                Assert.AreEqual(_catData.placeholderColor, spriteRenderer.color);
            }
        }

        [Test]
        public void OnEnemyDied_AllDead_FiresWaveClearedEvent()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            _manager.SpawnCurrentWave();
            int clearedWave = -1;
            _manager.OnWaveCleared += (index) => clearedWave = index;

            // Act — kill all 3 enemies
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();

            // Assert
            Assert.AreEqual(0, clearedWave);
            Assert.IsTrue(_manager.IsWaveCleared);
            Assert.AreEqual(0, _manager.AliveEnemyCount);
        }

        [Test]
        public void OnEnemyDied_NotAllDead_DoesNotClearWave()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            _manager.SpawnCurrentWave();

            // Act — kill only 1 of 3
            _manager.OnEnemyDied();

            // Assert
            Assert.IsFalse(_manager.IsWaveCleared);
            Assert.AreEqual(2, _manager.AliveEnemyCount);
        }

        [Test]
        public void AdvanceToNextWave_IncrementsWaveIndex()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            _manager.SpawnCurrentWave();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();

            // Act
            _manager.AdvanceToNextWave();

            // Assert
            Assert.AreEqual(1, _manager.CurrentWaveIndex);
        }

        [Test]
        public void StartEncounter_EmptyWaves_FiresOnEncounterComplete()
        {
            // Arrange
            var emptyWaveData = ScriptableObject.CreateInstance<WaveData>();
            emptyWaveData.waves = new WaveEntry[0];
            bool encounterDone = false;
            _manager.OnEncounterComplete += () => encounterDone = true;

            // Act
            _manager.StartEncounter(emptyWaveData);

            // Assert — empty encounter should still surface as complete via the event,
            // not silently flip the flag (subscribers waiting on the event would hang otherwise).
            Assert.IsTrue(_manager.IsEncounterComplete);
            Assert.IsTrue(encounterDone);

            UnityEngine.Object.DestroyImmediate(emptyWaveData);
        }

        [Test]
        public void AdvanceToNextWave_WaveNotCleared_IsNoOp()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            _manager.SpawnCurrentWave();

            // Act — advance without clearing
            _manager.AdvanceToNextWave();

            // Assert — index unchanged, still on wave 0
            Assert.AreEqual(0, _manager.CurrentWaveIndex);
            Assert.IsFalse(_manager.IsWaveCleared);
        }

        [Test]
        public void OnEncounterComplete_FiresExactlyOnce_FromFinalEnemyDeath()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            int completeCount = 0;
            _manager.OnEncounterComplete += () => completeCount++;

            // Act — fully clear both waves, then redundantly call AdvanceToNextWave
            _manager.SpawnCurrentWave();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();
            _manager.AdvanceToNextWave();
            _manager.SpawnCurrentWave();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();           // last enemy → encounter complete
            _manager.AdvanceToNextWave();     // should be no-op, not a second completion

            // Assert — single completion event, regardless of redundant advance calls.
            Assert.AreEqual(1, completeCount);
        }

        [Test]
        public void AllWavesCleared_FiresEncounterCompleteEvent()
        {
            // Arrange
            _manager.StartEncounter(_waveData);
            bool encounterDone = false;
            _manager.OnEncounterComplete += () => encounterDone = true;

            // Act — clear wave 0
            _manager.SpawnCurrentWave();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();
            _manager.AdvanceToNextWave();

            // Clear wave 1
            _manager.SpawnCurrentWave();
            _manager.OnEnemyDied();
            _manager.OnEnemyDied();

            // Assert
            Assert.IsTrue(_manager.IsEncounterComplete);
            Assert.IsTrue(encounterDone);
        }
    }
}
