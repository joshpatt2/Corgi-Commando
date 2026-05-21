# Platform Build Configuration

## Goal
Single source of truth for macOS and iOS build settings — scripting backend, architecture, orientation, safe area.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/PlatformBuildTests.cs pass
- [ ] Public API matches stubs in Code/Core/PlatformBuildConfig.cs, Code/Data/PlatformSettings.cs
- [ ] PlatformSettings SO has correct defaults (IL2CPP + ARM64 for iOS, landscape locked)
- [ ] PlatformBuildConfig.GetSafeArea() returns valid rect
- [ ] Orientation locked to landscape on both platforms
- [ ] Build scripts (if added) produce valid output for each target

## Tests to Pass
- PlatformSettings_LandscapeLocked_DefaultsTrue
- PlatformSettings_iOS_RequiresIL2CPP
- PlatformSettings_iOS_RequiresARM64
- PlatformSettings_iOS_MinimumVersion13
- PlatformBuildConfig_GetSafeArea_ReturnsValidRect

## Dependencies
- Issue #1 (Input Abstraction — platform-specific input routing)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- PlatformSettings is a ScriptableObject asset — create one at Assets/_Project/Settings/PlatformSettings.asset
- PlatformBuildConfig is a static utility class. IsIOS() / IsMacOS() check Application.platform at runtime.
- GetSafeArea() wraps Screen.safeArea. On macOS (no notch), returns full screen rect.
- Build scripts: optional Editor scripts under an Editor/ folder that set PlayerSettings per platform. Not strictly required for prototype but helpful for CI.
- iOS: PlayerSettings.iOS.targetOSVersionString = "13.0", ScriptingImplementation.IL2CPP, target ARM64 only.
- macOS: ScriptingImplementation.Mono2x (or IL2CPP), OSXArchitecture.Universal.
- Orientation: Screen.orientation = ScreenOrientation.LandscapeLeft, PlayerSettings.defaultInterfaceOrientation = UIInterfaceOrientation.LandscapeLeft on both.
