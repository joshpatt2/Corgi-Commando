using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using CorgiCommando.Testing;
using UnityEngine;
using UnityEngine.TestTools;

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
            PlaytestMetrics.LogKnockback(3f, "EnemyA");
            PlaytestMetrics.LogScreenShake(0.25f, "Heavy");
            PlaytestMetrics.LogStateTransition("EnemyAI:1", "Idle", "Chase");
            PlaytestMetrics.LogFrameTime(0.016f);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_append.json");
            PlaytestMetrics.WriteReport(path);

            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
            Assert.That(report.hitstops, Has.Count.EqualTo(1));
            Assert.That(report.knockbacks, Has.Count.EqualTo(1));
            Assert.That(report.screenShakes, Has.Count.EqualTo(1));
            Assert.That(report.stateTransitions, Has.Count.EqualTo(1));
            Assert.That(report.frameTimes, Has.Count.EqualTo(1));
            Assert.That(report.exceptions, Is.Empty);
        }

        [Test]
        public void PlaytestMetrics_Log_NoOp_WhenNotRecording()
        {
            PlaytestMetrics.LogHitstop(2f, 2.05f);
            PlaytestMetrics.LogKnockback(4f, "EnemyB");
            PlaytestMetrics.LogScreenShake(0.5f, "Special");
            PlaytestMetrics.LogStateTransition("EnemyAI:2", "Chase", "Attack");
            PlaytestMetrics.LogFrameTime(0.02f);

            string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_noop.json");
            PlaytestMetrics.WriteReport(path);

            var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
            Assert.That(report.hitstops, Is.Empty);
            Assert.That(report.knockbacks, Is.Empty);
            Assert.That(report.screenShakes, Is.Empty);
            Assert.That(report.stateTransitions, Is.Empty);
            Assert.That(report.frameTimes, Is.Empty);
            Assert.That(report.exceptions, Is.Empty);
        }

        [Test]
        public void PlaytestMetrics_WriteReport_RoundTrips()
        {
            PlaytestMetrics.IsRecording = true;
            PlaytestMetrics.LogHitstop(0f, 0.1f);
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
            Assert.That(report.knockbacks[0].targetId, Is.EqualTo("EnemyC"));
            Assert.That(report.screenShakes[0].source, Is.EqualTo("Special"));
            Assert.That(report.stateTransitions[0].componentId, Is.EqualTo("WhiskerbotController:9"));
            Assert.That(report.frameTimes[0].deltaTime, Is.EqualTo(1f / 60f).Within(0.0001f));
            Assert.That(report.exceptions, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PlaytestMetrics_LogException_CapturesNullRef()
        {
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                PlaytestMetrics.IsRecording = true;

                object missingReference = null;
                try
                {
                    _ = missingReference.ToString();
                }
                catch (NullReferenceException exception)
                {
                    Debug.LogException(exception);
                }

                yield return null;

                string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_exception_capture.json");
                PlaytestMetrics.WriteReport(path);

                var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
                Assert.That(report.exceptions, Has.Count.EqualTo(1));
                Assert.That(report.exceptions[0].condition, Does.Contain("NullReferenceException"));
                Assert.That(report.exceptions[0].stackTrace, Is.Not.Empty);
                Assert.That(report.exceptions[0].type, Is.EqualTo(LogType.Exception.ToString()));
                Assert.That(report.exceptions[0].frame, Is.GreaterThanOrEqualTo(0));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        [UnityTest]
        public IEnumerator PlaytestMetrics_Subscription_IdempotentAcrossResets()
        {
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                PlaytestMetrics.IsRecording = true;
                PlaytestMetrics.IsRecording = true;

                PlaytestMetrics.Reset();
                PlaytestMetrics.Reset();

                PlaytestMetrics.IsRecording = true;

                Debug.LogException(new InvalidOperationException("idempotent-subscription-check"));
                yield return null;

                string path = Path.Combine(Path.GetTempPath(), "playtest_metrics_subscription_idempotent.json");
                PlaytestMetrics.WriteReport(path);

                var report = JsonUtility.FromJson<PlaytestMetrics.PlaytestReport>(File.ReadAllText(path));
                Assert.That(report.exceptions, Has.Count.EqualTo(1));
                Assert.That(report.exceptions[0].condition, Does.Contain("idempotent-subscription-check"));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }
    }
}
