using System;
using System.Collections.Generic;
using CorgiCommando.Camera;
using CorgiCommando.Combat;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using UnityEngine;

namespace CorgiCommando.Core
{
    public enum SceneTickStage
    {
        InputGather,
        Combat,
        PlayerControllers,
        EnemyControllers,
        Revive,
        Camera
    }

    /// <summary>
    /// Scene-level runtime orchestrator that wires encounter dependencies and drives deterministic ticks.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [SerializeField] private WaveData _waveData;
        [SerializeField] private CorgiController _playerOne;
        [SerializeField] private SpawnManager _spawnManager;
        [SerializeField] private ArenaCameraLock _arenaCameraLock;
        [SerializeField] private GroupTargetCamera _groupTargetCamera;
        [SerializeField] private bool _autoStartEncounter = true;

        private readonly List<EnemyAI> _activeEnemies = new List<EnemyAI>();
        private readonly List<CorgiController> _activePlayers = new List<CorgiController>();
        private CombatSystem _combatSystem;
        private ReviveSystem _reviveSystem;
        private RunState _runState;
        private bool _encounterStarted;

        public event Action<SceneTickStage> OnTickStageExecuted;

        public RunState RunState => _runState;
        public CombatSystem CombatSystem => _combatSystem;
        public ReviveSystem ReviveSystem => _reviveSystem;
        public int ActiveEnemyCount => _activeEnemies.Count;
        public bool AutoStartEncounter
        {
            get => _autoStartEncounter;
            set => _autoStartEncounter = value;
        }

        public bool IsEncounterStarted => _encounterStarted;

        private void Start()
        {
            AcquireReferences();

            _combatSystem ??= new CombatSystem();
            _reviveSystem ??= new ReviveSystem();
            _runState = ScriptableObject.CreateInstance<RunState>();
            _runState.InitializeRun(3, 1);

            if (_groupTargetCamera != null)
            {
                _groupTargetCamera.UseManualTick = true;
                if (_playerOne != null)
                {
                    _groupTargetCamera.AddTarget(_playerOne.transform);
                }
            }

            CacheActivePlayers();

            for (int i = 0; i < _activePlayers.Count; i++)
            {
                _activePlayers[i]?.SetCombatSystem(_combatSystem);
            }

            if (_spawnManager != null)
            {
                _spawnManager.OnEnemySpawned += RegisterEnemy;
                _spawnManager.OnEnemyDeath += UnregisterEnemy;
                if (_autoStartEncounter)
                {
                    StartEncounter();
                }
            }

            EnemyAI[] existingEnemies = FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < existingEnemies.Length; i++)
            {
                RegisterEnemy(existingEnemies[i]);
            }
        }

        public void StartEncounter()
        {
            if (_spawnManager == null || _encounterStarted)
            {
                return;
            }

            _spawnManager.StartEncounter(_waveData);
            _spawnManager.SpawnCurrentWave();
            _encounterStarted = true;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            OnTickStageExecuted?.Invoke(SceneTickStage.InputGather);

            _combatSystem?.Tick(deltaTime);
            OnTickStageExecuted?.Invoke(SceneTickStage.Combat);

            _playerOne?.Tick(deltaTime);
            OnTickStageExecuted?.Invoke(SceneTickStage.PlayerControllers);

            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                EnemyAI enemy = _activeEnemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.Tick(deltaTime);
            }
            OnTickStageExecuted?.Invoke(SceneTickStage.EnemyControllers);

            if (TryTickRevive(deltaTime))
            {
                OnTickStageExecuted?.Invoke(SceneTickStage.Revive);
            }
        }

        private void LateUpdate()
        {
            _groupTargetCamera?.Tick(Time.deltaTime);
            OnTickStageExecuted?.Invoke(SceneTickStage.Camera);
        }

        private void OnDestroy()
        {
            if (_spawnManager != null)
            {
                _spawnManager.OnEnemySpawned -= RegisterEnemy;
                _spawnManager.OnEnemyDeath -= UnregisterEnemy;
            }

            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                if (_activeEnemies[i] != null)
                {
                    _activeEnemies[i].OnDeath -= OnEnemyEntityDied;
                }
            }
            _activeEnemies.Clear();
            _activePlayers.Clear();

            if (_runState != null)
            {
                Destroy(_runState);
                _runState = null;
            }
        }

        private void AcquireReferences()
        {
            _playerOne ??= FindPlayerOne();
            _spawnManager ??= FindObjectOfType<SpawnManager>();
            _arenaCameraLock ??= FindObjectOfType<ArenaCameraLock>();
            _groupTargetCamera ??= FindObjectOfType<GroupTargetCamera>();
        }

        private CorgiController FindPlayerOne()
        {
            CorgiController[] players = FindObjectsOfType<CorgiController>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].PlayerIndex == 0)
                {
                    return players[i];
                }
            }

            return players.Length > 0 ? players[0] : null;
        }

        private void CacheActivePlayers()
        {
            _activePlayers.Clear();

            CorgiController[] players = FindObjectsOfType<CorgiController>();
            for (int i = 0; i < players.Length; i++)
            {
                CorgiController player = players[i];
                if (player != null && !_activePlayers.Contains(player))
                {
                    _activePlayers.Add(player);
                }
            }
        }

        private void RegisterEnemy(EnemyAI enemy)
        {
            if (enemy == null || _activeEnemies.Contains(enemy))
            {
                return;
            }

            _activeEnemies.Add(enemy);
            enemy.OnDeath += OnEnemyEntityDied;
        }

        private void UnregisterEnemy(EnemyAI enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.OnDeath -= OnEnemyEntityDied;
            _activeEnemies.Remove(enemy);
        }

        private void OnEnemyEntityDied(Entity entity)
        {
            if (entity is not EnemyAI enemy)
            {
                return;
            }

            _spawnManager?.NotifyEnemyDied(enemy);
            UnregisterEnemy(enemy);
        }

        private bool TryTickRevive(float deltaTime)
        {
            if (_reviveSystem == null || _runState == null || _runState.ActivePlayerCount < 2)
            {
                return false;
            }

            if (_activePlayers.Count < 2)
            {
                return false;
            }

            CorgiController alivePlayer = null;
            CorgiController downedPlayer = null;

            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                CorgiController player = _activePlayers[i];
                if (player == null)
                {
                    _activePlayers.RemoveAt(i);
                    continue;
                }

                bool isDowned = !player.IsAlive || player.CurrentState == CorgiState.Knockdown || player.CurrentState == CorgiState.Dead;
                if (isDowned && downedPlayer == null)
                {
                    downedPlayer = player;
                }
                else if (!isDowned && alivePlayer == null)
                {
                    alivePlayer = player;
                }
            }

            if (alivePlayer == null || downedPlayer == null)
            {
                return false;
            }

            _reviveSystem.Tick(downedPlayer.PlayerIndex, alivePlayer.transform.position, downedPlayer.transform.position, deltaTime);
            return true;
        }
    }
}
