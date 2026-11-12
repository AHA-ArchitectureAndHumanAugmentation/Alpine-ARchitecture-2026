using Compas.Core.Geometry;
using Compas.Core.Transformations;
using Compas.Unity.Core.Conversions;
using Compas.Unity.Data;
using Compas.Unity.Visualization;
using CompasPb.Data;
using System;
using UnityEngine;

namespace Compas.Unity.Core.Geometry
{
    /// <summary>
    /// Unity-native representation of a sphere in 3D space.
    /// Extends the COMPAS Sphere class with Unity GameObject integration and visualization capabilities.
    /// </summary>
    [System.Serializable]
    public class UnitySphere : Sphere, IUnityProtoConvertible<UnitySphere, SphereData>, IUnityGeometry
    {
        /// <summary>Render-contract view of <see cref="SphereGameObject"/>.</summary>
        public GameObject CachedGameObject
        {
            get => SphereGameObject;
            set => SphereGameObject = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitySphere"/> class with the specified frame and radius.
        /// </summary>
        /// <param name="frame">The frame defining the sphere's position and orientation.</param>
        /// <param name="radius">The radius of the sphere in Unity units.</param>
        public UnitySphere(Frame frame, float radius) : base(frame, radius)
        {
        }

        /// <summary>
        /// Gets or sets the GameObject representation of this sphere.
        /// </summary>
        /// <remarks>
        /// This property stores a reference to the Unity GameObject visualization of the sphere.
        /// Created by calling <see cref="ToGameObject"/> or <see cref="FromGameObject"/>.
        /// </remarks>
        public GameObject SphereGameObject { get; set; }

        /// <summary>
        /// Creates or updates the Unity GameObject representation and stores it in SphereGameObject.
        /// </summary>
        public GameObject ToGameObject()
        {
            GameObject sphereObject = SphereGameObject ?? GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObject.transform.localScale = new Vector3(Radius * 2, Radius * 2, Radius * 2);
            var (position, rotation) = GeometryConversions.FrameToTransformValues(Frame);
            sphereObject.transform.SetPositionAndRotation(position, rotation);
            SphereGameObject = sphereObject;
            return sphereObject;
        }

        /// <summary>
        /// Creates a UnitySphere from a Unity GameObject and stores the reference in SphereGameObject.
        /// Uses localScale (authored size) rather than lossyScale (world size).
        /// TODO: Consider adding an overload or flag to support world-space scale extraction if needed.
        /// </summary>
        public static UnitySphere FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            Transform transform = gameObject.transform;
            Frame frame = GeometryConversions.FrameFromTransform(transform);
            Vector3 scale = transform.localScale;

            // Unity sphere: radius = scale.x/2 (assuming uniform scale)
            float radius = scale.x / 2f;

            UnitySphere unitySphere = new UnitySphere(frame, radius)
            {
                SphereGameObject = gameObject
            };
            return unitySphere;
        }

        /// <inheritdoc/>
        public object ToData(SupportedSystem target)
        {
            UnitySphere current = SphereGameObject != null ? FromGameObject(SphereGameObject) : this;
            return UnityGeometryFactory.FromUnitySystem(current, target);
        }

        /// <summary>
        /// Converts this UnitySphere to its protobuf representation.
        /// </summary>
        public new SphereData ToProto() => base.ToProto();

        /// <summary>
        /// Creates a UnitySphere from protobuf data.
        /// </summary>
        public static new UnitySphere FromProto(SphereData sphereData)
        {
            return FromSphere(Sphere.FromProto(sphereData));
        }

        /// <summary>
        /// Creates a UnitySphere from a CompasNet Sphere instance.
        /// </summary>
        public static UnitySphere FromSphere(Sphere sphere)
        {
            if (sphere == null)
            {
                throw new ArgumentNullException(nameof(sphere));
            }

            return new UnitySphere(sphere.Frame, sphere.Radius);
        }
    }
}