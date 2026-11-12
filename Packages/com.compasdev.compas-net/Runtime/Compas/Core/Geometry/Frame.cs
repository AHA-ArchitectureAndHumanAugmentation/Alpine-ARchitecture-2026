using System;
using System.Collections.Generic;
using System.Numerics;
using Compas.Core.Data.Conversions;
using Compas.Core.Data;
using CompasPb;

namespace Compas.Core.Geometry
{
    [Serializable]
    public class Frame : Geometry<Frame>, IProtoConvertible<Frame, CompasPb.Data.FrameData>
    {
        /// <summary>
        /// Frame: A class to define the structure of a frame in the assembly data structure.
        /// It is based off the Compas data structure for a frame.
        /// </summary>
        public Point Point { get; set; }
        public Vector XAxis { get; set; }
        public Vector YAxis { get; set; }
        
        /// <summary>
        /// The Z axis of the frame, computed as the cross product of XAxis and YAxis.
        /// This is a read-only property that follows the COMPAS convention.
        /// </summary>
        public Vector ZAxis => XAxis.Cross(YAxis);

        /// <summary>
        /// Initializes a new frame from point, X axis, and Y axis.
        /// </summary>
        /// <param name="point">Frame origin point.</param>
        /// <param name="xAxis">Frame X axis vector.</param>
        /// <param name="yAxis">Frame Y axis vector.</param>
        public Frame(Point point, Vector xAxis, Vector yAxis) : base()
        {
            Point = point;
            XAxis = xAxis;
            YAxis = yAxis;
        }

        /// <summary>
        /// Parses a frame from loosely-typed JSON dictionary data.
        /// </summary>
        /// <param name="jsondata">Input JSON dictionary object.</param>
        /// <returns>Frame instance.</returns>
        public static Frame Parse(object jsondata)
        {
            var frameDataDict = jsondata as Dictionary<string, object>;;
            return FromData(frameDataDict);
        }

        /// <summary>
        /// Creates a frame from a dictionary payload.
        /// </summary>
        /// <param name="frameDataDict">Dictionary containing point and axis data.</param>
        /// <returns>Frame instance.</returns>
        public new static Frame FromData(Dictionary<string, object> frameDataDict)
        {            
            float[] pointValues = NumericConverters.ConvertNumericDatatoFloatArray(frameDataDict["point"]);
            float[] xaxis = NumericConverters.ConvertNumericDatatoFloatArray(frameDataDict["xaxis"]);
            float[] yaxis = NumericConverters.ConvertNumericDatatoFloatArray(frameDataDict["yaxis"]);
            
            if (pointValues.Length != 3 || xaxis.Length != 3 || yaxis.Length != 3)
            {
                throw new ArgumentException("Invalid frame data.");
            }
            
            Point point = new Point(pointValues[0], pointValues[1], pointValues[2]);
            Vector xAxis = new Vector(xaxis[0], xaxis[1], xaxis[2]);
            Vector yAxis = new Vector(yaxis[0], yaxis[1], yaxis[2]);
            Frame frame = new Frame(point, xAxis, yAxis);
            return frame;
        }
        /// <summary>
        /// Converts this frame to a dictionary payload.
        /// </summary>
        /// <returns>Dictionary representation of the frame.</returns>
        public override Dictionary<string, object> ToData()
        {
            return new Dictionary<string, object>
            {
                { "point", Point.ToData() },
                { "xaxis", XAxis.ToData() },
                { "yaxis", YAxis.ToData() },
                { "dtype", this.DType} //TODO: Test this
            };
        }

        /// <summary>
        /// Creates a <see cref="Frame"/> from protobuf data.
        /// </summary>
        /// <param name="frameData">Input protobuf frame payload.</param>
        /// <returns>Frame instance.</returns>
        public static Frame FromProto(CompasPb.Data.FrameData frameData)
        {
            return new Frame(
                Point.FromProto(frameData.Point),
                Vector.FromProto(frameData.Xaxis),
                Vector.FromProto(frameData.Yaxis)
            )
            {
                Guid = frameData.Guid,
                Name = frameData.Name
            };
        }

        /// <summary>
        /// Converts this instance to protobuf data.
        /// </summary>
        /// <returns>Protobuf frame payload.</returns>
        public CompasPb.Data.FrameData ToProto()
        {
            return new CompasPb.Data.FrameData
            {
                Guid = Guid,
                Name = Name,
                Point = Point.ToProto(),
                Xaxis = XAxis.ToProto(),
                Yaxis = YAxis.ToProto()
            };
        }

        /// <summary>
        /// Returns a frame that represents the world XY plane.
        /// </summary>
        /// <returns>World XY frame.</returns>
        public static Frame WorldXY()
        {
            Point point = new Point(0, 0, 0);
            Vector xAxis = new Vector(1, 0, 0);
            Vector yAxis = new Vector(0, 1, 0);
            Frame frame = new Frame(point, xAxis, yAxis);
            return frame;
        }

        /// <summary>
        /// Applies a transformation matrix to this frame.
        /// </summary>
        /// <param name="matrix">4x4 column-major transformation matrix.</param>
        /// <returns>Transformed frame.</returns>
        public override Frame Transform(float[] matrix)
        {

            Point transformedPoint = Point.Transform(matrix);
            Vector transformedXAxis = XAxis.Transform(matrix);
            Vector transformedYAxis = YAxis.Transform(matrix);

            return new Frame(transformedPoint, transformedXAxis, transformedYAxis)
            {
                Guid = Guid,
                Name = Name
            };
        }
    }
}
