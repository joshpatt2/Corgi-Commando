# 23. Android Build Target & Meta XR SDK Setup

## Goal
Establish the project's third build target (Android, Quest-flavored) and install the Meta XR All-in-One SDK as the foundation for all subsequent Quest 3 work. This is the **Q1 spike**: prove the Android build pipeline works end-to-end before committing to render/input/CI work. Until this issue is closed, none of #24–#28 can land usefully.

## Acceptance Criteria

### Unity modules & packages
- [ ] Document `README.md` setup step: Unity Hub → Add Modules → **Android Build Support** (includes Android SDK, NDK, OpenJDK)
- [ ] Add `com.meta.xr.sdk.all` (latest 2022.3-compatible version, ≥65.x) to `Packages/manifest.json`
- [ ] Verify auto-pulled transitive deps: OpenXR plugin, AR Foundation, XR Core Utilities

### Project settings
- [ ] **Build profile** for Android added to `ProjectSettings/EditorBuildSettings.asset` (or set up via new Build Profiles system in Unity 2022.3)
- [ ] `PlayerSettings/Android` configured:
  - Scripting Backend: `IL2CPP`
  - Target Architectures: `ARM64` only (no ARMv7)
  - Minimum API Level: 29 (Android 10 — Quest minimum)
  - Target API Level: latest installed
  - Graphics API: `Vulkan` only (remove OpenGLES3)
  - Color Space: `Linear`
  - Multithreaded Rendering: enabled
  - Static Batching: enabled; Dynamic Batching: enabled
  - Render outside Safe Area: yes (Quest doesn't have a "notch")
- [ ] XR Plug-in Management → Android tab → enable **OpenXR** + **Meta XR feature group**
- [ ] OpenXR Settings → Android: add `Meta Quest` interaction profile, set Render Mode to `Multi-Pass` (Built-in pipeline supports this; Single-Pass Instanced is a follow-up)

### Signing
- [ ] Create a dev keystore at `~/.android/corgi-commando-dev.keystore` (do **NOT** commit). Document the `keytool` command in `docs/quest-deploy.md`.
- [ ] PlayerSettings → Publishing Settings: reference the keystore via env var; password goes in CI secrets

### Sanity build
- [ ] Build profile produces a valid `.apk` (or `.aab`) at `Builds/Android/CorgiCommando.apk`
- [ ] `adb install` to a Quest 3 (or Quest 3S) places the app in Unknown Sources → Library
- [ ] Launching shows the existing `Level_Backyard.unity` rendering through the default XR camera (will look weird; that's #24's problem). No crashes.

### Conditional code paths
- [ ] Add a `CORGI_QUEST` define to `PlayerSettings/Scripting Define Symbols` for the Android+XR target. Used by #24+ to gate Quest-only branches.

## Tests to Pass
- AndroidTargetExists_EditMode (asserts `BuildTargetGroup.Android` is supported in `EditorUserBuildSettings`)
- MetaXRSDK_PackageInstalled_EditMode (asserts `com.meta.xr.sdk.all` resolves in package manager)
- OpenXR_Android_MetaInteractionProfileEnabled (asserts via `OpenXRSettings.GetSettingsForBuildTargetGroup` that the Meta Quest interaction profile is present)
- QuestDefineSymbol_Present_EditMode (asserts `PlayerSettings.GetScriptingDefineSymbolsForGroup(Android)` contains `CORGI_QUEST`)

## Dependencies
None. This is foundational for all Quest work.

## Notes for Implementer

### Why "spike" framing
The cost floor of Quest 3 support depends entirely on whether the Android build pipeline works without surprises. Tag this issue closed only after a successful adb deploy to a real device. If signing or compile-time errors block deployment, raise to Claude before continuing.

### Things to verify
- `Packages/manifest.json` already has `com.unity.inputsystem`, `com.unity.cinemachine`, etc. Add Meta XR after them, matching style.
- Meta XR SDK pulls in a lot of transitive packages. Run `Window > Package Manager > Refresh` and verify nothing logs errors.
- IL2CPP on Android requires NDK. The Unity Hub module installer handles this, but if installed via standalone Unity, NDK must be pointed to manually in `Preferences > External Tools`.
- Keystore creation:
  ```bash
  keytool -genkey -v -keystore corgi-commando-dev.keystore \
    -alias corgi-commando -keyalg RSA -keysize 2048 -validity 10000
  ```

### Out of scope
- XR rig setup in scene — that's #24
- Input mapping — that's #25
- Quest CI — that's #27
- Production signing for Quest Store / App Lab — defer until production decision
