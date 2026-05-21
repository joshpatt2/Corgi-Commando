# Enemy AI Test Isolation — FindObjectsOfType Picks Up Stale Entities

## Goal
Fix two flaky EnemyAI tests that pass in isolation but fail in the EditMode suite because `EnemyAI.FindClosestPlayerTarget` uses `Object.FindObjectsOfType<Entity>()`, which scans the entire scene — including `Entity` GameObjects left behind by previous tests that didn't get fully cleaned up.

## Failing Tests
- `CorgiCommando.Tests.EditMode.EnemyAITests.FeralCatAI_Tick_WhenNoAggroSlot_RemainsInChaseAndCircles` — expects `EnemyState.Chase`, gets something else
- `CorgiCommando.Tests.EditMode.EnemyAITests.SprinklerTurret_Tick_CyclesTelegraphToAttack` — expects `EnemyState.Idle`, gets something else

Both ran green during PR #23 review (when run in isolation), but fail in the full EditMode suite (`128/138 → 130/138` if these were fixed).

## Root Cause Hypothesis
`Assets/_Project/Code/Enemies/EnemyAI.cs` `FindClosestPlayerTarget()`:
```csharp
private Entity FindClosestPlayerTarget()
{
    var entities = UnityEngine.Object.FindObjectsOfType<Entity>();
    // ... iterates all Entities in the scene
}
```

NUnit doesn't reset the scene between test methods. Entities created in `EnemyAI_TransitionTo_FiresOnStateChangedEvent` (or another upstream test) that the test forgot to `DestroyImmediate` show up in subsequent tests' `FindObjectsOfType` calls. The Sprinkler test creates no player Entity at all but still gets a non-null `CurrentTarget` from a leak — that target's position drives transitions.

## Acceptance Criteria
- [ ] Both failing tests pass in the full EditMode suite
- [ ] Add a `[TearDown]` in `EnemyAITests` that destroys all `Entity` GameObjects in the scene between tests, **OR** replace `FindObjectsOfType<Entity>` in `EnemyAI` with a registry pattern (preferred — see Notes)
- [ ] No regression in the other 8 passing EnemyAI tests

## Notes for Implementer
**Preferred fix (architectural):** Replace `FindObjectsOfType<Entity>` with a `PlayerRegistry` static class that `CorgiController` registers into on `Awake` and unregisters on `OnDestroy`. EnemyAI reads from the registry instead of scanning the scene. This is faster, more deterministic, and naturally avoids the test-leak problem. I called this out in the original PR #23 review as a follow-up — this issue is the right time to land it.

**Quick fix (test-only):** Add `[TearDown] public void CleanupEntities()` to `EnemyAITests` that finds and destroys all stray `Entity` GameObjects. This patches the symptom but leaves `FindObjectsOfType` in the production hot path.

Choose based on cost vs benefit; the registry approach is ~20–30 lines and aligns with the project's existing event-driven patterns.

## Dependencies
- None (all upstream code is on main)
