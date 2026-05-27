# 20. Art-Aware Data SOs

## Goal
Extend `CorgiData`, `EnemyData`, and `AttackData` ScriptableObjects with the field surface required to drop real 2D art and VFX in. Today these SOs carry stats and a `placeholderColor` — nothing addressable by a `SpriteRenderer` or `Animator`. This issue adds the slots; issues #21 and #22 wire the runtime to consume them.

## Acceptance Criteria

### `CorgiData` extensions
- [ ] `public Sprite portraitSprite;` (used by HUD per-player portrait)
- [ ] `public Sprite idleSprite;` (fallback when no animator)
- [ ] `public RuntimeAnimatorController animatorController;` (drives state-machine animations)
- [ ] `public Vector2 spritePivotOffset = new Vector2(0f, 0.5f);` (foot-anchored pivot tuning)
- [ ] `[Range(0,1)] public float spriteFlashIntensity = 0.8f;` (damage flash alpha)
- [ ] Existing `placeholderColor` field stays as fallback for art-less testing

### `EnemyData` extensions
- [ ] `public Sprite idleSprite;`
- [ ] `public RuntimeAnimatorController animatorController;`
- [ ] `public Vector2 spritePivotOffset = new Vector2(0f, 0.5f);`
- [ ] `[Range(0,1)] public float spriteFlashIntensity = 0.8f;`

### `AttackData` extensions
- [ ] `public GameObject hitsparkPrefab;` (instantiated at `HitResult.HitPosition` on connect; pooled)
- [ ] `public Color hitsparkColor = Color.white;` (overrides per-HitType default if set)
- [ ] `public AudioClip hitSound;` (optional, for future audio pass — slot in now to avoid a re-schema)

### Asset migration
- [ ] Existing `Sarge.asset`, all `FeralCat.asset` / `Raccoon.asset` / `Sprinkler.asset` / `Roomba.asset` / `Whiskerbot.asset` get the new fields with safe defaults (null sprite refs, default colors). YAML edit only — no code regression
- [ ] All existing `AttackData` assets get the new VFX fields with defaults

## Tests to Pass
- CorgiData_PortraitSprite_FieldExists (EditMode reflection assert)
- CorgiData_AnimatorController_FieldExists (EditMode)
- CorgiData_SpriteFlashIntensity_DefaultsToReasonableValue (EditMode)
- EnemyData_IdleSprite_FieldExists (EditMode)
- EnemyData_AnimatorController_FieldExists (EditMode)
- AttackData_HitsparkPrefab_FieldExists (EditMode)
- AttackData_HitsparkColor_DefaultsWhite (EditMode)
- ExistingAssetsLoadable_NoYamlBreakage (EditMode) — loads `Sarge.asset` and asserts non-null after schema change

## Dependencies
- Issue #19 (Art Pipeline Foundation) — sprite/sort layer/PPU all configured

## Notes for Implementer

### Schema-change risk
Unity SerializedObject is tolerant — adding new fields with default values won't break existing assets. But re-saving an asset in the Editor will rewrite its YAML and may reorder fields. **Do not bulk-resave assets** unless you've verified the YAML before/after.

### Test-only InternalsVisibleTo
For the reflection-based field-existence tests, you don't need `InternalsVisibleTo` — these fields are public.

### Why split from #19
Conceptually #19 is editor/project config, #20 is data schema. Splitting keeps PRs reviewable.

### Out of scope
- Wiring runtime to consume these fields — that's #21 (animation bridge) and #22 (hit feedback)
- Authoring actual art / animator controller assets — content task, separate workflow
