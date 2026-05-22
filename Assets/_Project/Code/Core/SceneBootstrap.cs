using System;
using System.Collections;
using System.Collections.Generic;
using CorgiCommando.Camera;
using CorgiCommando.Combat;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using CorgiCommando.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        [SerializeField] private ScreenShakeHandler _screenShakeHandler;
        [SerializeField] private Transform _bossIntroSpawnPoint;
        [SerializeField] private float _retryDelaySeconds = 2.5f;
        [SerializeField] private bool _reloadSceneOnGameOver = true;

        private readonly List<EnemyAI> _activeEnemies = new List<EnemyAI>();
        private readonly List<CorgiController> _activePlayers = new List<CorgiController>();
        private CombatSystem _combatSystem;
        private ReviveSystem _reviveSystem;
        private RunState _runState;
        private WhiskerbotController _bossController;
        private BossBannerUI _bossBannerUI;
        private Text _wipePromptText;
        private bool _isBossFightActive;
        private bool _partyWipeSequenceRunning;
        private bool _awaitingGameOverStartPress;

        public event Action<SceneTickStage> OnTickStageExecuted;
        public event Action<string> OnPromptShown;
        public event Action<string> OnSceneReloadRequested;

        public RunState RunState => _runState;
        public CombatSystem CombatSystem => _combatSystem;
        public ReviveSystem ReviveSystem => _reviveSystem;
        public int ActiveEnemyCount => _activeEnemies.Count;
        public bool IsAwaitingGameOverStartPress => _awaitingGameOverStartPress;

        private void Start()
        {
            AcquireReferences();

            _combatSystem ??= new CombatSystem();
            _reviveSystem ??= new ReviveSystem();
            _runState = ScriptableObject.CreateInstance<RunState>();
            _runState.InitializeRun(3, 1);
            _runState.OnPartyWiped += HandlePartyWipe;

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

            _screenShakeHandler?.SetCombatSystem(_combatSystem);

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

            RefreshBossReferences();
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

            TryDetectPartyWipe();
            TryHandleGameOverStartPress();
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

            if (_runState != null)
            {
                _runState.OnPartyWiped -= HandlePartyWipe;
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
            _screenShakeHandler ??= FindObjectOfType<ScreenShakeHandler>();
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

        /// <summary>
        /// Called by level trigger wiring when players enter the boss intro door.
        /// Activates boss-fight tracking and sets the boss-intro checkpoint.
        /// </summary>
        public void OnBossDoorTriggered()
        {
            _isBossFightActive = true;
            _runState?.SetCheckpoint(Checkpoint.BossIntro);
            RefreshBossReferences();
        }

        /// <summary>
        /// Test hook for game-over confirmation in environments without Start input.
        /// </summary>
        public void ConfirmGameOverReload()
        {
            if (!_awaitingGameOverStartPress)
            {
                return;
            }

            ReloadLevelBackyard();
        }

        /// <summary>
        /// Enables/disables scene reload on game-over. Primarily used by tests.
        /// </summary>
        public void SetReloadSceneOnGameOver(bool enabled)
        {
            _reloadSceneOnGameOver = enabled;
        }

        private void TryDetectPartyWipe()
        {
            if (_runState == null || _partyWipeSequenceRunning || _awaitingGameOverStartPress)
            {
                return;
            }

            int deadCount = 0;
            int consideredPlayers = 0;

            for (int i = _activePlayers.Count - 1; i >= 0; i--)
            {
                var player = _activePlayers[i];
                if (player == null)
                {
                    _activePlayers.RemoveAt(i);
                    continue;
                }

                consideredPlayers++;
                if (!player.IsAlive)
                {
                    deadCount++;
                }
            }

            if (consideredPlayers > 0 && deadCount >= consideredPlayers)
            {
                _runState.TriggerPartyWipe();
            }
        }

        private void HandlePartyWipe()
        {
            if (_partyWipeSequenceRunning)
            {
                return;
            }

            _partyWipeSequenceRunning = true;

            if (_runState != null && _runState.CurrentCheckpoint == Checkpoint.BossIntro && _isBossFightActive)
            {
                ShowPrompt("REGROUP\nRETRY");
                StartCoroutine(BossRetryRoutine());
                return;
            }

            ShowPrompt("GAME OVER\nPRESS START");
            _awaitingGameOverStartPress = true;
            _partyWipeSequenceRunning = false;
        }

        private IEnumerator BossRetryRoutine()
        {
            float retryDelay = Mathf.Max(0f, _retryDelaySeconds);
            if (retryDelay > 0f)
            {
                yield return new WaitForSeconds(retryDelay);
            }

            Vector3 spawnPosition = GetBossRetrySpawnPosition();

            for (int i = 0; i < _activePlayers.Count; i++)
            {
                var player = _activePlayers[i];
                if (player == null)
                {
                    continue;
                }

                player.transform.position = spawnPosition;
                player.Revive();
                player.TransitionTo(CorgiState.Idle);
            }

            if (_bossController != null)
            {
                _bossController.ResetToPhase1();
                var bossHealth = _bossController.GetEntityComponent<IHealthComponent>();
                if (bossHealth != null)
                {
                    _bossBannerUI?.Show(_bossController.GetBossName(), bossHealth.CurrentHP, bossHealth.MaxHP);
                }
            }

            HidePrompt();
            _partyWipeSequenceRunning = false;
        }

        private void TryHandleGameOverStartPress()
        {
            if (!_awaitingGameOverStartPress)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                ReloadLevelBackyard();
            }
        }

        private void ReloadLevelBackyard()
        {
            _awaitingGameOverStartPress = false;
            _partyWipeSequenceRunning = false;
            OnSceneReloadRequested?.Invoke("Level_Backyard");

            if (_reloadSceneOnGameOver)
            {
                SceneManager.LoadScene("Level_Backyard");
            }
        }

        private void ShowPrompt(string message)
        {
            EnsurePromptText();
            if (_wipePromptText == null)
            {
                return;
            }

            _wipePromptText.text = message;
            _wipePromptText.gameObject.SetActive(true);
            OnPromptShown?.Invoke(message);
        }

        private void HidePrompt()
        {
            if (_wipePromptText != null)
            {
                _wipePromptText.text = string.Empty;
                _wipePromptText.gameObject.SetActive(false);
            }
        }

        private void EnsurePromptText()
        {
            if (_wipePromptText != null)
            {
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("RetryPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var promptGo = new GameObject("RetryPromptText", typeof(RectTransform), typeof(Text));
            promptGo.transform.SetParent(canvas.transform, false);
            var text = promptGo.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 42;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = string.Empty;
            promptGo.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            promptGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            promptGo.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            promptGo.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 140f);
            promptGo.SetActive(false);
            _wipePromptText = text;
        }

        private void RefreshBossReferences()
        {
            if (_bossController == null)
            {
                _bossController = FindObjectOfType<WhiskerbotController>();
            }

            if (_bossBannerUI == null)
            {
                _bossBannerUI = FindObjectOfType<BossBannerUI>();
            }
        }

        private Vector3 GetBossRetrySpawnPosition()
        {
            if (_bossIntroSpawnPoint != null)
            {
                return _bossIntroSpawnPoint.position;
            }

            if (_activePlayers.Count > 0 && _activePlayers[0] != null)
            {
                return _activePlayers[0].transform.position;
            }

            return Vector3.zero;
        }
    }
}
