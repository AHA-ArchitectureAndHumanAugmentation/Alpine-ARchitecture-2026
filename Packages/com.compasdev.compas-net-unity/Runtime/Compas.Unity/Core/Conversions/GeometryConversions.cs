using Compas.Core.Geometry;
using UnityEngine;

namespace Compas.Unity.Core.Conversions
{
    public static class GeometryConversions
    {

        public static Vector3 CompasVectorToUnityVector3(Compas.Core.Geometry.Vector vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector UnityVector3ToCompasVector(Vector3 vector)
        {
            return new Vector(vector.x, vector.y, vector.z);
        }

        /// <summary>
        /// Converts a Transform to a Frame using world space coordinates.
        /// </summary>
        /// <param name="transform">The Transform to convert (uses world position and rotation).</param>
        /// <returns>A Frame representing the transform's world-space position and orientation.</returns>
        /// <remarks>
        /// This method uses world space (transform.position, transform.right, transform.up).
        /// If you need local space coordinates, use localPosition and local axes instead.
        /// </remarks>
        public static Frame FrameFromTransform(Transform transform)
        {
            return FrameFromTransform(transform.position, transform.rotation);
        }

        /// <summary>
        /// Converts a world-space position and rotation to a Frame.
        /// </summary>
        /// <param name="position">World-space origin of the frame.</param>
        /// <param name="rotation">World-space orientation; its right and up axes become the frame axes.</param>
        /// <returns>A Frame representing that position and orientation.</returns>
        /// <remarks>
        /// The rotation-taking overload exists so callers that need to undo a primitive-mesh axis
        /// correction before reading the frame back (see UnityCylinder.FromGameObject) can do so
        /// without duplicating axis extraction.
        /// </remarks>
        public static Frame FrameFromTransform(Vector3 position, Quaternion rotation)
        {
            Point point = new Point(position.x, position.y, position.z);
            Vector xAxis = UnityVector3ToCompasVector(rotation * Vector3.right);
            Vector yAxis = UnityVector3ToCompasVector(rotation * Vector3.up);
            return new Frame(point, xAxis, yAxis);
        }

        /// <summary>
        /// Converts a Frame to position and rotation values using world space coordinates.
        /// </summary>
        /// <param name="frame">The Frame to convert.</param>
        /// <returns>A tuple containing the world-space position and rotation quaternion.</returns>
        /// <remarks>
        /// This method extracts position and rotation data from a Frame without creating a GameObject.
        /// Returns a tuple of (Vector3 position, Quaternion rotation) that can be applied to any Transform.
        /// The geometry classes (Box, Cylinder, Sphere) use this method to apply Frames to GameObjects.
        /// </remarks>
        public static (Vector3 position, Quaternion rotation) FrameToTransformValues(Frame frame)
        {
            Vector3 position = new Vector3(frame.Point.X, frame.Point.Y, frame.Point.Z);
            Vector3 xAxis = CompasVectorToUnityVector3(frame.XAxis);
            Vector3 yAxis = CompasVectorToUnityVector3(frame.YAxis);
            Quaternion rotation = RotationFromAxes(xAxis, yAxis);
            return (position, rotation);
        }

        public static Quaternion RotationFromAxes(Vector3 xAxis, Vector3 yAxis)
        {
            if (xAxis.sqrMagnitude < 1e-8f || yAxis.sqrMagnitude < 1e-8f)
            {
                return Quaternion.identity;
            }

            xAxis.Normalize();
            yAxis.Normalize();

            Vector3 zAxis = Vector3.Cross(xAxis, yAxis);
            if (zAxis.sqrMagnitude < 1e-8f)
            {
                return Quaternion.identity;
            }

            zAxis.Normalize();
            yAxis = Vector3.Cross(zAxis, xAxis);

            return Quaternion.LookRotation(zAxis, yAxis);
        }


    }
}