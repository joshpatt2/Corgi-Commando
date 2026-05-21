using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;
using CorgiCommando.Combat;
using CorgiCommando.Data;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Tests for EnvironmentalWeaponEntity — pickup, swing, throw, durability.
    /// </summary>
    [TestFixture]
    public class EnvironmentalWeaponTests
    {
        private GameObject _weaponGo;
        private EnvironmentalWeaponEntity _weapon;
        private GameObject _holderGo;
        private Entity _holder;
        private AttackData _swingData;
        private AttackData _throwData;

        [SetUp]
        public void SetUp()
        {
            _weaponGo = new GameObject("TrashLid");
            _weapon = _weaponGo.AddComponent<EnvironmentalWeaponEntity>();

            _holderGo = new GameObject("Player");
            _holder = _holderGo.AddComponent<Entity>();
            _holder.Faction = Faction.Player;

            _swingData = ScriptableObject.CreateInstance<AttackData>();
            _swingData.attackName = "LidSwing";
            _swingData.damage = 15;

            _throwData = ScriptableObject.CreateInstance<AttackData>();
            _throwData.attackName = "LidThrow";
            _throwData.damage = 25;

            _weapon.Initialize(_swingData, _throwData, 3);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_weaponGo);
            UnityEngine.Object.DestroyImmediate(_holderGo);
            UnityEngine.Object.DestroyImmediate(_swingData);
            UnityEngine.Object.DestroyImmediate(_throwData);
        }

        [Test]
        public void Initialize_WeaponIsPickupable()
        {
            // Assert
            Assert.IsTrue(_weapon.IsPickupable);
            Assert.IsFalse(_weapon.IsHeld);
            Assert.AreEqual(3, _weapon.RemainingUses);
        }

        [Test]
        public void Pickup_SetsHeldState()
        {
            // Act
            _weapon.Pickup(_holder);

            // Assert
            Assert.IsTrue(_weapon.IsHeld);
            Assert.AreEqual(_holder, _weapon.Holder);
            Assert.IsFalse(_weapon.IsPickupable);
        }

        [Test]
        public void Swing_ReturnsAttackDataAndDecrementsUses()
        {
            // Arrange
            _weapon.Pickup(_holder);

            // Act
            var data = _weapon.Swing();

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("LidSwing", data.attackName);
            Assert.AreEqual(2, _weapon.RemainingUses);
        }

        // TODO: Should environmental weapons hit harder than standard combos,
        // or just feel different (wider arc, longer reach)? Decide during prototype playtests.
        [Test]
        public void Throw_ConsumesWeapon()
        {
            // Arrange
            _weapon.Pickup(_holder);

            // Act
            var data = _weapon.Throw();

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual("LidThrow", data.attackName);
            Assert.IsFalse(_weapon.IsHeld);
            Assert.AreEqual(0, _weapon.RemainingUses);
        }

        [Test]
        public void Swing_AfterAllUses_BreaksWeapon()
        {
            // Arrange
            _weapon.Pickup(_holder);
            bool broken = false;
            _weapon.OnWeaponBroken += (w) => broken = true;

            // Act — use all 3 swings
            _weapon.Swing();
            _weapon.Swing();
            _weapon.Swing();

            // Assert
            Assert.IsTrue(broken);
            Assert.AreEqual(0, _weapon.RemainingUses);
            Assert.IsFalse(_weapon.IsHeld);
        }

        [Test]
        public void HeldSpeedMultiplier_DefaultsToSlowed()
        {
            // Assert — holding a weapon should slow movement
            Assert.Less(_weapon.HeldSpeedMultiplier, 1f);
        }
    }
}
