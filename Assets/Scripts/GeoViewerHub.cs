using Compas.Core.Mqtt;
using Compas.Events;
using Compas.Unity.Core.Mqtt;
using Compas.Unity.Visualization;
using Compas.XR.Unity.Events;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Compas.GeoViewerXR
{
    /// <summary>
    /// The one place to reach the app from any script: GeoViewerHub.Instance gives its info, its events and its actions.
    /// </summary>
    // Runs first, so every other script can already use Instance in its own Awake and OnEnable.
    [DefaultExecutionOrder(-100)]
    public class GeoViewerHub : MonoBehaviour
    {
        [SerializeField] private MqttManager mqtt;
        [SerializeField] private GeometryVisualizer visualizer;

        [Header("Connection events")]
        [Tooltip("Connected to the MQTT broker.")]
        public UnityEvent OnConnected = new UnityEvent();
        [Tooltip("Could not connect to the MQTT broker.")]
        public UnityEvent OnConnectionFailed = new UnityEvent();
        [Tooltip("The connection state changed. Read it from ConnectionState.")]
        public UnityEvent OnConnectionStateChanged = new UnityEvent();
        [Tooltip("Subscribed to a topic. Gives the topic.")]
        public UnityEvent<string> OnTopicSubscribed = new UnityEvent<string>();
        [Tooltip("A new topic to listen on was set, connected or not. Gives the topic.")]
        public UnityEvent<string> OnSubscribeTopicChanged = new UnityEvent<string>();

        private int _objectsDrawn;

        public static GeoViewerHub Instance { get; private set; }

        /// <summary>Events about received geometry and its sequence.</summary>
        public GeometryEvents GeometryEvents { get; private set; }
        public GeometrySequence Sequence { get; private set; }
        public PlacedObjects PlacedObjects { get; private set; }

        public MqttConnectionStateUI ConnectionState { get; private set; } = MqttConnectionStateUI.Disconnected;
        public bool IsConnected => mqtt.IsConnected;

        // Two overlapping MqttManager.Connect calls share one client: the second replaces the
        // first's socket and leaves a session that reports connected but receives nothing.
        private bool IsConnecting => ConnectionState == MqttConnectionStateUI.AttemptingConnection;
        public string BrokerAddress => mqtt.brokerAddress;
        public int BrokerPort => mqtt.brokerPort;
        public string SubscribeTopic => mqtt.TopicsToSubscribe.FirstOrDefault();
        public string PublishTopic => mqtt.TopicsToPublish.FirstOrDefault();

        /// <summary>Where the latest data came from: an MQTT topic or a file path.</summary>
        public string LastSource { get; private set; }

        /// <summary>The latest data as sent from Grasshopper.</summary>
        public object LastMessage { get; private set; }

        /// <summary>Every received object in the scene, by path.</summary>
        public IReadOnlyDictionary<string, GameObject> RenderedReceivedObjects => visualizer.RenderedReceivedObjects;

        private int connectionCheckInterval = 5;
        private float lastCheckTime;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError($"{nameof(GeoViewerHub)}: there is already one in the scene, disabling this one.", this);
                enabled = false;
                return;
            }

            Instance = this;

            if (mqtt == null)
            {
                mqtt = FindAnyObjectByType<MqttManager>();
            }

            if (visualizer == null)
            {
                visualizer = FindAnyObjectByType<GeometryVisualizer>();
            }

            GeometryEvents = FindAnyObjectByType<GeometryEvents>();
            if (GeometryEvents == null)
            {
                Debug.LogError($"{nameof(GeoViewerHub)}: add the Geometry Events prefab to the scene.", this);
            }

            Sequence = FindAnyObjectByType<GeometrySequence>();
            PlacedObjects = FindAnyObjectByType<PlacedObjects>();

            if (PlayerPrefs.HasKey(PlayerPrefsKeys.SubscribeTopic))
            {
                mqtt.SetOneSubscribeTopic(PlayerPrefs.GetString(PlayerPrefsKeys.SubscribeTopic));
            }

            if (PlayerPrefs.HasKey(PlayerPrefsKeys.PublishTopic))
            {
                mqtt.SetPublishingTopic(PlayerPrefs.GetString(PlayerPrefsKeys.PublishTopic));
            }
        }

        private void OnEnable()
        {
            mqtt.ConnectionSucceeded += HandleConnectionSucceeded;
            mqtt.ConnectionFailed += HandleConnectionFailed;
            mqtt.TopicSubscribed += HandleTopicSubscribed;
            mqtt.MessageReceived += HandleMessageReceived;
            visualizer.GeometryRendered += HandleGeometryRendered;

            // The QR code package asks for a connection through the event bus.
            EventBus<MqttConnectRequested>.AddListener(HandleConnectRequested);
        }

        // In Start because the visualizer connects its own Message Source in Awake. Left
        // connected, every message would be drawn twice.
        private void Start()
        {
            visualizer.ClearMessagingSource();
        }

        private void Update()
        {
            if (Time.time > lastCheckTime + connectionCheckInterval)
            {
                if (ConnectionState == MqttConnectionStateUI.Connected && !mqtt.IsConnected)
                {
                    SetConnectionState(MqttConnectionStateUI.AttemptingConnection);
                }
                lastCheckTime = Time.time;
            }
        }

        private void OnDisable()
        {
            mqtt.ConnectionSucceeded -= HandleConnectionSucceeded;
            mqtt.ConnectionFailed -= HandleConnectionFailed;
            mqtt.TopicSubscribed -= HandleTopicSubscribed;
            mqtt.MessageReceived -= HandleMessageReceived;
            visualizer.GeometryRendered -= HandleGeometryRendered;

            EventBus<MqttConnectRequested>.RemoveListener(HandleConnectRequested);
        }

