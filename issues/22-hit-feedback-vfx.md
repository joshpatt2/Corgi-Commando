# 22. Hit Feedback VFX

## Goal
Make hits feel like hits. Subscribe to `CombatSystem.OnHitConnected` and `HealthComponent.OnDamaged` to spawn a hitspark VFX and flash the target's sprite. Per gameplay doc:
> Hitspark VFX: Different color per hit type (white = light, yellow = heavy, rainbow = special)
> Damage flash for colorblind-friendly hit feedback

Today `OnHitConnected` and `OnDamaged` events fire correctly but nothing visual responds.

## Acceptance Criteria

### `HitsparkSpawner`
- [ ] New `Assets/_Project/Code/Combat/HitsparkSpawner.cs` MonoBehaviour
- [ ] Subscribes to `ICombatSystem.OnHitConnected`
- [ ] On hit, spawns `AttackData.hitsparkPrefab` (from issue #20) at `HitResult.HitPosition`, tinted by `AttackData.hitsparkColor` if set, else by `HitResult.HitType`:
  - `Light` → white
  - `Heavy` → yellow
  - `Special` → magenta (rainbow shader is a follow-up; magenta for now per existing AttackData notes)
- [ ] **Object pool** for hitsparks — at least 16 pre-instantiated, reused via a queue. Zero `Instantiate` in the hot path after warmup.
- [ ] Spark auto-recycles to the pool after `lifetimeSeconds = 0.3f` (configurable via SerializeField)
- [ ] If `hitsparkPrefab` is null on the `AttackData`, log once and no-op for that attack

### `SpriteFlashOnHit`
- [ ] New `Assets/_Project/Code/Core/SpriteFlashOnHit.cs` MonoBehaviour (Entity-attached)
- [ ] Auto-acquires sibling `SpriteRenderer` (child of Entity GameObject)
- [ ] Subscribes to the Entity's `HealthComponent.OnDamaged`
- [ ] On damage, lerps SpriteRenderer color from white to red (or `CorgiData.spriteFlashIntensity` / `EnemyData.spriteFlashIntensity` controlled white→damage mix) over `flashDurationSeconds = 0.1f`, then back over the same duration
- [ ] Coroutine-driven; cancels and restarts cleanly on rapid successive hits
- [ ] Does not affect SpriteRenderer color if no health component present

### Wiring
- [ ] Add `SpriteFlashOnHit` to `Player.prefab`
- [ ] Document in `Art/README.md` that enemy prefabs need this component
- [ ] Add a single `HitsparkSpawner` GameObject to `Level_Backyard.unity` (or attach to `SceneBootstrap` when #32 lands) — must exist exactly once per scene so the pool is shared

## Tests to Pass
- HitsparkSpawner_OnHitConnected_SpawnsSparkAtHitPosition
- HitsparkSpawner_PoolReusedAfterLifetime
- HitsparkSpawner_NullHitsparkPrefab_DoesNotThrow
- HitsparkSpawner_LightHit_TintsWhite
- HitsparkSpawner_HeavyHit_TintsYellow
- HitsparkSpawner_SpecialHit_TintsMagenta
- HitsparkSpawner_AttackDataOverrideColor_TakesPrecedence
- SpriteFlashOnHit_OnDamaged_FlashesSprite
- SpriteFlashOnHit_RapidHits_DoesNotStackCoroutines
- SpriteFlashOnHit_NoHealthComponent_NoOp
- SpriteFlashOnHit_NoSpriteRenderer_NoOp

## Dependencies
- Issue #19 (Art Pipeline Foundation) — sprite/sort layers wired
- Issue #20 (Art-Aware Data SOs) — `AttackData.hitsparkPrefab`, `hitsparkColor`; `CorgiData/EnemyData.spriteFlashIntensity`
- Issue #4 (Combat System) — ✅ merged; provides `OnHitConnected`
- Issue #2 (Entity & Components) — ✅ merged; provides `HealthComponent.OnDamaged`

## Notes for Implementer

### Hitspark prefab
A placeholder hitspark prefab (`Assets/_Project/Art/VFX/Hitspark_Placeholder.prefab`) should be created with:
- `SpriteRenderer` on `Gameplay` sort layer, sort order = 10 (above characters)
- Built-in white square sprite (Unity builtin) — will be replaced with real art later
- No `Animator` needed initially — the lifetime-based recycle is the "animation"

### Pool implementation
```csharp
private readonly Queue<GameObject> _pool = new();

private GameObject Acquire() {
    if (_pool.Count > 0) {
        var go = _pool.Dequeue();
        go.SetActive(true);
        return go;
    }
    return Instantiate(_hitsparkPrefab);
}

private void Release(GameObject go) {
    go.SetActive(false);
    _pool.Enqueue(go);
}
```

### Combat-system access pattern
`CombatSystem` is currently `new`'d by `SceneBootstrap` (per #32). `HitsparkSpawner` needs the same `CombatSystem` instance. Two options:
1. **Service-locator:** `HitsparkSpawner` finds `SceneBootstrap` via `FindObjectOfType` and reads `CombatSystem`
2. **Bootstrap-driven:** `SceneBootstrap` injects itself into `HitsparkSpawner` after creating CombatSystem

Option 2 is cleaner. Add `public void Initialize(ICombatSystem combatSystem)` on `HitsparkSpawner` and call it from `SceneBootstrap.Start()`. The `SceneBootstrap` issue (#32) is in flight as PR #32 — coordinate with that PR's author or land this after #32 merges.

### Special-hit "rainbow"
Gameplay doc says rainbow for Special. That implies a shader or per-frame color cycle, which is out of scope here. Magenta is a clear "this is a Special hit" placeholder. A `HitsparkRainbowController` MonoBehaviour can be added later as a sibling on the special-hit prefab variant.

### Out of scope
- Audio feedback (`AttackData.hitSound`) — slotted in #20 for future audio pass
- Screen shake — separate concern (Cinemachine impulse)
- Damage number popups — separate VFX issue
- Real hitspark animation frames — content work after pipeline lands
