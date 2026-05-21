using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Enemies;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Edit Mode tests for Enemy AI FSM, aggro slot management,
    /// and specific enemy behaviors (raccoon fleeing, turret intervals).
    /// </summary>
    [TestFixture]
    public class EnemyAITests
    {
        private EnemyData _catData;
        private EnemyData _raccoonData;
        private EnemyData _turretData;

        [SetUp]
        public void SetUp()
        {
            _catData = ScriptableObject.CreateInstance<EnemyData>();
            _catData.enemyName = "FeralCat";
            _catData.maxHP = 30;
            _catData.moveSpeed = 3f;
            _catData.aggroRange = 8f;
            _catData.attackRange = 1.5f;
            _catData.behaviorPreset = EnemyBehaviorPreset.FeralCat;

            _raccoonData = ScriptableObject.CreateInstance<EnemyData>();
            _raccoonData.enemyName = "RaccoonBandit";
            _raccoonData.maxHP = 25;
            _raccoonData.moveSpeed = 4f;
            _raccoonData.behaviorPreset = EnemyBehaviorPreset.RaccoonBandit;

            _turretData = ScriptableObject.CreateInstance<EnemyData>();
            _turretData.enemyName = "SprinklerTurret";
            _turretData.maxHP = 20;
            _turretData.moveSpeed = 0f; // fixed position
            _turretData.behaviorPreset = EnemyBehaviorPreset.SprinklerTurret;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_catData);
            UnityEngine.Object.DestroyImmediate(_raccoonData);
            UnityEngine.Object.DestroyImmediate(_turretData);
            CleanupSceneEntities();
        }

        private static void CleanupSceneEntities()
        {
            var entities = UnityEngine.Object.FindObjectsOfType<Entity>();
            for (int i = 0; i < entities.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(entities[i].gameObject);
            }
        }

        [Test]
        public void EnemyAI_InitialState_IsIdle()
        {
            // Arrange
            var go = new GameObject("Cat");
            var cat = go.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);

            // Assert
            Assert.AreEqual(EnemyState.Idle, cat.CurrentState);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EnemyAI_TransitionTo_ValidTransition_Succeeds()
        {
            // Arrange
            var go = new GameObject("Cat");
            var cat = go.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);

            // Act
            bool result = cat.TransitionTo(EnemyState.Chase);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(EnemyState.Chase, cat.CurrentState);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EnemyAI_TransitionSequence_IdleChaseAttackStunnedRecover_Succeeds()
        {
            // Arrange
            var go = new GameObject("Cat");
            var cat = go.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);

            // Act / Assert
            Assert.AreEqual(EnemyState.Idle, cat.CurrentState);
            Assert.IsTrue(cat.TransitionTo(EnemyState.Chase));
            Assert.IsTrue(cat.TransitionTo(EnemyState.Attack));
            Assert.IsTrue(cat.TransitionTo(EnemyState.Stunned));
            Assert.IsTrue(cat.TransitionTo(EnemyState.Recover));
            Assert.AreEqual(EnemyState.Recover, cat.CurrentState);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void FeralCatAI_Tick_PlayerInAggroRange_TransitionsToChase()
        {
            // Arrange
            var catGo = new GameObject("Cat");
            var cat = catGo.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);
            catGo.transform.position = Vector3.zero;

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<Entity>();
            playerGo.transform.position = new Vector3(_catData.aggroRange - 0.5f, 0f, 0f);

            // Act
            cat.Tick(0.016f);

            // Assert
            Assert.AreEqual(EnemyState.Chase, cat.CurrentState);

            UnityEngine.Object.DestroyImmediate(catGo);
            UnityEngine.Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void FeralCatAI_Tick_PlayerInAttackRange_TransitionsToAttack()
        {
            // Arrange
            var catGo = new GameObject("Cat");
            var cat = catGo.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);
            catGo.transform.position = Vector3.zero;
            cat.TransitionTo(EnemyState.Chase);

            var playerGo = new GameObject("Player");
            var player = playerGo.AddComponent<Entity>();
            playerGo.transform.position = new Vector3(_catData.attackRange - 0.25f, 0f, 0f);

            // Act
            cat.Tick(0.016f);

            // Assert
            Assert.AreEqual(EnemyState.Attack, cat.CurrentState);

            UnityEngine.Object.DestroyImmediate(catGo);
            UnityEngine.Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void EnemyAI_OnHit_TransitionsToStunned()
        {
            // Arrange
            var go = new GameObject("Cat");
            var cat = go.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);
            cat.TransitionTo(EnemyState.Chase);

            // Act
            cat.OnHit();

            // Assert
            Assert.AreEqual(EnemyState.Stunned, cat.CurrentState);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void AggroSlotManager_ReserveSlot_Succeeds()
        {
            // Arrange
            var manager = new AggroSlotManager();
            var targetGo = new GameObject("Player");
            var target = targetGo.AddComponent<Entity>();
            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<FeralCatAI>();

            // Act
            bool reserved = manager.TryReserveSlot(enemy, target);

            // Assert
            Assert.IsTrue(reserved);
            Assert.AreEqual(1, manager.GetOccupiedSlots(target));

            manager.Dispose();
            UnityEngine.Object.DestroyImmediate(targetGo);
            UnityEngine.Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void AggroSlotManager_ExceedsMaxSlots_ReturnsFalse()
        {
            // Arrange
            var manager = new AggroSlotManager();
            manager.MaxSlotsPerTarget = 2;
            var targetGo = new GameObject("Player");
            var target = targetGo.AddComponent<Entity>();

            var e1Go = new GameObject("E1");
            var e1 = e1Go.AddComponent<FeralCatAI>();
            var e2Go = new GameObject("E2");
            var e2 = e2Go.AddComponent<FeralCatAI>();
            var e3Go = new GameObject("E3");
            var e3 = e3Go.AddComponent<FeralCatAI>();

            manager.TryReserveSlot(e1, target);
            manager.TryReserveSlot(e2, target);

            // Act — third enemy should be denied
            bool reserved = manager.TryReserveSlot(e3, target);

            // Assert
            Assert.IsFalse(reserved);
            Assert.AreEqual(2, manager.GetOccupiedSlots(target));

            manager.Dispose();
            UnityEngine.Object.DestroyImmediate(targetGo);
            UnityEngine.Object.DestroyImmediate(e1Go);
            UnityEngine.Object.DestroyImmediate(e2Go);
            UnityEngine.Object.DestroyImmediate(e3Go);
        }

        [Test]
        public void AggroSlotManager_ReleaseSlot_FreesSlot()
        {
            // Arrange
            var manager = new AggroSlotManager();
            var targetGo = new GameObject("Player");
            var target = targetGo.AddComponent<Entity>();
            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<FeralCatAI>();
            manager.TryReserveSlot(enemy, target);

            // Act
            manager.ReleaseSlot(enemy);

            // Assert
            Assert.AreEqual(0, manager.GetOccupiedSlots(target));
            Assert.IsTrue(manager.HasAvailableSlot(target));

            manager.Dispose();
            UnityEngine.Object.DestroyImmediate(targetGo);
            UnityEngine.Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void EnemyAI_TransitionTo_Stunned_ReleasesAggroSlot()
        {
            // Arrange
            var manager = new AggroSlotManager();
            var targetGo = new GameObject("Player");
            var target = targetGo.AddComponent<Entity>();
            var enemyGo = new GameObject("Enemy");
            var enemy = enemyGo.AddComponent<FeralCatAI>();
            enemy.Initialize(_catData);

            Assert.IsTrue(manager.TryReserveSlot(enemy, target));

            // Act
            enemy.TransitionTo(EnemyState.Stunned);

            // Assert
            Assert.AreEqual(0, manager.GetOccupiedSlots(target));
            Assert.IsFalse(enemy.HasAggroSlot);

            manager.Dispose();
            UnityEngine.Object.DestroyImmediate(targetGo);
            UnityEngine.Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void FeralCatAI_Tick_WhenNoAggroSlot_RemainsInChaseAndCircles()
        {
            // Arrange
            var manager = new AggroSlotManager { MaxSlotsPerTarget = 1 };

            var targetGo = new GameObject("Player");
            var target = targetGo.AddComponent<Entity>();
            targetGo.transform.position = new Vector3(1f, 0f, 0f);

            var slotHolderGo = new GameObject("SlotHolder");
            var slotHolder = slotHolderGo.AddComponent<FeralCatAI>();
            slotHolder.Initialize(_catData);
            slotHolder.TransitionTo(EnemyState.Chase);
            Assert.IsTrue(manager.TryReserveSlot(slotHolder, target));

            var catGo = new GameObject("Cat");
            var cat = catGo.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);
            cat.TransitionTo(EnemyState.Chase);
            catGo.transform.position = Vector3.zero;
            Vector3 before = catGo.transform.position;

            // Act
            cat.Tick(0.1f);

            // Assert
            Assert.AreEqual(EnemyState.Chase, cat.CurrentState);
            Assert.AreNotEqual(before, catGo.transform.position);

            manager.Dispose();
            UnityEngine.Object.DestroyImmediate(targetGo);
            UnityEngine.Object.DestroyImmediate(slotHolderGo);
            UnityEngine.Object.DestroyImmediate(catGo);
        }

        [Test]
        public void RaccoonBandit_StealTreats_TransitionsToFleeing()
        {
            // Arrange
            var go = new GameObject("Raccoon");
            var raccoon = go.AddComponent<RaccoonBanditAI>();
            raccoon.Initialize(_raccoonData);

            // Act
            raccoon.StealTreats(10);

            // Assert
            Assert.IsTrue(raccoon.IsCarryingTreats);
            Assert.AreEqual(10, raccoon.StolenTreatsAmount);
            Assert.AreEqual(EnemyState.Fleeing, raccoon.CurrentState);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void RaccoonBandit_DiesWhileCarryingTreats_FiresDropEvent()
        {
            // Arrange
            var go = new GameObject("Raccoon");
            var raccoon = go.AddComponent<RaccoonBanditAI>();
            raccoon.Initialize(_raccoonData);
            raccoon.StealTreats(10);
            int dropped = 0;
            raccoon.OnDroppedTreats += amount => dropped = amount;

            // Act
            raccoon.TransitionTo(EnemyState.Dead);

            // Assert
            Assert.AreEqual(10, dropped);
            Assert.IsFalse(raccoon.IsCarryingTreats);
            Assert.AreEqual(0, raccoon.StolenTreatsAmount);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void SprinklerTurret_HasFireInterval()
        {
            // Arrange
            var go = new GameObject("Turret");
            var turret = go.AddComponent<SprinklerTurretAI>();
            turret.Initialize(_turretData);

            // Assert — turret has configurable fire interval
            Assert.Greater(turret.FireInterval, 0f);
            Assert.Greater(turret.TelegraphDuration, 0f);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void SprinklerTurret_Tick_CyclesTelegraphToAttack()
        {
            // Arrange
            var go = new GameObject("Turret");
            var turret = go.AddComponent<SprinklerTurretAI>();
            turret.Initialize(_turretData);

            // Act
            turret.Tick(turret.FireInterval);

            // Assert
            Assert.IsTrue(turret.IsTelegraphing);
            Assert.AreEqual(EnemyState.Idle, turret.CurrentState);

            // Act
            turret.Tick(turret.TelegraphDuration);

            // Assert
            Assert.IsFalse(turret.IsTelegraphing);
            Assert.AreEqual(EnemyState.Attack, turret.CurrentState);

            // Act
            turret.Tick(0.01f);

            // Assert
            Assert.AreEqual(EnemyState.Idle, turret.CurrentState);
            Assert.IsFalse(turret.IsTelegraphing);

            // Act
            turret.Tick(turret.FireInterval);

            // Assert
            Assert.IsTrue(turret.IsTelegraphing);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EnemyAI_TransitionTo_FiresOnStateChangedEvent()
        {
            // Arrange
            var go = new GameObject("Cat");
            var cat = go.AddComponent<FeralCatAI>();
            cat.Initialize(_catData);
            EnemyState oldState = EnemyState.Idle;
            EnemyState newState = EnemyState.Idle;
            cat.OnStateChanged += (from, to) => { oldState = from; newState = to; };

            // Act
            cat.TransitionTo(EnemyState.Chase);

            // Assert
            Assert.AreEqual(EnemyState.Idle, oldState);
            Assert.AreEqual(EnemyState.Chase, newState);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void CleanupSceneEntities_DestroysAllEntityGameObjects()
        {
            // Arrange
            new GameObject("Player").AddComponent<Entity>();
            new GameObject("Enemy").AddComponent<FeralCatAI>();

            // Act
            CleanupSceneEntities();

            // Assert
            Assert.That(UnityEngine.Object.FindObjectsOfType<Entity>(), Is.Empty);
        }
    }
}
