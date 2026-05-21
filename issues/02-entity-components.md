# Entity Base & Components

## Goal
Provide the composition-based entity system that all combat actors (players, enemies, env weapons, bosses) build on — faction, health, hurtbox, knockback.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/EntityComponentTests.cs pass
- [ ] Public API matches stubs in Code/Core/Entity.cs, IEntityComponent.cs, IHealthComponent.cs, HealthComponent.cs, HurtboxComponent.cs, KnockbackReceiver.cs, Faction.cs
- [ ] Entity supports add/remove/get of IEntityComponent by type
- [ ] HealthComponent fires OnDamaged and OnDied events correctly
- [ ] HurtboxComponent can be enabled/disabled
- [ ] KnockbackReceiver stores impulse and fires event
- [ ] No new dependencies introduced outside Unity core

## Tests to Pass
- Entity_NewEntity_IsAliveByDefault
- Entity_FactionCanBeSet
- AddEntityComponent_ComponentCanBeRetrieved
- RemoveEntityComponent_ComponentNoLongerRetrievable
- HasEntityComponent_ReturnsTrueWhenAttached
- HasEntityComponent_ReturnsFalseWhenNotAttached
- HealthComponent_TakeDamage_ReducesHP
- HealthComponent_TakeDamage_FiresOnDamagedEvent
- HealthComponent_TakeFatalDamage_FiresOnDiedEvent
- HealthComponent_TakeDamage_ClampsToZero
- HealthComponent_Heal_ClampsToMax
- HurtboxComponent_DefaultEnabled
- HurtboxComponent_Disable_PreventsHits
- HurtboxComponent_EnableAfterDisable_Restores
- KnockbackReceiver_ApplyKnockback_SetsVelocity
- KnockbackReceiver_ApplyKnockback_FiresEvent
- KnockbackReceiver_ClearKnockback_ResetsState

## Dependencies
- None (foundational system)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- Entity.AddEntityComponent uses generic type T as the dictionary key — store components in a Dictionary<Type, IEntityComponent>
- HealthComponent is pure C# (not MonoBehaviour) for easy testing. It receives an Entity reference via OnAttach.
- HurtboxComponent.IsEnabled should default to true on construction — hurtboxes are active unless explicitly disabled.
- KnockbackReceiver is also pure C#. The movement controller will read KnockbackVelocity each frame to apply it.
- Entity.IsAlive should be true by default and set to false when HealthComponent fires OnDied.
