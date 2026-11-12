using UnityEngine;
using UnityEngine.Events;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Every event about received geometry and its sequence, for sounds, UI and your own scripts.
    /// </summary>
    public class GeometryEvents : MonoBehaviour
    {
        [Header("Geometry")]
        [Tooltip("A message arrived from Rhino, or a file was loaded. Nothing is drawn yet.")]
        public UnityEvent OnMessageReceived = new UnityEvent();

        [Tooltip("One object was drawn or updated. Gives its path, the object, and whether it is new.")]
        public UnityEvent<string, GameObject, bool> OnObjectDrawn = new UnityEvent<string, GameObject, bool>();

        [Tooltip("Some geometry was rendered.")]
        public UnityEvent OnGeometryRendered = new UnityEvent();

        [Tooltip("When all geometry was deleted.")]
        public UnityEvent OnGeometryDeleted = new UnityEvent();

        [Header("Sequence")]
        [Tooltip("The sequence moved to another step. Gives the step number, the first one is 0.")]
        public UnityEvent<int> OnStepChanged = new UnityEvent<int>();

        [Tooltip("The sequence reached its last step.")]
        public UnityEvent OnLastStep = new UnityEvent();

        private int _currentStep = -1;

        private void OnEnable()
        {
            GeoViewerHub.Instance.Sequence.OnStepShown.AddListener(OnStepShown);
        }

        private void OnDisable()
        {
            GeoViewerHub.Instance.Sequence.OnStepShown.RemoveListener(OnStepShown);
        }

        private void OnStepShown(int step)
        {
            if (step == _currentStep)
            {
                return;
            }

            _currentStep = step;
            OnStepChanged.Invoke(step);

            if (step == GeoViewerHub.Instance.Sequence.OrderedElements.Count - 1)
            {
                OnLastStep.Invoke();
            }
        }
    }
}
