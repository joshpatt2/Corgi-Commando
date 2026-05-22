using System;
using System.Collections.Generic;
using System.Reflection;
using CorgiCommando.Camera;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using NUnit.Framework;
using UnityEngine;

namespace CorgiCommando.Tests.EditMode
{
    [TestFixture]
    public class SceneBootstrapTests
    {
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void SceneBootstrap_OnStart_InitializesRunStateAndEncounter()
        {
            using var fixture = new SceneBootstrapFixture();

            InvokePrivate(fixture.Bootstrap, "Start");

            Assert.NotNull(fixture.Bootstrap.RunState);
            Assert.AreEqual(3, fixture.Bootstrap.RunState.LivesRemaining);
            Assert.AreEqual(1, fixture.Bootstrap.RunState.ActivePlayerCount);
            Assert.AreEqual(0, fixture.SpawnManager.CurrentWaveIndex);
            Assert.AreEqual(2, fixture.SpawnManager.AliveEnemyCount);
        }

        [Test]
        public void SceneBootstrap_OnEnemySpawned_RegistersEnemyForTicking()
        {
            using var fixture = new SceneBootstrapFixture();
            InvokePrivate(fixture.Bootstrap, "Start");
            int initialCount = fixture.Bootstrap.ActiveEnemyCount;

            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<EnemyAI>();
            enemy.Initialize(fixture.EnemyData);

            fixture.SpawnManager.NotifyEnemySpawned(enemy);

            Assert.AreEqual(initialCount + 1, fixture.Bootstrap.ActiveEnemyCount);
            UnityEngine.Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void SceneBootstrap_OnEnemyDied_UnregistersEnemyFromTicking()
        {
            using var fixture = new SceneBootstrapFixture();
            InvokePrivate(fixture.Bootstrap, "Start");
            int initialCount = fixture.Bootstrap.ActiveEnemyCount;

            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<EnemyAI>();
            enemy.Initialize(fixture.EnemyData);

            fixture.SpawnManager.NotifyEnemySpawned(enemy);
            fixture.SpawnManager.NotifyEnemyDied(enemy);

            Assert.AreEqual(initialCount, fixture.Bootstrap.ActiveEnemyCount);
            UnityEngine.Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void SceneBootstrap_TickOrder_InputBeforeControllersBeforeCamera()
        {
            using var fixture = new SceneBootstrapFixture();
            InvokePrivate(fixture.Bootstrap, "Start");

            var tickStages = new List<SceneTickStage>();
            fixture.Bootstrap.OnTickStageExecuted += stage => tickStages.Add(stage);

            InvokePrivate(fixture.Bootstrap, "Update");
            InvokePrivate(fixture.Bootstrap, "LateUpdate");

            int inputIndex = tickStages.IndexOf(SceneTickStage.InputGather);
            int combatIndex = tickStages.IndexOf(SceneTickStage.Combat);
            int playerIndex = tickStages.IndexOf(SceneTickStage.PlayerControllers);
            int enemyIndex = tickStages.IndexOf(SceneTickStage.EnemyControllers);
            int cameraIndex = tickStages.IndexOf(SceneTickStage.Camera);

            Assert.GreaterOrEqual(inputIndex, 0);
            Assert.Greater(combatIndex, inputIndex);
            Assert.Greater(playerIndex, combatIndex);
            Assert.Greater(enemyIndex, playerIndex);
            Assert.Greater(cameraIndex, enemyIndex);
        }

        private static void InvokePrivate(MonoBehaviour component, string methodName)
        {
            var method = component.GetType().GetMethod(methodName, InstancePrivate);
            Assert.NotNull(method, $"Expected method '{methodName}' to exist.");
            method.Invoke(component, null);
        }

        private sealed class SceneBootstrapFixture : IDisposable
        {
            public GameObject Root { get; }
            public SceneBootstrap Bootstrap { get; }
            public SpawnManager SpawnManager { get; }
            public EnemyData EnemyData { get; }

            private readonly CorgiData _corgiData;
            private readonly WaveData _waveData;
            private readonly AttackData _attackData;
            private readonly GameObject _playerGo;

            public SceneBootstrapFixture()
            {
                Root = new GameObject("SceneBootstrap");
                Bootstrap = Root.AddComponent<SceneBootstrap>();
                SpawnManager = Root.AddComponent<SpawnManager>();
                Root.AddComponent<GroupTargetCamera>();
                Root.AddComponent<ArenaCameraLock>();

                EnemyData = ScriptableObject.CreateInstance<EnemyData>();
                EnemyData.enemyName = "TestEnemy";
                EnemyData.maxHP = 10;

                _waveData = ScriptableObject.CreateInstance<WaveData>();
                _waveData.waves = new[]
                {
                    new WaveEntry
                    {
                        delayBeforeSpawn = 0f,
                        spawnGroups = new[] { new SpawnGroup { enemyData = EnemyData, count = 2, spawnPosition = Vector3.zero } }
                    }
                };

                _attackData = ScriptableObject.CreateInstance<AttackData>();
                _attackData.attackName = "TestAttack";

                _corgiData = ScriptableObject.CreateInstance<CorgiData>();
                _corgiData.corgiName = "Sarge";
                _corgiData.maxHP = 100;
                _corgiData.walkSpeed = 5f;
                _corgiData.depthSpeed = 3f;
                _corgiData.jumpForce = 10f;
                _corgiData.comboChain = new[] { _attackData };
                _corgiData.maxSpecialMeter = 100f;
                _corgiData.specialCost = 100f;

                _playerGo = new GameObject("P1");
                _playerGo.AddComponent<KinematicMovementController>();
                var inputBuffer = new InputBuffer();
                var player = _playerGo.AddComponent<CorgiController>();
                player.Initialize(_corgiData, inputBuffer, 0);

                SetPrivateField(Bootstrap, "_waveData", _waveData);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(_playerGo);
                UnityEngine.Object.DestroyImmediate(EnemyData);
                UnityEngine.Object.DestroyImmediate(_attackData);
                UnityEngine.Object.DestroyImmediate(_corgiData);
                UnityEngine.Object.DestroyImmediate(_waveData);
            }

            private static void SetPrivateField<T>(object instance, string fieldName, T value)
            {
                var field = instance.GetType().GetField(fieldName, InstancePrivate);
                Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
                field.SetValue(instance, value);
            }
        }
    }
}
