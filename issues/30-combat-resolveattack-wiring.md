# Wire Player Attacks to CombatSystem.ResolveAttack

## Goal
Connect player attack state (`CorgiController` Attack1/2/3/Special states) to `CombatSystem.ResolveAttack` so attacks actually deal damage. Today the combat infrastructure is fully built but disconnected — `CombatSystem` exists, has Z-band hit math, advances hitstop, and ticks per frame in the bootstrap (PR #32) — but **nothing calls `ResolveAttack`**, so attacks change state-machine states and never touch enemies.

## Background
- PR #17 (Issue #4) implemented `CombatSystem.ResolveAttack(...)` with Z-band tolerance, hitstop, combo tracking, special-meter helpers.
- PR #22 (Issue #5) implemented `CorgiController` with combo chain state transitions (Attack1 → Attack2 → Attack3 + Special) but the state machine only changes states.
- PR #32 (Issue #31) ticks `CombatSystem.Tick(deltaTime)` per frame (advances hitstop timer) but no code calls `ResolveAttack` to actually resolve a hit on enemies.

The hit-resolution gap is the reason "Sarge moves and attacks visually" but the FeralCats stay alive.

## Acceptance Criteria
- [ ] When `CorgiController` enters an attack state (Attack1, Attack2, Attack3, Special), a hit resolution fires **once** at the start of the active window (after `startupFrames`, not on every active frame)
- [ ] The resolution calls `CombatSystem.ResolveAttack(attacker, attackData, attackerPosition, facingDirection)` and gets back a `HitResult` list
- [ ] For each `HitResult` that landed:
  - Target Entity's `HealthComponent` takes `attackData.damage`
  - Target's `KnockbackReceiver` applies the knockback impulse (multiplied by facing direction)
  - Attacker's `CorgiController.AddSpecialMeter(CharacterData.specialGainPerHit)` is called — landing hits actually fills the meter
  - Combat hitstop is triggered for `attackData.hitstopFrames`
- [ ] `CorgiController` exposes `int Facing { get; private set; }` (1 = right, -1 = left), set from move axis sign on transitions to Walk
- [ ] Optional event `OnHitLanded(HitResult)` on `CorgiController` for future VFX/audio hookups
- [ ] PlayMode test that places Sarge + a FeralCat adjacent, ticks Sarge into Attack1, and verifies enemy HP decreases

## Tests to Pass
- `CombatSystem_PlayerPunchHitsAdjacentEnemy_DealsDamage` (PlayMode)
- `CombatSystem_PlayerAttack_FillsSpecialMeter` (PlayMode)
- `CombatSystem_PlayerAttack_MissesEnemyOutsideZBand` (verify Z-band tolerance gate)
- `CorgiController_AttackLandsHit_FiresEvent` (if `OnHitLanded` is added)
- `CorgiController_Facing_FlipsOnMoveAxisSign`

## Dependencies
- Most useful when paired with [[29-spawnmanager-instantiate-prefabs]] — otherwise there are no enemies to hit at runtime, only in tests
- All schema code on main (Entity, HealthComponent, KnockbackReceiver, HurtboxComponent, CombatSystem, AttackData)

## Notes for Implementer
- **Where the resolve call lives — three options:**
  - **(A) `CorgiController.OnEnterState(Attack1)`** schedules a coroutine that waits `attackData.startupFrames * (1f/60f)` seconds then calls `ResolveAttack`. **Recommended** — simple, minimal new types, fits the existing per-attack state-machine model.
  - **(B) A new `AttackResolver` component** on the Player prefab that owns the timing. More separation of concerns but adds a new class.
  - **(C) `CombatSystem.Tick` polls active CorgiControllers** to check state. Too coupled, don't do this.
- **Coroutine cancellation:** if the player gets hit (state → Hit) before active frames fire, the scheduled resolve must be cancelled. Track the coroutine handle and `StopCoroutine` on state changes.
- **Resolve once per attack:** a per-attack `_hasResolvedThisAttack` flag, reset in `OnEnterState`. Don't fire ResolveAttack every active frame.
- **Facing direction:** add `public int Facing { get; private set; } = 1;` to `CorgiController`. Update from `axis.x` sign in `Tick` — but only when the magnitude is meaningful (e.g., `Mathf.Abs(axis.x) > 0.1f`) to avoid stick-jitter flipping.
- **Knockback direction:** multiply `attackData.knockbackForce.x` by `Facing` so hits push the right way. Y/Z are facing-independent.
- **EnemyAI hurtbox prerequisite:** `CombatSystem.ResolveAttack` finds enemies via their `HurtboxComponent`. Enemies need to have one registered. Issue #29 adds this in the spawn path — if #29 lands first, that's free. If not, add a manual hurtbox registration to `EnemyAI.Initialize` as part of this issue.
- **Don't add new dependencies.** The CombatSystem already exposes `ResolveAttack`; `Entity.GetComponent<KnockbackReceiver>` and `Entity.GetComponent<IHealthComponent>` exist. All the pieces are on main.

## Related
- [[29-spawnmanager-instantiate-prefabs]] — enemies need to exist (and have hurtboxes) for hits to land in-game
- [[15-scene-bootstrap-tick-driver]] — bootstrap ticks `CombatSystem` per frame (hitstop advancing); this issue fills in the hit-resolve call site
