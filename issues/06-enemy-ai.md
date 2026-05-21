# Enemy AI

## Goal
Lightweight FSM-based AI for all Level 1 enemies, with aggro slot management to prevent dogpiling.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/EnemyAITests.cs pass
- [ ] Public API matches stubs in Code/Enemies/EnemyAI.cs, FeralCatAI.cs, RaccoonBanditAI.cs, SprinklerTurretAI.cs, AggroSlotManager.cs, EnemyState.cs
- [ ] FSM transitions: Idle → Chase → Attack → Stunned → Recover
- [ ] AggroSlotManager limits max 2 attackers per player target
- [ ] Slot released on stun/death
- [ ] RaccoonBandit has Fleeing state with stolen Treats
- [ ] SprinklerTurret fires on interval with telegraph phase

## Tests to Pass
- EnemyAI_InitialState_IsIdle
- EnemyAI_TransitionTo_ValidTransition_Succeeds
- EnemyAI_OnHit_TransitionsToStunned
- AggroSlotManager_ReserveSlot_Succeeds
- AggroSlotManager_ExceedsMaxSlots_ReturnsFalse
- AggroSlotManager_ReleaseSlot_FreesSlot
- RaccoonBandit_StealTreats_TransitionsToFleeing
- SprinklerTurret_HasFireInterval
- EnemyAI_TransitionTo_FiresOnStateChangedEvent

## Dependencies
- Issue #2 (Entity & Components — extends Entity)
- Issue #3 (Movement Controller — enemy movement)
- Issue #4 (Combat System — attack resolution)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- FeralCatAI: simplest enemy. Idle until player in aggro range → Chase → Attack when in range → Stunned on hit → Recover → back to Chase.
- RaccoonBanditAI: same base FSM but has unique Fleeing state. When near Treats pickup, steals them and flees. Killing a fleeing raccoon drops the stolen Treats.
- SprinklerTurretAI: fixed position, no Chase state. Cycles between Idle (wait) → Telegraph (warn) → Attack (fire). FireInterval and TelegraphDuration are tunable.
- AggroSlotManager: stores a Dictionary<Entity, List<EnemyAI>>. TryReserveSlot checks count < max. ReleaseSlot removes from list.
- Enemies that can't get a slot should enter a "circling" behavior — move laterally at attack range. This is a sub-behavior of Chase, not a separate state.
