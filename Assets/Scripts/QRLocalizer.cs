using Compas.Core.Geometry;
using Compas.Unity.Core.Transformations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Tap-to-localize against compas_xr QR codes. Lives on the XR Origin (AR Rig).
    ///
    /// <para>Grasshopper sends <c>"qr_frames": {"0": Frame, "1": Frame, ...}</c> inside the
    /// geometry message; compas_pb decodes each one to a <see cref="Frame"/>. Reference images are
    /// named QR0, QR1, ... so the number picks the frame.</para>
    ///
    /// <para>On tap, the XR Origin is moved so the tracked QR lands on its Rhino frame. The frame
    /// is the printed sheet's bottom-left corner (red X line, green Y line). Geometry is never
    /// touched: GeometryVisualizer keeps drawing in Rhino coordinates, and a QR placed at
    /// Frame.worldXY() makes that corner 0,0,0.</para>
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class QRLocalizer : MonoBehaviour
    {
        [Tooltip("The model was lined up with a QR code. Gives the QR number.")]
        public UnityEvent<int> OnLocalized = new UnityEvent<int>();

        private readonly Dictionary<int, Pose> qrFrames = new();
        private ARTrackedImageManager trackedImages;

        private void Awake()
        {
            trackedImages = GetComponent<ARTrackedImageManager>();
        }

        private void OnEnable()
        {
            GeoViewerHub.Instance.GeometryEvents.OnMessageReceived.AddListener(OnMessageReceived);
        }

        private void OnDisable()
        {
            GeoViewerHub.Instance.GeometryEvents.OnMessageReceived.RemoveListener(OnMessageReceived);
        }

        private void Update()
        {
            if (TapBegan() && !GeoViewerHub.Instance.PlacedObjects.IsWaitingForTap)
            {
                Localize();
            }
        }

        /// <summary>Moves the XR Origin so the currently tracked QR sits on its frame.</summary>
        public void Localize()
        {
            foreach (ARTrackedImage image in trackedImages.trackables)
            {
                if (image.trackingState != TrackingState.Tracking
                    || !int.TryParse(image.referenceImage.name?.Replace("QR", ""), out int index)
                    || !qrFrames.TryGetValue(index, out Pose qr))
                {
                    continue;
                }

                // The QR frame sits on the printed corner where the red X and green Y lines meet;
                // AR Foundation reports the image centre (+X right, +Z towards the top edge).
                Vector3 corner = image.transform.TransformPoint(-image.size.x / 2, 0, -image.size.y / 2);

                // Rigidly move the rig (and the tracked image with it) from that corner to the QR frame.
                Quaternion delta = qr.rotation * Quaternion.Inverse(image.transform.rotation);
                transform.SetPositionAndRotation(
                    qr.position + delta * (transform.position - corner),
                    delta * transform.rotation);

                Debug.Log($"{nameof(QRLocalizer)}: localized with QR{index}.", this);
                OnLocalized.Invoke(index);

                return;
            }
        }

        private void OnMessageReceived()
        {
            if (!(GeoViewerHub.Instance.LastMessage is IDictionary payload) || !(payload["qr_frames"] is IDictionary frames))
            {
                return;
            }

            qrFrames.Clear();
            bool qrFramesReceived = false;
            foreach (DictionaryEntry entry in frames)
            {
                if (entry.Value is Frame frame && int.TryParse(entry.Key.ToString(), out int index))
                {
                    var (position, rotation) = GameObjectTransformations.RhinoZUpFrameToUnityTransform(frame);
                    qrFrames[index] = new Pose(position, rotation);
                    qrFramesReceived = true;
                }
            }

            if (qrFramesReceived)
            {
                Localize();
            }

            Debug.Log($"{nameof(QRLocalizer)}: received {qrFrames.Count} QR frame(s).", this);
        }

        // A tap on UI or on a grabbable object is meant for that, not for localizing; otherwise
        // starting a drag would also move the whole rig.
        private static bool TapBegan()
        {
#if ENABLE_INPUT_SYSTEM
            var touch = Touchscreen.current?.primaryTouch;
            return touch != null && touch.press.wasPressedThisFrame
                && !IsOverUI(touch.touchId.ReadValue())
                && !IsOverInteractable(touch.position.ReadValue());
#else
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began
                && !IsOverUI(Input.GetTouch(0).fingerId)
                && !IsOverInteractable(Input.GetTouch(0).position);
#endif
        }

        private static bool IsOverUI(int pointerId) =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

        private static bool IsOverInteractable(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            return camera != null
                && Physics.Raycast(camera.ScreenPointToRay(screenPosition), out RaycastHit hit)
                && hit.collider.GetComponentInParent<XRGrabInteractable>() != null;
        }
    }
}