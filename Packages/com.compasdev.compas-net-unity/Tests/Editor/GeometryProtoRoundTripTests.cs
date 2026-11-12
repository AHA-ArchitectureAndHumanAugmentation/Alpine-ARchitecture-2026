using Compas.Core.Geometry;
using Compas.Unity.Core.DataStructures;
using Compas.Unity.Core.Geometry;
using CompasPb;
using NUnit.Framework;

namespace Compas.Unity.Tests.Editor
{
    /// <summary>
    /// Protobuf round-trip coverage for every geometry type, at two levels:
    ///
    ///   1. Direct  - type.ToProto() / Type.FromProto(data), the type's own conversion.
    ///   2. Serializer - CompasPbSerializer.Pack/Unpack(), which adds the registry type routing
    ///      set up in UnityProtoConversions and the google.protobuf.Any wrapper that goes over
    ///      the wire.
    ///
    /// This split is inherited from the dev scripts UnityGeometryProtobuffTest.cs and
    /// UnityBox/Cylinder/Sphere/PointProtoTest.cs, which tested each level separately.
    /// </summary>
    public class GeometryProtoRoundTripTests
    {
        private CompasPbSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new CompasPbSerializer();
        }

        private T RoundTripViaSerializer<T>(T original)
        {
            byte[] bytes = serializer.Pack(original);
            Assert.IsNotNull(bytes, "Serialization returned null bytes.");
            Assert.Greater(bytes.Length, 0, "Serialization returned empty bytes.");
            return (T)serializer.Unpack(bytes);
        }

        // ---------- Point ----------

        [Test]
        public void Point_DirectRoundTrip_PreservesCoordinates()
        {
            var original = GeometryTestUtilities.Point3D();

            var restored = UnityPoint.FromProto(original.ToProto());

            Assert.IsTrue(GeometryTestUtilities.PointsEquivalent(original, restored),
                $"Expected ({original.X}, {original.Y}, {original.Z}), got ({restored.X}, {restored.Y}, {restored.Z}).");
        }

        [Test]
        public void Point_SerializerRoundTrip_PreservesCoordinatesAndType()
        {
            var original = GeometryTestUtilities.Point3D();

            var restored = RoundTripViaSerializer(original);

            Assert.IsInstanceOf<UnityPoint>(restored, "Registry should route PointData back to UnityPoint.");
            Assert.IsTrue(GeometryTestUtilities.PointsEquivalent(original, restored));
        }

        // ---------- Box ----------

        [Test]
        public void Box_DirectRoundTrip_PreservesSizesAndFrame()
        {
            var original = GeometryTestUtilities.Box();

            var restored = UnityBox.FromProto(original.ToProto());

            Assert.AreEqual(original.XSize, restored.XSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.YSize, restored.YSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.ZSize, restored.ZSize, GeometryTestUtilities.Tolerance);
            Assert.IsTrue(GeometryTestUtilities.PointsEquivalent(original.Frame.Point, restored.Frame.Point));
        }

        [Test]
        public void Box_SerializerRoundTrip_PreservesSizesAndType()
        {
            var original = GeometryTestUtilities.Box();

            var restored = RoundTripViaSerializer(original);

            Assert.IsInstanceOf<UnityBox>(restored);
            Assert.AreEqual(original.XSize, restored.XSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.YSize, restored.YSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.ZSize, restored.ZSize, GeometryTestUtilities.Tolerance);
        }

        // ---------- Sphere ----------

        [Test]
        public void Sphere_DirectRoundTrip_PreservesRadius()
        {
            var original = GeometryTestUtilities.Sphere();

            var restored = UnitySphere.FromProto(original.ToProto());

            Assert.AreEqual(original.Radius, restored.Radius, GeometryTestUtilities.Tolerance);
        }

        [Test]
        public void Sphere_SerializerRoundTrip_PreservesRadiusAndType()
        {
            var original = GeometryTestUtilities.Sphere();

            var restored = RoundTripViaSerializer(original);

            Assert.IsInstanceOf<UnitySphere>(restored);
            Assert.AreEqual(original.Radius, restored.Radius, GeometryTestUtilities.Tolerance);
        }

        // ---------- Cylinder ----------

        [Test]
        public void Cylinder_DirectRoundTrip_PreservesRadiusAndHeight()
        {
            var original = GeometryTestUtilities.Cylinder();

            var restored = UnityCylinder.FromProto(original.ToProto());

            Assert.AreEqual(original.Radius, restored.Radius, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.Height, restored.Height, GeometryTestUtilities.Tolerance);
        }

        [Test]
        public void Cylinder_SerializerRoundTrip_PreservesRadiusHeightAndType()
        {
            var original = GeometryTestUtilities.Cylinder();

            var restored = RoundTripViaSerializer(original);

            Assert.IsInstanceOf<UnityCylinder>(restored);
            Assert.AreEqual(original.Radius, restored.Radius, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.Height, restored.Height, GeometryTestUtilities.Tolerance);
        }

        // ---------- Mesh ----------

        [Test]
        public void Mesh_DirectRoundTrip_PreservesTopology()
        {
            var original = GeometryTestUtilities.MixedFaceMesh();

            var restored = UnityMesh.FromProto(original.ToProto());

            Assert.IsTrue(GeometryTestUtilities.MeshesEquivalent(original, restored),
                "Mesh vertices/faces changed across a direct proto round-trip.");
        }

        [Test]
        public void Mesh_SerializerRoundTrip_PreservesTopologyAndType()
        {
            var original = GeometryTestUtilities.MixedFaceMesh();

            var restored = RoundTripViaSerializer(original);

            Assert.IsInstanceOf<UnityMesh>(restored);
            Assert.IsTrue(GeometryTestUtilities.MeshesEquivalent(original, restored));
        }

        [Test]
        public void Mesh_RoundTrip_KeepsNgonFacesUntriangulated()
        {
            // Triangulation belongs at the Unity rendering boundary (ToGameObject), not in the
            // wire format - a quad must still be a 4-vertex face after deserializing.
            var original = GeometryTestUtilities.Quad();

            var restored = RoundTripViaSerializer(original);

            Assert.IsInstanceOf<UnityMesh>(restored);
            Assert.AreEqual(1, restored.Faces.Count, "Face count changed.");
            Assert.AreEqual(4, restored.Faces[0].Count, "Quad was triangulated during serialization.");
        }

        [Test]
        public void Mesh_DeserializesAsBaseMesh()
        {
            // The same bytes must still satisfy a consumer that only knows the base COMPAS
            // type - which holds because UnityMesh derives from Mesh.
            var original = GeometryTestUtilities.MixedFaceMesh();
            byte[] bytes = serializer.Pack(original);

            var asBase = (Mesh)serializer.Unpack(bytes);

            Assert.IsNotNull(asBase, "Could not produce a base Mesh from the payload.");
            Assert.IsTrue(GeometryTestUtilities.MeshesEquivalent(original, asBase));
        }
    }
}
