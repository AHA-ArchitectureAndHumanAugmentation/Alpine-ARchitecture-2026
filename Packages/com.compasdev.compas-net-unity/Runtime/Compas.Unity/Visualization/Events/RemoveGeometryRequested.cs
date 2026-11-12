using Compas.Events;

namespace Compas.Unity.Visualization.Events
{
    /// <summary>
    /// Which geometry a removal request applies to. Geometry reaches a scene from two
    /// directions and each side owns its own objects, so a request names the side rather than
    /// the objects.
    /// </summary>
    public enum GeometryScope
    {
        /// <summary>Everything, whichever side put it there.</summary>
        All,

        /// <summary>Geometry received over the wire and drawn by a visualizer.</summary>
        Incoming,

        /// <summary>Geometry spawned locally, which is what gets published back.</summary>
        Outgoing,

        /// <summary>Only whatever currently holds interaction focus.</summary>
        Selected,
    }

    /// <summary>
    /// Asks for geometry to be removed from the scene.
    ///
    /// One event with a scope rather than one event per button: the UI states what the user
    /// asked for, and each side decides whether that includes it. Nothing has to know how many
    /// removal buttons exist or who else is listening.
    /// </summary>
    public readonly struct RemoveGeometryRequested : IEvent
    {
        /// <summary>The geometry this request applies to.</summary>
        public readonly GeometryScope scope;

        public RemoveGeometryRequested(GeometryScope scope)
        {
            this.scope = scope;
        }

        /// <summary>
        /// Whether a handler owning <paramref name="owned"/> should act on this request.
        /// True for its own scope and for <see cref="GeometryScope.All"/>.
        /// </summary>
        public bool Includes(GeometryScope owned)
        {
            return scope == owned || scope == GeometryScope.All;
        }
    }
}
