using System;
using UnityEngine;

namespace CorgiCommando.Core
{
    /// <summary>
    /// Defines the hurtbox (vulnerable area) for an entity.
    /// Can be enabled/disabled (e.g., during invincibility frames).
    /// </summary>
    public class HurtboxComponent : IEntityComponent
    {
        public Entity Owner { get; private set; }

        /// <summary>Whether this hurtbox is currently active and can receive hits.</summary>
        public bool IsEnabled { get; private set; }

        /// <summary>The bounding rect of the hurtbox in local space.</summary>
        public Rect Bounds { get; set; }

        public void OnAttach(Entity owner)
        {
            throw new NotImplementedException();
        }

        public void OnDetach()
        {
            throw new NotImplementedException();
        }

        /// <summary>Enables the hurtbox so it can receive hits.</summary>
        public void Enable()
        {
            throw new NotImplementedException();
        }

        /// <summary>Disables the hurtbox (invincibility, knockdown, etc.).</summary>
        public void Disable()
        {
            throw new NotImplementedException();
        }
    }
}
