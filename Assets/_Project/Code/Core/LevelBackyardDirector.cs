using CorgiCommando.Camera;
using CorgiCommando.Combat;
using CorgiCommando.Data;
using CorgiCommando.Enemies;
using CorgiCommando.Player;
using CorgiCommando.UI;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Scene-specific wiring for the Level_Backyard demo flow.
    /// </summary>
    public class LevelBackyardDirector : MonoBehaviour
    {
        private const int WaveThreeIndex = 2;

        [SerializeField] private SceneBootstrap _sceneBootstrap;
        [SerializeField] private SpawnManager _spawnManager;
        [SerializeField] private ArenaCameraLock _arenaCameraLock;
        [SerializeField] private GroupTargetCamera _groupTargetCamera;
        [SerializeField] private HUDController _hudController;
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform _arenaTriggerPoint;
        [SerializeField] private float _arenaTriggerHalfWidth = 1.5f;
        [SerializeField] private float _arenaMinX = 0f;
        [SerializeField] private float _arenaMaxX = 18f;
        [SerializeField] private Transform _bossDoorTriggerPoint;
        [SerializeField] private float _bossDoorTriggerHalfWidth = 1.5f;
        [SerializeField] private Transform _bossSpawnPoint;
        [SerializeField] private EnemyData _whiskerbotData;
        [SerializeField] private GameObject _whiskerbotPrefab;
        [SerializeField] private Transform[] _waveThreeWeaponSpawnPoints;
        [SerializeField] private GameObject[] _environmentalWeaponPrefabs;

        private CorgiController _playerOne;
        private BossBannerUI _bossBanner;
        private bool _weaponsSpawned;

        public bool IsArenaTriggered { get; private set; }
        public bool IsBossDoorUnlocked { get; private set; }
        public bool IsBossSpawned { get; private set; }

        private void Start()
        {
            AcquireReferences();

            if (_sceneBootstrap != null)
            {
                _sceneBootstrap.AutoStartEncounter = false;
            }

            if (_playerOne != null && _playerSpawnPoint != null)
            {
                _playerOne.transform.position = _playerSpawnPoint.position;
            }

            if (_arenaCameraLock != null)
            {
                _arenaCameraLock.ArenaMinX = _arenaMinX;
                _arenaCameraLock.ArenaMaxX = _arenaMaxX;
            }

            if (_spawnManager != null)
            {
                _spawnManager.OnWaveCleared += HandleWaveCleared;
                _spawnManager.OnWaveStarted += HandleWaveStarted;
            }
        }

        private void Update()
        {
            if (_playerOne == null)
            {
                return;
            }

            if (!IsArenaTriggered && IsPastTriggerX(_playerOne.transform.position.x, _arenaTriggerPoint, _arenaTriggerHalfWidth))
            {
                TriggerArena();
            }

            if (IsBossDoorUnlocked && !IsBossSpawned && IsPastTriggerX(_playerOne.transform.position.x, _bossDoorTriggerPoint, _bossDoorTriggerHalfWidth))
            {
                SpawnWhiskerbot();
            }
        }

        private void OnDestroy()
        {
            if (_spawnManager != null)
            {
                _spawnManager.OnWaveCleared -= HandleWaveCleared;
                _spawnManager.OnWaveStarted -= HandleWaveStarted;
            }
        }

        public void TriggerArena()
        {
            if (IsArenaTriggered)
            {
                return;
            }

            IsArenaTriggered = true;
            _arenaCameraLock?.Activate(_groupTargetCamera);

            if (_sceneBootstrap != null)
            {
                _sceneBootstrap.StartEncounter();
            }
        }

        private void HandleWaveCleared(int clearedWaveIndex)
        {
            if (_spawnManager == null)
            {
                return;
            }

            if (clearedWaveIndex < _spawnManager.TotalWaves - 1)
            {
                _spawnManager.AdvanceToNextWave();
                _spawnManager.SpawnCurrentWave();
                return;
            }

            IsBossDoorUnlocked = true;
        }

        private void HandleWaveStarted(int startedWaveIndex)
        {
            if (startedWaveIndex != WaveThreeIndex)
            {
                return;
            }

            SpawnEnvironmentalWeapons();
        }

        private void SpawnEnvironmentalWeapons()
        {
            if (_weaponsSpawned || _environmentalWeaponPrefabs == null || _waveThreeWeaponSpawnPoints == null)
            {
                return;
            }

            int count = Mathf.Min(_environmentalWeaponPrefabs.Length, _waveThreeWeaponSpawnPoints.Length);
            for (int i = 0; i < count; i++)
            {
                var prefab = _environmentalWeaponPrefabs[i];
                var spawnPoint = _waveThreeWeaponSpawnPoints[i];
                if (prefab == null || spawnPoint == null)
                {
                    continue;
                }

                Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            }

            _weaponsSpawned = true;
        }

        private void SpawnWhiskerbot()
        {
            if (IsBossSpawned)
            {
                return;
            }

            var spawnPosition = _bossSpawnPoint != null ? _bossSpawnPoint.position : transform.position;
            GameObject bossGo;
            WhiskerbotController boss;
            EnemyData bossData;

            if (_whiskerbotPrefab != null)
            {
                bossGo = Instantiate(_whiskerbotPrefab, spawnPosition, Quaternion.identity);
                boss = bossGo.GetComponent<WhiskerbotController>();
                var setup = bossGo.GetComponent<WhiskerbotPrefabSetup>();
                bossData = setup != null ? setup.EnemyData : null;

                if (boss == null || bossData == null)
                {
                    Debug.LogWarning("Whiskerbot prefab is missing required setup/components; boss spawn aborted.", this);
                    Destroy(bossGo);
                    return;
                }
            }
            else
            {
                bossGo = new GameObject("Whiskerbot");
                bossGo.transform.position = spawnPosition;
                bossGo.AddComponent<KinematicMovementController>();
                var sprite = bossGo.AddComponent<SpriteRenderer>();
                sprite.color = _whiskerbotData != null ? _whiskerbotData.placeholderColor : Color.red;
                boss = bossGo.AddComponent<WhiskerbotController>();
                if (_whiskerbotData != null)
                {
                    boss.Initialize(_whiskerbotData);
                }

                bossData = _whiskerbotData;
            }

            _bossBanner ??= FindObjectOfType<BossBannerUI>(true);
            if (_bossBanner != null && boss != null)
            {
                int maxHP = bossData != null ? bossData.maxHP : 1;
                string bossName = bossData != null ? bossData.enemyName : "Whiskerbot";
                _bossBanner.Show(bossName, maxHP, maxHP);
            }

            IsBossSpawned = true;
        }

        private void AcquireReferences()
        {
            _sceneBootstrap ??= FindObjectOfType<SceneBootstrap>();
            _spawnManager ??= FindObjectOfType<SpawnManager>();
            _arenaCameraLock ??= FindObjectOfType<ArenaCameraLock>();
            _groupTargetCamera ??= FindObjectOfType<GroupTargetCamera>();
            _hudController ??= FindObjectOfType<HUDController>();
            _playerOne ??= FindPlayerOne();

            if (_hudController != null)
            {
                _bossBanner = _hudController.GetComponentInChildren<BossBannerUI>(true);
            }
        }

        private static bool IsPastTriggerX(float xPosition, Transform triggerPoint, float halfWidth)
        {
            if (triggerPoint == null)
            {
                return false;
            }

            float clampedHalfWidth = Mathf.Max(0.01f, halfWidth);
            return xPosition >= triggerPoint.position.x - clampedHalfWidth;
        }

        private static CorgiController FindPlayerOne()
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
    }
}
