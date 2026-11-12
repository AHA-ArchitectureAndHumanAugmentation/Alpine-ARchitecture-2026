using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// Puts received geometry in the order it was clicked in Grasshopper and shows it step by step.
    /// </summary>
    public class GeometrySequence : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField]
        [Tooltip("The current step.")]
        private Material _highlightMaterial;
        [SerializeField]
        [Tooltip("Steps after the current one. Leave empty to hide them instead.")]
        private Material _ghostMaterial;
        [SerializeField]
        [Tooltip("Steps before the current one. Leave empty to keep their own material (e.g. from the GeometryBeautifier).")]
        private Material _builtMaterial;
        [SerializeField]
        [Tooltip("Hide steps after the current one even when a ghost material is set.")]
        private bool _hideFuture;

        [Header("Showing the order")]
        [SerializeField]
        [Tooltip("Write the received order to the Console.")]
        private bool _logOrder = true;
        [SerializeField]
        [Tooltip("Rename objects to \"#03 boxes[1]\" and sort them by order in the Hierarchy.")]
        private bool _orderHierarchy = true;
        [SerializeField]
        [Tooltip("Draw the order number on each object in the Scene view (editor only).")]
        private bool _sceneLabels = true;
        [SerializeField] private Color _labelColor = Color.white;

        [Header("Received (read only)")]
        [SerializeField]
        [Tooltip("Filled when a message arrives. Index = clicked order in Grasshopper.")]
        private List<GameObject> _orderedElements = new List<GameObject>();

        [Header("Events")]
        [Tooltip("A new sequence is ready. Gives its number of steps.")]
        public UnityEvent<int> OnSequenceReady = new UnityEvent<int>();
        [Tooltip("A step is now shown. Gives the step number, the first one is 0.")]
        public UnityEvent<int> OnStepShown = new UnityEvent<int>();

        private readonly List<string> _orderedPaths = new List<string>();
        // the material each object had before the sequence touched it
        private readonly Dictionary<GameObject, Material> _originalMaterials = new Dictionary<GameObject, Material>();

        private int _currentStep = -1;

        public IReadOnlyList<GameObject> OrderedElements => _orderedElements;

        private void OnEnable()
        {
            GeoViewerHub.Instance.GeometryEvents.OnGeometryRendered.AddListener(OnGeometryDrawn);
            GeoViewerHub.Instance.GeometryEvents.OnGeometryDeleted.AddListener(OnGeometryDeleted);
        }

        private void OnDisable()
        {
            GeoViewerHub.Instance.GeometryEvents.OnGeometryRendered.RemoveListener(OnGeometryDrawn);
            GeoViewerHub.Instance.GeometryEvents.OnGeometryDeleted.RemoveListener(OnGeometryDeleted);
        }

        /// <summary>Shows one step: earlier steps are built, later ones ghosted or hidden.</summary>
        public void ShowStep(int step)
        {
            _currentStep = Mathf.Clamp(step, 0, Mathf.Max(0, _orderedElements.Count - 1));
            ApplyStep(_currentStep);
            OnStepShown.Invoke(_currentStep);
        }

        public void ShowNextStep()
        {
            ShowStep(_currentStep + 1);
        }

        public void ShowPreviousStep()
        {
            ShowStep(_currentStep - 1);
        }

        private void OnGeometryDrawn()
        {
            GeoViewerHub hub = GeoViewerHub.Instance;
            if (!TryGetOrder(hub.LastMessage, out List<string> order))
            {
                return;
            }

            ResolveOrder(hub.LastSource, order);

            if (_orderHierarchy)
            {
                ApplyHierarchyOrder();
            }

            _currentStep = -1;
            OnSequenceReady.Invoke(_orderedElements.Count);
        }

        private void OnGeometryDeleted()
        {
            _orderedElements.Clear();
            _orderedPaths.Clear();
            _originalMaterials.Clear();

            _currentStep = -1;
            OnSequenceReady.Invoke(0);
        }

        private static bool TryGetOrder(object data, out List<string> order)
        {
            order = null;
            if (!(data is IDictionary dictionary)
                || !dictionary.Contains("order")
                || !(dictionary["order"] is IList entries))
            {
                return false;
            }

            order = new List<string>(entries.Count);
            foreach (object entry in entries)
            {
                order.Add(entry?.ToString());
            }
            return true;
        }

        private void ResolveOrder(string sourceKey, IReadOnlyList<string> order)
        {
            _orderedElements.Clear();
            _orderedPaths.Clear();

            var drawn = GeoViewerHub.Instance.RenderedReceivedObjects;
            var log = new StringBuilder();

            foreach (string entry in order)
            {
                if (entry != null
                    && drawn.TryGetValue($"{sourceKey}/{entry}", out GameObject go)
                    && go != null)
                {
                    log.AppendLine($"  {_orderedElements.Count,3}  {entry}");
                    _orderedElements.Add(go);
                    _orderedPaths.Add(entry);
                    if (!_originalMaterials.ContainsKey(go))
                    {
                        var r = go.GetComponentInChildren<Renderer>();
                        _originalMaterials[go] = r != null ? r.sharedMaterial : null;
                    }
                }
                else
                {
                    log.AppendLine($"  ---  {entry}  (not drawn)");
                }
            }

            if (_logOrder)
            {
                Debug.Log($"{nameof(GeometrySequence)}: order of {_orderedElements.Count} objects from '{sourceKey}':\n{log}", this);
            }
        }

        // "#03 boxes[1]", and siblings sorted so the Hierarchy reads in clicked order.
        private void ApplyHierarchyOrder()
        {
            for (int i = 0; i < _orderedElements.Count; i++)
            {
                GameObject go = _orderedElements[i];
                go.name = $"#{i:00} {_orderedPaths[i]}";
                go.transform.SetSiblingIndex(i);
            }
        }

        private void ApplyStep(int current)
        {
            for (int i = 0; i < _orderedElements.Count; i++)
            {
                GameObject go = _orderedElements[i];
                if (go == null)
                {
                    continue;
                }

                bool future = i > current;
                if (future && (_hideFuture || _ghostMaterial == null))
                {
                    go.SetActive(false);
                    continue;
                }
                go.SetActive(true);

                Material material = MaterialForStep(go, i, current);
                if (material == null)
                {
                    continue;
                }

                // sharedMaterial, not material! otherwise it clones the mat asset per object
                foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        private Material MaterialForStep(GameObject go, int index, int current)
        {
            if (index == current)
            {
                return _highlightMaterial;
            }

            if (index > current)
            {
                return _ghostMaterial;
            }

            if (_builtMaterial != null)
            {
                return _builtMaterial;
            }

            return _originalMaterials.TryGetValue(go, out Material original) ? original : null;
        }

#if UNITY_EDITOR
        // Order numbers in the Scene view, like the numbers in the Rhino viewport.
        private void OnDrawGizmos()
        {
            if (!_sceneLabels || _orderedElements == null)
            {
                return;
            }

            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = _labelColor;

            for (int i = 0; i < _orderedElements.Count; i++)
            {
                GameObject go = _orderedElements[i];
                if (go == null || !go.activeInHierarchy)
                {
                    continue;
                }

                var r = go.GetComponentInChildren<Renderer>();
                Vector3 position = r != null ? r.bounds.center : go.transform.position;
                Handles.Label(position, i.ToString(), style);
            }
        }
#endif
    }
}
