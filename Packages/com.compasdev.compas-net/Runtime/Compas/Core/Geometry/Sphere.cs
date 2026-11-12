using Compas.Core.Data;
using Compas.Core.Transformations;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Compas.Core.Geometry
{
    [Serializable]
    public class Sphere : Geometry<Sphere>, IProtoConvertible<Sphere, CompasPb.Data.SphereData>
    {
        /// <summary>
        /// Sphere: A class for defining a sphere in 3D space.
        /// It is based off of translations of the COMPAS Sphere class.
        /// </summary>
        /// <summary>
        /// Initializes a new sphere from frame and radius.
        /// </summary>
        /// <param name="frame">Sphere local frame.</param>
        /// <param name="radius">Sphere radius.</param>
        public Sphere(Frame frame, float radius) : base()
        {
            Frame = frame;
            Radius = radius;
        }

        /// <summary>
        /// Properties of the Sphere class.
        /// Center: The center of the sphere.
        /// Radius: The radius of the sphere.
        /// </summary>
        public Frame Frame { get; set; }
        public float Radius { get; set; }

        /// <summary>
        /// Parses a sphere from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="jsondata">Input JSON dictionary object.</param>
        /// <returns>Sphere instance.</returns>
        public static Sphere Parse(object jsondata)
        {
            var sphereDataDict = jsondata as Dictionary<string, object>;
            return FromData(sphereDataDict);
        }

        /// <summary>
        /// Creates a sphere from a dictionary payload.
        /// </summary>
        /// <param name="sphereDataDict">Dictionary containing center/frame and radius.</param>
        /// <returns>Sphere instance.</returns>
        public new static Sphere FromData(Dictionary<string, object> sphereDataDict)
        {
            Frame frame = Frame.FromData(sphereDataDict["center"] as Dictionary<string, object>);
            float radius = Convert.ToSingle(sphereDataDict["radius"]);
            Sphere sphere = new Sphere(frame, radius);
            return sphere;
        }

        /// <summary>
        /// Converts this sphere to a dictionary payload.
        /// </summary>
        /// <returns>Dictionary representation of the sphere.</returns>
        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "frame", Frame.ToData() },
                { "radius", Radius },
                { "dtype", this.DType}
            };
        }

        /// <summary>
        /// Creates a <see cref="Sphere"/> from protobuf data.
        /// </summary>
        /// <param name="sphereData">Input protobuf sphere payload.</param>
        /// <returns>Sphere instance.</returns>
        public static Sphere FromProto(CompasPb.Data.SphereData sphereData)
        {
            return new Sphere(
                Frame.FromProto(sphereData.Frame),
                (float)sphereData.Radius
            )
            {
                Guid = sphereData.Guid,
                Name = sphereData.Name
            };
        }

        /// <summary>
        /// Converts this instance to protobuf data.
        /// </summary>
        /// <returns>Protobuf sphere payload.</returns>
        public CompasPb.Data.SphereData ToProto()
        {
            return new CompasPb.Data.SphereData
            {
                Guid = Guid,
                Name = Name,
                Frame = Frame.ToProto(),
                Radius = Radius
            };
        }

        /// <summary>
        /// Applies a transformation matrix to this sphere.
        /// </summary>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed sphere.</returns>
        public override Sphere Transform(float[] matrix)
        {
            // Transform the frame
            Frame transformedFrame = Frame.Transform(matrix);

            // Decompose the transformation matrix to extract scale
            // The incoming matrix is column-major, so we need to transpose it for Matrix4x4 (row-major)
            Matrix4x4 mat = ObjectTransformations.ColumnMajorToRowMajor(matrix);

            Matrix4x4.Decompose(mat, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

            // Decompose folds the mirror of a handedness change into a negative scale component;
            // only the magnitude is a real scale factor. Unsigned, the average below is
            // (-1 + 1 + 1) / 3, silently shrinking the sphere to a third of its size.
            float scaleX = Math.Abs(scale.X);
            float scaleY = Math.Abs(scale.Y);
            float scaleZ = Math.Abs(scale.Z);

            // For a sphere: use average scale across all three dimensions
            float averageScale = (scaleX + scaleY + scaleZ) / 3.0f;
            float transformedRadius = Radius * averageScale;

            return new Sphere(transformedFrame, transformedRadius)
            {
                Guid = Guid,
                Name = Name
            };
        }

    }
}