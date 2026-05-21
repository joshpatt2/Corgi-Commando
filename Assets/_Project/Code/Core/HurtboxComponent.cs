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

        public HurtboxComponent()
        {
            IsEnabled = true;
        }

        public void OnAttach(Entity owner)
        {
            Owner = owner;
        }

        public void OnDetach()
        {
            Owner = null;
        }

        /// <summary>Enables the hurtbox so it can receive hits.</summary>
        public void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>Disables the hurtbox (invincibility, knockdown, etc.).</summary>
        public void Disable()
        {
            IsEnabled = false;
        }
    }
}
