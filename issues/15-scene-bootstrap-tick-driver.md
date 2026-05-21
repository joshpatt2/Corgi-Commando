# Scene Bootstrap & Per-Frame Tick Driver

## Goal
Make `Level_Backyard.unity` actually playable end-to-end by introducing a `SceneBootstrap` MonoBehaviour that wires runtime dependencies and drives every per-frame `Tick(float)` in a deterministic order. The codebase has all the systems (#1–#13) but **no per-frame driver** — every controller exposes a public `Tick(float deltaTime)` method that nothing calls. Without this, pressing input has no effect even after `Sarge.asset` is wired into the player prefab (#29 / #24).

## Acceptance Criteria
- [ ] New `Assets/_Project/Code/Core/SceneBootstrap.cs` MonoBehaviour
- [ ] Single `SceneBootstrap` GameObject placed in `Level_Backyard.unity`
- [ ] Bootstrap acquires references on `Start()`: P1 `CorgiController`, `CombatSystem`, `SpawnManager`, `ArenaCameraLock`, `GroupTargetCamera`, `ReviveSystem`, `RunState` (a runtime-created `ScriptableObject.CreateInstance<RunState>()`)
- [ ] Bootstrap kicks off the encounter on `Start()`: `runState.InitializeRun(3, 1)`, `spawnManager.StartEncounter(BackyardWave1.asset)`, `spawnManager.SpawnCurrentWave()`
- [ ] Bootstrap calls `CorgiController.Tick(Time.deltaTime)` every `Update()`
- [ ] Bootstrap calls `EnemyAI.Tick(Time.deltaTime)` for every alive enemy each frame (registers/unregisters on `SpawnManager.OnEnemySpawned` / `OnEnemyDied`)
- [ ] Bootstrap calls `CombatSystem.Tick(Time.deltaTime)` each frame
- [ ] Bootstrap calls `GroupTargetCamera.Tick(Time.deltaTime)` or uses `LateUpdate` for camera framing
- [ ] Bootstrap calls `ReviveSystem.Tick(...)` per frame when a player is downed (gated on game state)
- [ ] Deterministic execution order: **input gather → combat hits → controllers (P1, then enemies) → camera** (camera runs in `LateUpdate`)
- [ ] PlayMode test `SceneBootstrap_PressMoveRight_CorgiTranslates` that simulates input via `IInputBuffer.RecordInput` and asserts `CorgiController.transform.position.x` increased after a tick

## Tests to Pass
- SceneBootstrap_OnStart_InitializesRunStateAndEncounter
- SceneBootstrap_OnEnemySpawned_RegistersEnemyForTicking
- SceneBootstrap_OnEnemyDied_UnregistersEnemyFromTicking
- SceneBootstrap_PressMoveRight_CorgiTranslates (PlayMode)
- SceneBootstrap_TickOrder_InputBeforeControllersBeforeCamera

## Dependencies
- Issue #5 (Player Controller) — ✅ merged
- Issue #6 (Enemy AI) — ✅ merged
- Issue #7 (Camera) — ✅ merged
- Issue #8 (Spawn & Wave Management) — ✅ merged
- Issue #11 (Co-op Run State) — ✅ merged
- Issue #24 (Vertical Slice Content Authoring) — 🔄 in PR #29; the bootstrap consumes `Sarge.asset` and `BackyardWave1.asset` from #29 once merged. Authoring can land in parallel but full smoke test needs #29.

## Notes for Implementer

### Why this is needed
Every `Tick(float deltaTime)` method in `Code/Core`, `Code/Combat`, `Code/Enemies`, `Code/Player` is **public but uncalled**:

```
Assets/_Project/Code/Core/KinematicMovementController.cs    Tick(float)
Assets/_Project/Code/Core/SpawnManager.cs                   (no Tick — event-driven)
Assets/_Project/Code/Combat/CombatSystem.cs                 Tick(float)
Assets/_Project/Code/Enemies/EnemyAI.cs                     Tick(float)
Assets/_Project/Code/Enemies/FeralCatAI.cs                  Tick(float) override
Assets/_Project/Code/Enemies/RaccoonBanditAI.cs             Tick(float) override
Assets/_Project/Code/Enemies/SprinklerTurretAI.cs           Tick(float) override
Assets/_Project/Code/Enemies/WhiskerbotController.cs        Tick(float) — currently empty
Assets/_Project/Code/Player/CorgiController.cs              Tick(float)
```

The architecture intentionally separates "what to do per-frame" (controllers) from "when to do it" (a scene-level orchestrator). The orchestrator never got built.

### Architecture sketch
```csharp
public class SceneBootstrap : MonoBehaviour
{
    [SerializeField] private WaveData _waveData;          // BackyardWave1.asset
    [SerializeField] private CorgiController _p1;
    [SerializeField] private SpawnManager _spawnManager;
    [SerializeField] private GroupTargetCamera _camera;
    // ... other refs

    private RunState _runState;
    private CombatSystem _combatSystem;
    private ReviveSystem _reviveSystem;
    private readonly List<EnemyAI> _activeEnemies = new();

    private void Start()
    {
        _runState = ScriptableObject.CreateInstance<RunState>();
        _runState.InitializeRun(3, 1);
        _combatSystem = new CombatSystem();
        _reviveSystem = new ReviveSystem();

        _spawnManager.OnEnemySpawned += RegisterEnemy;
        _spawnManager.OnEnemyDied += UnregisterEnemy;
        _spawnManager.StartEncounter(_waveData);
        _spawnManager.SpawnCurrentWave();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _p1?.Tick(dt);
        for (int i = 0; i < _activeEnemies.Count; i++) _activeEnemies[i].Tick(dt);
        _combatSystem.Tick(dt);
        // ReviveSystem.Tick when downed player exists
    }

    private void LateUpdate()
    {
        _camera?.Tick(Time.deltaTime);
    }
}
```

### Things to verify in the implementation
- `SpawnManager` may not currently have `OnEnemySpawned` event — check and add if missing.
- `CombatSystem` is `new`-able (POCO) or `MonoBehaviour`? Verify and use the right pattern.
- Pause: `Time.timeScale = 0` (from #12 UI's pause toggle) will naturally stop everything via `Time.deltaTime`. No special handling needed.
- The Player prefab's `CorgiController` auto-initializes via `Awake → TryAutoInitialize()` if `_characterData` is serialized (added in #29). Don't duplicate that — the bootstrap just needs to start ticking.

### Out of scope
- UI rendering — that's #12
- Player join logic for P2 drop-in — handled separately when co-op gets exercised
- Audio / VFX hookups — separate concern
