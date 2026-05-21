using System;
using System.Collections.Generic;
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Spawns the current wave's enemies at their designated positions.
        /// </summary>
        public void SpawnCurrentWave()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when an enemy dies. Decrements alive count, checks wave clear.
        /// </summary>
        public void OnEnemyDied()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Advances to the next wave after the delay specified in WaveData.
        /// </summary>
        public void AdvanceToNextWave()
        {
            throw new NotImplementedException();
        }
    }
}
