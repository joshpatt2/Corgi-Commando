# Player Controller

## Goal
State machine for the player corgi: reads from InputBuffer, drives combo chains, manages special meter consumption.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/PlayerControllerTests.cs pass
- [ ] Public API matches stubs in Code/Player/CorgiController.cs, CorgiState.cs
- [ ] State machine enforces valid transitions only
- [ ] Combo chain: Punch → Punch → Kick (Attack1 → Attack2 → Attack3)
- [ ] GetCurrentAttackData returns correct AttackData per combo step
- [ ] UseSpecial fails if meter not full, succeeds and consumes meter if full
- [ ] OnHit transitions to Hit state
- [ ] OnStateChanged event fires on every transition

## Tests to Pass
- InitialState_IsIdle
- TransitionTo_IdleToWalk_Succeeds
- TransitionTo_IdleToAttack1_Succeeds
- TransitionTo_Attack1ToAttack2_DuringComboWindow
- TransitionTo_FiresOnStateChangedEvent
- GetCurrentAttackData_ReturnsCorrectDataForComboStep
- UseSpecial_WithFullMeter_ConsumesAndReturnsTrue
- TransitionTo_InvalidTransition_ReturnsFalse
- OnHit_TransitionsToHitState

## Dependencies
- Issue #1 (Input Abstraction — reads from InputBuffer)
- Issue #2 (Entity & Components — extends Entity)
- Issue #3 (Movement Controller — delegates movement)
- Issue #4 (Combat System — attack resolution, special meter)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- State transition rules: Idle/Walk can go to Attack1, Jump, Special. Attack1 can chain to Attack2 during combo window. Attack2 chains to Attack3. Hit can go to Knockdown or Idle (recovery). Knockdown goes to GetUp. GetUp goes to Idle.
- ComboStep tracks position in the CorgiData.comboChain array. Reset on combo break.
- CorgiController.Tick() should: read input buffer → decide state transition → update current state → delegate to movement/combat.
- The combo window is defined per-attack in AttackData.comboWindowFrames. During recovery frames + combo window, the next attack input is accepted.
