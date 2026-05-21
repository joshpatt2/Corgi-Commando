# Vertical Slice Content Authoring (Sarge + Enemies + Waves)

## Goal
Author the ScriptableObject `.asset` files needed to make `Level_Backyard.unity` playable: Sarge's character data, his combo + special AttackData, three enemy types + their attacks, and at least one wave configuration. The codebase already defines the data schemas — this issue ships the content that fills them.

## Acceptance Criteria
- [ ] `Sarge.asset` (CorgiData) at `Assets/_Project/Data/Corgis/Sarge.asset` with non-default maxHP, walkSpeed, depthSpeed, jumpForce, special meter fields
- [ ] 4 AttackData assets for Sarge: `Sarge_Punch1.asset`, `Sarge_Punch2.asset`, `Sarge_Kick.asset`, `Sarge_BarkShockwave.asset` under `Assets/_Project/Data/Attacks/Player/`
- [ ] `Sarge.asset.comboChain` references Punch1 → Punch2 → Kick in order (length == 3)
- [ ] `Sarge.asset.specialAttack` references BarkShockwave
- [ ] 3 EnemyData assets: `FeralCat.asset`, `RaccoonBandit.asset`, `SprinklerTurret.asset` under `Assets/_Project/Data/Enemies/`
- [ ] 3 AttackData assets for enemies (Cat_Swipe, Raccoon_Slash, Sprinkler_WaterShot) under `Assets/_Project/Data/Attacks/Enemies/`
- [ ] Each EnemyData's `primaryAttack` field is wired to the corresponding enemy AttackData
- [ ] `BackyardWave1.asset` (WaveData) under `Assets/_Project/Data/Waves/` with at least 2 wave entries mixing the three enemy types
- [ ] `Player.prefab` CorgiController's serialized character-data field references `Sarge.asset` (deferred OK if #5 hasn't landed and the field isn't serialized yet)
- [ ] `Level_Backyard.unity` SpawnManager (when wired in #5/#6 follow-up) consumes `BackyardWave1.asset`
- [ ] New Edit Mode test file `Tests/EditMode/VerticalSliceContentTests.cs` verifying assets load with non-default values

## Tests to Pass
- VerticalSliceContent_SargeAsset_LoadsAndHasComboChain (asserts comboChain.Length == 3 and each entry is non-null)
- VerticalSliceContent_SargeAsset_HasSpecialAttack
- VerticalSliceContent_FeralCatAsset_HasPrimaryAttackWired
- VerticalSliceContent_RaccoonBanditAsset_HasPrimaryAttackWired
- VerticalSliceContent_SprinklerTurretAsset_HasPrimaryAttackWired
- VerticalSliceContent_BackyardWave1_HasNonEmptyWaves
- VerticalSliceContent_AllAttackDataAssets_HaveNonZeroDamage

## Dependencies
- Issue #5 (Player Controller) — needed to confirm whether CorgiController consumes CorgiData via `[SerializeField]` (Inspector reference) or `Resources.Load`. Asset authoring can start in parallel; prefab wiring can defer until #5 lands.
- Issue #6 (Enemy AI) — same reasoning for EnemyData access pattern.
- Issue #8 (Spawn & Wave Management) — ✅ merged; consumer is ready for `BackyardWave1.asset`.

## Notes for Implementer
- This issue is **content authoring**, not new system code. ScriptableObject `.asset` files are Unity YAML — they can be created via the Editor (`Assets → Create → CorgiCommando → ...` menus from the `[CreateAssetMenu]` attributes) or written directly as YAML by referencing the schema class's GUID and field layout.
- Folder layout to create:
  ```
  Assets/_Project/Data/
    Corgis/Sarge.asset
    Attacks/Player/Sarge_Punch1.asset (+ Punch2, Kick, BarkShockwave)
    Attacks/Enemies/Cat_Swipe.asset (+ Raccoon_Slash, Sprinkler_WaterShot)
    Enemies/FeralCat.asset (+ RaccoonBandit, SprinklerTurret)
    Waves/BackyardWave1.asset
  ```
- Suggested **Sarge** stats (tune in playtests):
  - maxHP=100, walkSpeed=5, depthSpeed=3, jumpForce=10
  - maxSpecialMeter=100, specialGainPerHit=10, specialDecayRate=5
- Suggested **Sarge_Punch1** frame data:
  - startup=3, active=4, recovery=6, comboWindow=10, damage=8, knockback=(3,1,0), hitstun=8, hitstop=4
- Suggested **Punch2**: slightly more damage and knockback than Punch1; Kick: finisher with knockdown=true.
- Suggested **BarkShockwave**: hitType=Special, larger hitbox rect (e.g., 3×1), higher damage (~20), causesKnockdown=true.
- Suggested **enemy stats**:
  - FeralCat: maxHP=30, moveSpeed=4, aggroRange=8, attackRange=1.2
  - SprinklerTurret: maxHP=40, moveSpeed=0 (fixed), attackRange=6 (ranged), telegraph cycle handled by #6 AI
  - RaccoonBandit: maxHP=25, moveSpeed=4.5, aggroRange=10 (will flee with Treats per #9/#11)
- `BackyardWave1` suggested: Wave 0 = 3× FeralCat at (5,0,0); Wave 1 = 2× FeralCat + 1× RaccoonBandit; Wave 2 = 1× SprinklerTurret + 2× FeralCat.
- For tests, use `AssetDatabase.LoadAssetAtPath` in Edit Mode tests. If the access pattern in #5 uses `Resources.Load`, move assets into `Assets/_Project/Resources/` and load via that.
- **Out of scope:** environmental weapon assets (deferred to #9), boss data assets (deferred to #10 once `BossData` schema exists), UI assets (#12).
