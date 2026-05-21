using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for KinematicMovementController logic.
    /// Tests that don't require physics/colliders are Edit Mode.
    /// Collider tests are in PlayMode/MovementControllerPlayTests.cs.
    /// </summary>
    [TestFixture]
    public class MovementControllerTests
    {
        private GameObject _go;
        private KinematicMovementController _mover;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Mover");
            _mover = _go.AddComponent<KinematicMovementController>();
            _mover.WalkSpeed = 5f;
            _mover.DepthSpeed = 3f;
            _mover.Gravity = 30f;
            _mover.JumpForce = 10f;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void SetMoveInput_HorizontalInput_ProducesXVelocity()
        {
            // Arrange & Act
            _mover.SetMoveInput(new Vector2(1f, 0f));
            _mover.Tick(1f / 60f);

            // Assert — velocity should have positive X component
            Assert.Greater(_mover.Velocity.x, 0f);
        }

        [Test]
        public void SetMoveInput_DepthInput_ProducesZVelocity()
        {
            // Arrange & Act
            // Design intent: Input Y axis maps to world Z (depth into screen)
            _mover.SetMoveInput(new Vector2(0f, 1f));
            _mover.Tick(1f / 60f);

            // Assert — velocity should have positive Z component
            Assert.Greater(_mover.Velocity.z, 0f);
        }

        [Test]
        public void Tick_WhenAirborne_AppliesGravity()
        {
            // Arrange — put entity in the air
            _go.transform.position = new Vector3(0f, 5f, 0f);
            _mover.Jump(); // start airborne

            // Act — tick for one frame
            float dt = 1f / 60f;
            float initialY = _mover.Velocity.y;
            _mover.Tick(dt);

            // Assert — Y velocity should decrease due to gravity
            Assert.Less(_mover.Velocity.y, initialY);
        }

        [Test]
        public void Jump_WhenGrounded_SetsYVelocity()
        {
            // Arrange — assume grounded (implementation must handle grounded state)
            // Design intent: Jump should set Y velocity to JumpForce when grounded
            _mover.Jump();

            // Assert
            Assert.AreEqual(_mover.JumpForce, _mover.Velocity.y, 0.01f);
        }

        [Test]
        public void SpeedMultiplier_AffectsMovementSpeed()
        {
            // Arrange
            _mover.SpeedMultiplier = 0.5f;
            _mover.SetMoveInput(new Vector2(1f, 0f));

            // Act
            _mover.Tick(1f / 60f);

            // Assert — velocity should be half of normal
            float expectedMax = _mover.WalkSpeed * 0.5f;
            Assert.LessOrEqual(Mathf.Abs(_mover.Velocity.x), expectedMax + 0.01f);
        }

        [Test]
        public void ApplyExternalVelocity_OverridesCurrentVelocity()
        {
            // Arrange
            _mover.SetMoveInput(new Vector2(1f, 0f));
            _mover.Tick(1f / 60f);

            // Act — apply knockback
            var knockback = new Vector3(-5f, 3f, 0f);
            _mover.ApplyExternalVelocity(knockback);

            // Assert
            Assert.AreEqual(knockback.x, _mover.Velocity.x, 0.01f);
            Assert.AreEqual(knockback.y, _mover.Velocity.y, 0.01f);
        }

        [Test]
        public void ApplyExternalVelocity_WhenTicked_PreservesKnockbackDirection()
        {
            // Arrange
            var knockback = new Vector3(-5f, 3f, 0f);
            _mover.ApplyExternalVelocity(knockback);

            // Act
            _mover.Tick(1f / 60f);

            // Assert
            Assert.Less(_mover.Velocity.x, 0f);
        }

        [Test]
        public void Jump_WhenAirborne_DoesNotChangeYVelocity()
        {
            // Arrange
            _go.transform.position = new Vector3(0f, 5f, 0f);
            var initialYVelocity = _mover.Velocity.y;

            // Act
            _mover.Jump();

            // Assert
            Assert.AreEqual(initialYVelocity, _mover.Velocity.y, 0.01f);
        }

        [Test]
        public void Tick_WhenLanding_SetsGroundedAndClearsJumping()
        {
            // Arrange
            _mover.Jump();

            // Act
            const float dt = 1f / 60f;
            for (int i = 0; i < 120 && !_mover.IsGrounded; i++)
            {
                _mover.Tick(dt);
            }

            // Assert
            Assert.IsTrue(_mover.IsGrounded);
            Assert.IsFalse(_mover.IsJumping);
            Assert.AreEqual(0f, _mover.Velocity.y, 0.01f);
        }
    }
}
