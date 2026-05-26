# Unity Test Isolation — Fixture Leakage & Deterministic Timing

**Date:** 2026-05-25
**Context:** Late in the Milestone 1 sprint, multiple PlayMode and EditMode tests began failing in ways that didn't reproduce in isolation. The bugs were entirely in the test infrastructure, not the production code. This doc captures the patterns that caused the failures and the patterns that fixed them.

## Symptom 1: Leaked GameObjects Cross-Contaminate Tests

`SpawnManager_SpawnCurrentWave_InstantiatesEnemiesAtPositions` expected 3 enemies, observed 5. The spawn logic was correct — the extra two were leaked from a prior test in the same fixture class that didn't clean up its spawned enemies.

Unity's test runner does **not** reset the scene between `[UnityTest]` methods in the same `[TestFixture]`. `GameObject`s created via `new GameObject(...)`, `Instantiate(...)`, or any `SpawnManager.SpawnCurrentWave()` call persist into the next test.

`FindObjectsOfType<EnemyAI>()` (used in spec-style assertions) sees all of them.

### Fix: `[UnityTearDown]` that destroys per-type leftovers

```csharp
[UnityTearDown]
public IEnumerator TearDown()
{
    var enemies = Object.FindObjectsOfType<EnemyAI>();
    foreach (var enemy in enemies)
    {
        Object.Destroy(enemy.gameObject);
    }
    yield return null;  // let Destroy() complete before next test starts
}
```

The `yield return null` matters. `Destroy()` is deferred to end-of-frame; without yielding, the next test's `[UnitySetUp]` runs before the destroys process, and you've leaked from the cleanup itself.

### Why not `DestroyImmediate`?

`DestroyImmediate` works in EditMode but in PlayMode it can break MonoBehaviour lifecycle (OnDestroy/OnDisable timing). Default to `Destroy()` + yield in PlayMode TearDown.

### Scope of the cleanup

A fixture-level `[UnityTearDown]` only cleans up tests in its own class. If another test class leaks `EnemyAI` objects, that leak still contaminates this fixture. Two options:
- Add `[UnityTearDown]` to **every** fixture that spawns the type. (Done for `SpawnManagerPlayModeTests`.)
- Add a `[OneTimeSetUp]` to also clean up leftovers from earlier fixtures.

For Corgi Commando we picked the per-fixture pattern — it's the simplest thing that works without coupling test classes.

## Symptom 2: External "Dummy" Objects Break Death-Tracking Tests

The original `SceneBootstrap_OnEnemyDied_UnregistersEnemyFromTicking` test did:

```csharp
var enemyGo = new GameObject("Enemy");
var enemy = enemyGo.AddComponent<EnemyAI>();
enemy.Initialize(fixture.EnemyData);
fixture.SpawnManager.NotifyEnemySpawned(enemy);
fixture.SpawnManager.NotifyEnemyDied(enemy);
```

This worked when `NotifyEnemyDied` was a dumb counter increment. After PR #54 made `TryRegisterEnemyDeath` strict (only enemies actually in `_lowHpTriggerCohort` count), the dummy enemy was rejected — and the test silently asserted the wrong thing.

### Fix: Capture a real spawned enemy via event, drive death through the real pipeline

```csharp
EnemyAI capturedEnemy = null;
fixture.SpawnManager.OnEnemySpawned += e => { capturedEnemy ??= e; };

InvokePrivate(fixture.Bootstrap, "Start");
int initialCount = fixture.Bootstrap.ActiveEnemyCount;

Assert.That(capturedEnemy, Is.Not.Null, "Expected at least one enemy to be spawned by Start().");
var health = capturedEnemy.GetEntityComponent<IHealthComponent>();
Assert.That(health, Is.Not.Null);
health.TakeDamage(int.MaxValue);

Assert.AreEqual(initialCount - 1, fixture.Bootstrap.ActiveEnemyCount);
```

Key principles:
- **Use the production path, not the back door.** `health.TakeDamage(int.MaxValue)` triggers the real death event chain. `NotifyEnemyDied(externalEnemy)` is a back door that bypasses validation.
- **Capture real objects, don't fabricate fakes.** The fake had no `IHealthComponent`, no registration with the cohort tracker, no spawn-group membership — none of the state the production code relies on.
- **`??=` for first-capture.** Only grab the first spawn; ignore subsequent spawns from the same Start().

## Symptom 3: First-Frame `Time.deltaTime == 0` Flakiness

`SceneBootstrap_PressMoveRight_CorgiTranslates` flaked because the first PlayMode update after `[UnityTest]` setup can have `Time.deltaTime == 0`. `KinematicMovementController.Tick` multiplies velocity × deltaTime, so zero-frame inputs produce zero translation.

### Anti-pattern: retry-loop assertion

```csharp
// DON'T do this
for (int retryCount = 0; retryCount < 3 && player.transform.position.x <= startX; retryCount++)
{
    yield return null;
}
Assert.Greater(player.transform.position.x, startX);
```

This is asserting "eventually true," which:
- Hides the actual contract (how long should it take?)
- Passes even if the system is broken but happens to update on the 3rd frame
- Conflates "didn't move yet" with "won't move"

### Fix: deterministic wait

```csharp
private const float MovementObservationDelaySeconds = 0.02f;

inputBuffer.RecordInput(InputAction.MoveRight, Time.time, new Vector2(1f, 0f));
Assert.AreEqual(new Vector2(1f, 0f), inputBuffer.GetMoveAxis());

yield return new WaitForSeconds(MovementObservationDelaySeconds);

Assert.Greater(player.transform.position.x, startX);
```

Wait one tick's worth of real time. If the player hasn't moved by then, the bug is real, not a timing flake. The intermediate `inputBuffer.GetMoveAxis()` assertion also pinpoints which half of the pipeline broke (recording vs translation).

## Symptom 4: Input Axis Fallback for Test-Driven Movement

Test code calling `inputBuffer.RecordInput(InputAction.MoveRight, Time.time)` (no axis value) recorded `(0, 0)` as the move axis — semantically "press the move-right button but with zero magnitude." Production input handlers always pass an axis value, so this only bit tests.

### Fix: directional fallback inside `RecordInput`

```csharp
private static Vector2 ResolveMoveAxis(InputAction action, Vector2 axisValue)
{
    if (axisValue != Vector2.zero) return axisValue;
    return action switch
    {
        InputAction.MoveLeft => Vector2.left,
        InputAction.MoveRight => Vector2.right,
        InputAction.MoveUp => Vector2.up,
        InputAction.MoveDown => Vector2.down,
        _ => axisValue
    };
}
```

This treats "MoveRight with zero axis" as "MoveRight at unit magnitude" — matches the semantic intent of the action. Doesn't change production behavior (production always passes non-zero axis). Tests now work whether they pass an axis or not.

## General Principles

1. **Tests don't get scene resets for free.** Assume leftovers; clean up explicitly.
2. **Use production paths in tests, not back doors.** Reviewers will tighten the back doors over time; tests that depend on them break.
3. **Prefer deterministic waits over retry loops.** Retry loops hide bugs and make timing contracts implicit.
4. **Input contracts should match real-world callers.** If real input handlers always pass axis values, accept the test-shaped call too — don't make tests jump through hoops the production code doesn't require.
5. **`yield return null` after `Destroy()` in PlayMode TearDown.** Destruction is deferred.
