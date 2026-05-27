using System.Collections;
using System.IO;
using System.Linq;
using CorgiCommando.Core;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class PlaytestMetricsAssetResolutionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlaytestMetrics_LogAssetResolution_SuccessfulPrefabInstantiate()
        {
            PlaytestMetrics.Reset();
            PlaytestMetrics.IsRecording = true;

            GameObject managerGo = null;
            EnemyData enemyData = null;
            GameObject enemyPrefab = null;
            WaveData waveData = null;
            try
            {
                managerGo = new GameObject("SpawnManager");
                var manager = managerGo.AddComponent<SpawnManager>();
                enemyData = ScriptableObject.CreateInstance<EnemyData>();
                enemyData.enemyName = "FallbackEnemy";
                enemyData.behaviorPreset = EnemyBehaviorPreset.FeralCat;

                enemyPrefab = new GameObject("EnemyPrefab", typeof(FeralCatAI));
                waveData = ScriptableObject.CreateInstance<WaveData>();
                waveData.waves = new[]
                {
                    new WaveEntry
                    {
                        spawnGroups = new[]
                        {
                            new SpawnGroup
                            {
                                enemyPrefab = enemyPrefab,
                                enemyData = enemyData,
                                count = 1,
                                spawnPosition = Vector3.zero
                            }
                        }
                    }
                };

                manager.StartEncounter(waveData);
                manager.SpawnCurrentWave();
                yield return null;

                string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_asset_resolution_success.json");
                PlaytestMetrics.WriteReport(path);
                var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));

                Assert.That(report.assetResolutions.Any(entry =>
                    entry.succeeded &&
                    entry.assetType == nameof(GameObject) &&
                    entry.assetPath.Contains("EnemyPrefab")), Is.True);
            }
            finally
            {
                foreach (var enemy in Object.FindObjectsOfType<EnemyAI>())
                {
                    Object.Destroy(enemy.gameObject);
                }

                if (managerGo != null)
                {
                    Object.Destroy(managerGo);
                }

                if (enemyPrefab != null)
                {
                    Object.Destroy(enemyPrefab);
                }

                if (waveData != null)
                {
                    Object.Destroy(waveData);
                }

                if (enemyData != null)
                {
                    Object.Destroy(enemyData);
                }

                PlaytestMetrics.IsRecording = false;
                PlaytestMetrics.Reset();
            }
        }

        [UnityTest]
        public IEnumerator PlaytestMetrics_LogAssetResolution_FailedNullPrefab()
        {
            PlaytestMetrics.Reset();
            PlaytestMetrics.IsRecording = true;

            GameObject managerGo = null;
            EnemyData enemyData = null;
            WaveData waveData = null;
            try
            {
                managerGo = new GameObject("SpawnManager");
                var manager = managerGo.AddComponent<SpawnManager>();
                enemyData = ScriptableObject.CreateInstance<EnemyData>();
                enemyData.enemyName = "FallbackEnemy";
                enemyData.behaviorPreset = EnemyBehaviorPreset.FeralCat;

                waveData = ScriptableObject.CreateInstance<WaveData>();
                waveData.waves = new[]
                {
                    new WaveEntry
                    {
                        spawnGroups = new[]
                        {
                            new SpawnGroup
                            {
                                enemyPrefab = null,
                                enemyData = enemyData,
                                count = 1,
                                spawnPosition = Vector3.zero
                            }
                        }
                    }
                };

                manager.StartEncounter(waveData);
                manager.SpawnCurrentWave();
                yield return null;

                string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_asset_resolution_failure.json");
                PlaytestMetrics.WriteReport(path);
                var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));

                Assert.That(report.assetResolutions.Any(entry =>
                    !entry.succeeded &&
                    entry.assetType == nameof(GameObject) &&
                    entry.assetPath == "SpawnGroup.enemyPrefab"), Is.True);
            }
            finally
            {
                foreach (var enemy in Object.FindObjectsOfType<EnemyAI>())
                {
                    Object.Destroy(enemy.gameObject);
                }

                if (managerGo != null)
                {
                    Object.Destroy(managerGo);
                }

                if (waveData != null)
                {
                    Object.Destroy(waveData);
                }

                if (enemyData != null)
                {
                    Object.Destroy(enemyData);
                }

                PlaytestMetrics.IsRecording = false;
                PlaytestMetrics.Reset();
            }
        }
    }
}
