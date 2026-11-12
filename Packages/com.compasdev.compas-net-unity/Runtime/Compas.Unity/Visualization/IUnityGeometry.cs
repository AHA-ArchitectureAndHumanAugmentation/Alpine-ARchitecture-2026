using Compas.Core.Transformations;
using UnityEngine;

namespace Compas.Unity.Visualization
{
    /// <summary>
    /// Common interface shared by every Unity geometry type (<see cref="DataStructures.UnityMesh"/>,
    /// <see cref="Geometry.UnityBox"/>, <see cref="Geometry.UnityPoint"/>, <see cref="Geometry.UnitySphere"/>,
    /// <see cref="Geometry.UnityCylinder"/>), letting GeometryLogic, the visualizer and the publisher work
    /// with any geometry without knowing its generic type.
    ///
    /// Covers both directions: data -> scene (<see cref="ToGameObject"/>) and
    /// scene -> data (<see cref="ToData"/>). Each implementer handles its own outgoing
    /// coordinate conversion, since that is where the static type needed by
    /// ObjectTransformations.Transform&lt;T&gt; is known.
    /// </summary>
    public interface IUnityGeometry
    {
        /// <summary>
        /// The GameObject currently representing this geometry, if any. The scene binder sets
        /// this to an existing object before calling <see cref="ToGameObject"/> so that repeat
        /// messages update in place instead of spawning a new object. Each geometry type maps
        /// this to its own cache (MeshGameObject, BoxGameObject, ...).
        /// </summary>
        GameObject CachedGameObject { get; set; }

        /// <summary>
        /// Creates the GameObject for this geometry, or updates <see cref="CachedGameObject"/> if
        /// one is already set, and returns it.
        /// </summary>
        GameObject ToGameObject();

        /// <summary>
        /// Reads <see cref="CachedGameObject"/> back (so moves in the scene are picked up) and
        /// returns the geometry converted from Unity to <paramref name="target"/>, ready to publish.
        /// Falls back to the stored values when there is no GameObject.
        /// </summary>
        object ToData(SupportedSystem target);
    }
}
