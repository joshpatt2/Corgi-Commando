using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Camera;
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
        private const float HeavyShakeIntensity = 0.2f;
        private const float SpecialShakeIntensity = 0.3f;
        private const float NoShakeIntensity = 0f;

        private CombatSystem _combat;
        private GameObject _attackerGo;
        private GameObject _targetGo;
        private Entity _attacker;
        private Entity _target;
        private AttackData _punchData;
        private GameObject _screenShakeGo;
        private TestScreenShakeHandler _screenShakeHandler;

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

            _screenShakeGo = new GameObject("ScreenShakeHandler");
            _screenShakeHandler = _screenShakeGo.AddComponent<TestScreenShakeHandler>();
            _screenShakeHandler.SetCombatSystem(_combat);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_attackerGo);
            UnityEngine.Object.DestroyImmediate(_targetGo);
            UnityEngine.Object.DestroyImmediate(_screenShakeGo);
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

        [Test]
        public void ResolveAttack_MultipleTargetsInZBand_AllTakeDamage()
        {
            // Arrange — two enemies both at Z=0, within ±0.5 band
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);

            var target2Go = new GameObject("Target2");
            try
            {
                var target2 = target2Go.AddComponent<Entity>();
                target2.Faction = Faction.Enemy;
                target2Go.transform.position = new Vector3(-1f, 0f, 0f);
                var health2 = new HealthComponent(100);
                target2.AddEntityComponent<IHealthComponent>(health2);
                var hurtbox2 = new HurtboxComponent();
                target2.AddEntityComponent<HurtboxComponent>(hurtbox2);
                target2.AddEntityComponent<KnockbackReceiver>(new KnockbackReceiver());

                // Act
                var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target, target2 });

                // Assert — both enemies should have taken damage
                Assert.IsTrue(result.DidHit);
                Assert.AreEqual(90, _target.GetEntityComponent<IHealthComponent>().CurrentHP);
                Assert.AreEqual(90, health2.CurrentHP);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target2Go);
            }
        }

        [Test]
        public void ResolveAttack_FriendlyFire_Whiffs()
        {
            // Arrange — target set to same faction as attacker
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _target.Faction = Faction.Player;

            // Act
            var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.IsFalse(result.DidHit);
        }

        [Test]
        public void ResolveAttack_DisabledHurtbox_Whiffs()
        {
            // Arrange — hurtbox explicitly disabled (invincibility frames)
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _target.GetEntityComponent<HurtboxComponent>().Disable();

            // Act
            var result = _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Assert
            Assert.IsFalse(result.DidHit);
        }

        [Test]
        public void Hitstop_ClearsAfterDuration()
        {
            // Arrange
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            bool hitstopEnded = false;
            _combat.OnHitstopEnded += () => hitstopEnded = true;

            // Act — resolve attack to start hitstop (4 frames at 60fps = 4/60f ≈ 0.067s)
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });
            Assert.IsTrue(_combat.IsInHitstop, "IsInHitstop should be true immediately after hit");

            // Tick past hitstop duration
            _combat.Tick(0.1f);

            // Assert
            Assert.IsFalse(_combat.IsInHitstop);
            Assert.IsTrue(hitstopEnded);
        }

        [Test]
        public void ConsumeSpecialMeter_HappyPath_ReturnsTrue()
        {
            // Arrange — land a hit so meter is at 10
            _attackerGo.transform.position = new Vector3(0f, 0f, 0f);
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _combat.ResolveAttack(_attacker, _punchData, new[] { _target });

            // Act — consume less than what's available
            bool consumed = _combat.ConsumeSpecialMeter(_attacker, 5f);

            // Assert
            Assert.IsTrue(consumed);
            Assert.Less(_combat.GetSpecialMeter(_attacker), 10f);
        }

        [Test]
        public void ConsumeSpecialMeter_InsufficientMeter_ReturnsFalse()
        {
            // Arrange — meter starts at 0
            // Act — attempt to consume more than available
            bool consumed = _combat.ConsumeSpecialMeter(_attacker, 50f);

            // Assert
            Assert.IsFalse(consumed);
            Assert.AreEqual(0f, _combat.GetSpecialMeter(_attacker));
        }

        [Test]
        public void Combat_HeavyHit_FiresImpulse()
        {
            // Arrange
            _attackerGo.transform.position = Vector3.zero;
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _screenShakeHandler.MinimumShakeIntensity = 0.05f;
            var heavyAttack = CreateAttackData("HeavyKick", HeavyShakeIntensity, HitType.Heavy);

            try
            {
                // Act
                _combat.ResolveAttack(_attacker, heavyAttack, new[] { _target });

                // Assert
                Assert.AreEqual(1, _screenShakeHandler.ImpulseCount);
                Assert.Greater(_screenShakeHandler.LastMagnitude, 0f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heavyAttack);
            }
        }

        [Test]
        public void Combat_LightHit_DoesNotFireImpulse()
        {
            // Arrange
            _attackerGo.transform.position = Vector3.zero;
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _screenShakeHandler.MinimumShakeIntensity = 0.05f;
            var lightAttack = CreateAttackData("LightJab", NoShakeIntensity, HitType.Light);

            try
            {
                // Act
                _combat.ResolveAttack(_attacker, lightAttack, new[] { _target });

                // Assert
                Assert.AreEqual(0, _screenShakeHandler.ImpulseCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightAttack);
            }
        }

        [Test]
        public void Combat_Special_FiresImpulseWithHigherMagnitude()
        {
            // Arrange
            _attackerGo.transform.position = Vector3.zero;
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _screenShakeHandler.MinimumShakeIntensity = 0.05f;
            var heavyAttack = CreateAttackData("HeavyKick", HeavyShakeIntensity, HitType.Heavy);
            var specialAttack = CreateAttackData("BarkShockwave", SpecialShakeIntensity, HitType.Special);

            try
            {
                // Act
                _combat.ResolveAttack(_attacker, heavyAttack, new[] { _target });
                float heavyMagnitude = _screenShakeHandler.LastMagnitude;
                _combat.ResolveAttack(_attacker, specialAttack, new[] { _target });
                float specialMagnitude = _screenShakeHandler.LastMagnitude;

                // Assert
                Assert.AreEqual(2, _screenShakeHandler.ImpulseCount);
                Assert.Greater(specialMagnitude, heavyMagnitude);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(specialAttack);
            }
        }

        [Test]
        public void Combat_HitWithHitstop_SchedulesDelayedImpulse()
        {
            // Arrange
            _attackerGo.transform.position = Vector3.zero;
            _targetGo.transform.position = new Vector3(1f, 0f, 0f);
            _screenShakeHandler.MinimumShakeIntensity = 0.05f;
            var heavyAttack = CreateAttackData("HeavyKick", HeavyShakeIntensity, HitType.Heavy, hitstopFrames: 6);

            try
            {
                // Act
                _combat.ResolveAttack(_attacker, heavyAttack, new[] { _target });

                // Assert
                Assert.AreEqual(0, _screenShakeHandler.ImpulseCount);
                Assert.AreEqual(0.1f, _screenShakeHandler.LastScheduledDelay, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heavyAttack);
            }
        }

        private static AttackData CreateAttackData(string attackName, float shakeIntensity, HitType hitType, int hitstopFrames = 0)
        {
            var attack = ScriptableObject.CreateInstance<AttackData>();
            attack.attackName = attackName;
            attack.damage = 10;
            attack.knockbackForce = new Vector3(3f, 1f, 0f);
            attack.hitstopFrames = hitstopFrames;
            attack.hitType = hitType;
            attack.screenShakeIntensity = shakeIntensity;
            attack.hitboxRect = new Rect(0.5f, -0.25f, 1f, 0.5f);
            return attack;
        }

        private sealed class TestScreenShakeHandler : ScreenShakeHandler
        {
            public int ImpulseCount { get; private set; }
            public float LastMagnitude { get; private set; }
            public float LastScheduledDelay { get; private set; } = -1f;

            protected override void EmitImpulse(float magnitude)
            {
                ImpulseCount++;
                LastMagnitude = magnitude;
            }

            protected override void ScheduleDelayedImpulse(float delaySeconds, float magnitude)
            {
                LastScheduledDelay = delaySeconds;
            }
        }
    }
}
