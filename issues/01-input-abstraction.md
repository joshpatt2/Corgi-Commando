# Input Abstraction

## Goal
Wrap Unity Input System behind a timestamped buffer so combat and movement read from the buffer, not raw input — enabling replay and AI-driven players later.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/InputBufferTests.cs pass
- [ ] Public API matches the stubs in Code/Core/InputBuffer.cs, IInputBuffer.cs, PlayerInputHandler.cs, BufferedInput.cs, InputAction.cs
- [ ] PlayerInputHandler bridges Unity PlayerInput to IInputBuffer
- [ ] Gamepad + keyboard both write to the same buffer contract
- [ ] Controller disconnect fires OnControllerDisconnected event
- [ ] GetConnectedGamepadCount returns correct count (gates P2 join on iOS)
- [ ] No new dependencies introduced outside Unity Input System

## Tests to Pass
- RecordInput_StoresActionWithCorrectTimestamp
- PurgeStaleInputs_RemovesInputsOlderThanMaxAge
- ConsumeInput_ReturnsMatchingUnconsumedInput
- ConsumeInput_ReturnsNullWhenNoMatch
- ConsumeInput_DoesNotReturnAlreadyConsumedInput
- HasBufferedInput_ReturnsTrueWithinWindow
- Clear_RemovesAllBufferedInputs
- GetMoveAxis_ReturnsLatestAxisValue

## Dependencies
- None (foundational system)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- InputBuffer is pure C# — no MonoBehaviour, no Unity dependencies beyond Vector2. Easy to unit test.
- PlayerInputHandler IS a MonoBehaviour — it bridges Unity's PlayerInput events to IInputBuffer. May need Play Mode tests for the bridge, but the buffer logic itself is Edit Mode testable.
- Platform parity: identical contract on macOS and iOS. Keyboard bindings are macOS-only but buffer contract is the same.
- Buffer window default is 0.2s — this is the input leniency window for buffered attacks. Tunable.
- ConsumeInput should return the MOST RECENT unconsumed match, not the oldest.
