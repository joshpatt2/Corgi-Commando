using System.IO;
using NUnit.Framework;
using CorgiCommando.Testing;
using UnityEngine;

namespace CorgiCommando.Tests.EditMode
{
    [TestFixture]
    public class PlaytestMetricsTests
    {
        [SetUp]
        public void SetUp()
        {
            PlaytestMetrics.IsRecording = false;
            PlaytestMetrics.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            PlaytestMetrics.IsRecording = false;
            PlaytestMetrics.Reset();
        }

        [Test]
        public void PlaytestMetrics_Log_AppendsEntries_WhenRecording()
        {
            PlaytestMetrics.IsRecording = true;

            PlaytestMetrics.LogHitstop(1f, 1.05f);
            PlaytestMetrics.LogDamage("Sarge (CorgiController)", "EnemyA (Entity)", 10, "Light", false);
            PlaytestMetrics.LogKnockback(3f, "EnemyA");
            PlaytestMetrics.LogScreenShake(0.25f, "Heavy");
            PlaytestMetrics.LogStateTransition("EnemyAI:1", "Idle", "Chase");
            PlaytestMetrics.LogFrameTime(0.016f);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_append.json");
            PlaytestMetrics.WriteReport(path);

            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
            Assert.That(report.hitstops, Has.Count.EqualTo(1));
            Assert.That(report.damageEvents, Has.Count.EqualTo(1));
            Assert.That(report.knockbacks, Has.Count.EqualTo(1));
            Assert.That(report.screenShakes, Has.Count.EqualTo(1));
            Assert.That(report.stateTransitions, Has.Count.EqualTo(1));
            Assert.That(report.frameTimes, Has.Count.EqualTo(1));
        }

        [Test]
        public void PlaytestMetrics_Log_NoOp_WhenNotRecording()
        {
            PlaytestMetrics.LogHitstop(2f, 2.05f);
            PlaytestMetrics.LogDamage("Sarge (CorgiController)", "EnemyB (Entity)", 10, "Light", false);
            PlaytestMetrics.LogKnockback(4f, "EnemyB");
            PlaytestMetrics.LogScreenShake(0.5f, "Special");
            PlaytestMetrics.LogStateTransition("EnemyAI:2", "Chase", "Attack");
            PlaytestMetrics.LogFrameTime(0.02f);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_noop.json");
            PlaytestMetrics.WriteReport(path);

            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
            Assert.That(report.hitstops, Is.Empty);
            Assert.That(report.damageEvents, Is.Empty);
            Assert.That(report.knockbacks, Is.Empty);
            Assert.That(report.screenShakes, Is.Empty);
            Assert.That(report.stateTransitions, Is.Empty);
            Assert.That(report.frameTimes, Is.Empty);
        }

        [Test]
        public void PlaytestMetrics_WriteReport_RoundTrips()
        {
            PlaytestMetrics.IsRecording = true;
            PlaytestMetrics.LogHitstop(0f, 0.1f);
            PlaytestMetrics.LogDamage("Sarge (CorgiController)", "EnemyC (Entity)", 10, "Light", false);
            PlaytestMetrics.LogKnockback(7.5f, "EnemyC");
            PlaytestMetrics.LogScreenShake(0.75f, "Special");
            PlaytestMetrics.LogStateTransition("WhiskerbotController:9", "1", "2");
            PlaytestMetrics.LogFrameTime(1f / 60f);

            string directory = Path.Combine(Path.GetTempPath(), "playtest-metrics-tests");
            string path = Path.Combine(directory, "roundtrip.json");
            PlaytestMetrics.WriteReport(path);

            Assert.That(File.Exists(path), Is.True);

            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
            Assert.That(report.hitstops[0].durationFrames, Is.EqualTo(6f).Within(0.01f));
            Assert.That(report.damageEvents[0].source, Is.EqualTo("Sarge (CorgiController)"));
            Assert.That(report.damageEvents[0].target, Is.EqualTo("EnemyC (Entity)"));
            Assert.That(report.damageEvents[0].amount, Is.EqualTo(10));
            Assert.That(report.damageEvents[0].damageType, Is.EqualTo("Light"));
            Assert.That(report.damageEvents[0].killedTarget, Is.False);
            Assert.That(report.knockbacks[0].targetId, Is.EqualTo("EnemyC"));
            Assert.That(report.screenShakes[0].source, Is.EqualTo("Special"));
            Assert.That(report.stateTransitions[0].componentId, Is.EqualTo("WhiskerbotController:9"));
            Assert.That(report.frameTimes[0].deltaTime, Is.EqualTo(1f / 60f).Within(0.0001f));
        }

        [Test]
        public void PlaytestMetrics_LogDamage_RecordsEvent()
        {
            PlaytestMetrics.IsRecording = true;
            PlaytestMetrics.LogDamage("Sarge (CorgiController)", "FeralCat (Entity)", 12, "Heavy", false);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_damage_records.json");
            PlaytestMetrics.WriteReport(path);

            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
            Assert.That(report.damageEvents, Has.Count.EqualTo(1));
            Assert.That(report.damageEvents[0].source, Is.EqualTo("Sarge (CorgiController)"));
            Assert.That(report.damageEvents[0].target, Is.EqualTo("FeralCat (Entity)"));
            Assert.That(report.damageEvents[0].amount, Is.EqualTo(12));
            Assert.That(report.damageEvents[0].damageType, Is.EqualTo("Heavy"));
            Assert.That(report.damageEvents[0].killedTarget, Is.False);
            Assert.That(report.damageEvents[0].frame, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void PlaytestMetrics_LogDamage_KilledTargetFlag_AccurateOnLethalHit()
        {
            var attackerGo = new GameObject("Sarge");
            var targetGo = new GameObject("FeralCat");
            try
            {
                var attacker = attackerGo.AddComponent<Core.Entity>();
                var target = targetGo.AddComponent<Core.Entity>();
                var health = new Core.HealthComponent(5);
                target.AddEntityComponent<Core.IHealthComponent>(health);

                health.TakeDamage(5);
                bool killedTarget = health.CurrentHP <= 0;

                PlaytestMetrics.IsRecording = true;
                PlaytestMetrics.LogDamage($"{attacker.name} ({attacker.GetType().Name})", $"{target.name} ({target.GetType().Name})", 5, "Light", killedTarget);

                string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_damage_lethal.json");
                PlaytestMetrics.WriteReport(path);

                var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
                Assert.That(report.damageEvents, Has.Count.EqualTo(1));
                Assert.That(report.damageEvents[0].killedTarget, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(attackerGo);
                UnityEngine.Object.DestroyImmediate(targetGo);
            }
        }
    }
}
