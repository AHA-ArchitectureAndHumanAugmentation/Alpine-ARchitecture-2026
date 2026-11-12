using Compas.Core.Data;
using Compas.Core.Transformations;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Compas.Core.Geometry
{
    [Serializable]
    public class Cylinder : Geometry<Cylinder>, IProtoConvertible<Cylinder, CompasPb.Data.CylinderData>
    {
        /// <summary>
        /// Cylinder: A class for defining a cylinder in 3D space.
        /// It is based off of translations of the COMPAS Cylinder class.
        /// </summary>
        /// <summary>
        /// Initializes a new cylinder from frame, radius, and height.
        /// </summary>
        /// <param name="frame">Cylinder local frame.</param>
        /// <param name="radius">Cylinder radius.</param>
        /// <param name="height">Cylinder height.</param>
        public Cylinder(Frame frame, float radius, float height) : base()
        {
            Frame = frame;
            Radius = radius;
            Height = height;
        }

        /// <summary>
        /// Properties of the Cylinder class.
        /// Frame: The frame of the cylinder.
        /// Radius: The radius of the cylinder.
        /// Height: The height of the cylinder.
        /// </summary>
        public Frame Frame { get; set; }
        public float Radius { get; set; }
        public float Height { get; set; }

        /// <summary>
        /// Parses a cylinder from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="jsondata">Input JSON dictionary object.</param>
        /// <returns>Cylinder instance.</returns>
        public static Cylinder Parse(object jsondata)
        {
            var cylinderDataDict = jsondata as Dictionary<string, object>;
            return FromData(cylinderDataDict);
        }

        /// <summary>
        /// Creates a cylinder from a dictionary payload.
        /// </summary>
        /// <param name="cylinderDataDict">Dictionary containing frame, radius, and height.</param>
        /// <returns>Cylinder instance.</returns>
        public new static Cylinder FromData(Dictionary<string, object> cylinderDataDict)
        {
            Frame Frame = Frame.FromData(cylinderDataDict["frame"] as Dictionary<string, object>);
            float radius = Convert.ToSingle(cylinderDataDict["radius"]);
            float height = Convert.ToSingle(cylinderDataDict["height"]);
            Cylinder cylinder = new Cylinder(Frame, radius, height);
            return cylinder;
        }

        /// <summary>
        /// Converts this cylinder to a dictionary payload.
        /// </summary>
        /// <returns>Dictionary representation of the cylinder.</returns>
        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "frame", Frame.ToData() },
                { "radius", Radius },
                { "height", Height },
                { "dtype", this.DType}
            };
        }

        /// <summary>
        /// Creates a <see cref="Cylinder"/> from protobuf data.
        /// </summary>
        /// <param name="cylinderData">Input protobuf cylinder payload.</param>
        /// <returns>Cylinder instance.</returns>
        public static Cylinder FromProto(CompasPb.Data.CylinderData cylinderData)
        {
            return new Cylinder(
                Frame.FromProto(cylinderData.Frame),
                (float)cylinderData.Radius,
                (float)cylinderData.Height
            )
            {
                Guid = cylinderData.Guid,
                Name = cylinderData.Name
            };
        }

        /// <summary>
        /// Converts this instance to protobuf data.
        /// </summary>
        /// <returns>Protobuf cylinder payload.</returns>
        public CompasPb.Data.CylinderData ToProto()
        {
            return new CompasPb.Data.CylinderData
            {
                Guid = Guid,
                Name = Name,
                Frame = Frame.ToProto(),
                Radius = Radius,
                Height = Height
            };
        }

        /// <summary>
        /// Applies a transformation matrix to this cylinder.
        /// </summary>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed cylinder.</returns>
        public override Cylinder Transform(float[] matrix)
        {

            // Transform the frame
            Frame transformedFrame = Frame.Transform(matrix);

            // The incoming matrix is column-major, so we need to transpose it for Matrix4x4 (row-major)
            Matrix4x4 mat = ObjectTransformations.ColumnMajorToRowMajor(matrix);

            // Decompose the transformation matrix to extract scale
            Matrix4x4.Decompose(mat, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

            // Decompose folds the mirror of a handedness change into a negative scale component;
            // only the magnitude is a real scale factor. Unsigned, the average below cancels to
            // zero for a Y/Z swap - (-1 + 1) / 2 - collapsing the radius and hiding the cylinder.
            float scaleX = Math.Abs(scale.X);
            float scaleY = Math.Abs(scale.Y);
            float scaleZ = Math.Abs(scale.Z);

            // For a cylinder: radius scales by average of X and Y, height scales by Z
            float transformedRadius = Radius * (scaleX + scaleY) / 2.0f;
            float transformedHeight = Height * scaleZ;

            return new Cylinder(transformedFrame, transformedRadius, transformedHeight)
            {
                Guid = Guid,
                Name = Name
            };
        }
    }
}