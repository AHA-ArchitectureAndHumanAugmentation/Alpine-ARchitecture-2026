using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Keeps track of every shape placed from the object menu, for sending to Rhino, moving and deleting.
    /// </summary>
    public class PlacedObjects : MonoBehaviour
    {
        [SerializeField] private bool _interactableByDefault;

        [Tooltip("Placed objects became movable or fixed. Gives true when movable.")]
        public UnityEvent<bool> OnInteractableChanged = new UnityEvent<bool>();

        private readonly List<GeometryLogic> _objects = new List<GeometryLogic>();
        private ObjectSpawner _objectSpawner;
        private ARInteractorSpawnTrigger _tapToPlace;

        public IReadOnlyList<GeometryLogic> Objects => _objects;
        public bool IsInteractable { get; private set; }

        /// <summary>True when the next tap on a surface will place the shape picked in the menu.</summary>
        public bool IsWaitingForTap => _tapToPlace != null && _tapToPlace.enabled;

        private void Awake()
        {
            GeoViewerHub.Instance.GeometryEvents.OnGeometryDeleted.AddListener(DeleteAll);

            _objectSpawner = FindAnyObjectByType<ObjectSpawner>();
            if (_objectSpawner != null)
            {
                _tapToPlace = _objectSpawner.GetComponent<ARInteractorSpawnTrigger>();
                _objectSpawner.objectSpawned += OnObjectPlaced;
            }
        }

        private void OnDestroy()
        {
            GeoViewerHub.Instance.GeometryEvents.OnGeometryDeleted.RemoveListener(DeleteAll);

            if (_objectSpawner != null)
            {
                _objectSpawner.objectSpawned -= OnObjectPlaced;
            }
        }

        private void Start()
        {
            int savedValue = PlayerPrefs.GetInt(PlayerPrefsKeys.InteractableGeometry, _interactableByDefault ? 1 : 0);
            SetInteractable(savedValue == 1);
        }

        /// <summary>The next tap on a surface places this shape from the Object Spawner's list.</summary>
        public void PlaceOnNextTap(int shapeIndex)
        {
            _objectSpawner.spawnOptionIndex = shapeIndex;

            // Slightly later, so the tap on the menu button itself does not place anything.
            Invoke(nameof(StartListeningForTap), 0.1f);
        }

        private void StartListeningForTap()
        {
            _tapToPlace.enabled = true;
        }

        private void OnObjectPlaced(GameObject placed)
        {
            _tapToPlace.enabled = false;

            GeometryLogic logic = placed.AddComponent<GeometryLogic>();
            logic.SetInteractable(IsInteractable);
            _objects.Add(logic);
        }

        public void ToggleInteractable()
        {
            SetInteractable(!IsInteractable);
            PlayerPrefs.SetInt(PlayerPrefsKeys.InteractableGeometry, IsInteractable ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            foreach (GeometryLogic placedObject in _objects)
            {
                if (placedObject != null)
                {
                    placedObject.SetInteractable(IsInteractable);
                }
            }

            OnInteractableChanged.Invoke(IsInteractable);
        }

        private void DeleteAll()
        {
            foreach (GeometryLogic placedObject in _objects)
            {
                if (placedObject != null)
                {
                    placedObject.Delete();
                }
            }
            _objects.Clear();
        }
    }
}
