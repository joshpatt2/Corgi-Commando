# Combat System

## Goal
Centralized hit resolution with Z-band rules, hitstop, knockback, combo tracking, and special meter.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/CombatSystemTests.cs pass
- [ ] Public API matches stubs in Code/Combat/CombatSystem.cs, ICombatSystem.cs, HitResult.cs
- [ ] Attacks hit targets within ±0.5 Z-band, whiff outside
- [ ] Damage applied correctly from AttackData
- [ ] Hitstop duration matches AttackData.hitstopFrames
- [ ] Combo counter increments on chained hits, resets after 2s timeout
- [ ] Special meter fills on hit landed
- [ ] OnHitConnected event fires with correct HitResult
- [ ] No new dependencies beyond Core and Data

## Tests to Pass
- ResolveAttack_TargetInZBand_HitConnects
- ResolveAttack_TargetOutsideZBand_Whiffs
- ResolveAttack_HitConnects_AppliesDamage
- ResolveAttack_HitConnects_ReturnsCorrectHitstop
- ResolveAttack_HitConnects_FiresOnHitConnectedEvent
- ComboCounter_IncrementsOnChainedHits
- ComboCounter_ResetsAfterTimeout
- SpecialMeter_FillsOnHitLanded

## Dependencies
- Issue #1 (Input Abstraction — combat reads buffered input for timing)
- Issue #2 (Entity & Components — HealthComponent, HurtboxComponent, KnockbackReceiver)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- Z-band tolerance is ±0.5 units. Compare Mathf.Abs(attacker.transform.position.z - target.transform.position.z) <= 0.5f
- ResolveAttack should: check Z-band → check hurtbox enabled → check faction (no friendly fire) → apply damage → apply knockback → fire events → increment combo → add special meter
- Combo timeout is 2.0s. Tick() decrements the timer. If it expires, ResetCombo().
- Hitstop: the CombatSystem sets IsInHitstop = true and counts down frames. Game loop should check IsInHitstop and skip entity updates during hitstop.
- Special meter gain per hit: read from CorgiData.specialGainPerHit (default 10f). This means CombatSystem needs access to the attacker's CorgiData or a configurable gain amount.
- Knockback is applied via KnockbackReceiver.ApplyKnockback() on the target entity.
