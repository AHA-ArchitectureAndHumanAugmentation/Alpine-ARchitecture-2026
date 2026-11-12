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
    /// Unity-native representation of a cylinder in 3D space.
    /// Extends the COMPAS Cylinder class with Unity GameObject integration and visualization capabilities.
    /// </summary>
    [System.Serializable]
    public class UnityCylinder : Cylinder, IUnityProtoConvertible<UnityCylinder, CylinderData>, IUnityGeometry
    {
        /// <summary>Render-contract view of <see cref="CylinderGameObject"/>.</summary>
        public GameObject CachedGameObject
        {
            get => CylinderGameObject;
            set => CylinderGameObject = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityCylinder"/> class with the specified parameters.
        /// </summary>
        /// <param name="frame">The frame defining the cylinder's position and orientation.</param>
        /// <param name="radius">The radius of the cylinder in Unity units.</param>
        /// <param name="height">The height of the cylinder in Unity units.</param>
        public UnityCylinder(Frame frame, float radius, float height) : base(frame, radius, height)
        {
        }

        /// <summary>
        /// Gets or sets the GameObject representation of this cylinder.
        /// </summary>
        /// <remarks>
        /// This property stores a reference to the Unity GameObject visualization of the cylinder.
        /// Created by calling <see cref="ToGameObject"/> or <see cref="FromGameObject"/>.
        /// </remarks>
        public GameObject CylinderGameObject { get; set; }

        /// <summary>
        /// Aligns Unity's built-in cylinder mesh with the COMPAS cylinder axis.
        /// </summary>
        private static readonly Quaternion PrimitiveMeshAxisCorrection = Quaternion.Euler(90f, 0f, 0f);

        /// <summary>
        /// Creates or updates the Unity GameObject representation and stores it in CylinderGameObject.
        /// Enforces a perfect cylinder with circular cross-section (X and Z scale are equal).
        /// NOTE: If the original GameObject had non-uniform X/Z scales (e.g., elliptical), those are normalized to the X value.
        /// TODO: Consider supporting elliptical cylinders by adding a RadiusZ property if non-uniform scaling is needed.
        /// </summary>
        public GameObject ToGameObject()
        {
            GameObject cylinderObject = CylinderGameObject ?? GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderObject.transform.localScale = new Vector3(Radius * 2, Height / 2, Radius * 2);
            var (position, rotation) = GeometryConversions.FrameToTransformValues(Frame);
            cylinderObject.transform.SetPositionAndRotation(position, rotation * PrimitiveMeshAxisCorrection);
            CylinderGameObject = cylinderObject;
            return cylinderObject;
        }

        /// <summary>
        /// Creates a UnityCylinder from a Unity GameObject and stores the reference in CylinderGameObject.
        /// Uses localScale (authored size) rather than lossyScale (world size).
        /// Extracts radius from scale.x only, enforcing a perfect cylinder (circular cross-section).
        /// NOTE: If the input GameObject has non-uniform X/Z scales (elliptical), the Z value is ignored.
        /// TODO: Consider adding a RadiusZ property to support elliptical cylinders if needed.
        /// TODO: Consider adding an overload or flag to support world-space scale extraction if needed.
        /// </summary>
        public static UnityCylinder FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            Transform transform = gameObject.transform;
            // Undo the primitive-mesh axis correction applied by ToGameObject, so the frame read
            // back here is the COMPAS frame rather than the rotated Unity primitive's.
            Frame frame = GeometryConversions.FrameFromTransform(
                transform.position,
                transform.rotation * Quaternion.Inverse(PrimitiveMeshAxisCorrection));
            Vector3 scale = transform.localScale;

            // Unity cylinder: radius = scale.x/2, height = scale.y*2
            float radius = scale.x / 2f;
            float height = scale.y * 2f;

            UnityCylinder unityCylinder = new UnityCylinder(frame, radius, height)
            {
                CylinderGameObject = gameObject
            };
            return unityCylinder;
        }

        /// <inheritdoc/>
        public object ToData(SupportedSystem target)
        {
            UnityCylinder current = CylinderGameObject != null ? FromGameObject(CylinderGameObject) : this;
            return UnityGeometryFactory.FromUnitySystem(current, target);
        }

        /// <summary>
        /// Converts this UnityCylinder to its protobuf representation.
        /// </summary>
        public new CylinderData ToProto() => base.ToProto();

        /// <summary>
        /// Creates a UnityCylinder from protobuf data.
        /// </summary>
        public static new UnityCylinder FromProto(CylinderData cylinderData)
        {
            return FromCylinder(Cylinder.FromProto(cylinderData));
        }

        /// <summary>
        /// Creates a UnityCylinder from a CompasNet Cylinder instance.
        /// </summary>
        public static UnityCylinder FromCylinder(Cylinder cylinder)
        {
            if (cylinder == null)
            {
                throw new ArgumentNullException(nameof(cylinder));
            }

            return new UnityCylinder(cylinder.Frame, cylinder.Radius, cylinder.Height);
        }
    }
}