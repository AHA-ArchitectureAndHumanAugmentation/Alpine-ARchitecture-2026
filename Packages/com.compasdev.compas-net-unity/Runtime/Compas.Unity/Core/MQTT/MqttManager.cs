using Compas.Core.Extensions;
using Compas.Mqtt;
using M2MqttUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Compas.Unity.Core.Mqtt
{
    /// <summary>
    /// MQTT Quality of Service levels for message delivery.
    /// </summary>
    public enum QoS
    {
        /// <summary>Fire and forget. No delivery guarantee.</summary>
        BestEffort = 0,
        /// <summary>Message delivered at least once. May duplicate.</summary>
        Reliable = 1,
        /// <summary>Message delivered exactly once. Slowest but most reliable.</summary>
        Guaranteed = 2
    }

    /// <summary>
    /// Manages MQTT broker communication: connects, keeps a set of subscribed topics, decodes
    /// incoming payloads and raises them as <see cref="MessageReceived"/>.
    ///
    /// <para>Topics are declarative. <see cref="TopicsToSubscribe"/> is the set you want subscribed;
    /// the component subscribes them on every successful connection, so a dropped and
    /// re-established link restores the same subscriptions without the caller doing anything.
    /// Broker address, port and <c>autoConnect</c> come from the base component's Inspector
    /// fields.</para>
    ///
    /// <para>Where the connection details come from is deliberately not this component's
    /// business. Configure them in the Inspector, or hand it a
    /// <c>mqtt://host[:port][?topic=...]</c> URI at runtime via <see cref="ConnectWith(string)"/>
    /// - from a scanned QR code, a UI field, saved settings or anything else.</para>
    /// </summary>
    public class MqttManager : M2MqttUnityClient, IMqttTransferSettings
    {
        [field: SerializeField] public QoS QualityOfService { get; set; } = QoS.BestEffort;

        [field: SerializeField] public SerializationProtocol Protocol { get; set; } = SerializationProtocol.ProtoBuf;

        [SerializeField]
        [Tooltip("Topics to subscribe to (i.e. incoming data)")]
        private List<string> topicsToSubscribe = new();

        [SerializeField]
        [Tooltip("Topic(s) to publish to (i.e. outgoing data)")]
        private List<string> topicsToPublish = new();
        private object _decodedMessage;
        private FixedSizeQueue<string> _eventMessages = new FixedSizeQueue<string>(50);
        private Dictionary<string, MqttTopic> _subscribedTopics = new();
        protected string Msg;


        /// <summary>Provides access to the decoded message object. (Deserialized Protobuf or JSON object).</summary>
        public object DecodedMessage => _decodedMessage;

        /// <summary>Raised once per received message, carrying the topic and decoded payload.</summary>
        public event Action<MqttIncomingMessage> MessageReceived;
        public event Action<string> TopicSubscribed;

        /// <summary>True when the underlying client exists and holds a live connection.</summary>
        public bool IsConnected => client != null && client.IsConnected;

        public IReadOnlyList<string> TopicsToSubscribe => topicsToSubscribe;
        public IReadOnlyList<string> TopicsToPublish => topicsToPublish;

        /// <summary>
        /// Configures the MQTT client with the broker address and port.
        /// </summary>
        /// <param name="brokerAddress">The broker address to connect to.</param>
        /// <param name="brokerPort">The broker port to connect to.</param>
        public void ConfigureClient(string brokerAddress, int brokerPort)
        {
            this.brokerAddress = brokerAddress;
            this.brokerPort = brokerPort > 0 ? brokerPort : MqttUri.DefaultPort;
        }

        public void ConfigureClient(MqttUriInfo connectionInfo)
        {
            ConfigureClient(connectionInfo.BrokerAddress, connectionInfo.BrokerPort);
        }

        /// <summary>
        /// Points the client at the broker and topic described by an
        /// <c>mqtt://host[:port][?topic=...]</c> URI, then connects. Malformed input is reported
        /// and ignored rather than thrown, so arbitrary scanned text is safe to pass in.
        /// </summary>
        /// <returns>False if the URI could not be parsed.</returns>
        public bool ConnectWith(string uri)
        {
            if (!MqttUri.TryParse(uri, out MqttUriInfo mqttUri, out string error))
            {
                Debug.LogError($"MQTT: ConnectWith: {error}", this);
                return false;
            }

            ConnectWith(mqttUri);
            return true;
        }

        /// <inheritdoc cref="ConnectWith(string)"/>
        public void ConnectWith(MqttUriInfo connectionInfo)
        {
            // Register topic for subscribing afterwards
            if (!string.IsNullOrWhiteSpace(connectionInfo.Topic)
                && !topicsToSubscribe.Contains(connectionInfo.Topic))
            {
                topicsToSubscribe.Add(connectionInfo.Topic);
            }

            bool brokerChanged = brokerAddress != connectionInfo.BrokerAddress
                                || brokerPort != connectionInfo.BrokerPort;

            ConfigureClient(connectionInfo.BrokerAddress, connectionInfo.BrokerPort);
            if (brokerChanged && IsConnected)
            {
                // Connect() is a no-op while a connection is live, so a move to a different
                // broker has to tear the old one down first.
                StartCoroutine(ReconnectRoutine());
                return;
            }

            Connect();
        }

        /// <summary>
        /// Adds a topic to <see cref="TopicsToSubscribe"/>. Subscribes immediately when connected;
        /// otherwise the topic is subscribed on the next successful connection. Adding a topic
        /// that is already present does nothing.
        /// </summary>
        /// <param name="topicToSubscribe">The topic to subscribe to.</param>
        public void SubscribeToTopic(string topicToSubscribe)
        {
            if (string.IsNullOrWhiteSpace(topicToSubscribe))
            {
                Debug.LogError("MQTT: SubscribeToTopic: Topic is null or empty.", this);
                return;
            }

            if (!topicsToSubscribe.Contains(topicToSubscribe))
            {
                topicsToSubscribe.Add(topicToSubscribe);
            }

            if (_subscribedTopics.ContainsKey(topicToSubscribe))
            {
                return;
            }

            if (!IsConnected)
            {
                // Not an error: SubscribeTopics() applies the whole set once connected.
                return;
            }

            var topic = new MqttTopic(topicToSubscribe, this, client);
            if (topic.TrySubscribe())
            {
                _subscribedTopics.Add(topicToSubscribe, topic);
                TopicSubscribed?.Invoke(topicToSubscribe);

                Debug.Log("MQTT: SubscribeToTopic: Subscribed to topic: " + topicToSubscribe, this);
            }
            else
            {
                Debug.LogError("MQTT: SubscribeToTopic: Couldn't subscribe to: " + topicToSubscribe, this);
            }
        }

        /// <summary>
        /// Removes a topic from <see cref="TopicsToSubscribe"/> and drops its live subscription, so it is
        /// not restored on the next connection either.
        /// </summary>
        /// <param name="topicToUnsubscribe">The topic to unsubscribe from.</param>
        public void UnsubscribeFromTopic(string topicToUnsubscribe)
        {
            topicsToSubscribe.Remove(topicToUnsubscribe);

            if (_subscribedTopics.TryGetValue(topicToUnsubscribe, out var topic))
            {
                topic.TryUnsubscribe();
                _subscribedTopics.Remove(topicToUnsubscribe);
                Debug.Log("MQTT: UnsubscribeFromTopic: Unsubscribed from topic: " + topicToUnsubscribe, this);
            }
            else
            {
                Debug.LogWarning($"MQTT: UnsubscribeFromTopic: Topic `{topicToUnsubscribe}` not subscribed to yet", this);
            }
        }

        /// <summary>
        /// Publishes a message to a topic, encoding it with <see cref="MqttMessageSerializer"/>
        /// using the current <see cref="Protocol"/> - the mirror of the receive path, so send and
        /// receive stay in the same format. Pass any object the converter understands (a geometry
        /// type, a Dictionary, a primitive).
        /// 
        /// Note that we need 2 different topics 
        /// </summary>
        /// <param name="topicToPublish">The topic to publish to.</param>
        /// <param name="message">The message object to encode and publish.</param>
        public void PublishToTopic(string topicToPublish, object message)
        {
            var topic = new MqttTopic(topicToPublish, this, client);

            if (!IsConnected)
            {
                Debug.LogError("MQTT: Publish: no client is connected");
                return;
            }

            if (topic.TryPublish(message))
            {
                Debug.Log($"MQTT: PublishToTopic: Published to topic: {topicToPublish}", this);
            }
            else
            {
                Debug.LogError("MQTT: Publish: Client is null or not connected. Cannot publish message.", this);
            }
        }

        public void SetPublishingTopic(string publishingTopic)
        {
            // We keep a list of just 1 publishing topic
            topicsToPublish.Clear();
            topicsToPublish.Add(publishingTopic);
        }

        public void SetOneSubscribeTopic(string topicToSubscribe)
        {
            // Don't do it if it's the same topic
            if (_subscribedTopics.ContainsKey(topicToSubscribe))
            {
                Debug.Log("Already subscribed to this topic!");
                return;
            }

            UnsubscribeTopics();
            topicsToSubscribe.Clear();
            topicsToSubscribe.Add(topicToSubscribe);
            SubscribeToTopic(topicToSubscribe);
        }

        public void Publish(object message)
        {
            string topicToPublish = topicsToPublish.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(topicToPublish))
            {
                Debug.LogError("TODO: Ask user for topic to publish to");
                return;
            }

            PublishToTopic(topicToPublish, message);
        }

        /// <summary>
        /// Decodes the message received from the MQTT broker using the configured serialization protocol.
        /// Stores both the deserialized object and a string representation.
        /// </summary>
        /// <param name="topic">The topic the message was received from.</param>
        /// <param name="message">The message bytes to decode.</param>
        protected override void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                (_decodedMessage, Msg) = MqttMessageSerializer.Decode(Protocol, message);
                Debug.Log($"MQTT: DecodeMessage: Received {Protocol} message from topic: {topic}");
                MessageReceived?.Invoke(new MqttIncomingMessage(topic, _decodedMessage));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"MQTT: DecodeMessage: Failed to decode message from topic {topic}: {ex.Message}");
                _decodedMessage = null;
                Msg = $"[decode_error] {ex.Message}";
            }
            finally
            {
                StoreMessage(Msg);
            }
        }

        /// <summary>
        /// Called by the base component after every successful connection. Live subscriptions
        /// are rebuilt from scratch because each <see cref="MqttTopic"/> holds the client that
        /// existed when it was created, and the base component replaces that on reconnect.
        /// </summary>
        protected override void SubscribeTopics()
        {
            _subscribedTopics.Clear();

            foreach (string topic in topicsToSubscribe)
            {
                SubscribeToTopic(topic);
            }
        }

        /// <summary>
        /// Called by the base component when the connection is closed. Drops the live
        /// subscriptions but keeps <see cref="TopicsToSubscribe"/>, so reconnecting restores them.
        /// </summary>
        protected override void UnsubscribeTopics()
        {
            foreach (var topic in new List<MqttTopic>(_subscribedTopics.Values))
            {
                topic.TryUnsubscribe();
            }

            _subscribedTopics.Clear();
        }

        private IEnumerator ReconnectRoutine()
        {
            Disconnect();

            while (IsConnected)
            {
                yield return null;
            }

            Connect();
        }

        private void StoreMessage(string eventMsg)
        {
            /*
            * Method is used to store the last 50 messages received from the MQTT broker.
            */
            _eventMessages.Enqueue(eventMsg);
        }

        protected override void OnConnectionLost()
        {
            base.OnConnectionLost();
            Connect();
        }
    }
}
