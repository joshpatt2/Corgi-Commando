using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Player;
using CorgiCommando.Combat;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Edit Mode tests for CorgiController state machine, combo chains,
    /// and special meter consumption. Pure state logic.
    /// </summary>
    [TestFixture]
    public class PlayerControllerTests
    {
        private GameObject _playerGo;
        private CorgiController _player;
        private CorgiData _corgiData;
        private AttackData _punch;
        private AttackData _punch2;
        private AttackData _kick;
        private InputBuffer _inputBuffer;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("Player");
            _player = _playerGo.AddComponent<CorgiController>();

            _punch = ScriptableObject.CreateInstance<AttackData>();
            _punch.attackName = "Punch1";
            _punch.startupFrames = 2;
            _punch.activeFrames = 3;
            _punch.recoveryFrames = 4;
            _punch.comboWindowFrames = 10;
            _punch.damage = 10;

            _punch2 = ScriptableObject.CreateInstance<AttackData>();
            _punch2.attackName = "Punch2";
            _punch2.startupFrames = 2;
            _punch2.activeFrames = 3;
            _punch2.recoveryFrames = 5;
            _punch2.comboWindowFrames = 10;
            _punch2.damage = 12;

            _kick = ScriptableObject.CreateInstance<AttackData>();
            _kick.attackName = "Kick";
            _kick.startupFrames = 3;
            _kick.activeFrames = 4;
            _kick.recoveryFrames = 8;
            _kick.comboWindowFrames = 0; // finisher, no chain
            _kick.damage = 20;
            _kick.causesKnockdown = true;

            _corgiData = ScriptableObject.CreateInstance<CorgiData>();
            _corgiData.corgiName = "Sarge";
            _corgiData.maxHP = 100;
            _corgiData.comboChain = new[] { _punch, _punch2, _kick };
            _corgiData.maxSpecialMeter = 100f;
            _corgiData.specialCost = 100f;

            _inputBuffer = new InputBuffer();

            _player.Initialize(_corgiData, _inputBuffer, 0);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_playerGo);
            UnityEngine.Object.DestroyImmediate(_punch);
            UnityEngine.Object.DestroyImmediate(_punch2);
            UnityEngine.Object.DestroyImmediate(_kick);
            UnityEngine.Object.DestroyImmediate(_corgiData);
        }

        [Test]
        public void InitialState_IsIdle()
        {
            // Assert
            Assert.AreEqual(CorgiState.Idle, _player.CurrentState);
        }

        [Test]
        public void TransitionTo_IdleToWalk_Succeeds()
        {
            // Act
            bool result = _player.TransitionTo(CorgiState.Walk);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(CorgiState.Walk, _player.CurrentState);
        }

        [Test]
        public void TransitionTo_IdleToAttack1_Succeeds()
        {
            // Act
            bool result = _player.TransitionTo(CorgiState.Attack1);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(CorgiState.Attack1, _player.CurrentState);
        }

        [Test]
        public void TransitionTo_Attack1ToAttack2_DuringComboWindow()
        {
            // Arrange — enter Attack1 first
            _player.TransitionTo(CorgiState.Attack1);

            // Act — chain to Attack2 (within combo window)
            // Design intent: Attack1 → Attack2 is valid during the combo window frames
            bool result = _player.TransitionTo(CorgiState.Attack2);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(CorgiState.Attack2, _player.CurrentState);
        }

        [Test]
        public void TransitionTo_FiresOnStateChangedEvent()
        {
            // Arrange
            CorgiState oldState = CorgiState.Idle;
            CorgiState newState = CorgiState.Idle;
            _player.OnStateChanged += (from, to) => { oldState = from; newState = to; };

            // Act
            _player.TransitionTo(CorgiState.Walk);

            // Assert
            Assert.AreEqual(CorgiState.Idle, oldState);
            Assert.AreEqual(CorgiState.Walk, newState);
        }

        [Test]
        public void GetCurrentAttackData_ReturnsCorrectDataForComboStep()
        {
            // Arrange — enter first attack
            _player.TransitionTo(CorgiState.Attack1);

            // Act
            var data = _player.GetCurrentAttackData();

            // Assert — should be the first attack in combo chain
            Assert.AreEqual("Punch1", data.attackName);
            Assert.AreEqual(10, data.damage);
        }

        [Test]
        public void UseSpecial_WithFullMeter_ConsumesAndReturnsTrue()
        {
            // Arrange — fill meter through public gameplay API
            _player.AddSpecialMeter(_corgiData.maxSpecialMeter);

            // Meter readiness is computed by UseSpecial() from current meter/data.
            bool result = _player.UseSpecial();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0f, _player.SpecialMeter);
            Assert.AreEqual(CorgiState.Special, _player.CurrentState);
        }

        [Test]
        public void AddSpecialMeter_WhenCalled_ClampsToMaxAndMarksSpecialReady()
        {
            // Act
            _player.AddSpecialMeter(_corgiData.maxSpecialMeter + 50f);

            // Assert
            Assert.AreEqual(_corgiData.maxSpecialMeter, _player.SpecialMeter);
            Assert.IsTrue(_player.IsSpecialReady);
        }

        [Test]
        public void AddSpecialMeter_WithNegativeAmount_Throws()
        {
            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _player.AddSpecialMeter(-1f));
        }

        [Test]
        public void Initialize_WithInvalidPlayerIndex_Throws()
        {
            // Arrange
            var otherGo = new GameObject("OtherPlayer");
            var otherPlayer = otherGo.AddComponent<CorgiController>();

            try
            {
                // Act / Assert
                Assert.Throws<ArgumentOutOfRangeException>(() => otherPlayer.Initialize(_corgiData, _inputBuffer, 2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(otherGo);
            }
        }

        [Test]
        public void Tick_WithMoveInput_DelegatesToMovementController()
        {
            // Arrange
            var movement = _playerGo.AddComponent<KinematicMovementController>();
            // Re-initialize so CorgiController can cache the movement component added by this test.
            _player.Initialize(_corgiData, _inputBuffer, 0);
            _inputBuffer.RecordInput(InputAction.MoveRight, 0f, new Vector2(1f, 0f));

            // Act
            _player.Tick(1f / 60f);

            // Assert
            Assert.Greater(movement.Velocity.x, 0f);
            Assert.AreEqual(CorgiState.Walk, _player.CurrentState);
        }

        [Test]
        public void Tick_WhenNotAttacking_DecaysSpecialMeter()
        {
            // Arrange
            _player.AddSpecialMeter(50f);
            _corgiData.specialDecayRate = 5f;

            // Act
            _player.Tick(1f);

            // Assert
            Assert.AreEqual(45f, _player.SpecialMeter, 0.01f);
        }

        [Test]
        public void TransitionTo_InvalidTransition_ReturnsFalse()
        {
            // Arrange — in Idle state
            // Design intent: cannot go directly from Idle to GetUp (must be knocked down first)
            bool result = _player.TransitionTo(CorgiState.GetUp);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(CorgiState.Idle, _player.CurrentState);
        }

        [Test]
        public void OnHit_TransitionsToHitState()
        {
            // Arrange
            var hitResult = new HitResult
            {
                DidHit = true,
                DamageDealt = 10,
                KnockbackApplied = new Vector3(2f, 0f, 0f),
                HitstopFrames = 4
            };

            // Act
            _player.OnHit(hitResult);

            // Assert
            Assert.AreEqual(CorgiState.Hit, _player.CurrentState);
        }
    }
}
