using System;
using System.Collections.Generic;
using System.IO;
using CorgiCommando.Testing;
using NUnit.Framework;

namespace CorgiCommando.Tests.EditMode
{
    [TestFixture]
    public class PlaytestRecorderTests
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
        public void PlaytestRecorder_CapturesFlaggedTransitions_AndWritesSummary()
        {
            PlaytestMetrics.IsRecording = true;

            DateTime startTime = new DateTime(2026, 5, 27, 1, 0, 0, DateTimeKind.Utc);
            int tick = 0;
            var screenshots = new List<string>();
            using (var recorder = new PlaytestRecorder(
                       () => startTime.AddSeconds(tick++),
                       path => screenshots.Add(path)))
            {
                recorder.StartRun();
                PlaytestMetrics.LogStateTransition("SpawnManager:1", "Idle", "WaveStart");
                PlaytestMetrics.LogStateTransition("SpawnManager:1", "WaveStart", "WaveClear");
                PlaytestMetrics.LogStateTransition("WhiskerbotController:9", "0", "1");
                PlaytestMetrics.LogStateTransition("WhiskerbotController:9", "1", "2");
                PlaytestMetrics.LogStateTransition("SpawnManager:1", "WaveClear", "Victory");
                PlaytestMetrics.LogStateTransition("EnemyAI:1", "Idle", "Chase");
            }

            Assert.That(screenshots, Has.Count.EqualTo(5));
            Assert.That(Path.GetFileName(screenshots[0]), Is.EqualTo("frame-wave-start-001.png"));
            Assert.That(Path.GetFileName(screenshots[1]), Is.EqualTo("frame-wave-clear-002.png"));
            Assert.That(Path.GetFileName(screenshots[2]), Is.EqualTo("frame-boss-intro-003.png"));
            Assert.That(Path.GetFileName(screenshots[3]), Is.EqualTo("frame-boss-phase-transition-004.png"));
            Assert.That(Path.GetFileName(screenshots[4]), Is.EqualTo("frame-victory-005.png"));

            string runDirectory = Path.GetDirectoryName(screenshots[0]);
            Assert.That(runDirectory, Is.Not.Null.And.Not.Empty);
            string summaryPath = Path.Combine(runDirectory, "summary.md");
            Assert.That(File.Exists(summaryPath), Is.True);

            string summary = File.ReadAllText(summaryPath);
            Assert.That(summary, Does.Contain("# Playtest Run"));
            Assert.That(summary, Does.Contain("wave-start"));
            Assert.That(summary, Does.Contain("wave-clear"));
            Assert.That(summary, Does.Contain("boss-intro"));
            Assert.That(summary, Does.Contain("boss-phase-transition"));
            Assert.That(summary, Does.Contain("victory"));
        }
    }
}