#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            // From Connected, a dead link is already being reconnected by MqttManager (see Update).
            bool idle = ConnectionState == MqttConnectionStateUI.Disconnected
                        || ConnectionState == MqttConnectionStateUI.ConnectionFailed;
            if (focus && idle && !mqtt.IsConnected)
            {
                OnConnectionFailed.Invoke();
                SetConnectionState(MqttConnectionStateUI.AttemptingConnection);
                mqtt.Connect();
            }
        }
#endif

        /// <summary>Connects to the broker set on the MqttManager.</summary>
        public void Connect()
        {
            ConnectTo(null);
        }

        /// <summary>Connects to an address like mqtt://host:1883?topic=my/topic.</summary>
        public void ConnectTo(string connectPath)
        {
            bool noPath = string.IsNullOrWhiteSpace(connectPath);
            if (mqtt.IsConnected && noPath)
            {
                HandleConnectionSucceeded();
                return;
            }

            if (IsConnecting && noPath)
            {
                return;
            }

            // Connected with no live link: the link dropped and MqttManager is already
            // reconnecting by itself.
            if (ConnectionState == MqttConnectionStateUI.Connected && noPath)
            {
                SetConnectionState(MqttConnectionStateUI.AttemptingConnection);
                return;
            }

            MqttConnectionStateUI previousState = ConnectionState;
            SetConnectionState(MqttConnectionStateUI.AttemptingConnection);

            if (noPath)
            {
                mqtt.Connect();
                return;
            }

            bool wasConnected = mqtt.IsConnected;
            string brokerBefore = $"{mqtt.brokerAddress}:{mqtt.brokerPort}";
            if (!mqtt.ConnectWith(connectPath))
            {
                // Unreadable path: nothing was changed or started.
                SetConnectionState(previousState);
                return;
            }

            // On the broker already connected, ConnectWith only adds the topic, so no
            // ConnectionSucceeded follows.
            bool sameBroker = brokerBefore == $"{mqtt.brokerAddress}:{mqtt.brokerPort}";
            if (wasConnected && sameBroker)
            {
                HandleConnectionSucceeded();
            }
        }

        public void SetSubscribeTopic(string topic)
        {
            mqtt.SetOneSubscribeTopic(topic);
            SaveTopic(PlayerPrefsKeys.SubscribeTopic, topic);
            OnSubscribeTopicChanged.Invoke(topic);
        }

        public void SetPublishTopic(string topic)
        {
            mqtt.SetPublishingTopic(topic);
            SaveTopic(PlayerPrefsKeys.PublishTopic, topic);
        }

        /// <summary>Sends a message on the publish topic.</summary>
        public void Publish(object message)
        {
            mqtt.Publish(message);
        }

        /// <summary>Opens the camera to scan an MQTT QR code.</summary>
        public void StartQRScan()
        {
            EventBus<StartQRScanningRequested>.Raise(new StartQRScanningRequested());
        }

        /// <summary>Takes in data, from MQTT or anywhere else, and draws it.</summary>
        public void Receive(string source, object data)
        {
            LastSource = source;
            LastMessage = data;
            GeometryEvents.OnMessageReceived.Invoke();

            _objectsDrawn = 0;
            visualizer.RenderData(source, data);

            if (_objectsDrawn > 0)
            {
                GeometryEvents.OnGeometryRendered.Invoke();
            }
        }

        /// <summary>Deletes received and placed geometry.</summary>
        public void DeleteAllGeometry()
        {
            visualizer.ClearRenderedObjects();
            GeometryEvents.OnGeometryDeleted.Invoke();
        }

        /// <summary>The parent everything from a source is drawn under, to move it as a whole.</summary>
        public bool TryGetGeometryRoot(string source, out Transform root)
        {
            return visualizer.TryGetParentTransform(source, out root);
        }

        private void HandleConnectRequested(MqttConnectRequested request)
        {
            ConnectTo(request.connectPath);
        }

        private void HandleConnectionSucceeded()
        {
            SetConnectionState(MqttConnectionStateUI.Connected);
            OnConnected.Invoke();
        }

        private void HandleConnectionFailed()
        {
            SetConnectionState(MqttConnectionStateUI.ConnectionFailed);
            OnConnectionFailed.Invoke();
        }

        private void HandleTopicSubscribed(string topic)
        {
            OnTopicSubscribed.Invoke(topic);
        }

        private void HandleMessageReceived(MqttIncomingMessage message)
        {
            Receive(message.Topic, message.Decoded);
        }

        private void HandleGeometryRendered(string path, GameObject drawn, bool isNew)
        {
            _objectsDrawn++;
            GeometryEvents.OnObjectDrawn.Invoke(path, drawn, isNew);
        }

        private void SetConnectionState(MqttConnectionStateUI state)
        {
            if (state == ConnectionState)
            {
                return;
            }

            ConnectionState = state;
            OnConnectionStateChanged.Invoke();
        }

        private static void SaveTopic(string key, string topic)
        {
            PlayerPrefs.SetString(key, topic);
            PlayerPrefs.Save();
        }
    }
}
