# 21. State → Animation Bridge

## Goal
Wire the `CorgiController` state machine and `EnemyAI` FSM to drive an `Animator`. Today, `CorgiController.OnStateChanged(from, to)` and `EnemyAI.OnStateChanged(from, to)` fire on every transition but no consumer plays animations. This issue adds `CorgiAnimationBridge` and `EnemyAnimationBridge` MonoBehaviours that subscribe to those events and translate state transitions into Animator parameter changes.

## Acceptance Criteria

### `CorgiAnimationBridge`
- [ ] New `Assets/_Project/Code/Player/CorgiAnimationBridge.cs` MonoBehaviour
- [ ] Subscribes in `OnEnable` to a sibling `CorgiController.OnStateChanged`, unsubscribes in `OnDisable`
- [ ] Auto-acquires sibling `Animator` (child SpriteRenderer/Animator on `Sprite` GameObject)
- [ ] Maps each `CorgiState` to an animator trigger or bool — recommended scheme:
  - `Idle` → `Bool IsMoving = false`, `Bool IsAirborne = false`
  - `Walk` → `Bool IsMoving = true`
  - `Jump` → `Bool IsAirborne = true`, `Trigger Jump`
  - `Attack1` / `Attack2` / `Attack3` → `Trigger Attack` + `Int ComboStep = 1/2/3`
  - `Special` → `Trigger Special`
  - `Hit` → `Trigger Hit`
  - `Knockdown` → `Trigger Knockdown`, `Bool IsDowned = true`
  - `GetUp` → `Trigger GetUp`
  - `Dead` → `Trigger Death`
- [ ] If the `RuntimeAnimatorController` on the Animator is null, the bridge logs a warning once and no-ops — never throws
- [ ] No allocations in the hot path (no string interning per-transition; cache parameter hashes via `Animator.StringToHash` at `Awake`)

### `EnemyAnimationBridge`
- [ ] Same pattern, mapped to `EnemyState`:
  - `Idle` → `Bool IsMoving = false`
  - `Chase` → `Bool IsMoving = true`
  - `Attack` → `Trigger Attack`
  - `Stunned` → `Trigger Stunned`
  - `Recover` → `Trigger Recover`
  - `Fleeing` → `Bool IsFleeing = true`
  - `Dead` → `Trigger Death`

### Player & enemy prefab wiring
- [ ] Add `Animator` component to the `Sprite` child of `Player.prefab`
- [ ] Add `CorgiAnimationBridge` to `Player.prefab` root
- [ ] Document in `Art/README.md` that enemy prefabs (when created) follow the same pattern

## Tests to Pass

EditMode unit tests (use a stub Animator via component on a GameObject):
- CorgiAnimationBridge_OnIdleToWalk_SetsIsMovingTrue
- CorgiAnimationBridge_OnWalkToIdle_SetsIsMovingFalse
- CorgiAnimationBridge_OnAttack1_TriggersAttack_AndSetsComboStep1
- CorgiAnimationBridge_OnAttack2_SetsComboStep2
- CorgiAnimationBridge_OnHit_TriggersHit
- CorgiAnimationBridge_NullAnimatorController_DoesNotThrow
- CorgiAnimationBridge_OnDisable_UnsubscribesFromStateChanged
- EnemyAnimationBridge_OnIdleToChase_SetsIsMovingTrue
- EnemyAnimationBridge_OnAttack_TriggersAttack
- EnemyAnimationBridge_OnDeath_TriggersDeath

## Dependencies
- Issue #19 (Art Pipeline Foundation) — sort layers exist for the SpriteRenderer
- Issue #20 (Art-Aware Data SOs) — `CorgiData.animatorController` field exists
- Issue #5 (Player Controller) — ✅ merged; provides `OnStateChanged`
- Issue #6 (Enemy AI) — ✅ merged; provides `OnStateChanged`

## Notes for Implementer

### Animator parameter caching
```csharp
private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");
// ...
```

### Testing without a real AnimatorController
NUnit tests can verify the bridge calls `Animator.SetBool` / `SetTrigger` by adding an empty `RuntimeAnimatorController` and inspecting parameter values via `Animator.GetBool`/`GetInteger`. If the Animator is in a GameObject without a real controller, parameter sets become no-ops but the bridge logic still runs and tests can assert via spy/observer patterns.

For null-controller tests, simply don't assign `Animator.runtimeAnimatorController` and assert the bridge logs a warning + doesn't throw.

### Edge case: state change before Animator awakens
`CorgiController.Initialize` can fire `OnStateChanged(Idle, Idle)` before `Awake` runs on the bridge. Guard by either: (a) subscribing in `Awake`, not `OnEnable`, OR (b) idempotent setters (setting `IsMoving=false` when it's already false is fine).

### Out of scope
- Authoring the actual `.controller` asset (that's content work — drop placeholder anims first to validate, real anims later)
- VFX/audio feedback — that's #22
- Sprite sorting by Z — that's a separate render concern (per-frame sort order = `-(int)(transform.position.z * 100)`); defer until at least one character has a real sprite sheet
