using System;
using System.Collections;
using System.Reflection;
using CorgiCommando.Camera;
using CorgiCommando.Combat;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using CorgiCommando.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class LevelBackyardPlayModeTests
    {
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator Level_Backyard_ArenaTrigger_LocksCameraAndStartsWave1()
        {
            using var fixture = new LevelBackyardFixture();

            yield return null;

            fixture.Player.transform.position = new Vector3(4f, fixture.Player.transform.position.y, 0f);
            yield return null;

            Assert.IsTrue(fixture.ArenaCameraLock.IsActive);
            Assert.IsTrue(fixture.GroupTargetCamera.IsArenaLocked);
            Assert.Greater(fixture.SpawnManager.AliveEnemyCount, 0);
        }

        [UnityTest]
        public IEnumerator Level_Backyard_BossDoor_OnlyOpensAfterWave3Cleared()
        {
            using var fixture = new LevelBackyardFixture();

            yield return null;

            fixture.Player.transform.position = new Vector3(4f, fixture.Player.transform.position.y, 0f);
            yield return null;

            fixture.Player.transform.position = new Vector3(18f, fixture.Player.transform.position.y, 0f);
            yield return null;

            Assert.IsFalse(fixture.Director.IsBossDoorUnlocked);
            Assert.IsFalse(fixture.Director.IsBossSpawned);

            fixture.ClearAllWaves();
            yield return null;

            Assert.IsTrue(fixture.Director.IsBossDoorUnlocked);

            fixture.Player.transform.position = new Vector3(18f, fixture.Player.transform.position.y, 0f);
            yield return null;

            Assert.IsTrue(fixture.Director.IsBossSpawned);
        }

        [UnityTest]
        public IEnumerator Level_Backyard_PlayThrough_SpawnToBossIntro()
        {
            using var fixture = new LevelBackyardFixture();

            yield return null;

            fixture.Player.transform.position = new Vector3(4f, fixture.Player.transform.position.y, 0f);
            yield return null;

            Assert.IsTrue(fixture.Director.IsArenaTriggered);
            Assert.Greater(fixture.SpawnManager.AliveEnemyCount, 0);

            fixture.ClearAllWaves();
            yield return null;

            Assert.IsTrue(fixture.Director.IsBossDoorUnlocked);

            fixture.Player.transform.position = new Vector3(18f, fixture.Player.transform.position.y, 0f);
            yield return null;

            Assert.IsTrue(fixture.Director.IsBossSpawned);
            Assert.IsNotNull(UnityEngine.Object.FindObjectOfType<WhiskerbotController>());

            var banner = UnityEngine.Object.FindObjectOfType<BossBannerUI>(true);
            Assert.NotNull(banner);
            Assert.IsTrue(banner.IsVisible);
        }

        private sealed class LevelBackyardFixture : IDisposable
        {
            public SceneBootstrap SceneBootstrap { get; }
            public SpawnManager SpawnManager { get; }
            public GroupTargetCamera GroupTargetCamera { get; }
            public ArenaCameraLock ArenaCameraLock { get; }
            public LevelBackyardDirector Director { get; }
            public CorgiController Player { get; }

            private readonly EnemyData _enemyData;
            private readonly WaveData _waveData;
            private readonly AttackData _attackData;
            private readonly CorgiData _corgiData;
            private readonly EnemyData _bossData;
            private readonly AttackData _weaponSwing;
            private readonly AttackData _weaponThrow;
            private readonly GameObject _root;
            private readonly GameObject _hudRoot;
            private readonly GameObject _playerGo;
            private readonly GameObject _dummyEnemyGo;
            private readonly EnemyAI _dummyEnemy;
            private readonly GameObject[] _weaponPrefabs;

            public LevelBackyardFixture()
            {
                _root = new GameObject("SceneBootstrap");
                SceneBootstrap = _root.AddComponent<SceneBootstrap>();
                SpawnManager = _root.AddComponent<SpawnManager>();
                GroupTargetCamera = _root.AddComponent<GroupTargetCamera>();
                ArenaCameraLock = _root.AddComponent<ArenaCameraLock>();
                Director = _root.AddComponent<LevelBackyardDirector>();

                _hudRoot = new GameObject("HUDRoot");
                var hud = _hudRoot.AddComponent<HUDController>();

                _weaponSwing = ScriptableObject.CreateInstance<AttackData>();
                _weaponSwing.attackName = "Weapon Swing";
                _weaponThrow = ScriptableObject.CreateInstance<AttackData>();
                _weaponThrow.attackName = "Weapon Throw";

                _weaponPrefabs = new GameObject[5];
                for (int i = 0; i < _weaponPrefabs.Length; i++)
                {
                    var weaponPrefab = new GameObject($"WeaponPrefab_{i}");
                    weaponPrefab.AddComponent<SpriteRenderer>();
                    weaponPrefab.AddComponent<SphereCollider>().isTrigger = true;
                    weaponPrefab.AddComponent<EnvironmentalWeaponEntity>();
                    var setup = weaponPrefab.AddComponent<EnvironmentalWeaponPrefabSetup>();
                    SetPrivateField(setup, "_swingAttackData", _weaponSwing);
                    SetPrivateField(setup, "_throwAttackData", _weaponThrow);
                    _weaponPrefabs[i] = weaponPrefab;
                }

                _attackData = ScriptableObject.CreateInstance<AttackData>();
                _attackData.attackName = "Punch";

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
                Player = _playerGo.AddComponent<CorgiController>();
                Player.Initialize(_corgiData, new InputBuffer(), 0);

                _enemyData = ScriptableObject.CreateInstance<EnemyData>();
                _enemyData.enemyName = "FeralCat";
                _enemyData.maxHP = 20;
                _enemyData.behaviorPreset = EnemyBehaviorPreset.FeralCat;

                _waveData = ScriptableObject.CreateInstance<WaveData>();
                _waveData.waves = new[]
                {
                    new WaveEntry { spawnGroups = new[] { new SpawnGroup { enemyData = _enemyData, count = 1, spawnPosition = new Vector3(6f, 0f, 0f) } } },
                    new WaveEntry { spawnGroups = new[] { new SpawnGroup { enemyData = _enemyData, count = 1, spawnPosition = new Vector3(8f, 0f, 0f) } } },
                    new WaveEntry { spawnGroups = new[] { new SpawnGroup { enemyData = _enemyData, count = 1, spawnPosition = new Vector3(10f, 0f, 0f) } } }
                };

                _bossData = ScriptableObject.CreateInstance<EnemyData>();
                _bossData.enemyName = "Whiskerbot";
                _bossData.maxHP = 200;
                _bossData.placeholderColor = Color.red;

                var playerSpawn = CreateMarker("PlayerSpawnPoint", new Vector3(-2f, -4.26f, 0f));
                var arenaTrigger = CreateMarker("ArenaTriggerPoint", new Vector3(1.5f, -4.2f, 0f));
                var bossTrigger = CreateMarker("BossDoorTriggerPoint", new Vector3(14f, -4.2f, 0f));
                var bossSpawn = CreateMarker("BossSpawnPoint", new Vector3(17f, -4.2f, 0f));

                var weaponSpawns = new[]
                {
                    CreateMarker("WeaponSpawnPoint1", new Vector3(9.5f, -4.2f, 0f)),
                    CreateMarker("WeaponSpawnPoint2", new Vector3(10.7f, -4.2f, 0f)),
                    CreateMarker("WeaponSpawnPoint3", new Vector3(11.9f, -4.2f, 0f)),
                    CreateMarker("WeaponSpawnPoint4", new Vector3(13.1f, -4.2f, 0f)),
                    CreateMarker("WeaponSpawnPoint5", new Vector3(14.3f, -4.2f, 0f))
                };

                SetPrivateField(SceneBootstrap, "_waveData", _waveData);
                SetPrivateField(SceneBootstrap, "_playerOne", Player);
                SetPrivateField(SceneBootstrap, "_spawnManager", SpawnManager);
                SetPrivateField(SceneBootstrap, "_arenaCameraLock", ArenaCameraLock);
                SetPrivateField(SceneBootstrap, "_groupTargetCamera", GroupTargetCamera);
                SetPrivateField(SceneBootstrap, "_autoStartEncounter", false);

                SetPrivateField(Director, "_sceneBootstrap", SceneBootstrap);
                SetPrivateField(Director, "_spawnManager", SpawnManager);
                SetPrivateField(Director, "_arenaCameraLock", ArenaCameraLock);
                SetPrivateField(Director, "_groupTargetCamera", GroupTargetCamera);
                SetPrivateField(Director, "_hudController", hud);
                SetPrivateField(Director, "_playerSpawnPoint", playerSpawn);
                SetPrivateField(Director, "_arenaTriggerPoint", arenaTrigger);
                SetPrivateField(Director, "_bossDoorTriggerPoint", bossTrigger);
                SetPrivateField(Director, "_bossSpawnPoint", bossSpawn);
                SetPrivateField(Director, "_whiskerbotData", _bossData);
                SetPrivateField(Director, "_waveThreeWeaponSpawnPoints", weaponSpawns);
                SetPrivateField(Director, "_environmentalWeaponPrefabs", _weaponPrefabs);

                _dummyEnemyGo = new GameObject("DummyEnemy");
                _dummyEnemy = _dummyEnemyGo.AddComponent<EnemyAI>();
                _dummyEnemy.Initialize(_enemyData);
            }

            public void ClearAllWaves()
            {
                int guard = 10;
                while (!Director.IsBossDoorUnlocked && guard-- > 0)
                {
                    int alive = SpawnManager.AliveEnemyCount;
                    for (int i = 0; i < alive; i++)
                    {
                        SpawnManager.NotifyEnemyDied(_dummyEnemy);
                    }
                }
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_root);
                UnityEngine.Object.DestroyImmediate(_hudRoot);
                UnityEngine.Object.DestroyImmediate(_playerGo);
                UnityEngine.Object.DestroyImmediate(_dummyEnemyGo);

                for (int i = 0; i < _weaponPrefabs.Length; i++)
                {
                    if (_weaponPrefabs[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_weaponPrefabs[i]);
                    }
                }

                foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAI>())
                {
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                }

                foreach (var weapon in UnityEngine.Object.FindObjectsOfType<EnvironmentalWeaponEntity>())
                {
                    if (weapon != null)
                    {
                        UnityEngine.Object.DestroyImmediate(weapon.gameObject);
                    }
                }

                foreach (var boss in UnityEngine.Object.FindObjectsOfType<WhiskerbotController>())
                {
                    UnityEngine.Object.DestroyImmediate(boss.gameObject);
                }

                UnityEngine.Object.DestroyImmediate(_enemyData);
                UnityEngine.Object.DestroyImmediate(_bossData);
                UnityEngine.Object.DestroyImmediate(_waveData);
                UnityEngine.Object.DestroyImmediate(_attackData);
                UnityEngine.Object.DestroyImmediate(_weaponSwing);
                UnityEngine.Object.DestroyImmediate(_weaponThrow);
                UnityEngine.Object.DestroyImmediate(_corgiData);
            }

            private Transform CreateMarker(string name, Vector3 position)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_root.transform, false);
                go.transform.position = position;
                return go.transform;
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
