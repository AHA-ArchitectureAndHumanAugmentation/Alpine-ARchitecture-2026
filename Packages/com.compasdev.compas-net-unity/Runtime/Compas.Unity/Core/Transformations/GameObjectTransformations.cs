using Compas.Core.Geometry;
using Compas.Core.Transformations;
using UnityEngine;

namespace Compas.Unity.Core.Transformations
{
    /// <summary>
    /// Provides transformation utilities for converting between CompasNet coordinate systems and Unity transforms.
    /// Handles coordinate system conversions for external systems such as Rhino and Motive.
    /// </summary>
    /// <remarks>
    /// This class manages the complexity of converting between different 3D coordinate systems:
    /// - Rhino ZUp: Z-axis points up (right-handed)
    /// - Rhino YUp: Y-axis points up (right-handed)
    /// - Motive: Y-axis points up (right-handed with specific conventions)
    /// - Unity: Y-axis points up (left-handed)
    /// 
    /// Conversions account for both axis reorientation and handedness changes where applicable.
    /// </remarks>
    public static class GameObjectTransformations
    {
        /// <summary>
        /// Converts a Frame to Unity position and rotation values, accounting for axis swapping requirements.
        /// </summary>
        /// <param name="frame">The frame to convert.</param>
        /// <param name="axisSwap">Specifies how axes should be swapped during conversion.</param>
        /// <returns>A tuple containing the resulting Vector3 position and Quaternion rotation suitable for Unity transforms.</returns>
        /// <remarks>
        /// This method handles the low-level matrix conversions and quaternion calculations.
        /// The axis swap parameter determines the order of cross products and LookRotation parameters.
        /// </remarks>
        private static (Vector3, Quaternion) FrameToTransform(Frame frame, AxisSwapping axisSwap)
        {
            Vector3 xAxis = new Vector3(frame.XAxis.Values[0], frame.XAxis.Values[1], frame.XAxis.Values[2]);
            Vector3 yAxis = new Vector3(frame.YAxis.Values[0], frame.YAxis.Values[1], frame.YAxis.Values[2]);

            // Cross product order depends on axis swapping
            Vector3 zAxis = axisSwap == AxisSwapping.YZSwapped
                ? Vector3.Cross(yAxis, xAxis)  // Y × X when Y↔Z swapped
                : Vector3.Cross(xAxis, yAxis); // X × Y otherwise

            Matrix4x4 basis = new Matrix4x4(
                new Vector4(xAxis.x, xAxis.y, xAxis.z, 0),
                new Vector4(yAxis.x, yAxis.y, yAxis.z, 0),
                new Vector4(zAxis.x, zAxis.y, zAxis.z, 0),
                new Vector4(0, 0, 0, 1)
            );

            // LookRotation column order also depends on axis swapping
            Quaternion rotation = axisSwap == AxisSwapping.YZSwapped
                ? Quaternion.LookRotation(basis.GetColumn(1), basis.GetColumn(2))  // Y forward, Z up (swapped)
                : Quaternion.LookRotation(basis.GetColumn(2), basis.GetColumn(1)); // Z forward, Y up (standard)

            return (
                new Vector3(frame.Point.Values[0], frame.Point.Values[1], frame.Point.Values[2]),
                rotation
            );
        }

        // ============================================================
        // RHINO ZUP TRANSFORMS
        // ============================================================

        /// <summary>
        /// Converts a Rhino ZUp frame to Unity transform using SupportedSystem API.
        /// RhinoZUp(X,Y,Z) -> Unity(X,Z,Y): Y→forward becomes Z→forward, Z→up becomes Y→up
        /// </summary>
        public static (Vector3, Quaternion) RhinoZUpFrameToUnityTransform(Frame frame)
        {
            var unityFrame = ObjectTransformations.Transform(frame, SupportedSystem.RhinoZUp, SupportedSystem.Unity);
            var axisSwap = ObjectTransformations.GetAxisSwapping(SupportedSystem.RhinoZUp, SupportedSystem.Unity);
            return FrameToTransform(unityFrame, axisSwap);
        }

        /// <summary>
        /// Converts a Rhino ZUp frame to Unity transform using TransformVariant API.
        /// RhinoZUp(X,Y,Z) -> Unity(X,Z,Y): swap Y↔Z axes (right-handed to left-handed)
        /// </summary>
        public static (Vector3, Quaternion) RhinoZUpFrameToUnityTransform(Frame frame, TransformVariant variant, HandednessChange handednessChange)
        {
            var unityFrame = ObjectTransformations.Transform(frame, variant, handednessChange);
            var axisSwap = ObjectTransformations.GetAxisSwapping(variant);
            return FrameToTransform(unityFrame, axisSwap);
        }

        // ============================================================
        // RHINO YUP TRANSFORMS
        // ============================================================

        /// <summary>
        /// Converts a Rhino YUp frame to Unity transform using SupportedSystem API.
        /// RhinoYUp(X,Y,Z) -> Unity(X,Y,-Z): axes align, flip Z for handedness change
        /// </summary>
        public static (Vector3, Quaternion) RhinoYUpFrameToUnityTransform(Frame frame)
        {
            var unityFrame = ObjectTransformations.Transform(frame, SupportedSystem.RhinoYUp, SupportedSystem.Unity);
            var axisSwap = ObjectTransformations.GetAxisSwapping(SupportedSystem.RhinoYUp, SupportedSystem.Unity);
            return FrameToTransform(unityFrame, axisSwap);
        }

        /// <summary>
        /// Converts a Rhino YUp frame to Unity transform using TransformVariant API.
        /// RhinoYUp(X,Y,Z) -> Unity(X,Y,-Z): flip Z for handedness change (axes already swapped by Rhino export)
        /// </summary>
        public static (Vector3, Quaternion) RhinoYUpFrameToUnityTransform(Frame frame, TransformVariant variant, HandednessChange handednessChange)
        {
            var unityFrame = ObjectTransformations.Transform(frame, variant, handednessChange);
            var axisSwap = ObjectTransformations.GetAxisSwapping(variant);
            return FrameToTransform(unityFrame, axisSwap);
        }

        // ============================================================
        // MOTIVE TRANSFORMS
        // ============================================================

        /// <summary>
        /// Converts a Motive frame to Unity transform using SupportedSystem API.
        /// Motive(X,Y,Z) -> Unity(X,Y,-Z): flip Z axis for handedness change
        /// </summary>
        public static (Vector3, Quaternion) MotiveFrameToUnityTransform(Frame frame)
        {
            var unityFrame = ObjectTransformations.Transform(frame, SupportedSystem.Motive, SupportedSystem.Unity);
            var axisSwap = ObjectTransformations.GetAxisSwapping(SupportedSystem.Motive, SupportedSystem.Unity);
            return FrameToTransform(unityFrame, axisSwap);
        }

        /// <summary>
        /// Converts a Motive frame to Unity transform using TransformVariant API.
        /// Motive(X,Y,Z) -> Unity(X,Y,-Z): flip Z axis for handedness change
        /// </summary>
        public static (Vector3, Quaternion) MotiveFrameToUnityTransform(Frame frame, TransformVariant variant, HandednessChange handednessChange)
        {
            var unityFrame = ObjectTransformations.Transform(frame, variant, handednessChange);
            var axisSwap = ObjectTransformations.GetAxisSwapping(variant);
            return FrameToTransform(unityFrame, axisSwap);
        }
    }
}