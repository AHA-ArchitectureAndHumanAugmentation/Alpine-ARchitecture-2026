using Compas.Core.Geometry;
using Compas.Core.Transformations;
using Compas.Unity.Core.DataStructures;
using Compas.Unity.Core.Geometry;
using BaseMesh = Compas.Core.Geometry.Mesh;

namespace Compas.Unity.Visualization
{
    /// <summary>
    /// Turns a decoded payload object into something renderable: applies the coordinate-system
    /// conversion, then upcycles the base COMPAS type to its Unity subclass, and hands back the
    /// <see cref="IUnityGeometry"/> view of it.
    ///
    /// Both steps live here rather than on the geometry types because both need the *static*
    /// type in hand - <see cref="ObjectTransformations.Transform{T}"/> is generic over
    /// <c>Geometry{T}</c>, and the upcycle factories (<c>UnityBox.FromBox</c> and friends) are
    /// per-type statics. A payload arriving off the wire is only ever <c>object</c>, so the
    /// type switch has to happen somewhere; keeping it in one place means adding a geometry
    /// type touches this file and nothing else in the visualization path.
    /// </summary>
    public static class UnityGeometryFactory
    {
        /// <summary>
        /// True if <paramref name="item"/> is a geometry type this factory can render. Lets a
        /// caller skip the primitives and strings that share a payload with geometry, without
        /// building anything.
        /// </summary>
        public static bool IsGeometry(object item)
        {
            return item is Box
                || item is Cylinder
                || item is Sphere
                || item is Point
                || item is BaseMesh;
        }

        /// <summary>
        /// Converts one decoded item into a renderable Unity geometry, or returns null if the
        /// item is not geometry (an int, a string, a nested dictionary - all legal payload
        /// content, none of it drawable).
        ///
        /// Items that are already Unity subclasses are still rebuilt, so any GameObject they had
        /// cached is dropped. That is intentional: the scene binder owns GameObject reuse and
        /// re-assigns <see cref="IUnityGeometry.CachedGameObject"/> before rendering.
        /// </summary>
        /// <param name="item">A decoded payload item.</param>
        /// <param name="sourceSystem">
        /// The coordinate system the geometry was authored in. Rhino publishes Z-up, so
        /// <see cref="SupportedSystem.RhinoZUp"/> is the usual value; pass
        /// <see cref="SupportedSystem.Unity"/> to skip conversion entirely.
        /// </param>
        public static IUnityGeometry Create(object item, SupportedSystem sourceSystem)
        {
            switch (item)
            {
                case Box box:
                    return UnityBox.FromBox(ToUnitySystem(box, sourceSystem));
                case Cylinder cylinder:
                    return UnityCylinder.FromCylinder(ToUnitySystem(cylinder, sourceSystem));
                case Sphere sphere:
                    return UnitySphere.FromSphere(ToUnitySystem(sphere, sourceSystem));
                case Point point:
                    return UnityPoint.FromPoint(ToUnitySystem(point, sourceSystem));
                case BaseMesh mesh:
                    return UnityMesh.FromMesh(ToUnitySystem(mesh, sourceSystem));
                default:
                    return null;
            }
        }

        /// <summary>
        /// Applies the source-to-Unity matrix, returning a new instance. A no-op when the source
        /// is already Unity, so a payload published in Unity coordinates is not transformed by an
        /// identity round-trip it does not need.
        /// </summary>
        private static T ToUnitySystem<T>(Geometry<T> geometry, SupportedSystem source)
            where T : Geometry<T>
        {
            if (source == SupportedSystem.Unity)
            {
                return (T)geometry;
            }

            return ObjectTransformations.Transform(geometry, source, SupportedSystem.Unity);
        }

        /// <summary>
        /// Outgoing counterpart of <see cref="ToUnitySystem{T}"/>: applies the Unity-to-target
        /// matrix, or returns the geometry unchanged when the target is already Unity.
        /// </summary>
        public static T FromUnitySystem<T>(Geometry<T> geometry, SupportedSystem target)
            where T : Geometry<T>
        {
            if (target == SupportedSystem.Unity)
            {
                return (T)geometry;
            }

            return ObjectTransformations.Transform(geometry, SupportedSystem.Unity, target);
        }
    }
}
