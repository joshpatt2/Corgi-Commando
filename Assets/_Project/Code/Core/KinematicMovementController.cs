using System;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Custom kinematic 2.5D movement controller.
    /// X = horizontal, Z = depth into screen, Y = jump only (gravity-driven).
    /// Does NOT use Rigidbody2D dynamics — transform.position += velocity * dt.
    /// Grounded checks via raycast or overlap.
    /// </summary>
    public class KinematicMovementController : MonoBehaviour
    {
        /// <summary>Current velocity vector (X, Y, Z).</summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>Whether the entity is on the ground.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>Whether the entity is currently jumping.</summary>
        public bool IsJumping { get; private set; }

        /// <summary>Gravity acceleration applied when airborne (units/sec^2).</summary>
        public float Gravity { get; set; } = 30f;

        /// <summary>Horizontal walk speed (units/sec).</summary>
        public float WalkSpeed { get; set; } = 5f;

        /// <summary>Depth (Z-axis) walk speed (units/sec).</summary>
        public float DepthSpeed { get; set; } = 3f;

        /// <summary>Jump initial velocity.</summary>
        public float JumpForce { get; set; } = 10f;

        /// <summary>Movement speed multiplier (used for env weapon slow, etc.).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>
        /// Sets the desired movement input. Called each frame by the player controller
        /// or AI controller. X = horizontal, Y = depth (mapped to Z in world).
        /// </summary>
        /// <param name="input">Normalized 2D input vector.</param>
        public void SetMoveInput(Vector2 input)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initiates a jump if grounded.
        /// </summary>
        public void Jump()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Applies an external velocity (knockback). Overrides current velocity.
        /// </summary>
        public void ApplyExternalVelocity(Vector3 velocity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs the movement update: apply input, gravity, collision, update position.
        /// Called from FixedUpdate or a manual tick.
        /// </summary>
        /// <param name="deltaTime">Time step.</param>
        public void Tick(float deltaTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces a grounded state check. Returns true if standing on ground.
        /// </summary>
        public bool CheckGrounded()
        {
            throw new NotImplementedException();
        }
    }
}
