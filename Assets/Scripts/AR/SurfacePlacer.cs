using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Puts spawned geometry on the AR surface the camera is looking at.
    ///
    /// The one place that knows about AR raycasting: spawners hand over a transform and get it
    /// back placed, without referencing AR Foundation themselves. Lives next to the
    /// <see cref="ARRaycastManager"/> on the XR Origin.
    ///
    /// The target is found by raycasting from the centre of the screen against detected planes,
    /// keeping the first one that faces up (floor, table) so nothing gets stuck to a wall. With no
    /// plane under the crosshair the object is placed a fixed distance in front of the camera
    /// instead, so a spawn button never silently does nothing.
    /// </summary>
    public class SurfacePlacer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Raycasts against the detected AR planes.")]
        private ARRaycastManager _raycastManager;

        [SerializeField]
        [Tooltip("Camera whose view decides where things go. Uses Camera.main if left empty.")]
        private Camera _camera;

        [SerializeField]
        [Tooltip("Distance in front of the camera when no surface is found.")]
        private float _fallbackDistance = 1.5f;

        [SerializeField]
        [Tooltip("How far below eye level to place things when no surface is found.")]
        private float _fallbackDropBelowEye = 0.5f;

        private const float MinUpDot = 0.9f;

        private static readonly List<ARRaycastHit> hits = new();

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        /// <summary>
        /// Rotates <paramref name="target"/> to face along the camera's view, then moves it so
        /// the bottom centre of everything it renders sits on the surface in front of the camera.
        /// Works for a single primitive and for a root holding many pieces alike.
        /// </summary>
        public void PlaceInFrontOfCamera(Transform target)
        {
            Pose pose = GetPoseInFrontOfCamera();
            target.rotation = pose.rotation;

            if (!TryGetRenderBounds(target, out Bounds bounds))
            {
                target.position = pose.position;
                return;
            }

            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            target.position += pose.position - bottomCenter;
        }

        /// <summary>
        /// Where to put an object: on the upward-facing plane at the screen centre, or in front
        /// of the camera if there is none. The rotation is yaw-only and looks along the camera's
        /// view, so geometry authored in Rhino's Front view is seen the same way here.
        /// </summary>
        private Pose GetPoseInFrontOfCamera()
        {
            Transform cam = _camera.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-4f)
            {
                // Looking straight down or up: fall back to the camera's up vector for heading.
                flatForward = Vector3.ProjectOnPlane(cam.up, Vector3.up);
            }
            flatForward.Normalize();
            Quaternion yaw = Quaternion.LookRotation(flatForward, Vector3.up);

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (_raycastManager != null && _raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                // Hits are sorted by distance, so the first upward-facing one is the nearest surface.
                foreach (ARRaycastHit hit in hits)
                {
                    if (Vector3.Dot(hit.pose.up, Vector3.up) >= MinUpDot)
                    {
                        return new Pose(hit.pose.position, yaw);
                    }
                }
            }

            Vector3 position = cam.position + flatForward * _fallbackDistance;
            position.y = cam.position.y - _fallbackDropBelowEye;
            return new Pose(position, yaw);
        }

        private static bool TryGetRenderBounds(Transform target, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }
    }
}
