using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for RunState (co-op shared state) and ReviveSystem.
    /// </summary>
    [TestFixture]
    public class RunStateTests
    {
        private RunState _runState;
        private ReviveSystem _revive;

        [SetUp]
        public void SetUp()
        {
            _runState = ScriptableObject.CreateInstance<RunState>();
            _runState.InitializeRun(3, 2); // 3 lives, 2 players

            _revive = new ReviveSystem();
            _revive.ReviveTime = 3.0f;
            _revive.ReviveRange = 2.0f;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_runState);
        }

        [Test]
        public void InitializeRun_SetsStartingState()
        {
            // Assert
            Assert.AreEqual(3, _runState.LivesRemaining);
            Assert.AreEqual(0, _runState.TreatsCollected);
            Assert.AreEqual(2, _runState.ActivePlayerCount);
        }

        [Test]
        public void AddTreats_IncreasesTotal()
        {
            // Act
            _runState.AddTreats(15);

            // Assert
            Assert.AreEqual(15, _runState.TreatsCollected);
        }

        [Test]
        public void AddTreats_FiresOnTreatsChangedEvent()
        {
            // Arrange
            int reported = 0;
            _runState.OnTreatsChanged += (total) => reported = total;

            // Act
            _runState.AddTreats(10);

            // Assert
            Assert.AreEqual(10, reported);
        }

        [Test]
        public void ConsumeLife_DecrementsLives()
        {
            // Act
            bool consumed = _runState.ConsumeLife();

            // Assert
            Assert.IsTrue(consumed);
            Assert.AreEqual(2, _runState.LivesRemaining);
        }

        [Test]
        public void ConsumeLife_NoLivesRemaining_ReturnsFalse()
        {
            // Arrange — consume all lives
            _runState.ConsumeLife();
            _runState.ConsumeLife();
            _runState.ConsumeLife();

            // Act
            bool consumed = _runState.ConsumeLife();

            // Assert
            Assert.IsFalse(consumed);
            Assert.AreEqual(0, _runState.LivesRemaining);
        }

        [Test]
        public void OnPlayerDied_BothDead_NoLives_FiresGameOver()
        {
            // Arrange
            _runState.ConsumeLife();
            _runState.ConsumeLife();
            _runState.ConsumeLife(); // 0 lives
            bool gameOver = false;
            _runState.OnGameOver += () => gameOver = true;

            // Act — both players die
            _runState.OnPlayerDied(0);
            _runState.OnPlayerDied(1);

            // Assert
            Assert.IsTrue(gameOver);
        }

        [Test]
        public void OnPlayerDropIn_IncreasesPlayerCount_DoesNotResetTreats()
        {
            // Arrange — start with 1 player, add some Treats
            var soloRun = ScriptableObject.CreateInstance<RunState>();
            soloRun.InitializeRun(3, 1);
            soloRun.AddTreats(50);

            // Act — P2 drops in
            soloRun.OnPlayerDropIn(1);

            // Assert — Treats preserved, player count increased
            Assert.AreEqual(2, soloRun.ActivePlayerCount);
            Assert.AreEqual(50, soloRun.TreatsCollected);

            UnityEngine.Object.DestroyImmediate(soloRun);
        }

        [Test]
        public void ReviveSystem_ProximityCountsDown()
        {
            // Arrange
            var alivePos = Vector3.zero;
            var downedPos = new Vector3(1f, 0f, 0f); // within range

            // Act — tick for 1 second
            _revive.Tick(alivePos, downedPos, 1.0f);

            // Assert
            Assert.AreEqual(1.0f, _revive.ReviveProgress, 0.01f);
            Assert.IsTrue(_revive.IsReviving);
        }

        [Test]
        public void ReviveSystem_CompletesRevive_FiresEvent()
        {
            // Arrange
            var alivePos = Vector3.zero;
            var downedPos = new Vector3(1f, 0f, 0f);
            int revivedPlayer = -1;
            _revive.OnReviveComplete += (idx) => revivedPlayer = idx;

            // Act — tick for full revive time
            _revive.Tick(alivePos, downedPos, 3.0f);

            // Assert
            Assert.AreEqual(0, revivedPlayer); // default player index
        }

        [Test]
        public void ReviveSystem_OutOfRange_DoesNotProgress()
        {
            // Arrange — players too far apart
            var alivePos = Vector3.zero;
            var downedPos = new Vector3(10f, 0f, 0f); // outside 2.0 range

            // Act
            _revive.Tick(alivePos, downedPos, 1.0f);

            // Assert
            Assert.AreEqual(0f, _revive.ReviveProgress, 0.01f);
            Assert.IsFalse(_revive.IsReviving);
        }
    }
}
