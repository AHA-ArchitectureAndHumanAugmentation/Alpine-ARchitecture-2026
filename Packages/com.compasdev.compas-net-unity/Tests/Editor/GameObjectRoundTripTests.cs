using Compas.Unity.Core.DataStructures;
using Compas.Unity.Core.Geometry;
using CompasPb;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Compas.Unity.Tests.Editor
{
    /// <summary>
    /// Covers the full Unity-facing loop: GameObject -> geometry -> protobuf -> geometry ->
    /// GameObject. Ported from UnityProtobuffTest.TestUnityBoxWithGameObject.
    ///
    /// This is the where we test the Unity-specific conventions: localScale vs lossyScale, the
    /// radius/diameter factor on primitives, and fan-triangulation of n-gons (not just serialization 
    /// tests)
    /// </summary>
    public class GameObjectRoundTripTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        private GameObject Track(GameObject go)
        {
            if (go != null)
            {
                spawned.Add(go);
            }
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode tests do not unload the scene between cases, so anything created here
            // would otherwise leak into later tests.
            foreach (var go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            spawned.Clear();
        }

        [Test]
        public void Box_FromGameObject_ReadsLocalScaleAsSize()
        {
            var cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            cube.transform.localScale = new Vector3(2f, 4f, 6f);

            var box = UnityBox.FromGameObject(cube);

            Assert.AreEqual(2f, box.XSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(4f, box.YSize, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(6f, box.ZSize, GeometryTestUtilities.Tolerance);
        }

        [Test]
        public void Box_GameObjectRoundTripThroughProto_PreservesScale()
        {
            var cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            cube.transform.localScale = new Vector3(2f, 4f, 6f);
            cube.transform.position = new Vector3(1f, 2f, 3f);

            var serializer = new CompasPbSerializer();
            var original = UnityBox.FromGameObject(cube);

            byte[] bytes = serializer.Pack(original);
            var restored = (UnityBox)serializer.Unpack(bytes);

            // Clear the source reference so ToGameObject builds a fresh object rather than
            // mutating the one we read from.
            restored.BoxGameObject = null;
            var rebuilt = Track(restored.ToGameObject());

            Assert.AreEqual(new Vector3(2f, 4f, 6f), rebuilt.transform.localScale);
            Assert.AreEqual(1f, rebuilt.transform.position.x, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(2f, rebuilt.transform.position.y, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(3f, rebuilt.transform.position.z, GeometryTestUtilities.Tolerance);
        }

        [Test]
        public void Sphere_FromGameObject_ConvertsScaleToRadius()
        {
            // Unity primitive spheres are diameter-scaled, so radius = scale.x / 2.
            var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            sphere.transform.localScale = new Vector3(5f, 5f, 5f);

            var unitySphere = UnitySphere.FromGameObject(sphere);

            Assert.AreEqual(2.5f, unitySphere.Radius, GeometryTestUtilities.Tolerance);
        }

        [Test]
        public void Sphere_ToGameObject_ConvertsRadiusBackToDiameterScale()
        {
            var unitySphere = GeometryTestUtilities.Sphere(); // radius 2.5

            var go = Track(unitySphere.ToGameObject());

            Assert.AreEqual(new Vector3(5f, 5f, 5f), go.transform.localScale);
        }

        [Test]
        public void Mesh_ToGameObject_FanTriangulatesNgons()
        {
            // A quad face must become 2 triangles (6 indices) at the rendering boundary,
            // while the source mesh keeps its 4-vertex face.
            var mesh = GeometryTestUtilities.Quad();

            var go = Track(mesh.ToGameObject());
            var meshFilter = go.GetComponent<MeshFilter>();

            Assert.IsNotNull(meshFilter, "ToGameObject did not attach a MeshFilter.");
            Assert.AreEqual(4, meshFilter.sharedMesh.vertexCount);
            Assert.AreEqual(6, meshFilter.sharedMesh.triangles.Length,
                "Quad should fan-triangulate into 2 triangles.");
            Assert.AreEqual(4, mesh.Faces[0].Count, "Source mesh face should remain an n-gon.");
        }

        [Test]
        public void Mesh_ToGameObject_CalledTwice_ReusesSameGameObject()
        {
            // MeshGameObject caching is what lets a live MQTT feed update in place instead of
            // spawning a new object per message.
            var mesh = GeometryTestUtilities.Quad();

            var first = Track(mesh.ToGameObject());
            var second = mesh.ToGameObject();

            Assert.AreSame(first, second,
                "ToGameObject should reuse the cached MeshGameObject rather than instantiating.");
        }

        [Test]
        public void Mesh_FromGameObject_ReadsBackTriangles()
        {
            var mesh = GeometryTestUtilities.Quad();
            var go = Track(mesh.ToGameObject());

            var readBack = UnityMesh.FromGameObject(go);

            Assert.AreEqual(4, readBack.Vertices.Count);
            // Read back from the Unity mesh, so faces are the triangulated form, not the quad.
            Assert.AreEqual(2, readBack.Faces.Count,
                "Reading back from a Unity mesh yields triangles, not the original n-gon.");
        }
    }
}
