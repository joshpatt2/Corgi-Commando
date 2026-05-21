using System;
using NUnit.Framework;
using UnityEngine;
using CorgiCommando.Core;

namespace CorgiCommando.Tests.EditMode
{
    /// <summary>
    /// Edit Mode tests for Entity composition system — component add/remove/get,
    /// health, hurtbox, knockback. Pure logic tests where possible.
    /// </summary>
    [TestFixture]
    public class EntityComponentTests
    {
        [Test]
        public void Entity_NewEntity_IsAliveByDefault()
        {
            // Arrange & Act
            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<Entity>();

            // Assert
            // Design intent: entities are alive when created; death is an explicit event
            Assert.IsTrue(entity.IsAlive);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Entity_FactionCanBeSet()
        {
            // Arrange
            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<Entity>();

            // Act
            entity.Faction = Faction.Enemy;

            // Assert
            Assert.AreEqual(Faction.Enemy, entity.Faction);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void AddEntityComponent_ComponentCanBeRetrieved()
        {
            // Arrange
            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<Entity>();
            var health = new HealthComponent(100);

            // Act
            entity.AddEntityComponent<IHealthComponent>(health);
            var retrieved = entity.GetEntityComponent<IHealthComponent>();

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(100, retrieved.MaxHP);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void RemoveEntityComponent_ComponentNoLongerRetrievable()
        {
            // Arrange
            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<Entity>();
            var health = new HealthComponent(100);
            entity.AddEntityComponent<IHealthComponent>(health);

            // Act
            bool removed = entity.RemoveEntityComponent<IHealthComponent>();

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNull(entity.GetEntityComponent<IHealthComponent>());

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HasEntityComponent_ReturnsTrueWhenAttached()
        {
            // Arrange
            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<Entity>();
            var health = new HealthComponent(50);
            entity.AddEntityComponent<IHealthComponent>(health);

            // Act & Assert
            Assert.IsTrue(entity.HasEntityComponent<IHealthComponent>());

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HasEntityComponent_ReturnsFalseWhenNotAttached()
        {
            // Arrange
            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<Entity>();

            // Act & Assert
            Assert.IsFalse(entity.HasEntityComponent<IHealthComponent>());

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HealthComponent_TakeDamage_ReducesHP()
        {
            // Arrange
            var health = new HealthComponent(100);

            // Act
            health.TakeDamage(30);

            // Assert
            Assert.AreEqual(70, health.CurrentHP);
        }

        [Test]
        public void HealthComponent_TakeDamage_FiresOnDamagedEvent()
        {
            // Arrange
            var health = new HealthComponent(100);
            int reportedDamage = 0;
            health.OnDamaged += (dmg) => reportedDamage = dmg;

            // Act
            health.TakeDamage(25);

            // Assert
            Assert.AreEqual(25, reportedDamage);
        }

        [Test]
        public void HealthComponent_TakeFatalDamage_FiresOnDiedEvent()
        {
            // Arrange
            var health = new HealthComponent(50);
            bool died = false;
            health.OnDied += () => died = true;

            // Act
            health.TakeDamage(50);

            // Assert
            Assert.IsTrue(died);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(0, health.CurrentHP);
        }

        [Test]
        public void HealthComponent_TakeDamage_ClampsToZero()
        {
            // Arrange
            var health = new HealthComponent(30);

            // Act — overkill damage
            health.TakeDamage(999);

            // Assert — HP should not go negative
            Assert.AreEqual(0, health.CurrentHP);
        }

        [Test]
        public void HealthComponent_Heal_ClampsToMax()
        {
            // Arrange
            var health = new HealthComponent(100);
            health.TakeDamage(20);

            // Act — heal more than missing
            health.Heal(999);

            // Assert
            Assert.AreEqual(100, health.CurrentHP);
        }

        [Test]
        public void HurtboxComponent_DefaultEnabled()
        {
            // Arrange & Act
            var hurtbox = new HurtboxComponent();

            // Assert
            // Design intent: hurtboxes are enabled by default when attached.
            // Disable is an explicit action (invincibility, knockdown, etc.)
            Assert.IsTrue(hurtbox.IsEnabled);
        }

        [Test]
        public void HurtboxComponent_Disable_PreventsHits()
        {
            // Arrange
            var hurtbox = new HurtboxComponent();

            // Act
            hurtbox.Disable();

            // Assert
            Assert.IsFalse(hurtbox.IsEnabled);
        }

        [Test]
        public void HurtboxComponent_EnableAfterDisable_Restores()
        {
            // Arrange
            var hurtbox = new HurtboxComponent();
            hurtbox.Disable();

            // Act
            hurtbox.Enable();

            // Assert
            Assert.IsTrue(hurtbox.IsEnabled);
        }

        [Test]
        public void KnockbackReceiver_ApplyKnockback_SetsVelocity()
        {
            // Arrange
            var knockback = new KnockbackReceiver();
            var impulse = new Vector3(5f, 2f, 0f);

            // Act
            knockback.ApplyKnockback(impulse);

            // Assert
            Assert.AreEqual(impulse, knockback.KnockbackVelocity);
            Assert.IsTrue(knockback.IsInKnockback);
        }

        [Test]
        public void KnockbackReceiver_ApplyKnockback_FiresEvent()
        {
            // Arrange
            var knockback = new KnockbackReceiver();
            Vector3 reported = Vector3.zero;
            knockback.OnKnockbackApplied += (v) => reported = v;
            var impulse = new Vector3(3f, 1f, 0f);

            // Act
            knockback.ApplyKnockback(impulse);

            // Assert
            Assert.AreEqual(impulse, reported);
        }

        [Test]
        public void KnockbackReceiver_ClearKnockback_ResetsState()
        {
            // Arrange
            var knockback = new KnockbackReceiver();
            knockback.ApplyKnockback(new Vector3(5f, 2f, 0f));

            // Act
            knockback.ClearKnockback();

            // Assert
            Assert.IsFalse(knockback.IsInKnockback);
            Assert.AreEqual(Vector3.zero, knockback.KnockbackVelocity);
        }
    }
}
