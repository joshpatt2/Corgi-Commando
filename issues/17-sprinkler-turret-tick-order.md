# Sprinkler Turret Tick Cycle Test Failure

## Goal
Fix `SprinklerTurret_Tick_CyclesTelegraphToAttack` regression. The turret should be in `EnemyState.Idle` after the first `Tick(FireInterval)`, but is transitioning elsewhere first.

## Failing Test
- `CorgiCommando.Tests.EditMode.EnemyAITests.SprinklerTurret_Tick_CyclesTelegraphToAttack` — expected `EnemyState.Idle`, got something else at line 377

## Root Cause Hypothesis
`SprinklerTurretAI.Tick(deltaTime)` calls `base.Tick(deltaTime)` first. The base `EnemyAI.Tick` was extended in PR #23 to add a perception loop:

```csharp
// EnemyAI.Tick
_targetSearchTimer -= deltaTime;
if (CurrentTarget == null || ... || _targetSearchTimer <= 0f)
{
    CurrentTarget = FindClosestPlayerTarget();
    _targetSearchTimer = 0.25f;
}
// ... transitions to Chase/Attack based on distance
```

For a stationary turret, this perception loop shouldn't run — turrets don't chase. The turret subclass needs to either skip `base.Tick` or override the targeting behavior.

This may share root cause with [[16-enemy-ai-test-isolation]] (stale entities from `FindObjectsOfType`). If that issue's fix lands first, this test may pass without further changes. If not, address here.

## Acceptance Criteria
- [ ] `SprinklerTurret_Tick_CyclesTelegraphToAttack` passes in the full EditMode suite
- [ ] Turret remains in `Idle` until its own cooldown timer triggers `IsTelegraphing = true`
- [ ] Turret never enters `Chase` state (stationary enemy — no chasing)
- [ ] No regression in `SprinklerTurret_HasFireInterval` (the other turret test)

## Notes for Implementer
The turret's behavior is **fundamentally different** from chasing enemies — it stays put and fires on a timer. Options:
1. Have `SprinklerTurretAI.Tick` skip `base.Tick` entirely and implement its own minimal lifecycle (Dead check, then cooldown/telegraph/attack cycle).
2. Add a `bool IsStationary` virtual property on `EnemyAI` that gates the perception loop. Subclasses opt out.

Option 2 is cleaner if other stationary enemy types are planned. Option 1 is faster.

## Dependencies
- [[16-enemy-ai-test-isolation]] — may fix this one as a side effect; check first
