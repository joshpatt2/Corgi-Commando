# 25. Quest Controller Input Mapping

## Goal
Map Meta Quest Touch controllers to the existing `InputAction` enum (`MoveLeft`/`MoveRight`/`MoveUp`/`MoveDown`/`Punch`/`Kick`/`Jump`/`Special`/`Pause`) so `InputBuffer` and `CorgiController` receive identical input events whether the player is on a gamepad, keyboard, or Quest controllers. **No gameplay code should change** — only the `.inputactions` asset.

## Acceptance Criteria

### Input action asset
- [ ] Open `Assets/_Project/CorgiCommando.inputactions` in the Input Actions editor
- [ ] Add a new Control Scheme: `Quest` (Device requirements: `XR Controller`)
- [ ] Bind to the **Player** action map:
  - `Move` → Left Quest Controller Thumbstick (2D Vector)
  - `Punch` → Right Quest Controller `secondaryButton` (X on right controller = Punch)
  - `Kick` → Right Quest Controller `primaryButton` (A on right controller = Kick)
  - **WAIT**: per gameplay doc input table, the canonical mapping uses Xbox letters (X=Punch, Y=Kick, A=Jump, B=Special). On Quest, the right controller has A/B (lower/upper) and the left has X/Y (lower/upper). Match to the canonical map:
    - Quest Right A (`primaryButton` on `XRController{R}`) → `Jump` (matches "South=Jump")
    - Quest Right B (`secondaryButton` on `XRController{R}`) → `Special` (matches "East=Special")
    - Quest Left X (`primaryButton` on `XRController{L}`) → `Punch` (matches "West=Punch")
    - Quest Left Y (`secondaryButton` on `XRController{L}`) → `Kick` (matches "North=Kick")
  - `Pause` → Either controller's `menuButton`
- [ ] Add a `Recenter` action (new) bound to: hold-both-triggers-for-1-second OR Quest Left/Right `gripButton` simultaneously. Calls `QuestRigBootstrap.RecenterPlayer()` from #24.
- [ ] Ensure `PlayerInputHandler` accepts the `Quest` control scheme (no code change needed if it already auto-switches; verify)

### Code touchpoints
- [ ] `Code/Core/InputAction.cs` enum: no change (Quest reuses existing actions)
- [ ] `Code/Player/PlayerInputHandler.cs`: verify no hardcoded scheme name; add `Recenter` callback if not already present
- [ ] Wire `OnRecenter` to `QuestRigBootstrap.RecenterPlayer()` via SerializeField or service-locator lookup

### Documentation
- [ ] Update `Corgi_Commando_Technical.docx` input table with a Quest column — defer until docs reformat pass; for now, add a section to `docs/quest-deploy.md` (created in #28) showing the controller-to-action map

## Tests to Pass
- QuestInputScheme_Exists_EditMode (asserts the `.inputactions` file contains a `Quest` control scheme via JSON parse)
- QuestInputScheme_BindsAllRequiredActions_EditMode (asserts each `InputAction` enum value has at least one Quest binding)
- QuestInputScheme_RecenterAction_Bound_EditMode
- InputBuffer_QuestPunchInput_BehavesIdenticallyToGamepad (EditMode — feed a synthetic Quest button press, assert `InputBuffer.HasBufferedInput(InputAction.Punch, …)` returns true the same way as a gamepad would)

## Dependencies
- Issue #23 (Android Build Target & Meta XR SDK) — Input System XR namespace requires the SDK
- Issue #24 (XR Rig & Theater Scene) — `RecenterPlayer()` method must exist
- Issue #1 (Input Abstraction) — ✅ merged; provides `InputBuffer`

## Notes for Implementer

### Why this is small
The whole point of the input abstraction in #1 was to make this kind of port trivial. `InputBuffer` doesn't know what physical button caused an event — it only sees `InputAction` enum values. So the entire Quest input layer is a `.inputactions` asset change with zero C# code change (except the new `Recenter` callback wiring).

### Quest button naming reference
Per Unity Input System's `UnityEngine.XR` package:
- Left controller: `XRController{LeftHand}` with `primaryButton` (X), `secondaryButton` (Y), `gripButton`, `triggerButton`, `thumbstick`, `menuButton`
- Right controller: `XRController{RightHand}` with `primaryButton` (A), `secondaryButton` (B), `gripButton`, `triggerButton`, `thumbstick`

The OpenXR + Meta XR feature set surfaces the same names — your existing PlayerInput component should see them automatically once the Quest control scheme is added.

### Handedness
Quest 3 has no built-in left/right preference setting at the OS level — apps decide. Stick with the canonical mapping above; a settings menu for handedness swap is a follow-up.

### Out of scope
- Hand tracking (Scenario B)
- Touch pad / floor-relative locomotion
- In-game settings UI for input remapping
