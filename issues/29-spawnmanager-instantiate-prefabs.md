# SpawnManager — Instantiate Enemy Prefabs from SpawnGroup Data

## Goal
Replace the count-only `SpawnCurrentWave` stub with real enemy prefab instantiation. Today the encounter "starts" by tracking `AliveEnemyCount` but no enemy GameObjects actually appear in the scene. The Backyard scene is empty even after the Scene Bootstrap (PR #32 / Issue #31) kicks off the wave.

## Background
- PR #19 (Issue #8) implemented `SpawnManager` as **wave-state tracking only**. Its docstring explicitly says: *"Enemy instantiation (prefab spawning at SpawnGroup positions) is not yet implemented — callers drive AliveEnemyCount transitions via OnEnemyDied."*
- PR #32 (Issue #31) closes the per-frame Tick driver loop but ticks an empty `_activeEnemies` list because no enemies are ever instantiated.
- `WaveData.SpawnGroup` already has `enemyData` (EnemyData ScriptableObject reference), `count`, and `spawnPosition` — the schema for spawning is complete; only the runtime code is missing.

## Acceptance Criteria
- [ ] `SpawnManager.SpawnCurrentWave()` instantiates one GameObject per `SpawnGroup.count` at `SpawnGroup.spawnPosition` (with small per-instance offset so they don't overlap)
- [ ] Each instantiated GameObject has the appropriate `EnemyAI` subclass component attached via `gameObject.AddComponent<...>()`, then `Initialize(enemyData)`:
  - `EnemyBehaviorPreset.FeralCat` → `FeralCatAI`
  - `EnemyBehaviorPreset.Raccoon` → `RaccoonBanditAI`
  - `EnemyBehaviorPreset.Sprinkler` → `SprinklerTurretAI`
- [ ] Each instantiated GameObject has a `KinematicMovementController` (so the AI can drive it)
- [ ] Each instantiated GameObject has a `SpriteRenderer` with a 1×1 solid-color sprite using `EnemyData.placeholderColor` (prototype visual per `README.md`)
- [ ] Each instantiated GameObject has a `HurtboxComponent` registered on its `Entity` so the combat system can find it (see [[30-combat-resolveattack-wiring]] for why this matters)
- [ ] Spawning fires `SpawnManager.OnEnemySpawned(enemy)` for the bootstrap to register the enemy and add it to the tick loop
- [ ] `AliveEnemyCount` accurately reflects instantiated enemies after `SpawnCurrentWave()`
- [ ] Existing `SpawnManager` tests still pass (count tracking should not regress)

## Tests to Pass
- `SpawnManager_SpawnCurrentWave_InstantiatesEnemiesAtPositions` (PlayMode — verifies actual GameObjects exist after spawn)
- `SpawnManager_SpawnCurrentWave_FiresOnEnemySpawned` (EditMode — counts event invocations)
- `SpawnManager_SpawnCurrentWave_AttachesCorrectAIType` (EditMode — maps preset → component type)
- `SpawnManager_SpawnCurrentWave_AttachesMovementAndHurtbox` (EditMode)

## Dependencies
- All upstream merged. Ready to implement.

## Notes for Implementer
- **Enemy type mapping:** `EnemyData.behaviorPreset` (enum) already exists. Use a switch in `SpawnManager` that maps `BehaviorPreset` → `Type` and calls `gameObject.AddComponent(type)`. Keep the mapping in `SpawnManager` rather than scattering it across data assets.
- **Per-instance offset:** for `count > 1`, space instances along the X axis (e.g., `spawnPosition + new Vector3(i * 1.5f, 0, 0)`) so they don't z-fight or stack.
- **Placeholder visual:** create a `Texture2D(1, 1)` filled with `enemyData.placeholderColor` at runtime, wrap in a `Sprite.Create(...)`, assign to a `SpriteRenderer`. Simple, no asset dependencies.
- **Hurtbox component:** call `entity.AddEntityComponent(new HurtboxComponent())`. The bounds can be a default 1×1 rect; specific sizing can be tuned later.
- **Out of scope:** authored enemy prefabs in `Assets/_Project/Prefabs/`. The current design is programmatic instantiation. When proper prefabs land (Phase A art per #19-22), this code switches from `new GameObject(...).AddComponent` to `Instantiate(prefab)` — but that's a future migration, not this issue.
- **Don't forget:** SpawnManager currently has no `EnemyAI` import. Add `using CorgiCommando.Enemies;`.

## Related
- [[15-scene-bootstrap-tick-driver]] — bootstrap consumes spawned enemies via `OnEnemySpawned`
- [[30-combat-resolveattack-wiring]] — combat needs the hurtbox registration this issue adds
