# Camera

## Goal
Cinemachine-based shared camera that frames both players, enforces distance cap, and supports arena locking for wave/boss encounters.

## Acceptance Criteria
- [ ] All tests in Tests/PlayMode/CameraTests.cs pass
- [ ] Public API matches stubs in Code/Camera/GroupTargetCamera.cs, ArenaCameraLock.cs
- [ ] Camera tracks single player correctly
- [ ] Camera frames both players when two are active
- [ ] Distance cap fires event when exceeded
- [ ] Arena lock clamps camera X bounds
- [ ] Arena unlock releases bounds on wave clear

## Tests to Pass
- AddTarget_SinglePlayer_CameraTracksPlayer
- AddTarget_TwoPlayers_CameraFramesBoth
- DistanceCap_ExceedsMax_FiresEvent
- LockToArena_SetsArenaLockedTrue
- UnlockArena_SetsArenaLockedFalse

## Dependencies
- Issue #5 (Player Controller — camera tracks CorgiController transforms)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- Uses Cinemachine TargetGroup component. AddTarget/RemoveTarget wrap CinemachineTargetGroup.AddMember/RemoveMember.
- Distance cap: check horizontal distance between targets each frame. If > MaxPlayerDistance, fire event. The trailing player's movement controller should apply a soft pull-in force — that logic lives in the player controller, not camera.
- Arena lock: set CinemachineConfiner2D bounds or manual clamping of camera position.
- Camera framing in co-op is the #1 risk per technical doc. Budget extra tuning time.
- Boss arenas use a separate VCam triggered by ArenaCameraLock collider entry.
