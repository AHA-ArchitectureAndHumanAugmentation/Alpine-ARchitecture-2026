using System.Collections.Generic;
using Compas.Core.Geometry;
using Compas.Unity.Core.DataStructures;
using Compas.Unity.Core.Geometry;
using BaseMesh = Compas.Core.Geometry.Mesh;

namespace Compas.Unity.Tests.Editor
{
    /// <summary>
    /// Shared mock data and comparison helpers for the geometry/proto tests.
    ///
    /// The equivalence helpers are salvaged from the dev script
    /// CompasXR2UnityDevTestingFile/Assets/Scripts/UnityMeshProtoTest.cs, which compared
    /// meshes vertex-by-vertex with a tolerance.
    ///
    /// Mock data are deliberatel asymmetric so that a wrong or missing coordinate-system
    /// swap actually changes the values. 
    /// </summary>
    public static class GeometryTestUtilities
    {
        public const float Tolerance = 1e-5f;

        /// <summary>A single 1x1 quad face. Smallest mesh that still exercises n-gon handling.</summary>
        public static UnityMesh Quad()
        {
            var vertices = new List<Point>
            {
                new Point(0f, 0f, 0f),
                new Point(1f, 0f, 0f),
                new Point(1f, 1f, 0f),
                new Point(0f, 1f, 0f),
            };
            var faces = new List<List<int>> { new List<int> { 0, 1, 2, 3 } };
            return new UnityMesh(vertices, faces);
        }

        /// <summary>
        /// A quad and a triangle sharing an edge: mixed face sizes, so fan-triangulation
        /// and n-gon preservation are both exercised. Taken from UnityMeshProtoTest.
        /// </summary>
        public static UnityMesh MixedFaceMesh()
        {
            var vertices = new List<Point>
            {
                new Point(0f, 0f, 0f),
                new Point(1f, 0f, 0f),
                new Point(1f, 1f, 0f),
                new Point(0f, 1f, 0f),
                new Point(0.5f, 1.5f, 0f),
            };
            var faces = new List<List<int>>
            {
                new List<int> { 0, 1, 2, 3 },
                new List<int> { 2, 3, 4 },
            };
            return new UnityMesh(vertices, faces);
        }

        /// <summary>A mesh whose vertices differ in every axis, so any axis swap is visible.</summary>
        public static UnityMesh AsymmetricMesh()
        {
            var vertices = new List<Point>
            {
                new Point(0f, 0f, 0f),
                new Point(1f, 0f, 0f),
                new Point(1f, 2f, 3f),
                new Point(0f, 2f, 3f),
            };
            var faces = new List<List<int>> { new List<int> { 0, 1, 2, 3 } };
            return new UnityMesh(vertices, faces);
        }

        /// <summary>Box with three distinct edge lengths, so X/Y/Z are all distinguishable.</summary>
        public static UnityBox Box() => new UnityBox(Frame.WorldXY(), 2f, 4f, 6f);

        public static UnityCylinder Cylinder() => new UnityCylinder(Frame.WorldXY(), 1.25f, 5f);

        public static UnitySphere Sphere() => new UnitySphere(Frame.WorldXY(), 2.5f);

        public static UnityPoint Point3D() => new UnityPoint(1.5f, -2.25f, 3.75f);

        public static bool Approximately(float a, float b) => System.Math.Abs(a - b) < Tolerance;

        public static bool PointsEquivalent(Point a, Point b)
        {
            if (a == null || b == null)
            {
                return false;
            }
            return Approximately(a.X, b.X) && Approximately(a.Y, b.Y) && Approximately(a.Z, b.Z);
        }

        /// <summary>Compares 2 meshes vertex-by-vertex and face-by-face, with a small float point tolerance.</summary>
        public static bool MeshesEquivalent(BaseMesh a, BaseMesh b)
        {
            if (a == null || b == null)
            {
                return false;
            }
            if (a.Vertices.Count != b.Vertices.Count || a.Faces.Count != b.Faces.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Vertices.Count; i++)
            {
                if (!PointsEquivalent(a.Vertices[i], b.Vertices[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < a.Faces.Count; i++)
            {
                if (a.Faces[i].Count != b.Faces[i].Count)
                {
                    return false;
                }
                for (int j = 0; j < a.Faces[i].Count; j++)
                {
                    if (a.Faces[i][j] != b.Faces[i][j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
