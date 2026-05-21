namespace CorgiCommando.Core
{
    /// <summary>
    /// Marker interface for all components that can be attached to an Entity.
    /// Components are composed onto entities rather than using deep inheritance.
    /// </summary>
    public interface IEntityComponent
    {
        /// <summary>The entity this component is attached to.</summary>
        Entity Owner { get; }

        /// <summary>
        /// Called when the component is added to an entity.
        /// </summary>
        void OnAttach(Entity owner);

        /// <summary>
        /// Called when the component is removed from an entity.
        /// </summary>
        void OnDetach();
    }
}
