using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CorgiCommando.Core;
using CorgiCommando.Combat;
using CorgiCommando.Data;
using CorgiCommando.Player;

namespace CorgiCommando.Tests.PlayMode
{
    /// <summary>
    /// Play Mode tests for player attack wiring to CombatSystem.ResolveAttack.
    /// Verifies that entering an attack state resolves hits, deals damage,
    /// fills special meter, and fires the OnHitLanded event.
    /// </summary>
    [TestFixture]
    public class CombatSystemPlayModeTests
    {
        private AttackData _punchData;
        private CorgiData _corgiData;
        private CombatSystem _combatSystem;
        private GameObject _playerGo;
        private CorgiController _player;
        private InputBuffer _inputBuffer;
        private GameObject _enemyGo;
        private Entity _enemy;
        private HealthComponent _enemyHealth;

        [SetUp]
        public void SetUp()
        {
            _punchData = ScriptableObject.CreateInstance<AttackData>();
            _punchData.attackName = "TestPunch";
            _punchData.damage = 10;
            _punchData.startupFrames = 0; // Fire on next frame — simplifies timing in tests
            _punchData.knockbackForce = new Vector3(3f, 1f, 0f);
            _punchData.hitstopFrames = 0;
            _punchData.comboWindowFrames = 10;

            _corgiData = ScriptableObject.CreateInstance<CorgiData>();
            _corgiData.corgiName = "TestSarge";
            _corgiData.maxHP = 100;
            _corgiData.walkSpeed = 5f;
            _corgiData.depthSpeed = 3f;
            _corgiData.jumpForce = 10f;
            _corgiData.comboChain = new[] { _punchData };
            _corgiData.specialGainPerHit = 10f;
            _corgiData.maxSpecialMeter = 100f;
            _corgiData.specialCost = 100f;
            _corgiData.specialDecayRate = 0f;

            _combatSystem = new CombatSystem();

            _playerGo = new GameObject("Player");
            _playerGo.transform.position = Vector3.zero;
            _player = _playerGo.AddComponent<CorgiController>();
            _inputBuffer = new InputBuffer();
            _player.Initialize(_corgiData, _inputBuffer, 0);
            _player.SetCombatSystem(_combatSystem);
            // Faction defaults to Faction.Player (enum value 0)

            _enemyGo = new GameObject("Enemy");
            _enemyGo.transform.position = new Vector3(1f, 0f, 0f); // Adjacent, same Z-band
            _enemy = _enemyGo.AddComponent<Entity>();
            _enemy.Faction = Faction.Enemy;
            _enemyHealth = new HealthComponent(100);
            _enemy.AddEntityComponent<IHealthComponent>(_enemyHealth);
            _enemy.AddEntityComponent<HurtboxComponent>(new HurtboxComponent());
            _enemy.AddEntityComponent<KnockbackReceiver>(new KnockbackReceiver());
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_enemyGo);
            Object.DestroyImmediate(_punchData);
            Object.DestroyImmediate(_corgiData);
        }

        [UnityTest]
        public IEnumerator CombatSystem_PlayerPunchHitsAdjacentEnemy_DealsDamage()
        {
            // Act — enter Attack1; coroutine fires after startup frames (0 → next frame)
            _player.TransitionTo(CorgiState.Attack1);
            yield return null; // Wait one frame for the coroutine to execute

            // Assert
            Assert.Less(_enemyHealth.CurrentHP, 100, "Enemy adjacent in Z-band should take damage from Attack1");
        }

        [UnityTest]
        public IEnumerator CombatSystem_PlayerAttack_FillsSpecialMeter()
        {
            // Arrange — initial meter is 0
            Assert.AreEqual(0f, _player.SpecialMeter, "Special meter should start at 0");

            // Act
            _player.TransitionTo(CorgiState.Attack1);
            yield return null; // Wait one frame for the coroutine to execute

            // Assert
            Assert.Greater(_player.SpecialMeter, 0f, "Special meter should increase after landing a hit");
        }

        [UnityTest]
        public IEnumerator CombatSystem_PlayerAttack_MissesEnemyOutsideZBand()
        {
            // Arrange — move enemy far outside Z-band tolerance (>0.5f)
            _enemyGo.transform.position = new Vector3(1f, 0f, 2f);

            // Act
            _player.TransitionTo(CorgiState.Attack1);
            yield return null; // Wait one frame for the coroutine to execute

            // Assert — no damage should be dealt (Z-band whiff)
            Assert.AreEqual(100, _enemyHealth.CurrentHP, "Enemy outside Z-band should take no damage");
        }

        [UnityTest]
        public IEnumerator CorgiController_AttackLandsHit_FiresEvent()
        {
            // Arrange
            HitResult? received = null;
            _player.OnHitLanded += (r) => received = r;

            // Act
            _player.TransitionTo(CorgiState.Attack1);
            yield return null; // Wait one frame for the coroutine to execute

            // Assert
            Assert.IsTrue(received.HasValue, "OnHitLanded should have fired after a hit connects");
            Assert.IsTrue(received.Value.DidHit, "HitResult.DidHit should be true");
        }

        [UnityTest]
        public IEnumerator CorgiController_Facing_FlipsOnMoveAxisSign()
        {
            // Arrange — default facing is 1 (right)
            Assert.AreEqual(1, _player.Facing, "Default facing should be 1 (right)");

            // Act — record a left-axis input and tick
            _inputBuffer.RecordInput(InputAction.MoveLeft, Time.time, new Vector2(-1f, 0f));
            _player.Tick(0.016f);

            // Assert
            Assert.AreEqual(-1, _player.Facing, "Facing should flip to -1 when moving left");

            // Act — record a right-axis input and tick
            _inputBuffer.RecordInput(InputAction.MoveRight, Time.time, new Vector2(1f, 0f));
            _player.Tick(0.016f);

            // Assert
            Assert.AreEqual(1, _player.Facing, "Facing should flip back to 1 when moving right");

            yield return null; // Required to satisfy IEnumerator
        }
    }
}
