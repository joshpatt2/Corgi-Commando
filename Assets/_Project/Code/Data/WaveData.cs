using System;
using UnityEngine;

namespace CorgiCommando.Data
{
    /// <summary>
    /// ScriptableObject defining wave-based encounter data for an arena.
    /// Used by SpawnManager to drive enemy spawning.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWaveData", menuName = "CorgiCommando/WaveData")]
    public class WaveData : ScriptableObject
    {
        [Tooltip("Ordered list of waves in this arena encounter")]
        public WaveEntry[] waves;
    }

    /// <summary>
    /// A single wave within an arena encounter.
    /// </summary>
    [Serializable]
    public class WaveEntry
    {
        [Tooltip("Delay in seconds before this wave begins (after previous wave clears)")]
        public float delayBeforeSpawn = 1.0f;

        [Tooltip("Enemy spawn groups in this wave")]
        public SpawnGroup[] spawnGroups;
    }

    /// <summary>
    /// A group of enemies to spawn at a specific point.
    /// </summary>
    [Serializable]
    public class SpawnGroup
    {
        [Tooltip("Enemy type to spawn")]
        public EnemyData enemyData;

        [Tooltip("Number of this enemy type to spawn")]
        public int count = 1;

        [Tooltip("Spawn point position in world space")]
        public Vector3 spawnPosition;
    }
}
