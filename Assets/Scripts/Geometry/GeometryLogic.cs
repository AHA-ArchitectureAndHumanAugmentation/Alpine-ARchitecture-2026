using Compas.Core.Transformations;
using Compas.Unity.Core.DataStructures;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Point = Compas.Core.Geometry.Point;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Sits on every shape placed in the app, so it can be sent to Rhino, made movable or deleted.
    /// </summary>
    public class GeometryLogic : MonoBehaviour
    {
        /// <summary>The shape as it is now in the scene, converted to <paramref name="target"/>.</summary>
        public object ToData(SupportedSystem target)
        {
            return CreateMeshAtCurrentPose().ToData(target);
        }

        // Uses world positions so the mesh keeps where the object is, how it is turned and how big it is.
        private UnityMesh CreateMeshAtCurrentPose()
        {
            var vertices = new List<Point>();
            var faces = new List<List<int>>();

            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable)
                {
                    Debug.LogError($"{filter.name}: cannot send this mesh, tick Read/Write in its model's import settings.", filter);
                    continue;
                }

                int firstVertex = vertices.Count;
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 world = filter.transform.TransformPoint(vertex);
                    vertices.Add(new Point(world.x, world.y, world.z));
                }

                int[] triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    faces.Add(new List<int>
                    {
                        firstVertex + triangles[i],
                        firstVertex + triangles[i + 1],
                        firstVertex + triangles[i + 2]
                    });
                }
            }

            return new UnityMesh(vertices, faces);
        }

        public void SetInteractable(bool isEnabled)
        {
            if (!TryGetComponent(out XRGrabInteractable interactable))
            {
                Debug.LogWarning($"{name} has no XRGrabInteractable.");
                return;
            }

            interactable.enabled = isEnabled;
        }

        public void Delete()
        {
            Destroy(gameObject);
        }
    }
}
