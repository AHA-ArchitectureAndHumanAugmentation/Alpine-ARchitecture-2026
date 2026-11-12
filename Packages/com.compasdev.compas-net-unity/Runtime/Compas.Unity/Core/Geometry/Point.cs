using Compas.Core.Geometry;
using Compas.Core.Transformations;
using Compas.Unity.Data;
using Compas.Unity.Visualization;
using CompasPb.Data;
using System;
using UnityEngine;

namespace Compas.Unity.Core.Geometry
{
    /// <summary>
    /// Unity-native representation of a point in 3D space.
    /// Extends the COMPAS Point class with Unity GameObject integration and visualization capabilities.
    /// </summary>
    [System.Serializable]
    public class UnityPoint : Point, IUnityProtoConvertible<UnityPoint, PointData>, IUnityGeometry
    {
        /// <summary>Render-contract view of <see cref="PointGameObject"/>.</summary>
        public GameObject CachedGameObject
        {
            get => PointGameObject;
            set => PointGameObject = value;
        }

        /// <summary>
        /// Render-contract entry point. Delegates to <see cref="ToGameObject(float?)"/> with the
        /// default radius. Explicit so it does not collide with the radius-taking overload.
        /// </summary>
        GameObject IUnityGeometry.ToGameObject() => ToGameObject(null);

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityPoint"/> class with the specified coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <param name="y">The y-coordinate of the point.</param>
        /// <param name="z">The z-coordinate of the point.</param>
        public UnityPoint(float x, float y, float z) : base(x, y, z)
        {
        }

        /// <summary>
        /// Gets or sets the GameObject representation of this point.
        /// </summary>
        /// <remarks>
        /// This property stores a reference to the Unity GameObject visualization of the point.
        /// Created by calling <see cref="ToGameObject"/> or <see cref="FromGameObject"/>.
        /// </remarks>
        public GameObject PointGameObject { get; set; }

        /// <summary>
        /// Gets or sets the radius used for the sphere visualization of this point.
        /// Default value is 0.1 units.
        /// </summary>
        public float DisplayRadius { get; set; } = 0.01f;

        /// <summary>
        /// Creates or updates the Unity GameObject representation and stores it in PointGameObject.
        /// </summary>
        /// <param name="radius">The radius for the visual representation (default: uses DisplayRadius property)</param>
        public GameObject ToGameObject(float? radius = null)
        {
            float effectiveRadius = radius ?? DisplayRadius;
            GameObject pointObject = PointGameObject ?? GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointObject.transform.localScale = new Vector3(effectiveRadius * 2, effectiveRadius * 2, effectiveRadius * 2);
            pointObject.transform.position = new Vector3(X, Y, Z);
            PointGameObject = pointObject;
            DisplayRadius = effectiveRadius;
            return pointObject;
        }

        /// <summary>
        /// Creates a UnityPoint from a Unity GameObject and stores the reference in PointGameObject.
        /// Uses localScale (authored size) rather than lossyScale (world size).
        /// </summary>
        public static UnityPoint FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            Transform transform = gameObject.transform;
            Vector3 position = transform.position;
            Vector3 scale = transform.localScale;

            // Extract radius from sphere scale
            float radius = scale.x / 2f;

            UnityPoint unityPoint = new UnityPoint(position.x, position.y, position.z)
            {
                PointGameObject = gameObject,
                DisplayRadius = radius
            };
            return unityPoint;
        }

        /// <inheritdoc/>
        public object ToData(SupportedSystem target)
        {
            UnityPoint current = PointGameObject != null ? FromGameObject(PointGameObject) : this;
            return UnityGeometryFactory.FromUnitySystem(current, target);
        }

        /// <summary>
        /// Converts this UnityPoint to its protobuf representation.
        /// </summary>
        public new PointData ToProto() => base.ToProto();

        /// <summary>
        /// Creates a UnityPoint from protobuf data.
        /// </summary>
        public static new UnityPoint FromProto(PointData pointData)
        {
            return FromPoint(Point.FromProto(pointData));
        }

        /// <summary>
        /// Creates a UnityPoint from a CompasNet Point instance.
        /// </summary>
        public static UnityPoint FromPoint(Point point)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            return new UnityPoint(point.X, point.Y, point.Z);
        }
    }
}