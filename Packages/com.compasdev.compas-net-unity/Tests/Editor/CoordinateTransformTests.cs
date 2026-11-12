using Compas.Core.Geometry;
using Compas.Core.Transformations;
using Compas.Unity.Core.Transformations;
using NUnit.Framework;
using UnityEngine;

namespace Compas.Unity.Tests.Editor
{
    /// <summary>
    /// Coverage for the coordinate system conversions in
    /// Compas.Core.Transformations.ObjectTransformations. 
    /// The previous consumer was the dev script GeoSerializationSubscriber.cs, verified by eye.
    ///
    /// <para>
    /// This matters because COMPAS/Rhino is Z-up and Unity is Y-up. Per the matrix table in
    /// ObjectTransformations.cs, RhinoZUp -> Unity is a pure Y/Z swap:
    ///</para>
    ///<para>
    ///     RhinoZUp(X, Y, Z)  ->  Unity(X, Z, Y)
    ///</para>
    ///
    /// Every fixture here is asymmetric so that wrong orientation can be detected easily.
    /// </summary>
    public class CoordinateTransformTests
    {
        // ---------- The matrix itself ----------

        [Test]
        public void RhinoZUpToUnity_MatrixSwapsYAndZ()
        {
            float[] matrix = ObjectTransformations.GetTransform(
                SupportedSystem.RhinoZUp, SupportedSystem.Unity);

            Assert.AreEqual(16, matrix.Length, "Expected a 4x4 matrix.");

            // The matrix is column-major: Point.Transform computes
            //   x = X*m[0] + Y*m[4] + Z*m[8],  y = X*m[1] + Y*m[5] + Z*m[9],
            //   z = X*m[2] + Y*m[6] + Z*m[10]
            // so the coefficient naming below follows that indexing, not the visual row
            // layout of the literal in ObjectTransformations.cs.
            Assert.AreEqual(1f, matrix[0], GeometryTestUtilities.Tolerance, "x should take X.");
            Assert.AreEqual(1f, matrix[9], GeometryTestUtilities.Tolerance, "y should take Z.");
            Assert.AreEqual(1f, matrix[6], GeometryTestUtilities.Tolerance, "z should take Y.");

            // And the cross terms that would leak the untouched axes must be zero.
            Assert.AreEqual(0f, matrix[5], GeometryTestUtilities.Tolerance, "y must not take Y.");
            Assert.AreEqual(0f, matrix[10], GeometryTestUtilities.Tolerance, "z must not take Z.");
        }

