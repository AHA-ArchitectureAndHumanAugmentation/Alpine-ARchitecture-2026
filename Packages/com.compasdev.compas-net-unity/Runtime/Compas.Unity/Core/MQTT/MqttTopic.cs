using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Compas.Unity.Core.Mqtt
{
    public class MqttTopic
    {
        public string Topic { get; private set; }
        private IMqttTransferSettings _settings { get; set; }

        private MqttClient _client { get; set; }

        public MqttTopic(string topic, IMqttTransferSettings transferSettings, MqttClient client)
        {
            Topic = topic;
            _settings = transferSettings;
            _client = client;
        }

        public bool HasConnectedClient => _client != null && _client.IsConnected;

        /// <summary>
        /// Subscribes to the MQTT topic.
        /// </summary>
        public bool TrySubscribe()
        {
            if (_client == null)
            {
                return false;
            }

            var messageId = _client.Subscribe(new string[] { Topic }, new byte[] { ToBackendQoS(_settings.QualityOfService) });
            return true;
        }

        /// <summary>
        /// Unsubscribes from the MQTT topic.
        /// </summary>
        public bool TryUnsubscribe()
        {
            if (_client == null)
            {
                return false;
            }

            var messageId = _client.Unsubscribe(new string[] { Topic });
            return true;
        }

        /// <summary>
        /// Publishes a message to a topic, encoding it with <see cref="MqttMessageSerializer"/>
        /// using the current <see cref="Protocol"/> - the mirror of the receive path, so send and
        /// receive stay in the same format. Pass any object the converter understands (a geometry
        /// type, a Dictionary, a primitive).
        /// </summary>
        /// <param name="message">The message object to encode and publish.</param>
        /// 
        public bool TryPublish(object message)
        {
            if (!HasConnectedClient)
                return false;

            byte[] payload = MqttMessageSerializer.Encode(_settings.Protocol, message);
            var messageId = _client.Publish(Topic, payload, ToBackendQoS(_settings.QualityOfService), false);
            return true;
        }

        private static byte ToBackendQoS(QoS qos)
        {
            switch (qos)
            {
                case QoS.BestEffort:
                    return MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
                case QoS.Reliable:
                    return MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
                case QoS.Guaranteed:
                    return MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
                default:
                    return MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
            }
        }
    }
}