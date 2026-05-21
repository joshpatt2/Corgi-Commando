# Environmental Weapons

## Goal
Pickup, swing, and throw environmental objects as weapons — a classic brawler staple.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/EnvironmentalWeaponTests.cs pass
- [ ] Public API matches stubs in Code/Combat/EnvironmentalWeaponEntity.cs, Code/Player/PickupHandler.cs
- [ ] Weapon initializes as pickupable with correct use count
- [ ] Pickup sets held state and assigns holder
- [ ] Swing returns attack data and decrements uses
- [ ] Throw consumes weapon immediately
- [ ] Weapon breaks after max uses and fires OnWeaponBroken
- [ ] HeldSpeedMultiplier defaults to < 1.0 (movement penalty)

## Tests to Pass
- Initialize_WeaponIsPickupable
- Pickup_SetsHeldState
- Swing_ReturnsAttackDataAndDecrementsUses
- Throw_ConsumesWeapon
- Swing_AfterAllUses_BreaksWeapon
- HeldSpeedMultiplier_DefaultsToSlowed

## Dependencies
- Issue #2 (Entity & Components — extends Entity)
- Issue #4 (Combat System — uses AttackData)
- Issue #5 (Player Controller — PickupHandler on player, state transition to PickupHold)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- PickupHandler.TryPickup: OverlapSphere/Circle to find nearby EnvironmentalWeaponEntity objects within PickupRange. Call Pickup() on the nearest one.
- Throw should set RemainingUses = 0 and IsHeld = false. The thrown weapon becomes a projectile with throwData as its attack.
- Movement penalty: when weapon is picked up, set the holder's KinematicMovementController.SpeedMultiplier to HeldSpeedMultiplier. Reset on drop/break/throw.
- Weapon types (trash lid, gnome, rake, mailbox, tennis ball) differ only in AttackData and use count. No type-specific code needed.
- TODO: Should environmental weapons hit harder than standard combos, or just feel different? Decide during playtests.
