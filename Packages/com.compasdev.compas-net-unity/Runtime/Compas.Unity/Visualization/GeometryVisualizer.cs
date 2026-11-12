using Compas.Core.Transformations;
using Compas.Events;
using Compas.Unity.Core.Mqtt;
using Compas.Unity.Visualization.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Compas.Unity.Visualization
{
    /// <summary>
    /// Draws the geometry arriving on an <see cref="MqttManager"/>. 
    ///
    /// The data is not necessarily one shape: the publisher can send a single geometry, a list,
    /// or a Dictionary{string, object} mixing named geometry with primitives. This walks whatever
    /// shape arrives, renders every geometry it finds, and ignores the rest.
    ///
    /// Repeat messages update in place. Each item gets a stable path built from its stopic and its
    /// position in the data; the GameObject spawned for that path is reused next time the same
    /// path appears, so a publisher streaming updates moves existing objects instead of piling up
    /// duplicates in the scene.
    ///
    /// Everything from one source (a topic, a file) is grouped under its own parent transform, so
    /// an app can move, hide or place a whole model at once. The pieces keep their source
    /// coordinates relative to that parent: moving the parent moves the model, and later updates
    /// from the same source land in the moved frame instead of snapping back.
    /// </summary>
    public class GeometryVisualizer : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Manager whose decoded messages get drawn.")]
        [SerializeField] private MqttManager messageSource;

        [Header("Geometry")]
        [Tooltip("Coordinate system the incoming geometry was authored in.")]
        public SupportedSystem sourceSystem = SupportedSystem.RhinoZUp;

        [Tooltip("Where the per-source parent transforms are created. Leave empty to create them under this component.")]
        public Transform spawnParent;

        [Header("Logging")]
        [Tooltip("Log how many geometries each message produced.")]
        public bool logRenderedGeometry = true;

        private MqttManager subscribedSource;
        private readonly Dictionary<string, GameObject> renderedObjects = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Transform> parentTransforms = new Dictionary<string, Transform>();

        /// <summary>Received objects currently in the scene, keyed by data path.</summary>
        public IReadOnlyDictionary<string, GameObject> RenderedReceivedObjects => renderedObjects;

        /// <summary>
        /// The parent transform everything from <paramref name="topicSource"/> is drawn under.
        /// False until that source has rendered at least one geometry.
        /// </summary>
        public bool TryGetParentTransform(string topicSource, out Transform root)
        {
            return parentTransforms.TryGetValue(topicSource, out root) && root != null;
        }

        /// <summary>
        /// Raised after an item is rendered to allow per-app customization.
        /// </summary>
        public event Action<string, GameObject, bool> GeometryRendered;

        /// <summary>
        /// Raised once per <see cref="RenderData"/> call after all of its geometry is drawn,
        /// whatever delivered it (MQTT or a Resources file), with the source key and the decoded
        /// data. Lets apps read non-geometry keys from the data.
        /// </summary>
        public event Action<string, object> DataRendered;

        private void Awake()
        {
            if (spawnParent == null)
            {
                spawnParent = transform;
            }

            if (messageSource != null)
            {
                SetMessagingSource(messageSource);
            }

            EventBus<RemoveGeometryRequested>.AddListener(OnRemoveGeometryRequested);
        }

        private void OnDestroy()
        {
            // The bus holds a static delegate, so a listener left behind outlives this
            // component and keeps a destroyed object reachable.
            EventBus<RemoveGeometryRequested>.RemoveListener(OnRemoveGeometryRequested);
            ClearMessagingSource();
        }

        /// <summary>
        /// Starts drawing messages from <paramref name="newSource"/>, replacing any current
        /// binding. This is the seam the Inspector field is only a convenience for - a test or a
        /// different transport can bind its own source here.
        /// </summary>
        public void SetMessagingSource(MqttManager newSource)
        {
            if (newSource == null)
            {
                return;
            }

            ClearMessagingSource();
            subscribedSource = newSource;
            subscribedSource.MessageReceived += OnMessageReceived;
        }

        /// <summary>Stops drawing. Spawned objects are left in the scene.</summary>
        public void ClearMessagingSource()
        {
            if (subscribedSource == null)
            {
                return;
            }

            subscribedSource.MessageReceived -= OnMessageReceived;
            subscribedSource = null;
        }

        /// <summary>
        /// Acts only on requests that cover received geometry. Locally spawned objects belong
        /// to whoever spawned them, so this ignores those scopes.
        /// </summary>
        private void OnRemoveGeometryRequested(RemoveGeometryRequested requested)
        {
            if (requested.Includes(GeometryScope.Incoming))
            {
                ClearRenderedObjects();
            }
        }

        /// <summary>Destroys every spawned object and its parent transform, so the next message starts clean.</summary>
        public void ClearRenderedObjects()
        {
            // Objects as well as parents: something may have reparented a piece out of its parent.
            foreach (KeyValuePair<string, GameObject> entry in renderedObjects)
            {
                if (entry.Value != null)
                {
                    Destroy(entry.Value);
                }
            }
            renderedObjects.Clear();

            foreach (KeyValuePair<string, Transform> entry in parentTransforms)
            {
                if (entry.Value != null)
                {
                    Destroy(entry.Value.gameObject);
                }
            }
            parentTransforms.Clear();
        }

        private void OnMessageReceived(MqttIncomingMessage message)
        {
            RenderData(message.Topic, message.Decoded);
        }

        /// <summary>
        /// Draws decoded data, whatever delivered it. <paramref name="topicSource"/> identifies
        /// where it came from (an MQTT topic, a Resources path) and prefixes the reuse keys, so
        /// repeat data from the same source updates its objects in place.
        /// </summary>
        public void RenderData(string topicSource, object data)
        {
            // ToGameObject() writes world-space poses, so the parent transform has to sit at the
            // origin while drawing for those poses to become its local ones. Its placement is
            // restored after, carrying the updated pieces along with it.
            bool hasRoot = TryGetParentTransform(topicSource, out Transform root);
            Vector3 rootPosition = default;
            Quaternion rootRotation = default;
            if (hasRoot)
            {
                root.GetPositionAndRotation(out rootPosition, out rootRotation);
                root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            int count;
            try
            {
                count = Render(topicSource, topicSource, data);
            }
            finally
            {
                if (hasRoot)
                {
                    root.SetPositionAndRotation(rootPosition, rootRotation);
                }
            }

            if (logRenderedGeometry)
            {
                Debug.Log($"GeometryVisualizer: rendered {count} geometries from '{topicSource}'.");
            }

            DataRendered?.Invoke(topicSource, data);
        }

        /// <summary>
        /// Walks the data and renders every geometry in it, returning how many were drawn.
        /// Recursive, so geometry nested in a list inside a dictionary is still found; the
        /// <paramref name="path"/> accumulates as it descends and becomes the reuse key.
        /// </summary>
        private int Render(string topicSource, string path, object data)
        {
            if (data == null)
            {
                return 0;
            }

            // Geometry first: a check for the drawable case before the container cases, since
            // some geometry types are themselves enumerable.
            if (UnityGeometryFactory.IsGeometry(data))
            {
                return RenderGeometry(topicSource, path, data) ? 1 : 0;
            }

            if (data is IDictionary dictionary)
            {
                int count = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    count += Render(topicSource, $"{path}/{entry.Key}", entry.Value);
                }
                return count;
            }

            if (data is IList list)
            {
                int count = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    count += Render(topicSource, $"{path}[{i}]", list[i]);
                }
                return count;
            }

            return 0;
        }

        /// <summary>
        /// Renders one geometry item, reusing the GameObject previously spawned for this path.
        /// Returns false if the item turned out not to be drawable after all.
        /// </summary>
        private bool RenderGeometry(string topicSource, string path, object item)
        {
            IUnityGeometry visual = UnityGeometryFactory.Create(item, sourceSystem);
            if (visual == null)
            {
                return false;
            }

            // Unity's null semantics: an entry whose object was destroyed compares equal to null,
            // so a stale key spawns a fresh object rather than resurrecting a dead one.
            bool isNew = true;
            if (renderedObjects.TryGetValue(path, out GameObject existing) && existing != null)
            {
                visual.CachedGameObject = existing;
                isNew = false;
            }

            GameObject geometry = visual.ToGameObject();
            if (geometry == null)
            {
                return false;
            }

            // ToGameObject() positions in world space, so keep that when parenting.
            Transform root = GetOrCreateParentTransform(topicSource);
            if (geometry.transform.parent != root)
            {
                geometry.transform.SetParent(root, worldPositionStays: true);
            }

            geometry.name = path.Replace('/', '.');
            renderedObjects[path] = geometry;

            GeometryRendered?.Invoke(path, geometry, isNew);
            return true;
        }

        /// <summary>
        /// Created lazily on the first geometry, so a source that sends only non-geometry data
        /// leaves no empty parent behind. A new parent starts at the world origin, which is what
        /// <see cref="RenderData"/> needs while drawing.
        /// </summary>
        private Transform GetOrCreateParentTransform(string topicSource)
        {
            if (TryGetParentTransform(topicSource, out Transform root))
            {
                return root;
            }

            root = new GameObject(topicSource.Replace('/', '.')).transform;
            root.SetParent(spawnParent, worldPositionStays: false);
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            parentTransforms[topicSource] = root;
            return root;
        }
    }
}
