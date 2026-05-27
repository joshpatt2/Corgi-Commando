using System.Collections;
using System.Reflection;
using CorgiCommando.Camera;
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
    public class BossCheckpointPlayModeTests
    {
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int DamageToKeepBossInPhase2 = 80;

        [UnityTest]
        public IEnumerator RunState_PartyWipeAtBoss_ResetsBossToPhase1()
        {
            using var fixture = new BossCheckpointFixture();
            fixture.Bootstrap.OnBossDoorTriggered();

            fixture.Boss.CheckPhaseTransition(150, 200);
            fixture.Boss.GetEntityComponent<IHealthComponent>().TakeDamage(DamageToKeepBossInPhase2);
            Assert.AreEqual(2, fixture.Boss.CurrentPhase);

            fixture.KillPlayers();
            yield return null;
            yield return null;

            Assert.AreEqual(1, fixture.Boss.CurrentPhase);
            Assert.AreEqual(
                fixture.Boss.GetEntityComponent<IHealthComponent>().MaxHP,
                fixture.Boss.GetEntityComponent<IHealthComponent>().CurrentHP);
        }

        [UnityTest]
        public IEnumerator RunState_PartyWipeAtBoss_ResetsPlayersToFullHP()
        {
            using var fixture = new BossCheckpointFixture();
            fixture.Bootstrap.OnBossDoorTriggered();

            var playerHealth = fixture.Player.GetEntityComponent<IHealthComponent>();
            playerHealth.TakeDamage(playerHealth.MaxHP);

            yield return null;
            yield return null;

            Assert.IsTrue(fixture.Player.IsAlive);
            Assert.AreEqual(playerHealth.MaxHP, playerHealth.CurrentHP);
            Assert.That(Vector3.Distance(fixture.Player.transform.position, fixture.BossIntroSpawn.position), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator RunState_PartyWipePreBoss_NoBossCheckpoint_TriggersFullReload()
        {
            using var fixture = new BossCheckpointFixture();
            fixture.Bootstrap.SetReloadEnabledForTesting(false);

            string requestedScene = null;
            fixture.Bootstrap.OnSceneReloadRequested += sceneName => requestedScene = sceneName;

            fixture.KillPlayers();
            yield return null;

            Assert.IsTrue(fixture.Bootstrap.IsAwaitingGameOverStartPress);

            fixture.Bootstrap.ConfirmGameOverReload();
            yield return null;

            Assert.AreEqual("Level_Backyard", requestedScene);
        }

        [UnityTest]
        public IEnumerator RunState_BossBanner_RePlaysOnRetry()
        {
            using var fixture = new BossCheckpointFixture();
            fixture.Bootstrap.OnBossDoorTriggered();

            fixture.BossBanner.Hide();
            Assert.IsFalse(fixture.BossBanner.IsVisible);

            fixture.KillPlayers();
            yield return null;
            yield return null;

            Assert.IsTrue(fixture.BossBanner.IsVisible);
            Assert.AreEqual("WHISKERBOT-9000", fixture.BossBanner.BossName);
        }

        private sealed class BossCheckpointFixture : System.IDisposable
        {
            public GameObject Root { get; }
            public SceneBootstrap Bootstrap { get; }
            public CorgiController Player { get; }
            public WhiskerbotController Boss { get; }
            public BossBannerUI BossBanner { get; }
            public Transform BossIntroSpawn { get; }

            private readonly GameObject _playerGo;
            private readonly GameObject _bossGo;
            private readonly GameObject _canvasGo;
            private readonly CorgiData _corgiData;
            private readonly AttackData _attackData;
            private readonly EnemyData _bossData;
            private readonly WaveData _waveData;

            public BossCheckpointFixture()
            {
                Root = new GameObject("SceneBootstrap");
                Bootstrap = Root.AddComponent<SceneBootstrap>();
                Root.AddComponent<SpawnManager>();
                Root.AddComponent<GroupTargetCamera>();
                Root.AddComponent<ArenaCameraLock>();

                var spawnPointGo = new GameObject("BossIntroSpawnPoint");
                BossIntroSpawn = spawnPointGo.transform;
                BossIntroSpawn.position = new Vector3(12f, 0f, 2f);
                SetPrivateField(Bootstrap, "_bossIntroSpawnPoint", BossIntroSpawn);
                SetPrivateField(Bootstrap, "_retryDelaySeconds", 0f);

                _waveData = ScriptableObject.CreateInstance<WaveData>();
                _waveData.waves = new WaveEntry[0];
                SetPrivateField(Bootstrap, "_waveData", _waveData);

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
                Player = _playerGo.AddComponent<CorgiController>();
                Player.Initialize(_corgiData, new InputBuffer(), 0);
                Player.AddEntityComponent<IHealthComponent>(new HealthComponent(_corgiData.maxHP));

                _bossGo = new GameObject("Whiskerbot");
                Boss = _bossGo.AddComponent<WhiskerbotController>();
                _bossData = ScriptableObject.CreateInstance<EnemyData>();
                _bossData.enemyName = "WHISKERBOT-9000";
                _bossData.maxHP = 200;
                _bossData.behaviorPreset = EnemyBehaviorPreset.Boss;
                Boss.Initialize(_bossData);

                _canvasGo = new GameObject("HUDCanvas", typeof(Canvas));
                BossBanner = new GameObject("BossBanner", typeof(RectTransform), typeof(BossBannerUI)).GetComponent<BossBannerUI>();
                BossBanner.transform.SetParent(_canvasGo.transform, false);
            }

            public void KillPlayers()
            {
                var health = Player.GetEntityComponent<IHealthComponent>();
                health.TakeDamage(health.MaxHP);
            }

            public void Dispose()
            {
                Object.Destroy(Root);
                Object.Destroy(_playerGo);
                Object.Destroy(_bossGo);
                Object.Destroy(_canvasGo);
                if (BossIntroSpawn != null)
                {
                    Object.Destroy(BossIntroSpawn.gameObject);
                }

                Object.Destroy(_waveData);
                Object.Destroy(_attackData);
                Object.Destroy(_corgiData);
                Object.Destroy(_bossData);
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
