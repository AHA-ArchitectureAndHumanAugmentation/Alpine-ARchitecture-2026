using System;
using System.Collections.Generic;
using System.Linq;
using Compas.Core.Geometry;
using Compas.Core.Transformations;
using Compas.Unity.Data;
using Compas.Unity.Visualization;
using CompasPb.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace Compas.Unity.Core.DataStructures
{
	/// <summary>
	/// Unity-native representation of a mesh in 3D space.
	/// Extends the COMPAS Mesh class with Unity GameObject integration and visualization capabilities.
	/// Supports n-gon faces internally; triangulates at the Unity rendering boundary.
	/// </summary>
	public class UnityMesh : Compas.Core.Geometry.Mesh, IUnityProtoConvertible<UnityMesh, MeshData>, IUnityGeometry
	{
		/// <summary>
		/// Gets or sets the GameObject representation of this mesh.
		/// </summary>
		public GameObject MeshGameObject { get; set; }

		/// <summary>Render-contract view of <see cref="MeshGameObject"/>.</summary>
		public GameObject CachedGameObject
		{
			get => MeshGameObject;
			set => MeshGameObject = value;
		}

		/// <summary>
        /// The active render pipeline's default material.
        /// </summary>
        private static Material DefaultMaterial
        {
            get
            {
                if (defaultMaterial == null)
                {
                    var pipeline = GraphicsSettings.currentRenderPipeline;
                    if (pipeline != null)
                    {
                        defaultMaterial = pipeline.defaultMaterial;
                    }
                    else
                    {
                        defaultMaterial = new Material(Shader.Find("Standard"));
                    }
                }
                return defaultMaterial;
            }
        }

        /// <summary>
		/// Initializes a new UnityMesh with optional vertices, faces, and attribute dictionaries.
		/// </summary>
		public UnityMesh(
			IEnumerable<Point> vertices = null,
			IEnumerable<IEnumerable<int>> faces = null,
			Dictionary<string, object> attributes = null,
			Dictionary<string, object> defaultEdgeAttributes = null,
			Dictionary<string, object> defaultFaceAttributes = null,
			Dictionary<string, object> defaultVertexAttributes = null)
			: base(vertices, faces, attributes, defaultEdgeAttributes, defaultFaceAttributes, defaultVertexAttributes)
		{
		}

		/// <summary>
        /// Material applied when the renderer has none of its own. Leave null to fall back to the
        /// active render pipeline's default.
        /// </summary>
        public Material DisplayMaterial { get; set; }

        private static Material defaultMaterial;

        /// <summary>
		/// Creates or updates the Unity GameObject representation of this mesh.
		/// N-gon faces are fan-triangulated at this boundary.
		/// </summary>
		public GameObject ToGameObject()
		{
            // Use Unity's null semantics so destroyed-object references are treated as null.
            GameObject go = MeshGameObject;
            if (go == null)
            {
                go = new GameObject("UnityMesh");
            }

            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = go.AddComponent<MeshFilter>();
            }

            // Repeat messages update the same GameObject, so reuse the mesh it already owns.
            // Allocating a replacement would orphan the old one, and an unreferenced Mesh is not
            // collected along with its GameObject - that leaks one Mesh per incoming message.
            var mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new UnityEngine.Mesh { name = "UnityMesh" };
            }
            else
            {
                mesh.Clear();
            }

			// Convert COMPAS vertices to Unity Vector3 (coordinate system is consistent - no swap needed)
            var vertices = Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();

            // The default 16-bit index buffer cannot address more than 65535 vertices.
            if (vertices.Length > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = vertices;

			// Fan-triangulate all faces (n-gon safe: triangles pass through, quads split into 2, etc.)
			var triangles = new List<int>();
			foreach (var face in Faces)
			{
				if (face.Count < 3) continue;
				for (int i = 1; i < face.Count - 1; i++)
				{
					triangles.Add(face[0]);
					triangles.Add(face[i]);
					triangles.Add(face[i + 1]);
				}
			}
			mesh.triangles = triangles.ToArray();
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();

            SetMaterial(go);

            meshFilter.sharedMesh = mesh;
            MeshGameObject = go;
            return go;
			}

        private void SetMaterial(GameObject go)
			{
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = go.AddComponent<MeshRenderer>();
			}

            // A MeshRenderer from AddComponent has no material and renders magenta.
            if (meshRenderer.sharedMaterial == null)
			{
                meshRenderer.sharedMaterial = DisplayMaterial ?? DefaultMaterial;
			}
		}

		/// <summary>
		/// Creates a UnityMesh from a Unity GameObject containing a MeshFilter.
		/// Triangle faces are read back as-is from the Unity mesh.
		/// </summary>
		public static UnityMesh FromGameObject(GameObject gameObject)
		{
			if (gameObject == null)
				throw new ArgumentNullException(nameof(gameObject));

			var meshFilter = gameObject.GetComponent<MeshFilter>();
			if (meshFilter == null || meshFilter.sharedMesh == null)
				throw new ArgumentException("GameObject does not have a MeshFilter with a mesh.", nameof(gameObject));

			var unityMesh = meshFilter.sharedMesh;
			var vertices = unityMesh.vertices.Select(v => new Point(v.x, v.y, v.z)).ToList();

			var faces = new List<List<int>>();
			int[] tris = unityMesh.triangles;
			for (int i = 0; i < tris.Length; i += 3)
			{
				faces.Add(new List<int> { tris[i], tris[i + 1], tris[i + 2] });
			}

			return new UnityMesh(vertices, faces) { MeshGameObject = gameObject };
		}

		/// <summary>
		/// Creates a UnityMesh from a base COMPAS Mesh instance, preserving all attributes.
		/// </summary>
		public static UnityMesh FromMesh(Compas.Core.Geometry.Mesh mesh)
		{
			if (mesh == null)
				throw new ArgumentNullException(nameof(mesh));

			return new UnityMesh(
				mesh.Vertices,
				mesh.Faces,
				mesh.Attributes,
				mesh.DefaultEdgeAttributes,
				mesh.DefaultFaceAttributes,
				mesh.DefaultVertexAttributes);
		}

		/// <inheritdoc/>
		public object ToData(SupportedSystem target)
		{
			UnityMesh current = MeshGameObject != null ? FromGameObject(MeshGameObject) : this;
			return UnityGeometryFactory.FromUnitySystem(current, target);
		}

		/// <summary>
		/// Converts this UnityMesh to its protobuf representation.
		/// </summary>
		public new MeshData ToProto() => base.ToProto();

		/// <summary>
		/// Creates a UnityMesh from protobuf data.
		/// </summary>
		public static new UnityMesh FromProto(MeshData meshData)
		{
			return FromMesh(Compas.Core.Geometry.Mesh.FromProto(meshData));
		}
	}
}