using System;
using System.Collections.Generic;
using Compas.Core.Geometry;
using CompasPb.Data;
using Compas.Models;

[assembly: CompasPbRegistrations(typeof(Compas.Core.Data.CompasProtoConversions))]

namespace Compas.Core.Data
{
    /// <summary>
    /// Registers this package's domain types with the CompasPb <see cref="Registry"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CompasPb already walks nested lists and dictionaries and consults the registry for every
    /// value it meets, so registering each mapping once here covers arbitrarily nested payloads.
    /// Registration is written out explicitly rather than reflected over
    /// <see cref="IProtoConvertible{T, TProto}"/> so that IL2CPP keeps the generic instantiations
    /// and no type scanning happens at runtime.
    /// </para>
    /// <para>
    /// The assembly-level <see cref="CompasPbRegistrationsAttribute"/> above points CompasPb at
    /// <see cref="Register"/>, so these conversions apply as soon as this assembly is loaded.
    /// Nothing has to call into this class. Other packages declare their own attribute and
    /// registrar; this list covers only the types this package owns.
    /// </para>
    /// </remarks>
    public static class CompasProtoConversions
    {
        /// <summary>
        /// The types <em>this package</em> maps to protobuf messages. Downstream packages register
        /// their own and do not appear here.
        /// </summary>
        public static readonly IReadOnlyCollection<Type> ConvertibleTypes = new[]
        {
            typeof(Point),
            typeof(Vector),
            typeof(Frame),
            typeof(Box),
            typeof(Sphere),
            typeof(Cylinder),
            typeof(Mesh),
        };

        /// <summary>
        /// Registers this package's conversions. Invoked by CompasPb through the assembly
        /// attribute; CompasPb guarantees it runs at most once.
        /// </summary>
        public static void Register()
        {
            Registry.Register<Point, PointData>(value => value.ToProto(), Point.FromProto);
            Registry.Register<Vector, VectorData>(value => value.ToProto(), Vector.FromProto);
            Registry.Register<Frame, FrameData>(value => value.ToProto(), Frame.FromProto);
            Registry.Register<Box, BoxData>(value => value.ToProto(), Box.FromProto);
            Registry.Register<Sphere, SphereData>(value => value.ToProto(), Sphere.FromProto);
            Registry.Register<Cylinder, CylinderData>(value => value.ToProto(), Cylinder.FromProto);
            Registry.Register<Mesh, MeshData>(value => value.ToProto(), Mesh.FromProto);

            // Model has no protobuf schema, so it travels as a COMPAS JSON dump. Registering the
            // fallback is what lets a payload come back as a Model rather than a raw dictionary.
            Registry.RegisterFallback<Model>(
                Model.ModelDType,
                model => model.ToFallbackData(),
                values => Model.FromData(ToPayload(values))
            );

            // Element is also a valid top-level compas_pb value. Registering its base dtype makes
            // standalone elements deserialize back to Element instead of a raw fallback dictionary.
            Registry.RegisterFallback<Element>(
                Element.ElementDType,
                element => element.ToFallbackData(),
                values => Element.FromData(ToPayload(values))
            );
        }

        private static Dictionary<string, object> ToPayload(IDictionary<string, object?> values)
        {
            var payload = new Dictionary<string, object>(values.Count);
            foreach (var entry in values)
            {
                payload[entry.Key] = entry.Value!;
            }
            return payload;
        }
    }
}
