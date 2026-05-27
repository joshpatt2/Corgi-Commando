# 24. XR Rig & Theater Scene (Render-to-Quad)

## Goal
Implement the "Virtual Theater" render architecture for Quest 3: the existing 2.5D game continues to render normally into an off-screen `RenderTexture`; the user sees a flat quad floating in front of them displaying that RT. Quest controllers act as gamepads. The XR camera is *only* used to position the head in front of the quad — gameplay logic is untouched.

This is the central architectural piece of Scenario A. Once this is in, Corgi Commando runs on a Quest 3 the same way it runs on iOS, just inside a virtual theater.

## Acceptance Criteria

### Scene structure
- [ ] New `Assets/_Project/Scenes/Level_Backyard_Quest.unity` scene (copy of `Level_Backyard.unity` with Quest-only modifications). Alternative: runtime branch in `SceneBootstrap` keyed on `CORGI_QUEST` define — choose whichever is cleaner for the merge.
- [ ] Existing game `Camera` (now called `GameCamera`) renders to a `RenderTexture` asset: `Art/RT/QuestTheater_GameOutput.renderTexture`, 1280×720, RGBA32, linear, point-filter
- [ ] `GameCamera` has the `PixelPerfectCamera` component (from #19) — RT resolution feeds PPU math
- [ ] New `XRRig` GameObject with: `XR Origin`, `Camera Offset`, `Main Camera` tagged `MainCamera`, `Left Controller` + `Right Controller` (Meta Quest interaction profile)
- [ ] `TheaterQuad` GameObject: `Quad` mesh, positioned 2.0 m in front of XR Origin at eye-height (~1.6 m), scaled to 1.6×0.9 m (16:9 to match RT). Material: unlit, RT bound as albedo, no fog.
- [ ] Optional backdrop: solid dark color or low-poly skybox (NOT passthrough — Scenario A is closed-environment)

### `QuestRigBootstrap` MonoBehaviour
- [ ] New `Assets/_Project/Code/Core/QuestRigBootstrap.cs`
- [ ] On `Awake`, if not building for Quest (`#if !CORGI_QUEST`), disable self and the entire XRRig subtree — never run in non-VR builds
- [ ] On `Start`:
  - Acquire `GameCamera` and `TheaterQuad` references via `[SerializeField]`
  - Create the `RenderTexture` at runtime if its asset is missing (defensive)
  - Bind RT to `GameCamera.targetTexture`
  - Bind RT to `TheaterQuad`'s `MeshRenderer.material.mainTexture`
- [ ] Public `Vector3 QuadDistanceOffset` SerializeField defaulting to `(0, 0, 2.0)` for tunability
- [ ] Public method `RecenterPlayer()` (bound to a button in #25) re-seats the user in front of the quad

### Render pipeline config
- [ ] Confirm Built-in Render Pipeline works with OpenXR Multi-Pass on Quest 3 (it does; verify in Q1 spike)
- [ ] If single-pass is preferred for perf, document the perf delta but defer the migration to a follow-up issue

### Comfort defaults
- [ ] Quad is *world-locked* (anchored to XR Origin position at scene start, not head-locked) — head-locked quads cause nausea
- [ ] No fog, no auto-orbit, no idle camera motion on the XR camera

## Tests to Pass

EditMode:
- QuestRigBootstrap_NonQuestBuild_DisablesSelf (uses `[ConditionalAttribute]`-style branch; asserts XRRig is disabled when `CORGI_QUEST` is undefined)
- QuestRigBootstrap_AssignsRenderTextureToGameCamera
- QuestRigBootstrap_BindsRenderTextureToQuadMaterial
- TheaterQuad_DefaultDistance_Is2Meters

PlayMode (deferred until #27 CI builds Quest, but stub the test):
- QuestRig_RecenterPlayer_ResetsXROriginPosition

## Dependencies
- Issue #23 (Android Build Target & Meta XR SDK) — must merge first
- Issue #19 (Art Pipeline Foundation) — provides `Art/` folder structure; `PixelPerfectCamera` setup carries over
- Issue #32 (Scene Bootstrap) — solo-player scene flow works on Quest as-is; bootstrap doesn't need Quest-aware branching for solo

## Notes for Implementer

### Why render-to-quad
A naive "put the game camera as the VR camera" approach makes you literally inside the 2.5D plane, which is disorienting and breaks the camera framing logic. Render-to-quad lets the existing 2.5D camera (Cinemachine target group, distance cap, arena lock) run unchanged — the VR layer is purely about *displaying that output to the user's eyes*.

### RenderTexture sizing tradeoff
1280×720 is the recommended starting point — high enough that pixel art reads clearly on the Quest 3's 2064×2208/eye displays, low enough to render at 90 Hz. If text or sprites look soft, bump to 1920×1080. If frame rate drops, reduce to 960×540.

### Material setup
The TheaterQuad needs an **unlit** shader for RT display — lit shaders darken/tint the RT based on scene lighting. Use Unity's `Unlit/Texture` shader, or write a tiny custom unlit shader if you want to handle the linear→sRGB gamma curve explicitly.

### Comfort considerations
- Quad too close (<1.5 m): eye strain, vergence-accommodation conflict
- Quad too far (>3 m): pixel art reads tiny, loses appeal
- Quad too large: peripheral motion can induce nausea
- 2.0 m × 1.6m wide is the sweet spot from VR media-viewer apps

### Out of scope
- Input wiring (controllers → gameplay) — that's #25
- HUD inside the theater quad — that's #26
- Passthrough background — Scenario B, not in scope
- Hand tracking — Scenario B, not in scope
- Spatial audio — separate follow-up
