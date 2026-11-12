using Compas.Core.Geometry;
using Compas.Unity.Core.DataStructures;
using Compas.Unity.Core.Geometry;
using CompasPb;
using NUnit.Framework;
using System.Collections.Generic;
using BaseMesh = Compas.Core.Geometry.Mesh;

namespace Compas.Unity.Tests.Editor
{
    /// <summary>
    /// A payload is not always a single geometry: the Rhino side can publish a
    /// Dictionary{string, object} carrying several named items plus primitives. This logic comes 
    /// from the dev scripts MqttProtoTest.cs and GeoSerializationSubscriber.cs.
    ///
    /// Also pins down the "upcycling" behaviour that GeoSerializationSubscriber worked around:
    /// geometry nested inside a dictionary comes back as base COMPAS types (Box, Point,
    /// Mesh), not their Unity subclasses, so a consumer that needs ToGameObject() must convert
    /// via the FromBox/FromPoint/FromMesh factories. 
    /// </summary>
    public class DictionaryPayloadTests
    {
        private CompasPbSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new CompasPbSerializer();
        }

        private static Dictionary<string, object> BuildMixedPayload()
        {
            return new Dictionary<string, object>
            {
                { "label", "test-payload" },
                { "scale", 2.5f },
                { "enabled", true },
                { "count", 42 },
                { "origin", new Point(1f, 2f, 3f) },
                { "box", new Box(Frame.WorldXY(), 2f, 4f, 6f) },
            };
        }

        [Test]
        public void MixedDictionary_RoundTrips_PreservingKeys()
        {
            var payload = BuildMixedPayload();

            byte[] bytes = serializer.Pack(payload);
            var restored = (Dictionary<string, object>)serializer.Unpack(bytes);

            Assert.IsNotNull(restored, "Dictionary payload deserialized to null.");
            CollectionAssert.AreEquivalent(payload.Keys, restored.Keys,
                "Dictionary keys changed across the round-trip.");
        }

        [Test]
        public void MixedDictionary_RoundTrips_PreservingPrimitiveValues()
        {
            var payload = BuildMixedPayload();

            byte[] bytes = serializer.Pack(payload);
            var restored = (Dictionary<string, object>)serializer.Unpack(bytes);

            Assert.AreEqual("test-payload", restored["label"].ToString());
            Assert.AreEqual(2.5f, System.Convert.ToSingle(restored["scale"]), GeometryTestUtilities.Tolerance);
            Assert.IsTrue(System.Convert.ToBoolean(restored["enabled"]));
            Assert.AreEqual(42, System.Convert.ToInt32(restored["count"]));
        }

        [Test]
        public void MixedDictionary_RoundTrips_PreservingNestedGeometry()
        {
            var payload = BuildMixedPayload();

            byte[] bytes = serializer.Pack(payload);
            var restored = (Dictionary<string, object>)serializer.Unpack(bytes);

            // Assert against the base types: this holds whether or not the converter happens
            // happens to hand back a Unity subclass, so the test pins the contract, not the detail.
            Assert.IsInstanceOf<Point>(restored["origin"], "Nested Point did not survive as a Point.");
            Assert.IsInstanceOf<Box>(restored["box"], "Nested Box did not survive as a Box.");

            var origin = (Point)restored["origin"];
            Assert.IsTrue(GeometryTestUtilities.PointsEquivalent(new Point(1f, 2f, 3f), origin));

            var box = (Box)restored["box"];
            Assert.AreEqual(2f, box.XSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(4f, box.YSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(6f, box.ZSize, GeometryTestUtilities.Tolerance);
        }

        [Test]
        public void BaseGeometry_UpcyclesToUnityTypes()
        {
            // Mirrors GeoSerializationSubscriber.UpcycleGeometryObject: the conversion a
            // consumer must perform before it can call ToGameObject().
            object upcycledBox = Upcycle(new Box(Frame.WorldXY(), 2f, 4f, 6f));
            object upcycledPoint = Upcycle(new Point(1f, 2f, 3f));
            object upcycledMesh = Upcycle(GeometryTestUtilities.Quad());

            Assert.IsInstanceOf<UnityBox>(upcycledBox);
            Assert.IsInstanceOf<UnityPoint>(upcycledPoint);
            Assert.IsInstanceOf<UnityMesh>(upcycledMesh);
        }

        [Test]
        public void Upcycling_PreservesGeometryValues()
        {
            var original = new Box(Frame.WorldXY(), 2f, 4f, 6f);

            var upcycled = (UnityBox)Upcycle(original);

            Assert.AreEqual(original.XSize, upcycled.XSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.YSize, upcycled.YSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(original.ZSize, upcycled.ZSize, GeometryTestUtilities.Tolerance);
            Assert.IsTrue(GeometryTestUtilities.PointsEquivalent(original.Frame.Point, upcycled.Frame.Point));
        }

        private static object Upcycle(object item)
        {
            switch (item)
            {
                case UnityBox _:
                case UnityCylinder _:
                case UnitySphere _:
                case UnityPoint _:
                case UnityMesh _:
                    return item; // already a Unity type
                case Box box:
                    return UnityBox.FromBox(box);
                case Cylinder cylinder:
                    return UnityCylinder.FromCylinder(cylinder);
                case Sphere sphere:
                    return UnitySphere.FromSphere(sphere);
                case Point point:
                    return UnityPoint.FromPoint(point);
                case BaseMesh mesh:
                    return UnityMesh.FromMesh(mesh);
                default:
                    return item;
            }
        }
    }
}
