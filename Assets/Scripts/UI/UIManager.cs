using UnityEngine;
using UnityEngine.InputSystem;

namespace Compas.GeoViewerXR.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private UIOptionsMenu _optionsMenu;
        [SerializeField] private UIMqttTopicsMenu _mqttTopicsMenu;
        [SerializeField] private UIObjectMenu _objectMenu;

        [Header("Dismissal")]
        [SerializeField]
        [Tooltip("Close the open panel when the user taps outside it.")]
        private bool _closeOnOutsideTap = true;

        public void ToggleOptionsMenu()
        {
            _optionsMenu.gameObject.SetActive(!_optionsMenu.gameObject.activeSelf);
        }

        public void ToggleTopicsMenu()
        {
            _mqttTopicsMenu.gameObject.SetActive(!_mqttTopicsMenu.gameObject.activeSelf);
        }

        private void Start()
        {
            _optionsMenu.Initialize();
            _optionsMenu.gameObject.SetActive(false);
            _mqttTopicsMenu.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_closeOnOutsideTap || !TryGetPressPosition(out Vector2 screenPosition))
            {
                return;
            }

            if (_objectMenu.IsOpen && !IsInside(_objectMenu.Panel, screenPosition))
            {
                _objectMenu.Close();
            }

            // The topics menu opens on top of the options menu, so a tap outside closes only the
            // topmost one; the next outside tap closes the options menu.
            if (IsOpen(_mqttTopicsMenu))
            {
                if (!IsInside(_mqttTopicsMenu.transform, screenPosition))
                {
                    _mqttTopicsMenu.gameObject.SetActive(false);
                }
                return;
            }

            if (IsOpen(_optionsMenu) && !IsInside(_optionsMenu.transform, screenPosition))
            {
                _optionsMenu.gameObject.SetActive(false);
            }
        }

        private static bool IsOpen(MonoBehaviour panel) => panel != null && panel.gameObject.activeSelf;

        /// <summary>
        /// Where a press began this frame, if one did. Pointer covers touch, mouse and pen alike,
        /// so this behaves the same on a phone and in the Editor. Buttons fire on release, so the
        /// press that opens a panel can never also close it.
        /// </summary>
        private static bool TryGetPressPosition(out Vector2 screenPosition)
        {
            screenPosition = default;
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return false;
            }

            screenPosition = pointer.position.ReadValue();
            return true;
        }

        /// <summary>
        /// Whether a screen point falls within a panel. A panel without a RectTransform counts as
        /// inside, so it is never closed by accident.
        /// </summary>
        private static bool IsInside(Transform panel, Vector2 screenPosition)
        {
            if (!(panel is RectTransform rect))
            {
                return true;
            }

            // An overlay canvas draws straight to the screen and takes no camera.
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, uiCamera);
        }
    }
}
