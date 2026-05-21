using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Combat;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Edit Mode tests for CombatSystem — hit resolution, Z-band rules,
    /// combo tracking, special meter, hitstop.
    /// </summary>
    [TestFixture]
    public class CombatSystemTests
    {
        private CombatSystem _combat;
        private GameObject _attackerGo;
        private GameObject _targetGo;
        private Entity _attacker;
        private Entity _target;
        private AttackData _punchData;

        [SetUp]
        public void SetUp()
        {
            _combat = new CombatSystem();

            _attackerGo = new GameObject("Attacker");
            _attacker = _attackerGo.AddComponent<Entity>();
            _attacker.Faction = Faction.Player;

            _targetGo = new GameObject("Target");
            _target = _targetGo.AddComponent<Entity>();
            _target.Faction = Faction.Enemy;

            // Add health and hurtbox to target
            var health = new HealthComponent(100);
            _target.AddEntityComponent<IHealthComponent>(health);
            var hurtbox = new HurtboxComponent();
            _target.AddEntityComponent<HurtboxComponent>(hurtbox);
            var knockback = new KnockbackReceiver();
            _target.AddEntityComponent<KnockbackReceiver>(knockback);

            // Create attack data
            _punchData = ScriptableObject.CreateInstance<AttackData>();
            _punchData.attackName = "Punch";
            _punchData.damage = 10;
            _punchData.knockbackForce = new Vector3(3f, 1f, 0f);
            _punchData.hitstopFrames = 4;
            _punchData.hitType = HitType.Light;
            _punchData.hitboxRect = new Rect(0.5f, -0.25f, 1f, 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_attackerGo);
            UnityEngine.Object.DestroyImmediate(_targetGo);
            UnityEngine.Object.DestroyImmediate(_punchData);
        }

        [Test]
        public void ResolveAttack_TargetInZBand_HitConnects()
        {
            // Arrange — both at Z=0 (within ±0.5 band)
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);

            // Act
            var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.IsTrue(result.DidHit);
            Assert.AreEqual(_target, result.Target);
        }

        [Test]
        public void ResolveAttack_TargetOutsideZBand_Whiffs()
        {
            // Arrange — target at Z=2.0, outside ±0.5 band
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 2f);

            // Act
            var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.IsFalse(result.DidHit);
        }

        [Test]
        public void ResolveAttack_HitConnects_AppliesDamage()
        {
            // Arrange
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);

            // Act
            var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.AreEqual(_punchData.damage, result.DamageDealt);
        }

        [Test]
        public void ResolveAttack_HitConnects_ReturnsCorrectHitstop()
        {
            // Arrange
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);

            // Act
            var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert — hitstop frames should match attack data
            Assert.AreEqual(_punchData.hitstopFrames, result.HitstopFrames);
        }

        [Test]
        public void ResolveAttack_HitConnects_FiresOnHitConnectedEvent()
        {
            // Arrange
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            HitResult? reported = null;
            _combat.OnHitConnected += (r) => reported = r;

            // Act
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.IsTrue(reported.HasValue);
            Assert.IsTrue(reported.Value.DidHit);
        }

        [Test]
        public void ComboCounter_IncrementsOnChainedHits()
        {
            // Arrange — hit twice in rapid succession
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);

            // Act
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.AreEqual(2, _combat.GetComboCount(_attacker));
        }

        [Test]
        public void ComboCounter_ResetsAfterTimeout()
        {
            // Arrange
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Act — tick past the combo timeout
            _combat.Tick(CombatSystem.ComboTimeoutSeconds + 0.1f);

            // Assert
            Assert.AreEqual(0, _combat.GetComboCount(_attacker));
        }

        [Test]
        public void SpecialMeter_FillsOnHitLanded()
        {
            // Arrange
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);

            // Act
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert — meter should be > 0 after landing a hit
            Assert.Greater(_combat.GetSpecialMeter(_attacker), 0f);
        }
    }
}
