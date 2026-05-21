using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// ScriptableObject holding shared run state for co-op.
    /// Shared lives pool, shared Treats counter, revive tracking.
    /// Persists across scenes within a single run (not between sessions).
    /// </summary>
    [CreateAssetMenu(fileName = "RunState", menuName = "CorgiCommando/RunState")]
    public class RunState : ScriptableObject
    {
        private readonly HashSet<int> _deadPlayers = new HashSet<int>();

        /// <summary>Total shared lives remaining.</summary>
        public int LivesRemaining { get; private set; }

        /// <summary>Total Treats collected (shared currency).</summary>
        public int TreatsCollected { get; private set; }

        /// <summary>Number of active players (1 or 2).</summary>
        public int ActivePlayerCount { get; private set; }

        /// <summary>Fired when Treats are added.</summary>
        public event Action<int> OnTreatsChanged;

        /// <summary>Fired when lives change.</summary>
        public event Action<int> OnLivesChanged;

        /// <summary>Fired when both players are dead — game over.</summary>
        public event Action OnGameOver;

        /// <summary>Fired when a player drops in.</summary>
        public event Action<int> OnPlayerJoined;

        /// <summary>
        /// Initializes the run state for a new run.
        /// </summary>
        public void InitializeRun(int startingLives, int playerCount)
        {
            LivesRemaining = Mathf.Max(0, startingLives);
            TreatsCollected = 0;
            ActivePlayerCount = Mathf.Max(1, playerCount);
            _deadPlayers.Clear();
        }

        /// <summary>
        /// Adds Treats to the shared pool.
        /// </summary>
        public void AddTreats(int amount)
        {
            TreatsCollected += amount;
            OnTreatsChanged?.Invoke(TreatsCollected);
        }

        /// <summary>
        /// Consumes a life from the shared pool. Returns false if no lives remain.
        /// </summary>
        public bool ConsumeLife()
        {
            if (LivesRemaining <= 0)
            {
                return false;
            }

            LivesRemaining--;
            OnLivesChanged?.Invoke(LivesRemaining);
            return true;
        }

        /// <summary>
        /// Called when a player dies. Checks for game over (both dead, no lives).
        /// </summary>
        public void OnPlayerDied(int playerIndex)
        {
            _deadPlayers.Add(playerIndex);

            if (_deadPlayers.Count >= ActivePlayerCount && LivesRemaining <= 0)
            {
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Called when P2 drops in mid-run.
        /// </summary>
        public void OnPlayerDropIn(int playerIndex)
        {
            if (playerIndex < ActivePlayerCount || ActivePlayerCount >= 2)
            {
                return;
            }

            if (!ConsumeLife())
            {
                return;
            }

            ActivePlayerCount++;
            OnPlayerJoined?.Invoke(playerIndex);
        }
    }
}
