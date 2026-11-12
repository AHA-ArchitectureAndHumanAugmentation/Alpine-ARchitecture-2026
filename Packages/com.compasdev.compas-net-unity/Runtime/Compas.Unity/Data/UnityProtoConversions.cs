using Compas.Unity.Core.DataStructures;
using Compas.Unity.Core.Geometry;
using CompasPb.Data;
using System;
using System.Collections.Generic;


[assembly: CompasPbRegistrations(typeof(Compas.Unity.Data.UnityProtoConversions))]

namespace Compas.Unity.Data
{
    public static class UnityProtoConversions
    {
        /// <summary>
        /// The types <em>this package</em> maps to protobuf messages. Downstream packages register
        /// their own and do not appear here.
        /// </summary>
        public static readonly IReadOnlyCollection<Type> ConvertibleTypes = new[]
        {
            typeof(UnityPoint),
            typeof(UnityBox),
            typeof(UnitySphere),
            typeof(UnityCylinder),
            typeof(UnityMesh),
        };

        /// <summary>
        /// Registers this package's conversions. Invoked by CompasPb through the assembly
        /// attribute; CompasPb guarantees it runs at most once.
        /// </summary>
        public static void Register()
        {
            Registry.Register<UnityPoint, PointData>(value => value.ToProto(), UnityPoint.FromProto);
            Registry.Register<UnityBox, BoxData>(value => value.ToProto(), UnityBox.FromProto);
            Registry.Register<UnitySphere, SphereData>(value => value.ToProto(), UnitySphere.FromProto);
            Registry.Register<UnityCylinder, CylinderData>(value => value.ToProto(), UnityCylinder.FromProto);
            Registry.Register<UnityMesh, MeshData>(value => value.ToProto(), UnityMesh.FromProto);
        }
    }
}