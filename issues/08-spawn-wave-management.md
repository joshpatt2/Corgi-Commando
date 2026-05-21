# Spawn & Wave Management

## Goal
Drive wave-based enemy encounters from WaveData ScriptableObjects, track wave clear state, coordinate with arena camera locks.

## Acceptance Criteria
- [ ] All tests in Tests/EditMode/SpawnManagerTests.cs pass
- [ ] Public API matches stubs in Code/Core/SpawnManager.cs and Code/Data/WaveData.cs
- [ ] StartEncounter initializes wave state from WaveData
- [ ] SpawnCurrentWave sets correct alive enemy count
- [ ] OnEnemyDied decrements count, fires OnWaveCleared when all dead
- [ ] AdvanceToNextWave moves to next wave index
- [ ] IsEncounterComplete and OnEncounterComplete fire after all waves cleared

## Tests to Pass
- StartEncounter_InitializesWaveState
- SpawnCurrentWave_SetsAliveEnemyCount
- OnEnemyDied_AllDead_FiresWaveClearedEvent
- OnEnemyDied_NotAllDead_DoesNotClearWave
- AdvanceToNextWave_IncrementsWaveIndex
- AllWavesCleared_FiresEncounterCompleteEvent

## Dependencies
- Issue #2 (Entity & Components — spawned enemies are entities)
- Issue #6 (Enemy AI — spawned enemies need AI initialization)
- Issue #7 (Camera — wave clear unlocks arena camera)

## Notes for Implementer
- Cost-sensitive: prefer Copilot first, escalate to Claude only if stuck
- SpawnManager instantiates enemy prefabs at positions defined in SpawnGroup.spawnPosition.
- Subscribe to each spawned enemy's OnDeath event → call OnEnemyDied().
- Wave delay: after wave clear, wait WaveEntry.delayBeforeSpawn seconds before spawning next wave. Use a coroutine or timer.
- Arena gate coordination: SpawnManager should fire events that ArenaCameraLock listens to, or directly call Activate/Deactivate.
- Enemy density scaling for co-op: check active player count and multiply spawn counts. This is a future enhancement — note as TODO.
