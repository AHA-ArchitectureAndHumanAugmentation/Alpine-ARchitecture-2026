using System;
using System.Collections.Generic;
using Compas.Core.Data.Conversions;
using Newtonsoft.Json;
using System.Numerics;
using CompasPb;
using Compas.Core.Data;

namespace Compas.Core.Geometry
{
    [Serializable]
    public class Point : Geometry<Point>, IProtoConvertible<Point, CompasPb.Data.PointData>
    {
        /// <summary>
        /// Point: A class for defining a point in 3D space.
        /// It is based off of translations of the COMPAS Point class.
        /// </summary>
        /// <summary>
        /// Initializes a new point from X, Y, and Z coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        public Point(float x, float y, float z) : base()
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Properties of the Point class.
        /// X: The x-coordinate of the point.
        /// Y: The y-coordinate of the point.
        /// Z: The z-coordinate of the point.
        /// Values: A float array of the x, y, and z coordinates.
        /// </summary>

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public float[] Values { get { return new float[] { X, Y, Z }; } }

        /// <summary>
        /// Parses a point from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="jsonData">Input JSON dictionary object.</param>
        /// <returns>Point instance.</returns>
        public static Point Parse(object jsonData)
        {
            var data = (Dictionary<string, object>)jsonData;
            float[] point = NumericConverters.ConvertNumericDatatoFloatArray(data["point"]);
            return FromData(point);
        }

        /// <summary>
        /// Creates a point from numeric array data.
        /// </summary>
        /// <param name="data">Array containing X, Y, and Z values.</param>
        /// <returns>Point instance.</returns>
        public static Point FromData(float[] data)
        {
            return new Point(data[0], data[1], data[2]);
        }

        /// <summary>
        /// Converts this point to a dictionary payload.
        /// </summary>
        /// <returns>Dictionary representation of the point.</returns>
        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "point", Values }, //TODO: This should just return a dictionary of values
                { "dtype", this.DType}
            };
        }

        /// <summary>
        /// Creates a <see cref="Point"/> from protobuf data.
        /// </summary>
        /// <param name="pointData">Input protobuf point payload.</param>
        /// <returns>Point instance.</returns>
        public static Point FromProto(CompasPb.Data.PointData pointData)
        {
            return new Point((float)pointData.X, (float)pointData.Y, (float)pointData.Z)
            {
                Guid = pointData.Guid,
                Name = pointData.Name
            };
        }

        /// <summary>
        /// Converts this instance to protobuf data.
        /// </summary>
        /// <returns>Protobuf point payload.</returns>
        public CompasPb.Data.PointData ToProto()
        {
            return new CompasPb.Data.PointData
            {
                Guid = Guid,
                Name = Name,
                X = X,
                Y = Y,
                Z = Z
            };
        }

        /// <summary>
        /// Returns a point at the world origin.
        /// </summary>
        /// <returns>Point(0,0,0).</returns>
        public static Point PointAtOrigin()
        {
            return new Point(0, 0, 0);
        }

        /// <summary>
        /// Applies a transformation matrix to this point.
        /// </summary>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed point.</returns>
        public override Point Transform(float[] matrix)
        {

            float x = X * matrix[0] + Y * matrix[4] + Z * matrix[8] + matrix[12];
            float y = X * matrix[1] + Y * matrix[5] + Z * matrix[9] + matrix[13];
            float z = X * matrix[2] + Y * matrix[6] + Z * matrix[10] + matrix[14];

            return new Point(x, y, z)
            {
                Guid = Guid,
                Name = Name
            };
        }

    }

}