        [Test]
        public void UndefinedTransformPair_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => ObjectTransformations.GetTransform(SupportedSystem.Unity, SupportedSystem.Motive),
                "An undefined system pair should fail loudly rather than silently returning identity.");
        }

        // ---------- Point ----------

        [Test]
        public void Point_RhinoZUpToUnity_SwapsYAndZ()
        {
            var rhinoPoint = new Point(1f, 2f, 3f);

            var unityPoint = ObjectTransformations.Transform(
                rhinoPoint, SupportedSystem.RhinoZUp, SupportedSystem.Unity);

            Assert.AreEqual(1f, unityPoint.X, GeometryTestUtilities.Tolerance, "X should be unchanged.");
            Assert.AreEqual(3f, unityPoint.Y, GeometryTestUtilities.Tolerance, "Unity Y should take Rhino Z.");
            Assert.AreEqual(2f, unityPoint.Z, GeometryTestUtilities.Tolerance, "Unity Z should take Rhino Y.");
        }

        [Test]
        public void Point_RhinoZUpToUnity_IsNotIdentity()
        {
            // Explicit guard: if someone replaces the matrix with identity, the assertions
            // above could still pass for a fixture where Y == Z. This one cannot.
            var rhinoPoint = new Point(1f, 2f, 3f);

            var unityPoint = ObjectTransformations.Transform(
                rhinoPoint, SupportedSystem.RhinoZUp, SupportedSystem.Unity);

            Assert.IsFalse(GeometryTestUtilities.PointsEquivalent(rhinoPoint, unityPoint),
                "RhinoZUp -> Unity returned the input unchanged; the swap is not being applied.");
        }

        [Test]
        public void Point_RoundTripThroughUnityAndBack_RestoresOriginal()
        {
            var original = new Point(1f, 2f, 3f);

            var toUnity = ObjectTransformations.Transform(
                original, SupportedSystem.RhinoZUp, SupportedSystem.Unity);
            var backToRhino = ObjectTransformations.Transform(
                toUnity, SupportedSystem.Unity, SupportedSystem.RhinoZUp);

            Assert.IsTrue(GeometryTestUtilities.PointsEquivalent(original, backToRhino),
                $"Round-trip drifted: ({original.X},{original.Y},{original.Z}) -> "
                + $"({backToRhino.X},{backToRhino.Y},{backToRhino.Z}).");
        }

        // ---------- Mesh ----------

        [Test]
        public void Mesh_RhinoZUpToUnity_SwapsEveryVertex()
        {
            var mesh = GeometryTestUtilities.AsymmetricMesh();
            // Vertex 2 is (1, 2, 3) - distinct in all three axes.
            var originalVertex = mesh.Vertices[2];

            var transformed = ObjectTransformations.Transform(
                mesh, SupportedSystem.RhinoZUp, SupportedSystem.Unity);

            var swapped = transformed.Vertices[2];
            Assert.AreEqual(originalVertex.X, swapped.X, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(originalVertex.Z, swapped.Y, GeometryTestUtilities.Tolerance, "Unity Y should take Rhino Z.");
            Assert.AreEqual(originalVertex.Y, swapped.Z, GeometryTestUtilities.Tolerance, "Unity Z should take Rhino Y.");
        }

        [Test]
        public void Mesh_RhinoZUpToUnity_PreservesTopology()
        {
            var mesh = GeometryTestUtilities.AsymmetricMesh();

            var transformed = ObjectTransformations.Transform(
                mesh, SupportedSystem.RhinoZUp, SupportedSystem.Unity);

            Assert.AreEqual(mesh.Vertices.Count, transformed.Vertices.Count, "Vertex count changed.");
            Assert.AreEqual(mesh.Faces.Count, transformed.Faces.Count, "Face count changed.");
            CollectionAssert.AreEqual(mesh.Faces[0], transformed.Faces[0],
                "Face vertex indices should be untouched by a coordinate transform.");
        }

        // ---------- Box ----------

        [Test]
        public void Box_RhinoZUpToUnity_MovesFrameOrigin()
        {
            var box = new Box(new Frame(new Point(1f, 2f, 3f), new Vector(1f, 0f, 0f), new Vector(0f, 1f, 0f)),
                              2f, 4f, 6f);

            var transformed = ObjectTransformations.Transform(
                box, SupportedSystem.RhinoZUp, SupportedSystem.Unity);

            Assert.AreEqual(1f, transformed.Frame.Point.X, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(3f, transformed.Frame.Point.Y, GeometryTestUtilities.Tolerance, "Unity Y should take Rhino Z.");
            Assert.AreEqual(2f, transformed.Frame.Point.Z, GeometryTestUtilities.Tolerance, "Unity Z should take Rhino Y.");
        }

        // ---------- Unity-side frame conversion ----------

        [Test]
        public void XRTransformsUnity_RhinoZUpFrame_MatchesObjectTransformationsOrigin()
        {
            // The Unity-side helper and the core matrix API must agree, or geometry and the
            // transforms placing it would disagree about where "up" is.
            var frame = new Frame(new Point(1f, 2f, 3f), new Vector(1f, 0f, 0f), new Vector(0f, 1f, 0f));

            (Vector3 position, Quaternion _) = GameObjectTransformations.RhinoZUpFrameToUnityTransform(frame);

            Assert.AreEqual(1f, position.x, GeometryTestUtilities.Tolerance);
            Assert.AreEqual(3f, position.y, GeometryTestUtilities.Tolerance, "Unity Y should take Rhino Z.");
            Assert.AreEqual(2f, position.z, GeometryTestUtilities.Tolerance, "Unity Z should take Rhino Y.");
        }
    }
}
