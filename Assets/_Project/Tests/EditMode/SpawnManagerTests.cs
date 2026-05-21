using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for SpawnManager wave management logic.
    /// Edit Mode tests for pure state tracking; spawn instantiation is Play Mode.
    /// </summary>
    [TestFixture]
    public class SpawnManagerTests
    {
        private GameObject _managerGo;
        private SpawnManager _manager;
        private WaveData _waveData;
        private EnemyData _catData;

        [SetUp]
        public void SetUp()
        {
            _managerGo = new GameObject("SpawnManager");
            _manager = _managerGo.AddComponent<SpawnManager>();

            _catData = ScriptableObject.CreateInstance<EnemyData>();
            _catData.enemyName = "FeralCat";
            _catData.maxHP = 30;

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
            UnityEngine.Object.DestroyImmediate(_managerGo);
            UnityEngine.Object.DestroyImmediate(_catData);
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
