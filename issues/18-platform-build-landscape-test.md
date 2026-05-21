# PlatformBuildConfig Landscape Test — Invalid Editor-Mode Assumption

## Goal
Fix `PlatformBuildConfig_IsLandscapeLocked_ReturnsTrueForLandscapeOrientations`. The test fails because `Screen.orientation = X` is a no-op in Unity Editor batchmode EditMode tests — the setter doesn't actually mutate `Screen.orientation`, so the subsequent assert hits the default orientation, not the value the test set.

## Failing Test
- `CorgiCommando.Tests.EditMode.PlatformBuildTests.PlatformBuildConfig_IsLandscapeLocked_ReturnsTrueForLandscapeOrientations` — expected `True`, got `False` at line 124

## Root Cause
The current test:
```csharp
Screen.orientation = ScreenOrientation.LandscapeLeft;
Assert.IsTrue(PlatformBuildConfig.IsLandscapeLocked());  // FAILS
```

And the production implementation:
```csharp
public static bool IsLandscapeLocked()
{
    return Screen.orientation == ScreenOrientation.LandscapeLeft
        || Screen.orientation == ScreenOrientation.LandscapeRight;
}
```

The production code is correct. The test setup is wrong: in headless EditMode tests, `Screen.orientation` is read-only in practice — assigning to it doesn't change the underlying value, so `Screen.orientation` stays at whatever default (probably `Portrait` or `AutoRotation`). The assert then fails.

## Acceptance Criteria
- [ ] `PlatformBuildConfig_IsLandscapeLocked_ReturnsTrueForLandscapeOrientations` passes in the EditMode suite
- [ ] No regression in `IsLandscapeLocked` behavior at runtime (on actual macOS/iOS builds)

## Notes for Implementer
**Recommended fix:** Make `IsLandscapeLocked` injectable for testing. Add an internal hook that defaults to reading `Screen.orientation`:

```csharp
internal static Func<ScreenOrientation> OrientationProvider = () => Screen.orientation;

public static bool IsLandscapeLocked()
{
    var o = OrientationProvider();
    return o == ScreenOrientation.LandscapeLeft || o == ScreenOrientation.LandscapeRight;
}
```

Then the test swaps the provider:
```csharp
PlatformBuildConfig.OrientationProvider = () => ScreenOrientation.LandscapeLeft;
Assert.IsTrue(PlatformBuildConfig.IsLandscapeLocked());
// Reset in TearDown
```

**Simpler fix:** Mark the test `[Explicit]` or convert it to a PlayMode test where `Screen.orientation` actually mutates. But that loses coverage; injection is better.

## Dependencies
- None
