using System;
using System.Collections.Generic;
using System.Numerics;
using Compas.Core.Data.Conversions;
using CompasPb;
using Compas.Core.Data;

namespace Compas.Core.Geometry
{
    [Serializable]
    public class Vector : Geometry<Vector>, IProtoConvertible<Vector, CompasPb.Data.VectorData>
    {
        /// <summary>
        /// Vector: A class for defining a vector in 3D space.
        /// It is based off of translations of the COMPAS Vector class and Inherits from abstract BaseVector.
        /// </summary>
        /// <summary>
        /// Initializes a new vector from X, Y, and Z components.
        /// </summary>
        /// <param name="x">X component.</param>
        /// <param name="y">Y component.</param>
        /// <param name="z">Z component.</param>
        public Vector(float x, float y, float z) : base()
        {
            X = x;
            Y = y;
            Z = z;
            _vector = new Vector3(x, y, z);
        }

        /// <summary>
        /// Properties of the Vector class.
        /// Vector (Private): The vector as a System.Numerics.Vector3.
        /// X: The x value of the vector.
        /// Y: The y value of the vector.
        /// Z: The z value of the vector.
        /// Values: A float array of the x, y, and z values.
        /// </summary>
        //TODO: Length : The length of the vector.
        private Vector3 _vector;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public float[] Values { get { return new float[] { X, Y, Z }; } }

        /// <summary>
        /// Converts this vector to a dictionary payload.
        /// </summary>
        /// <returns>Dictionary representation of the vector.</returns>
        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "vector", Values }, //TODO: This should remove the "vector" explicit naming to match COMPAS, but works for deserialization FromJson() now.
                { "dtype", this.DType} 
            };
        }

        /// <summary>
        /// Creates a vector from numeric array data.
        /// </summary>
        /// <param name="data">Array containing X, Y, and Z values.</param>
        /// <returns>Vector instance.</returns>
        public static Vector FromData(float [] data)
        {
            return new Vector(data[0], data[1], data[2]);
        }

        /// <summary>
        /// Parses a vector from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="JsonData">Input JSON dictionary object.</param>
        /// <returns>Vector instance.</returns>
        public static Vector Parse(object JsonData)
        {
            var data = (Dictionary<string, object>)JsonData;
            float[] vector = NumericConverters.ConvertNumericDatatoFloatArray(data["vector"]);
            return FromData(vector);
        }

        /// <summary>
        /// Returns the unit X-axis vector.
        /// </summary>
        /// <returns>Vector(1,0,0).</returns>
        public static Vector Xaxis()
        {
            return new Vector(1, 0, 0);
        }

        /// <summary>
        /// Returns the unit Y-axis vector.
        /// </summary>
        /// <returns>Vector(0,1,0).</returns>
        public static Vector Yaxis()
        {
            return new Vector(0, 1, 0);
        }

        /// <summary>
        /// Returns the unit Z-axis vector.
        /// </summary>
        /// <returns>Vector(0,0,1).</returns>
        public static Vector Zaxis()
        {
            return new Vector(0, 0, 1);
        }

        /// <summary>
        /// Creates a <see cref="Vector"/> from protobuf data.
        /// </summary>
        /// <param name="vectorData">Input protobuf vector payload.</param>
        /// <returns>Vector instance.</returns>
        public static Vector FromProto(CompasPb.Data.VectorData vectorData)
        {
            return new Vector((float)vectorData.X, (float)vectorData.Y, (float)vectorData.Z)
            {
                Guid = vectorData.Guid,
                Name = vectorData.Name
            };
        }

        /// <summary>
        /// Converts this instance to protobuf data.
        /// </summary>
        /// <returns>Protobuf vector payload.</returns>
        public CompasPb.Data.VectorData ToProto()
        {
            return new CompasPb.Data.VectorData
            {
                Guid = Guid,
                Name = Name,
                X = X,
                Y = Y,
                Z = Z
            };
        }

        /// <summary>
        /// Creates a vector from <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector">Source numerics vector.</param>
        /// <returns>Vector instance.</returns>
        public static Vector FromSystemNumericsVector3(Vector3 vector)
        {
            return new Vector(vector.X, vector.Y, vector.Z);
        }

        /// <summary>
        /// Converts this vector to <see cref="Vector3"/>.
        /// </summary>
        /// <returns>System.Numerics vector.</returns>
        public Vector3 ToSystemNumaricsVector3()
        {
            return _vector;
        }

        /// <summary>
        /// Computes the cross product with another vector.
        /// </summary>
        /// <param name="otherVector">The other vector.</param>
        /// <returns>Cross product vector.</returns>
        public Vector Cross(Vector otherVector)
        {
            Vector3 cross = Vector3.Cross(_vector, otherVector.ToSystemNumaricsVector3());
            return FromSystemNumericsVector3(cross);
        }

        /// <summary>
        /// Computes the dot product with another vector.
        /// </summary>
        /// <param name="otherVector">The other vector.</param>
        /// <returns>Dot product scalar.</returns>
        public float Dot(Vector otherVector)
        {
            return Vector3.Dot(_vector, otherVector.ToSystemNumaricsVector3());
        }

        /// <summary>
        /// Applies a transformation matrix to this vector.
        /// </summary>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed vector.</returns>
        public override Vector Transform(float[] matrix)
        {

            float x = X * matrix[0] + Y * matrix[4] + Z * matrix[8];
            float y = X * matrix[1] + Y * matrix[5] + Z * matrix[9];
            float z = X * matrix[2] + Y * matrix[6] + Z * matrix[10];

            return new Vector(x, y, z)
            {
                Guid = Guid,
                Name = Name
            };
        }

    }
}