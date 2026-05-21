# Boss Controller

## Goal
Three-phase WHISKERBOT-9000 boss fight with HP-threshold transitions, laser pointer mechanic, and pilot eject.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/BossControllerTests.cs pass
- [ ] Public API matches stubs in Code/Enemies/WhiskerbotController.cs, BossEntity.cs
- [ ] Phase 1 → 2 at 75% HP, Phase 2 → 3 at 35% HP
- [ ] Laser pointer activates/deactivates in Phase 2
- [ ] Pilot ejects as separate Entity at 0% mech HP
- [ ] Pilot has own HP (separate from mech)
- [ ] OnPhaseChanged and OnPilotEjected events fire correctly

## Tests to Pass
- InitialPhase_IsPhase1
- CheckPhaseTransition_At75Percent_TransitionsToPhase2
- CheckPhaseTransition_At35Percent_TransitionsToPhase3
- ActivateLaser_SetsLaserActive
- EjectPilot_CreatesSeparateEntity
- PilotEntity_HasOwnHP

## Dependencies
- Issue #2 (Entity & Components — extends Entity/BossEntity)
- Issue #4 (Combat System — hit resolution)
- Issue #6 (Enemy AI — base FSM)
- Issue #8 (Spawn & Wave Management — boss arena trigger)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- Phase thresholds: compare currentHP / maxHP against 0.75 and 0.35. Only transition forward (never back).
- Laser pointer: the laser chase mechanic is a real gameplay risk — corgis instinctively chase laser pointers. Implement as a moving point that the player must resist following. Details in gameplay doc.
- Pilot entity: instantiate a new Entity GameObject with its own HealthComponent when EjectPilot() is called. The pilot is a Maine Coon with its own stats.
- Boss uses scripted phase transitions via UnityEvents as per tech doc.
- Phase 1: stomp + claw attacks. Phase 2: debris throws + missiles + laser. Phase 3: pilot 1v1.
