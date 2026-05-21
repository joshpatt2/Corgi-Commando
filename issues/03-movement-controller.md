# Movement Controller

## Goal
Provide custom kinematic 2.5D movement: X horizontal, Z depth, Y jump with gravity — no Rigidbody2D dynamics.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/MovementControllerTests.cs pass
- [ ] Public API matches stub in Code/Core/KinematicMovementController.cs
- [ ] Input vector produces correct velocity on X and Z axes
- [ ] Gravity applied when airborne, Y velocity decreases each frame
- [ ] Jump sets Y velocity when grounded, ignored when airborne
- [ ] SpeedMultiplier scales movement (used for env weapon slow)
- [ ] ApplyExternalVelocity overrides current velocity (for knockback)
- [ ] No Rigidbody2D — pure kinematic position updates

## Tests to Pass
- SetMoveInput_HorizontalInput_ProducesXVelocity
- SetMoveInput_DepthInput_ProducesZVelocity
- Tick_WhenAirborne_AppliesGravity
- Jump_WhenGrounded_SetsYVelocity
- SpeedMultiplier_AffectsMovementSpeed
- ApplyExternalVelocity_OverridesCurrentVelocity

## Dependencies
- Issue #2 (Entity Base & Components — movement controller attaches to entities)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- Movement is transform.position += velocity * dt. No physics engine.
- Input Y maps to world Z (depth). This is the 2.5D convention.
- Grounded check: raycast down from entity position or check Y <= ground plane height. Ground plane Y=0 for prototype.
- Collision with static geometry needs a separate pass (overlap check against BoxColliders). Play Mode tests cover this.
- SpeedMultiplier defaults to 1.0f. Environmental weapon pickup sets it to ~0.7f.
