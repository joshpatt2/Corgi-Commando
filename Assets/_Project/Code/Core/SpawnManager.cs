using System;
using UnityEngine;
using CorgiCommando.Data;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Drives wave-based encounters from WaveData ScriptableObjects.
    /// Tracks alive enemy count to determine wave clear state.
    /// Coordinates with ArenaCameraLock for gate management.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        private WaveData _waveData;

        /// <summary>Current wave index (0-based).</summary>
        public int CurrentWaveIndex { get; private set; }

        /// <summary>Total number of waves in the current encounter.</summary>
        public int TotalWaves { get; private set; }

        /// <summary>Number of alive enemies in the current wave.</summary>
        public int AliveEnemyCount { get; private set; }

        /// <summary>Whether the current wave is cleared (all enemies dead).</summary>
        public bool IsWaveCleared { get; private set; }

        /// <summary>Whether all waves in the encounter are complete.</summary>
        public bool IsEncounterComplete { get; private set; }

        /// <summary>Fired when a wave is cleared.</summary>
        public event Action<int> OnWaveCleared;

        /// <summary>Fired when the entire encounter is complete.</summary>
        public event Action OnEncounterComplete;

        /// <summary>Fired when a new wave begins spawning.</summary>
        public event Action<int> OnWaveStarted;

        /// <summary>
        /// Begins an encounter using the given wave data.
        /// </summary>
        public void StartEncounter(WaveData waveData)
        {
            if (waveData == null)
            {
                throw new ArgumentNullException(nameof(waveData));
            }

            _waveData = waveData;
            CurrentWaveIndex = 0;
            TotalWaves = _waveData.waves?.Length ?? 0;
            ResetWaveState();
            IsEncounterComplete = TotalWaves == 0;
        }

        /// <summary>
        /// Spawns the current wave's enemies at their designated positions.
        /// </summary>
        public void SpawnCurrentWave()
        {
            if (_waveData == null || IsEncounterComplete || CurrentWaveIndex < 0 || CurrentWaveIndex >= TotalWaves)
            {
                return;
            }

            var wave = _waveData.waves[CurrentWaveIndex];
            ResetWaveState();

            if (wave?.spawnGroups != null)
            {
                foreach (var spawnGroup in wave.spawnGroups)
                {
                    if (spawnGroup == null)
                    {
                        continue;
                    }

                    AliveEnemyCount += Math.Max(0, spawnGroup.count);
                }
            }

            OnWaveStarted?.Invoke(CurrentWaveIndex);

            if (AliveEnemyCount == 0)
            {
                ClearCurrentWave();
            }
        }

        /// <summary>
        /// Called when an enemy dies. Decrements alive count, checks wave clear.
        /// </summary>
        public void OnEnemyDied()
        {
            if (_waveData == null || IsEncounterComplete || IsWaveCleared || AliveEnemyCount <= 0)
            {
                return;
            }

            AliveEnemyCount--;
            if (AliveEnemyCount == 0)
            {
                ClearCurrentWave();
            }
        }

        /// <summary>
        /// Advances to the next wave after the delay specified in WaveData.
        /// </summary>
        public void AdvanceToNextWave()
        {
            if (_waveData == null || IsEncounterComplete || !IsWaveCleared)
            {
                return;
            }

            CurrentWaveIndex++;

            if (CurrentWaveIndex >= TotalWaves)
            {
                CompleteEncounter();
                return;
            }

            ResetWaveState();
        }

        private void ClearCurrentWave()
        {
            IsWaveCleared = true;
            OnWaveCleared?.Invoke(CurrentWaveIndex);

            if (CurrentWaveIndex >= TotalWaves - 1)
            {
                CompleteEncounter();
            }
        }

        private void CompleteEncounter()
        {
            if (IsEncounterComplete)
            {
                return;
            }

            IsEncounterComplete = true;
            OnEncounterComplete?.Invoke();
        }

        private void ResetWaveState()
        {
            AliveEnemyCount = 0;
            IsWaveCleared = false;
        }
    }
}
