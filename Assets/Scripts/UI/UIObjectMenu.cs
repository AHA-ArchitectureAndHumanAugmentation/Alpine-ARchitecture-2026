using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// The menu of shapes to place in the room. Call its methods from a button's On Click.
    /// </summary>
    public class UIObjectMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _openButton;
        [SerializeField] private GameObject _deleteSelectedButton;
        [SerializeField] private XRInteractionGroup _interactionGroup;

        public bool IsOpen { get; private set; }
        public Transform Panel => _panel.transform;

        public void Open()
        {
            IsOpen = true;
            _panel.SetActive(true);
            _animator.SetBool("Show", true);
            _openButton.SetActive(false);
        }

        public void Close()
        {
            IsOpen = false;
            _animator.SetBool("Show", false);
            _openButton.SetActive(true);
        }

        /// <summary>Closes the menu; the next tap on a surface places this shape from the Object Spawner's list.</summary>
        public void PickShape(int shapeIndex)
        {
            Close();
            GeoViewerHub.Instance.PlacedObjects.PlaceOnNextTap(shapeIndex);
        }

        public void DeleteSelectedObject()
        {
            Destroy(_interactionGroup.focusInteractable.transform.gameObject);
        }

        // Leaves the open button alone: UICoaching hides it while the greeting shows.
        private void Start()
        {
            _animator.SetBool("Show", false);
        }

        private void Update()
        {
            _deleteSelectedButton.SetActive(!IsOpen && _interactionGroup.focusInteractable != null);
        }
    }
}
