using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CorgiCommando.Core;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using CorgiCommando.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CorgiCommando.Tests.PlayMode
{
    [TestFixture]
    public class FullArcPlaytest
    {
        private static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const string FullArcScriptPath = "Assets/_Project/Data/PlaytestScripts/FullArcDemoScript.asset";

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator FullArc_SpawnToVictory_CompletesWithoutErrors()
        {
            var unhandledExceptions = new List<string>();
            var startedWaves = new HashSet<int>();
            var bossPhasesEntered = new HashSet<int>();
            bool bossIntroFired = false;
            bool victoryReached = false;
            float spawnToBossIntroSeconds = 0f;
            string reportPath = Path.Combine("Temp", "PlaytestReports", $"FullArcPlaytest-{Guid.NewGuid():N}.json");
            Action<int> waveStartedHandler = waveIndex => { startedWaves.Add(waveIndex); };

            float originalTimeScale = Time.timeScale;
            GameObject botRoot = null;
            Action<string, string, LogType> logHandler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Exception)
                {
                    unhandledExceptions.Add($"{condition}\n{stackTrace}");
                }
            };

            PlaytestMetrics.Reset();
            PlaytestMetrics.IsRecording = true;
            Application.logMessageReceived += logHandler;

            try
            {
                Time.timeScale = 30f;

                yield return SceneManager.LoadSceneAsync("Level_Backyard", LoadSceneMode.Single);
                yield return null;

                var director = UnityEngine.Object.FindObjectOfType<LevelBackyardDirector>();
                var spawnManager = UnityEngine.Object.FindObjectOfType<SpawnManager>();
                var player = UnityEngine.Object.FindObjectOfType<CorgiController>();

                Assert.NotNull(director, "Expected LevelBackyardDirector in Level_Backyard scene.");
                Assert.NotNull(spawnManager, "Expected SpawnManager in Level_Backyard scene.");
                Assert.NotNull(player, "Expected player in Level_Backyard scene.");

                IInputBuffer buffer = player.GetComponent<PlayerInputHandler>()?.Buffer;
                Assert.NotNull(buffer, "Expected initialized PlayerInputHandler buffer.");

                spawnManager.OnWaveStarted += waveStartedHandler;

                var script = LoadAssetAtPath<PlaytestScript>(FullArcScriptPath);
                Assert.NotNull(script, $"Expected playtest script asset at path: {FullArcScriptPath}");

                botRoot = new GameObject("FullArcPlaytestBot");
                var playtestBot = botRoot.AddComponent<PlaytestBot>();
                playtestBot.StartCoroutine(playtestBot.Play(buffer, script));

                float spawnTimestamp = Time.time;

                player.transform.position = new Vector3(4f, player.transform.position.y, player.transform.position.z);
                yield return null;

                yield return WaitForCondition(() => UnityEngine.Object.FindObjectOfType<EnemyAI>() != null, 240);

                EnemyAI openingEnemy = UnityEngine.Object.FindObjectOfType<EnemyAI>();
                var bootstrap = UnityEngine.Object.FindObjectOfType<SceneBootstrap>();
                if (openingEnemy != null && bootstrap?.CombatSystem != null && player.CharacterData?.comboChain?.Length > 0)
                {
                    EnsureKnockbackReceiver(openingEnemy);
                    player.transform.position = new Vector3(openingEnemy.transform.position.x - 0.2f, openingEnemy.transform.position.y, openingEnemy.transform.position.z);
                    bootstrap.CombatSystem.ResolveAttack(player, player.CharacterData.comboChain[0], new Entity[] { openingEnemy });
                    for (int i = 0; i < 5; i++)
                    {
                        yield return null;
                    }
                }

                yield return ClearAllActiveEnemiesUntil(() => director.IsBossDoorUnlocked, 600);
                Assert.IsTrue(director.IsBossDoorUnlocked, "Expected all three waves to unlock the boss door.");

                player.transform.position = new Vector3(18f, player.transform.position.y, player.transform.position.z);
                yield return WaitForCondition(() => director.IsBossSpawned, 180);

                bossIntroFired = director.IsBossSpawned;
                float bossIntroTimestamp = Time.time;
                spawnToBossIntroSeconds = bossIntroTimestamp - spawnTimestamp;

                var boss = UnityEngine.Object.FindObjectOfType<WhiskerbotController>();
                Assert.NotNull(boss, "Expected boss to spawn after crossing boss trigger.");

                bossPhasesEntered.Add(boss.CurrentPhase);
                boss.OnPhaseChanged += (_, to) => bossPhasesEntered.Add(to);

                var bossHealth = boss.GetEntityComponent<IHealthComponent>();
                Assert.NotNull(bossHealth, "Expected boss to have health component.");

                int maxBossHp = Mathf.Max(1, bossHealth.MaxHP);
                DamageBossToThresholdAndCheckPhase(boss, bossHealth, WhiskerbotController.Phase2Threshold, 2, maxBossHp);
                yield return null;
                DamageBossToThresholdAndCheckPhase(boss, bossHealth, WhiskerbotController.Phase3Threshold, 3, maxBossHp);
                yield return null;

                bossHealth.TakeDamage(int.MaxValue);
                yield return null;

                victoryReached = !boss.IsAlive || bossHealth.IsDead;

                spawnManager.OnWaveStarted -= waveStartedHandler;
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Application.logMessageReceived -= logHandler;

                PlaytestMetrics.WriteReport(reportPath);
                TestContext.AddTestAttachment(reportPath, "Full arc playtest metrics");

                PlaytestMetrics.IsRecording = false;
                PlaytestMetrics.Reset();

                if (botRoot != null)
                {
                    UnityEngine.Object.Destroy(botRoot);
                }
            }

            var report = LoadReport(reportPath);
            float averageHitstopFrames = report.hitstops.Count > 0 ? report.hitstops.Average(hitstop => hitstop.durationFrames) : 0f;

            Assert.That(unhandledExceptions, Is.Empty, $"Unhandled exceptions were logged:\n{string.Join("\n---\n", unhandledExceptions)}");
            CollectionAssert.IsSupersetOf(startedWaves, new[] { 0, 1, 2 }, "Expected all three waves to start.");
            Assert.IsTrue(bossIntroFired, "Expected boss intro to fire.");
            CollectionAssert.IsSupersetOf(bossPhasesEntered, new[] { 1, 2, 3 }, "Expected boss phases 1/2/3 to be entered.");
            Assert.IsTrue(victoryReached, "Expected victory state (boss defeated) to be reached.");
            Assert.That(spawnToBossIntroSeconds, Is.InRange(60f, 240f), "Spawn-to-boss-intro time should remain in design range.");
            Assert.That(averageHitstopFrames, Is.InRange(3f, 6f), "Average hitstop should remain in design range.");
            Assert.GreaterOrEqual(report.knockbacks.Count, 1, "Expected at least one knockback metric event.");
            Assert.GreaterOrEqual(report.screenShakes.Count, 1, "Expected at least one screen shake metric event.");
        }

        private static IEnumerator WaitForCondition(Func<bool> condition, int maxFrames)
        {
            int frames = 0;
            while (!condition() && frames++ < maxFrames)
            {
                yield return null;
            }
        }

        private static IEnumerator ClearAllActiveEnemiesUntil(Func<bool> stopCondition, int maxFrames)
        {
            int frames = 0;
            while (!stopCondition() && frames++ < maxFrames)
            {
                EnemyAI[] aliveEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                for (int i = 0; i < aliveEnemies.Length; i++)
                {
                    if (aliveEnemies[i] == null)
                    {
                        continue;
                    }

                    EnsureKnockbackReceiver(aliveEnemies[i]);
                    aliveEnemies[i].GetEntityComponent<IHealthComponent>()?.TakeDamage(int.MaxValue);
                }

                yield return null;
            }
        }

        private static void EnsureKnockbackReceiver(EnemyAI enemy)
        {
            if (enemy != null && !enemy.HasEntityComponent<KnockbackReceiver>())
            {
                enemy.AddEntityComponent(new KnockbackReceiver());
            }
        }

        private static void DamageBossToThresholdAndCheckPhase(WhiskerbotController boss, IHealthComponent bossHealth, float thresholdRatio, int expectedPhase, int maxBossHp)
        {
            int thresholdHp = Mathf.CeilToInt(maxBossHp * Mathf.Clamp01(thresholdRatio));
            int requiredDamage = Mathf.Max(1, bossHealth.CurrentHP - thresholdHp);
            bossHealth.TakeDamage(requiredDamage);
            boss.CheckPhaseTransition(bossHealth.CurrentHP, maxBossHp);
            Assert.AreEqual(expectedPhase, boss.CurrentPhase);
        }

        private static PlaytestMetrics.PlaytestReport LoadReport(string path)
        {
            string json = File.ReadAllText(path);
            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(json) ?? new PlaytestMetrics.PlaytestReport();
            report.hitstops ??= new List<PlaytestMetrics.HitstopEntry>();
            report.knockbacks ??= new List<PlaytestMetrics.KnockbackEntry>();
            report.screenShakes ??= new List<PlaytestMetrics.ScreenShakeEntry>();
            report.stateTransitions ??= new List<PlaytestMetrics.StateTransitionEntry>();
            report.frameTimes ??= new List<PlaytestMetrics.FrameTimeEntry>();
            return report;
        }

        private static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
            Type assetDatabaseType = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
            Assert.NotNull(assetDatabaseType, "UnityEditor.AssetDatabase type should be available in PlayMode tests.");

            var loadMethod = assetDatabaseType.GetMethod(
                "LoadAssetAtPath",
                PublicStatic,
                binder: null,
                types: new[] { typeof(string), typeof(Type) },
                modifiers: null);

            Assert.NotNull(loadMethod, "Expected UnityEditor.AssetDatabase.LoadAssetAtPath(string, Type) to exist.");
            return loadMethod.Invoke(null, new object[] { path, typeof(T) }) as T;
        }
    }
}
