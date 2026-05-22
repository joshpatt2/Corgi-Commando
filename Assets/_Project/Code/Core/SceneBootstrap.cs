using System;
using System.Collections.Generic;
using CorgiCommando.Camera;
using CorgiCommando.Combat;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using CorgiCommando.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private HUDController _hudController;
        [SerializeField] private PlayerInputManager _playerInputManager;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Vector3 _playerTwoSpawnOffset = new Vector3(2f, 0f, 0f);
        [SerializeField] private float _dropOutHoldDurationSeconds = 1f;

        private readonly List<EnemyAI> _activeEnemies = new List<EnemyAI>();
        private readonly List<CorgiController> _activePlayers = new List<CorgiController>();
        private CombatSystem _combatSystem;
        private ReviveSystem _reviveSystem;
        private RunState _runState;
        private CorgiController _playerTwo;
        private PlayerInputHandler _playerTwoInputHandler;
        private float _dropOutHoldTimer;

        public event Action<SceneTickStage> OnTickStageExecuted;

        public RunState RunState => _runState;
        public CombatSystem CombatSystem => _combatSystem;
        public ReviveSystem ReviveSystem => _reviveSystem;
        public int ActiveEnemyCount => _activeEnemies.Count;
        public CorgiController PlayerTwo => _playerTwo;

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
            _hudController?.SetPlayerStripVisible(0, true);
            _hudController?.SetPlayerStripVisible(1, false);

            for (int i = 0; i < _activePlayers.Count; i++)
            {
                _activePlayers[i]?.SetCombatSystem(_combatSystem);
            }

            if (_spawnManager != null)
            {
                _spawnManager.OnEnemySpawned += RegisterEnemy;
                _spawnManager.OnEnemyDeath += UnregisterEnemy;
                _spawnManager.StartEncounter(_waveData);
                _spawnManager.SpawnCurrentWave();
            }

            EnemyAI[] existingEnemies = FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < existingEnemies.Length; i++)
            {
                RegisterEnemy(existingEnemies[i]);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            OnTickStageExecuted?.Invoke(SceneTickStage.InputGather);

            TryHandleDropInRequest();
            TryHandleDropOutRequest(deltaTime);

            _combatSystem?.Tick(deltaTime);
            OnTickStageExecuted?.Invoke(SceneTickStage.Combat);

            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                CorgiController player = _activePlayers[i];
                if (player == null)
                {
                    _activePlayers.RemoveAt(i);
                    continue;
                }

                player.Tick(deltaTime);
            }
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

            if (_playerTwo != null)
            {
                DropOutPlayerTwo();
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
            _playerTwo = null;
            _playerTwoInputHandler = null;

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
            _hudController ??= FindObjectOfType<HUDController>();
            _playerInputManager ??= FindObjectOfType<PlayerInputManager>();
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

            _playerOne = FindPlayerOne();
            _playerTwo = FindPlayerByIndex(1);
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

        public bool TryDropInPlayerTwo(Gamepad joiningGamepad = null)
        {
            if (_runState == null || _runState.ActivePlayerCount >= 2 || _playerTwo != null)
            {
                return false;
            }

            GameObject playerTwoObject = CreatePlayerTwoObject(joiningGamepad);
            if (playerTwoObject == null)
            {
                return false;
            }

            Vector3 spawnPosition = GetPlayerTwoSpawnPosition();
            playerTwoObject.transform.position = spawnPosition;

            CorgiController playerTwo = playerTwoObject.GetComponent<CorgiController>();
            PlayerInputHandler inputHandler = playerTwoObject.GetComponent<PlayerInputHandler>();
            if (playerTwo == null || inputHandler == null)
            {
                Destroy(playerTwoObject);
                return false;
            }

            _runState.OnPlayerDropIn(1);
            if (_runState.ActivePlayerCount < 2)
            {
                Destroy(playerTwoObject);
                return false;
            }

            var inputBuffer = inputHandler.Buffer ?? new InputBuffer();
            inputHandler.Initialize(inputBuffer, 1);
            inputHandler.OnControllerDisconnected -= OnControllerDisconnected;
            inputHandler.OnControllerDisconnected += OnControllerDisconnected;

            var characterData = playerTwo.CharacterData ?? _playerOne?.CharacterData;
            if (characterData != null)
            {
                playerTwo.Initialize(characterData, inputBuffer, 1);
            }
            playerTwo.SetCombatSystem(_combatSystem);

            if (!_activePlayers.Contains(playerTwo))
            {
                _activePlayers.Add(playerTwo);
            }

            _playerTwo = playerTwo;
            _playerTwoInputHandler = inputHandler;
            _dropOutHoldTimer = 0f;

            _groupTargetCamera?.AddTarget(playerTwo.transform);
            _hudController?.SetPlayerStripVisible(1, true);
            return true;
        }

        public void DropOutPlayerTwo()
        {
            if (_playerTwo == null && (_runState == null || _runState.ActivePlayerCount < 2))
            {
                return;
            }

            _runState?.OnPlayerDropOut(1);
            _dropOutHoldTimer = 0f;

            if (_playerTwoInputHandler != null)
            {
                _playerTwoInputHandler.OnControllerDisconnected -= OnControllerDisconnected;
            }

            if (_playerTwo != null)
            {
                _groupTargetCamera?.RemoveTarget(_playerTwo.transform);
                _activePlayers.Remove(_playerTwo);
                Destroy(_playerTwo.gameObject);
            }

            _playerTwo = null;
            _playerTwoInputHandler = null;
            _hudController?.SetPlayerStripVisible(1, false);
        }

        private void TryHandleDropInRequest()
        {
            if (_runState == null || _runState.ActivePlayerCount >= 2 || _playerTwo != null)
            {
                return;
            }

            if (PlayerInputHandler.GetConnectedGamepadCount() < 2)
            {
                return;
            }

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad gamepad = Gamepad.all[i];
                if (gamepad == null || !WasDropInPressedThisFrame(gamepad))
                {
                    continue;
                }

                if (PlayerInput.FindFirstPairedToDevice(gamepad) != null)
                {
                    continue;
                }

                if (TryDropInPlayerTwo(gamepad))
                {
                    return;
                }
            }
        }

        private void TryHandleDropOutRequest(float deltaTime)
        {
            if (_playerTwo == null)
            {
                return;
            }

            if (_playerTwoInputHandler != null && !_playerTwoInputHandler.IsGamepadConnected)
            {
                DropOutPlayerTwo();
                return;
            }

            Gamepad playerTwoPad = GetPlayerTwoGamepad();
            if (playerTwoPad == null)
            {
                _dropOutHoldTimer = 0f;
                return;
            }

            bool isHoldingDropOutButtons = playerTwoPad.selectButton.isPressed && playerTwoPad.startButton.isPressed;
            _dropOutHoldTimer = isHoldingDropOutButtons ? _dropOutHoldTimer + deltaTime : 0f;

            if (_dropOutHoldTimer >= _dropOutHoldDurationSeconds)
            {
                DropOutPlayerTwo();
            }
        }

        private void OnControllerDisconnected(int playerIndex)
        {
            if (playerIndex == 1)
            {
                DropOutPlayerTwo();
            }
        }

        private GameObject CreatePlayerTwoObject(Gamepad joiningGamepad)
        {
            if (_playerInputManager != null)
            {
                var joinedPlayer = _playerInputManager.JoinPlayer(1, -1, null, joiningGamepad);
                if (joinedPlayer != null)
                {
                    return joinedPlayer.gameObject;
                }
            }

            if (_playerPrefab != null)
            {
                return Instantiate(_playerPrefab);
            }

            return _playerOne != null ? Instantiate(_playerOne.gameObject) : null;
        }

        private Vector3 GetPlayerTwoSpawnPosition()
        {
            Vector3 basePosition = _playerOne != null
                ? _playerOne.transform.position + _playerTwoSpawnOffset
                : _playerTwoSpawnOffset;

            if (Physics2D.OverlapCircle(basePosition, 0.25f) != null || Physics.CheckSphere(basePosition, 0.25f))
            {
                basePosition += new Vector3(0f, 1f, 0f);
            }

            return basePosition;
        }

        private static bool WasDropInPressedThisFrame(Gamepad gamepad)
        {
            return gamepad.startButton.wasPressedThisFrame
                || gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.buttonEast.wasPressedThisFrame
                || gamepad.buttonWest.wasPressedThisFrame
                || gamepad.buttonNorth.wasPressedThisFrame;
        }

        private Gamepad GetPlayerTwoGamepad()
        {
            if (_playerTwo == null)
            {
                return null;
            }

            PlayerInput playerInput = _playerTwo.GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                return null;
            }

            for (int i = 0; i < playerInput.devices.Count; i++)
            {
                if (playerInput.devices[i] is Gamepad gamepad)
                {
                    return gamepad;
                }
            }

            return null;
        }

        private CorgiController FindPlayerByIndex(int playerIndex)
        {
            for (int i = 0; i < _activePlayers.Count; i++)
            {
                CorgiController player = _activePlayers[i];
                if (player != null && player.PlayerIndex == playerIndex)
                {
                    return player;
                }
            }

            return null;
        }
    }
}
