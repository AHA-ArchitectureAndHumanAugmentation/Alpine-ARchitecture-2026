using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Compas.GeoViewerXR.UI
{
    public class UIOptionsMenu : MonoBehaviour
    {
        [Header("Toggle Interactable")]
        [SerializeField] private DebugSlider _interactableStateSlider;

        [Header("AR options")]
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private DebugSlider _showPlanesSlider;
        [SerializeField] private ARDebugMenu _debugMenu;
        [SerializeField] private DebugSlider _debugMenuSlider;

        private bool _showPlanes;
        private float _debugMenuPlanesValue;

        public void TogglePlanes()
        {
            _showPlanes = !_showPlanes;
            _showPlanesSlider.value = _showPlanes ? 1 : 0;
            PlayerPrefs.SetInt(PlayerPrefsKeys.ShowPlanes, _showPlanes ? 1 : 0);
            PlayerPrefs.Save();

            foreach (ARPlane plane in _planeManager.trackables)
            {
                ApplyPlaneVisibility(plane);
            }
        }

        public void ToggleDebugMenu()
        {
            bool show = !_debugMenu.gameObject.activeSelf;
            _debugMenuSlider.value = show ? 1 : 0;

            // The debug menu turns its own plane lines off whenever it is enabled, so remember that setting.
            if (show)
            {
                _debugMenu.gameObject.SetActive(true);
                _debugMenu.showPlanesButton.value = _debugMenuPlanesValue;
            }
            else
            {
                _debugMenuPlanesValue = _debugMenu.showPlanesButton.value;
                _debugMenu.showPlanesButton.value = 0f;
                _debugMenu.gameObject.SetActive(false);
            }
        }

        /// <summary>Called once at startup by UIManager, because this panel may start switched off and Awake would come too late.</summary>
        public void Initialize()
        {
            GeoViewerHub.Instance.PlacedObjects.OnInteractableChanged.AddListener(ShowInteractableState);
            _planeManager.trackablesChanged.AddListener(OnPlanesChanged);

            ShowInteractableState(GeoViewerHub.Instance.PlacedObjects.IsInteractable);
            _showPlanes = PlayerPrefs.GetInt(PlayerPrefsKeys.ShowPlanes, 1) == 1;
            _showPlanesSlider.value = _showPlanes ? 1 : 0;
            _debugMenuSlider.value = 0;
        }

        private void OnDestroy()
        {
            GeoViewerHub.Instance.PlacedObjects.OnInteractableChanged.RemoveListener(ShowInteractableState);
            _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        }

        private void ShowInteractableState(bool isInteractable)
        {
            _interactableStateSlider.value = isInteractable ? 1 : 0;
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> changes)
        {
            foreach (ARPlane plane in changes.added)
            {
                ApplyPlaneVisibility(plane);
            }
        }

        private void ApplyPlaneVisibility(ARPlane plane)
        {
            if (plane.TryGetComponent(out ARPlaneMeshVisualizerFader fader))
            {
                fader.visualizeSurfaces = _showPlanes;
            }
        }
    }
}
