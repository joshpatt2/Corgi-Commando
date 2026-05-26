using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiCommando.Core
{
    public enum Checkpoint
    {
        None = 0,
        BossIntro = 1
    }

    /// <summary>
    /// ScriptableObject holding shared run state for co-op.
    /// Shared lives pool, shared Treats counter, revive tracking.
    /// Persists across scenes within a single run (not between sessions).
    /// </summary>
    [CreateAssetMenu(fileName = "RunState", menuName = "CorgiCommando/RunState")]
    public class RunState : ScriptableObject
    {
        private const int MaxPlayers = 2;
        private readonly HashSet<int> _deadPlayers = new HashSet<int>();

        /// <summary>Total shared lives remaining.</summary>
        public int LivesRemaining { get; private set; }

        /// <summary>Total Treats collected (shared currency).</summary>
        public int TreatsCollected { get; private set; }

        /// <summary>Number of active players (1 or 2).</summary>
        public int ActivePlayerCount { get; private set; }

        /// <summary>Current run checkpoint used for retry behavior.</summary>
        public Checkpoint CurrentCheckpoint { get; private set; }

        /// <summary>Fired when Treats are added.</summary>
        public event Action<int> OnTreatsChanged;

        /// <summary>Fired when lives change.</summary>
        public event Action<int> OnLivesChanged;

        /// <summary>Fired when both players are dead — game over.</summary>
        public event Action OnGameOver;

        /// <summary>Fired when a full party wipe occurs.</summary>
        public event Action OnPartyWiped;

        /// <summary>Fired when a player drops in.</summary>
        public event Action<int> OnPlayerJoined;

        /// <summary>
        /// Initializes the run state for a new run.
        /// </summary>
        public void InitializeRun(int startingLives, int playerCount)
        {
            LivesRemaining = Mathf.Max(0, startingLives);
            TreatsCollected = 0;
            ActivePlayerCount = Mathf.Clamp(playerCount, 1, MaxPlayers);
            CurrentCheckpoint = Checkpoint.None;
            _deadPlayers.Clear();
        }

        /// <summary>
        /// Sets the current checkpoint.
        /// </summary>
        public void SetCheckpoint(Checkpoint checkpoint)
        {
            CurrentCheckpoint = checkpoint;
        }

        /// <summary>
        /// Triggers the party-wipe event.
        /// </summary>
        public void TriggerPartyWipe()
        {
            OnPartyWiped?.Invoke();
        }

        /// <summary>
        /// Adds Treats to the shared pool.
        /// </summary>
        public void AddTreats(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Treat amount cannot be negative.");
            }

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
            if (playerIndex < 0 || playerIndex >= MaxPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Player index must be 0 (P1) or 1 (P2).");
            }

            if (playerIndex < ActivePlayerCount || ActivePlayerCount >= MaxPlayers)
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
