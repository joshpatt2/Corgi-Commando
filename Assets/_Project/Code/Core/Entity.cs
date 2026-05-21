using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Base class for all combat-relevant actors (players, enemies, env weapons, bosses).
    /// Provides component composition, faction, and alive state.
    /// Static level geometry is NOT an entity — just colliders.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        private readonly Dictionary<Type, IEntityComponent> _components = new Dictionary<Type, IEntityComponent>();

        /// <summary>Faction affiliation for targeting and friendly-fire rules.</summary>
        public Faction Faction { get; set; }

        /// <summary>Whether this entity is alive (not destroyed/dead).</summary>
        public bool IsAlive { get; protected set; } = true;

        /// <summary>Fired when this entity dies (HP reaches zero).</summary>
        public event Action<Entity> OnDeath;

        /// <summary>
        /// Adds a component of type T to this entity.
        /// </summary>
        public void AddEntityComponent<T>(T component) where T : class, IEntityComponent
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var type = typeof(T);
            if (_components.TryGetValue(type, out var existing))
            {
                if (existing is IHealthComponent existingHealth)
                {
                    existingHealth.OnDied -= RaiseDeath;
                }

                existing.OnDetach();
            }

            _components[type] = component;
            component.OnAttach(this);

            if (component is IHealthComponent health)
            {
                health.OnDied += RaiseDeath;
            }
        }

        /// <summary>
        /// Removes the component of type T from this entity.
        /// Returns true if a component was removed.
        /// </summary>
        public bool RemoveEntityComponent<T>() where T : class, IEntityComponent
        {
            var type = typeof(T);
            if (!_components.TryGetValue(type, out var component))
            {
                return false;
            }

            if (component is IHealthComponent health)
            {
                health.OnDied -= RaiseDeath;
            }

            component.OnDetach();
            _components.Remove(type);
            return true;
        }

        /// <summary>
        /// Gets the component of type T, or null if not attached.
        /// </summary>
        public T GetEntityComponent<T>() where T : class, IEntityComponent
        {
            return _components.TryGetValue(typeof(T), out var component) ? component as T : null;
        }

        /// <summary>
        /// Returns true if a component of type T is attached.
        /// </summary>
        public bool HasEntityComponent<T>() where T : class, IEntityComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Invokes the OnDeath event. Called by HealthComponent when HP reaches zero.
        /// </summary>
        protected void RaiseDeath()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            OnDeath?.Invoke(this);
        }
    }
}
