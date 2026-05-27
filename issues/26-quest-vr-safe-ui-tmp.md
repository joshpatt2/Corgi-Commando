# 26. VR-Safe UI (TextMeshPro Migration)

## Goal
Make the HUD safe to render on Quest 3 by replacing every Legacy `UnityEngine.UI.Text` + `Arial.ttf` reference with **TextMeshPro**. This is a Quest-3 requirement because:
1. `Resources.GetBuiltinResource<Font>("Arial.ttf")` returns `null` on Android (Quest's OS) — see review on PR #30
2. Legacy `Text` does not antialias well on a high-DPI headset; pixel-art HUDs read crunchy

It is also a NOW-actionable fix for iOS — same problem there.

The HUD itself stays *inside* the game RenderTexture (per #24's theater architecture), so this is purely a font/text component swap.

## Acceptance Criteria

### Package
- [ ] Verify `com.unity.textmeshpro` is in `Packages/manifest.json` (Unity 2022.3 ships it by default — if missing, add)
- [ ] Run TextMeshPro → Import TMP Essential Resources via Editor menu — adds `Assets/TextMesh Pro/` with the default font atlas
- [ ] Author one project font atlas at `Assets/_Project/Art/UI/CorgiCommando_TMP.asset` from a pixel-friendly source font (e.g., `LegacyRuntime.ttf` ships in Unity; or a CC0 pixel font like "Press Start 2P")

### Code migration (HUD PR #30 dependents)
- [ ] `Code/UI/HUDController.cs`:
  - Replace `using UnityEngine.UI` Text references with `using TMPro` `TMP_Text`
  - Replace `GetDefaultFont() → Resources.GetBuiltinResource<Font>("Arial.ttf")` with a SerializeField `TMP_FontAsset _font;` populated from the project atlas
  - Update `BuildPauseMenu()` to instantiate `TextMeshProUGUI` instead of `Text`
- [ ] `Code/UI/ComboCounterUI.cs`: same swap
- [ ] `Code/UI/BossBannerUI.cs`: same swap
- [ ] Remove the now-dead `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` fallback (no longer needed once TMP is the only path)

### Scene wiring
- [ ] Update `Level_Backyard.unity` HUD prefab/object: any `Text` MonoBehaviour replaced by `TextMeshProUGUI`
- [ ] Update `Level_Backyard_Quest.unity` (from #24) same way

### Existing tests
- [ ] `UITests.cs` assertions that reference `Text` need to be updated to `TextMeshProUGUI`. Specifically: any `GetComponentInChildren<Text>()` becomes `GetComponentInChildren<TextMeshProUGUI>()`
- [ ] `HUDController_Awake_CreatesVisualElements` and friends must still pass

## Tests to Pass

EditMode (all should already exist from #30 / UITests.cs; update assertions to TMP):
- HUDController_Awake_CreatesVisualElements (updated to check for `TextMeshProUGUI`)
- BossBannerUI_Show_DisplaysBossInfo (assert TMP text content)
- ComboCounterUI_SetComboCount_UpdatesDisplay (assert TMP text content)

New:
- HUDController_DefaultFont_IsNotNull_EditMode (asserts a non-null `TMP_FontAsset` is assigned at Awake)
- HUDController_NoLegacyTextComponents_EditMode (asserts `GetComponentsInChildren<Text>(true).Length == 0`)

## Dependencies
- Issue #30 (HUD UI) — currently in PR #30, must land first OR coordinate the change inside #30's branch
- Issue #23 (Android Build Target) — soft dep; this can land before Quest is wired, since the fix is also valuable for iOS

## Notes for Implementer

### Why this is split from #30
At the time of writing, PR #30 already has a review comment requesting the `Arial.ttf` fix. The author may resolve it before this issue is assigned. If #30 already migrates to TMP, **close this issue as superseded**. If #30 ships with a Resources.GetBuiltinResource fallback, this issue completes the migration.

### TMP_FontAsset authoring tip
For a "Saturday morning cartoon" pixel-art aesthetic, the right font is bold and chunky. Recommended: download "Press Start 2P" (Google Fonts, OFL) or "VT323" — both free pixel fonts. Drop the `.ttf` in `Assets/_Project/Art/UI/Fonts/`, then Window → TextMeshPro → Font Asset Creator → generate an SDF atlas at 512×512 or 1024×1024.

### Don't migrate the test asserts to runtime checks
NUnit EditMode tests that reference Legacy `Text` should be updated to expect `TextMeshProUGUI`. Don't add runtime fallbacks that accept both — that's noise that survives long after the migration.

### Out of scope
- Audio components for UI (button click sounds etc.)
- Localization
- Dynamic font sizing for safe-area variance
- Replacing the `Image`-based health-fill bars (they already render as Quad meshes, no font involved)
