# Co-op Run State

## Goal
Shared lives pool, shared Treats counter, and partner revive system for 2-player co-op.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/RunStateTests.cs pass
- [ ] Public API matches stubs in Code/Core/RunState.cs, ReviveSystem.cs
- [ ] InitializeRun sets starting state correctly
- [ ] AddTreats increments and fires event
- [ ] ConsumeLife decrements lives, returns false when empty
- [ ] Both players dead + no lives = game over event
- [ ] Drop-in adds player without resetting Treats
- [ ] Revive timer progresses on proximity, completes after ReviveTime
- [ ] Revive does not progress when out of range

## Tests to Pass
- InitializeRun_SetsStartingState
- AddTreats_IncreasesTotal
- AddTreats_FiresOnTreatsChangedEvent
- ConsumeLife_DecrementsLives
- ConsumeLife_NoLivesRemaining_ReturnsFalse
- OnPlayerDied_BothDead_NoLives_FiresGameOver
- OnPlayerDropIn_IncreasesPlayerCount_DoesNotResetTreats
- ReviveSystem_ProximityCountsDown
- ReviveSystem_CompletesRevive_FiresEvent
- ReviveSystem_OutOfRange_DoesNotProgress

## Dependencies
- Issue #2 (Entity & Components — player entity death events)
- Issue #5 (Player Controller — player death/revive state)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- RunState is a ScriptableObject — lives across scenes within a single run but resets on new run.
- Treats are split automatically on pickup per gameplay doc. Implementation: when a Treat is collected, call AddTreats(amount) on RunState. Both players benefit.
- Revive system: each frame, check distance between alive and downed player. If within ReviveRange, tick the timer. If timer >= ReviveTime, fire OnReviveComplete and restore the downed player.
- Game over condition: both players in Dead state AND LivesRemaining == 0.
- Drop-in: P2 presses a button to join mid-run. Active player count increases, Treats are preserved, a life is consumed to spawn P2.
