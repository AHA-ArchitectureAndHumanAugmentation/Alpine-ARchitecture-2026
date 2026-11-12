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
    /// Unity-native representation of a box (rectangular prism) in 3D space.
    /// Extends the COMPAS Box class with Unity GameObject integration and visualization capabilities.
    /// </summary>
    public class UnityBox : Box, IUnityProtoConvertible<UnityBox, BoxData>, IUnityGeometry
    {
        /// <summary>Render-contract view of <see cref="BoxGameObject"/>.</summary>
        public GameObject CachedGameObject
        {
            get => BoxGameObject;
            set => BoxGameObject = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityBox"/> class with the specified parameters.
        /// </summary>
        /// <param name="frame">The frame defining the box's position and orientation.</param>
        /// <param name="xsize">The size of the box along the x-axis in Unity units.</param>
        /// <param name="ysize">The size of the box along the y-axis in Unity units.</param>
        /// <param name="zsize">The size of the box along the z-axis in Unity units.</param>
        public UnityBox(Frame frame, float xsize, float ysize, float zsize) : base(frame, xsize, ysize, zsize)
        {
        }

        /// <summary>
        /// Gets or sets the GameObject representation of this box.
        /// </summary>
        /// <remarks>
        /// This property stores a reference to the Unity GameObject visualization of the box.
        /// Created by calling <see cref="ToGameObject"/> or <see cref="FromGameObject"/>.
        /// </remarks>

        public GameObject BoxGameObject { get; set; }
        /// <summary>
        /// Creates or updates the Unity GameObject representation and stores it in BoxGameObject.
        /// </summary>
        public GameObject ToGameObject()
        {
            GameObject boxObject = BoxGameObject ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObject.transform.localScale = new Vector3(XSize, YSize, ZSize);
            var (position, rotation) = GeometryConversions.FrameToTransformValues(Frame);
            boxObject.transform.SetPositionAndRotation(position, rotation);
            BoxGameObject = boxObject;
            return boxObject;
        }

        /// <summary>
        /// Creates a UnityBox from a Unity GameObject and stores the reference in BoxGameObject.
        /// Uses localScale (authored size) rather than lossyScale (world size).
        /// TODO: Consider adding an overload or flag to support world-space scale extraction if needed.
        /// </summary>
        public static UnityBox FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            Transform transform = gameObject.transform;
            Frame frame = GeometryConversions.FrameFromTransform(transform);
            Vector3 scale = transform.localScale;
            UnityBox unityBox = new UnityBox(frame, scale.x, scale.y, scale.z)
            {
                BoxGameObject = gameObject
            };
            return unityBox;
        }

        /// <inheritdoc/>
        public object ToData(SupportedSystem target)
        {
            UnityBox current = BoxGameObject != null ? FromGameObject(BoxGameObject) : this;
            return UnityGeometryFactory.FromUnitySystem(current, target);
        }

        /// <summary>
        /// Converts this UnityBox to its protobuf representation.
        /// If BoxGameObject exists, includes GameObject metadata for reconstruction.
        /// </summary>
        public new BoxData ToProto() => base.ToProto();

        /// <summary>
        /// Creates a UnityBox from protobuf data.
        /// If GameObject metadata is present, recreates the GameObject.
        /// </summary>
        public static new UnityBox FromProto(BoxData boxData)
        {
            return FromBox(Box.FromProto(boxData));
        }

        /// <summary>
        /// Creates a UnityBox from a CompasNet Box instance.
        /// </summary>
        public static UnityBox FromBox(Box box)
        {
            if (box == null)
            {
                throw new ArgumentNullException(nameof(box));
            }

            return new UnityBox(box.Frame, box.XSize, box.YSize, box.ZSize);
        }

    }
}