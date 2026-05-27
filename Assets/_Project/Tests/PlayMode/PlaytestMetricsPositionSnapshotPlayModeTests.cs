using System.Collections;
using System.IO;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using CorgiCommando.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class PlaytestMetricsPositionSnapshotPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlaytestMetrics_PositionSnapshot_AtBossPhaseTransition()
        {
            PlaytestMetrics.Reset();
            PlaytestMetrics.IsRecording = true;

            var playerData = ScriptableObject.CreateInstance<CorgiData>();
            playerData.maxHP = 100;
            playerData.walkSpeed = 5f;
            playerData.depthSpeed = 3f;
            playerData.jumpForce = 10f;
            playerData.maxSpecialMeter = 100f;
            playerData.specialCost = 100f;
            playerData.comboChain = new AttackData[0];

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(1f, 2f, 3f);
            playerGo.AddComponent<KinematicMovementController>();
            var player = playerGo.AddComponent<CorgiController>();
            player.Initialize(playerData, new InputBuffer(), 0);

            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.maxHP = 10;
            enemyData.behaviorPreset = EnemyBehaviorPreset.FeralCat;

            var enemyGo = new GameObject("Enemy");
            enemyGo.transform.position = new Vector3(4f, 5f, 6f);
            enemyGo.AddComponent<KinematicMovementController>();
            var enemy = enemyGo.AddComponent<EnemyAI>();
            enemy.Initialize(enemyData);

            var bossData = ScriptableObject.CreateInstance<EnemyData>();
            bossData.maxHP = 200;
            bossData.behaviorPreset = EnemyBehaviorPreset.Boss;

            var bossGo = new GameObject("Boss");
            bossGo.transform.position = new Vector3(7f, 8f, 9f);
            var boss = bossGo.AddComponent<WhiskerbotController>();
            boss.Initialize(bossData);

            yield return null;

            boss.CheckPhaseTransition(150, 200);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_boss_phase_snapshot.json");
            PlaytestMetrics.WriteReport(path);
            string json = File.ReadAllText(path);

            StringAssert.Contains("\"positionSnapshots\": [", json);
            StringAssert.Contains("\"label\": \"boss-phase-1-to-2\"", json);
            StringAssert.Contains("\"player-1\"", json);
            StringAssert.Contains("\"enemy-1-enemyai\"", json);
            StringAssert.Contains("\"boss\"", json);
            StringAssert.Contains("\"frame\":", json);

            PlaytestMetrics.IsRecording = false;
            PlaytestMetrics.Reset();
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(bossGo);
            Object.DestroyImmediate(playerData);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(bossData);
        }
    }
}
