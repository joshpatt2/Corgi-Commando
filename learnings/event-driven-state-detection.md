# Event-Driven State Detection vs Polling

**Date:** 2026-05-25
**Context:** PR #54 introduced the OnLowHP reinforcement system: when the active enemy cohort's collective HP drops below a threshold, spawn the next wave's pending reinforcements. The first implementation polled `_lowHpTriggerCohort` HP every frame from `Update()`. PR #69 rewrote it to subscribe to `IHealthComponent.OnDamaged` events instead. This doc captures when each approach is appropriate and why the event-driven version is correct here.

## The Original Polling Approach

```csharp
private void Update()
{
    if (_pendingLowHpSpawnGroups.Count == 0) return;
    EvaluateLowHpSpawnGroups();  // runs every frame
}

private void EvaluateLowHpSpawnGroups()
{
    int currentHp = CalculateLowHpTriggerCohortHp();  // iterates _lowHpTriggerCohort
    if (currentHp <= _lowHpTriggerThresholdHp)
    {
        SpawnPendingGroups();
    }
}
```

This works but has problems:
- **Wasted work between damage events.** Enemies take damage at discrete moments; polling checks ~60×/sec regardless.
- **`CalculateLowHpTriggerCohortHp` walks the cohort each frame.** O(N) per frame for N=20 enemies is fine, but it's noise on the profiler.
- **Couples the trigger to `Update()` ordering.** If `SpawnManager.Update` runs before the frame's damage application, the trigger lags by one frame.

## The Event-Driven Rewrite

PR #69:

```csharp
private void SpawnGroupEnemies(SpawnGroup spawnGroup, bool includeInLowHpTriggerCohort)
{
    // ...
    if (includeInLowHpTriggerCohort)
    {
        _lowHpTriggerCohort.Add(spawnedEnemy);
        var health = spawnedEnemy.GetEntityComponent<IHealthComponent>();
        if (health != null)
        {
            health.OnDamaged += HandleCohortEnemyDamaged;
        }
    }
}

private void HandleCohortEnemyDamaged(int _)
{
    EvaluateLowHpSpawnGroups();
}
```

Plus matching unsubscribe sites in `ResetWaveState()` and `TryRegisterEnemyDeath()`:

```csharp
if (_lowHpTriggerCohort.Remove(enemy))
{
    var health = enemy.GetEntityComponent<IHealthComponent>();
    if (health != null)
    {
        health.OnDamaged -= HandleCohortEnemyDamaged;
    }
}
```

Now:
- Trigger evaluation happens **exactly when something might have changed**, not on a timer.
- Zero work between damage events.
- Frame-ordering insensitive — the event fires synchronously from `TakeDamage`, before `Update` returns.
- Edge case: damage taken on the same frame as wave-cleared logic now triggers in the same frame, no one-frame lag.

## When to Pick Which

### Use polling when:
- The condition can change without a discrete event you can subscribe to. (Position-based proximity, time-since-last-X, accumulating heuristics.)
- The condition is checked at coarse granularity (per-second, per-wave, not per-frame).
- The subscription bookkeeping would be more code than the polling logic.

### Use events when:
- A discrete trigger source already exists. (TakeDamage, OnDeath, OnButtonPressed.)
- The condition is sparse — most frames have no change.
- Latency matters at sub-frame granularity. (Combo windows, damage chains, parry timing.)

## The Subscription Bookkeeping Tax

Event-driven code has a real cost: you must unsubscribe everywhere the source object's lifecycle ends. PR #69 needed three unsubscribe sites:
1. `TryRegisterEnemyDeath` — when an enemy dies normally.
2. `ResetWaveState` — when a wave is forcibly cleared (e.g., game reset).
3. Implicit: if `_lowHpTriggerCohort.Remove(enemy)` returns false, **do not unsubscribe** — the enemy wasn't tracked, so we never subscribed in the first place.

That last point is subtle. PR #69's reviewer (me) almost missed it. The pattern that gets this right:

```csharp
if (_lowHpTriggerCohort.Remove(enemy))   // only unsubscribe if remove succeeded
{
    var health = enemy.GetEntityComponent<IHealthComponent>();
    if (health != null)
    {
        health.OnDamaged -= HandleCohortEnemyDamaged;
    }
}
```

The boolean-guarded `Remove` makes subscription and tracking-set membership stay in lock-step.

## A Subtle Bug in the Original Polling

The original code in PR #54 had an issue that's easier to see now that we've moved to events: damage that occurred *after* `_pendingLowHpSpawnGroups.Count` hit zero (because all reinforcements already spawned) was being ignored, but `EvaluateLowHpSpawnGroups` was still called every frame anyway. The early-return shaved CPU but didn't fix correctness — the correctness was already there. Switching to events made the early-return logic disappear entirely; you can only fire `HandleCohortEnemyDamaged` if you subscribed, and you only subscribe when there's a pending group. The dead state stops being representable.

This is the deeper benefit of event-driven design: not just "do less work" but "make invalid states unrepresentable." When subscription = intent, you can't accidentally evaluate a stale trigger because there's nothing wired up to fire it.

## Testing Implications

Event-driven systems need event-driven tests. PR #70's test fix uses this pattern explicitly:

```csharp
EnemyAI capturedEnemy = null;
fixture.SpawnManager.OnEnemySpawned += e => { capturedEnemy ??= e; };
// ... drive the system ...
health.TakeDamage(int.MaxValue);  // triggers the OnDeath/OnDamaged chain
```

Tests that drive via the event source are the most reliable — they exercise the same code path production will. Tests that poke private back doors (`NotifyEnemySpawned(externalObject)`) bypass the wiring you actually care about.

## Carmack-style summary

Polling is fine when the condition is continuous and you don't have a hook. Events are better when the condition is discrete and a hook exists. Most "low HP triggers next spawn" problems are events. Most "is the player near the door" problems are polling. Pick by the nature of the condition, not by stylistic preference.
