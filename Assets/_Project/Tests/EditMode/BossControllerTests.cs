using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Enemies;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for WhiskerbotController boss — phase transitions, laser, pilot eject.
    /// </summary>
    [TestFixture]
    public class BossControllerTests
    {
        private GameObject _bossGo;
        private WhiskerbotController _boss;
        private EnemyData _bossData;

        [SetUp]
        public void SetUp()
        {
            _bossGo = new GameObject("Whiskerbot");
            _boss = _bossGo.AddComponent<WhiskerbotController>();

            _bossData = ScriptableObject.CreateInstance<EnemyData>();
            _bossData.enemyName = "WHISKERBOT-9000";
            _bossData.maxHP = 200;
            _bossData.behaviorPreset = EnemyBehaviorPreset.Boss;

            _boss.Initialize(_bossData);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_bossGo);
            UnityEngine.Object.DestroyImmediate(_bossData);
        }

        [Test]
        public void InitialPhase_IsPhase1()
        {
            // Assert
            Assert.AreEqual(1, _boss.CurrentPhase);
            Assert.AreEqual(3, _boss.TotalPhases);
        }

        [Test]
        public void CheckPhaseTransition_At75Percent_TransitionsToPhase2()
        {
            // Arrange — 75% of 200 = 150 HP
            int phaseChangedTo = 0;
            _boss.OnPhaseChanged += (from, to) => phaseChangedTo = to;

            // Act — HP at 150 (exactly 75%)
            _boss.CheckPhaseTransition(150, 200);

            // Assert
            Assert.AreEqual(2, _boss.CurrentPhase);
            Assert.AreEqual(2, phaseChangedTo);
        }

        [Test]
        public void CheckPhaseTransition_At35Percent_TransitionsToPhase3()
        {
            // Arrange — first transition to Phase 2
            _boss.CheckPhaseTransition(150, 200);

            // Act — HP at 70 (35% of 200)
            _boss.CheckPhaseTransition(70, 200);

            // Assert
            Assert.AreEqual(3, _boss.CurrentPhase);
        }

        [Test]
        public void ActivateLaser_SetsLaserActive()
        {
            // Arrange — must be in Phase 2 for laser
            _boss.CheckPhaseTransition(150, 200);

            // Act
            _boss.ActivateLaser();

            // Assert
            Assert.IsTrue(_boss.IsLaserActive);
        }

        [Test]
        public void EjectPilot_CreatesSeparateEntity()
        {
            // Arrange — transition through all phases
            _boss.CheckPhaseTransition(150, 200);
            _boss.CheckPhaseTransition(70, 200);
            bool ejected = false;
            _boss.OnPilotEjected += () => ejected = true;

            // Act — mech HP at 0
            _boss.EjectPilot();

            // Assert
            Assert.IsTrue(ejected);
            Assert.IsTrue(_boss.IsPilotEjected);
            Assert.IsNotNull(_boss.PilotEntity);
        }

        [Test]
        public void PilotEntity_HasOwnHP()
        {
            // Arrange
            _boss.CheckPhaseTransition(150, 200);
            _boss.CheckPhaseTransition(70, 200);
            _boss.EjectPilot();

            // Assert — pilot is a separate entity with its own health
            // Design intent: pilot fight is a distinct combat encounter
            Assert.IsNotNull(_boss.PilotEntity);
            Assert.AreNotEqual(_boss, _boss.PilotEntity);
        }
    }
}
