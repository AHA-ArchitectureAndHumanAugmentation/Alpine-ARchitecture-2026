using Compas.Core.Data;
using Compas.Core.Transformations;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Compas.Core.Geometry
{
    [Serializable]
    public class Box : Geometry<Box>, IProtoConvertible<Box, CompasPb.Data.BoxData>
    {
        /// <summary>
        /// Box: A class to define the structure of a box in the assembly data structure.
        /// It is based off the Compas data structure for a box.
        /// </summary>
        public Frame Frame { get; set; }
        public float XSize { get; set; }
        public float YSize { get; set; }
        public float ZSize { get; set; }

        /// <summary>
        /// Initializes a new box from frame and dimensions.
        /// </summary>
        /// <param name="frame">Local frame of the box.</param>
        /// <param name="xSize">Size along local X axis.</param>
        /// <param name="ySize">Size along local Y axis.</param>
        /// <param name="zSize">Size along local Z axis.</param>
        public Box(Frame frame, float xSize, float ySize, float zSize) : base()
        {
            Frame = frame;
            XSize = xSize;
            YSize = ySize;
            ZSize = zSize;
        }

        /// <summary>
        /// Converts this box to a dictionary payload.
        /// </summary>
        /// <returns>Dictionary representation of the box.</returns>
        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "frame", Frame.ToData() },
                { "xsize", XSize },
                { "ysize", YSize },
                { "zsize", ZSize },
                { "dtype", this.DType}
            };
        }

        /// <summary>
        /// Creates a <see cref="Box"/> from protobuf data.
        /// </summary>
        /// <param name="boxData">Input protobuf box payload.</param>
        /// <returns>Box instance.</returns>
        public static Box FromProto(CompasPb.Data.BoxData boxData)
        {
            return new Box(
                Frame.FromProto(boxData.Frame),
                (float)boxData.Xsize,
                (float)boxData.Ysize,
                (float)boxData.Zsize
            )
            {
                Guid = boxData.Guid,
                Name = boxData.Name
            };
        }

        /// <summary>
        /// Converts this instance to protobuf data.
        /// </summary>
        /// <returns>Protobuf box payload.</returns>
        public CompasPb.Data.BoxData ToProto()
        {
            return new CompasPb.Data.BoxData
            {
                Guid = Guid,
                Name = Name,
                Frame = Frame.ToProto(),
                Xsize = XSize,
                Ysize = YSize,
                Zsize = ZSize
            };
        }

        /// <summary>
        /// Parses a box from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="jsondata">Input JSON dictionary object.</param>
        /// <returns>Box instance.</returns>
        public static Box Parse(object jsondata)
        {
            var jsonDataDict = jsondata as Dictionary<string, object>;
            return FromData(jsonDataDict);
        }

        /// <summary>
        /// Creates a box from a dictionary payload.
        /// </summary>
        /// <param name="jsonDataDict">Dictionary containing frame and size fields.</param>
        /// <returns>Box instance.</returns>
        public new static Box FromData(Dictionary<string, object> jsonDataDict)
        {
            Frame frame = Frame.FromData(jsonDataDict["frame"] as Dictionary<string, object>);
            float XSize = Convert.ToSingle(jsonDataDict["xsize"]);
            float YSize = Convert.ToSingle(jsonDataDict["ysize"]);
            float ZSize = Convert.ToSingle(jsonDataDict["zsize"]);
            Box box = new Box(frame, XSize, YSize, ZSize);
            return box;
        }

        /// <summary>
        /// Applies a transformation matrix to this box.
        /// </summary>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed box.</returns>
        public override Box Transform(float[] matrix)
        {
            // Transform the frame
            Frame transformedFrame = Frame.Transform(matrix);

            // The incoming matrix is column-major, so we need to transpose it for Matrix4x4 (row-major)
            Matrix4x4 mat = ObjectTransformations.ColumnMajorToRowMajor(matrix);

            // Decompose the transformation matrix to extract scale
            Matrix4x4.Decompose(mat, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

            // Decompose cannot express a reflection as scale * rotation (rotations are always
            // determinant +1), so a handedness change folds the mirror into a negative scale
            // component. A coordinate conversion never resizes geometry, so take the magnitude
            // only - unsigned, XSize comes back negated and the cube renders inside-out.
            float scaleX = Math.Abs(scale.X);
            float scaleY = Math.Abs(scale.Y);
            float scaleZ = Math.Abs(scale.Z);

            // For a box: each dimension scales by its corresponding axis
            float transformedXSize = XSize * scaleX;
            float transformedYSize = YSize * scaleY;
            float transformedZSize = ZSize * scaleZ;

            return new Box(transformedFrame, transformedXSize, transformedYSize, transformedZSize)
            {
                Guid = Guid,
                Name = Name
            };
        }
    }
}