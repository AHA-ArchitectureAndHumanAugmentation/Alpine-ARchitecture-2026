using System;
using System.Text;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Compas.Core.Data;
using CompasPb;
using CompasPb.Data;

namespace Compas.Mqtt
{
    /// <summary>
    /// Wrapper for MqttClient that provides string-based APIs and a foundation for future features
    /// Eliminates the need to manually convert strings to byte arrays
    /// </summary>
    public class MqttClientWrapper : MqttClient
    {
        private static readonly ICompasPbSerializer _serializer = new CompasPbSerializer();

        /// <summary>
        /// Constructor - connects to MQTT broker on default port (1883)
        /// </summary>
        public MqttClientWrapper(string brokerHostName)
            : base(brokerHostName)
        {
        }

        /// <summary>
        /// Constructor with custom port and security settings
        /// </summary>
        public MqttClientWrapper(string brokerHostName, int brokerPort, bool secure,
            System.Security.Cryptography.X509Certificates.X509Certificate caCert,
            System.Security.Cryptography.X509Certificates.X509Certificate clientCert,
            MqttSslProtocols sslProtocol)
            : base(brokerHostName, brokerPort, secure, caCert, clientCert, sslProtocol)
        {
        }

        /// <summary>
        /// Publish a string message (default QoS 0, not retained)
        /// </summary>
        public ushort Publish(string topic, string message)
        {
            return Publish(topic, message, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }

        /// <summary>
        /// Publish a string message with QoS level
        /// </summary>
        public ushort Publish(string topic, string message, byte qosLevel)
        {
            return Publish(topic, message, qosLevel, false);
        }

        /// <summary>
        /// Publish a string message with QoS level and retain flag
        /// </summary>
        public ushort Publish(string topic, string message, byte qosLevel, bool retain)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            return base.Publish(topic, payload, qosLevel, retain);
        }

        /// <summary>
        /// Subscribe to a single topic with default QoS 0
        /// </summary>
        public ushort Subscribe(string topic)
        {
            return base.Subscribe(new[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        }

        /// <summary>
        /// Subscribe to a single topic with specified QoS
        /// </summary>
        public ushort Subscribe(string topic, byte qosLevel)
        {
            return base.Subscribe(new[] { topic }, new byte[] { qosLevel });
        }

        /// <summary>
        /// Unsubscribe from a single topic
        /// </summary>
        public ushort Unsubscribe(string topic)
        {
            return base.Unsubscribe(new[] { topic });
        }

        /// <summary>
        /// Get the string representation of a received message payload
        /// </summary>
        public static string GetMessageAsString(byte[] payload)
        {
            return Encoding.UTF8.GetString(payload);
        }

        /// <summary>
        /// Publish an object as a JSON string (default QoS 0, not retained)
        /// </summary>
        public ushort PublishJson<T>(string topic, T payload)
        {
            return PublishJson(topic, payload, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }

        /// <summary>
        /// Publish an object as a JSON string with QoS level
        /// </summary>
        public ushort PublishJson<T>(string topic, T payload, byte qosLevel)
        {
            return PublishJson(topic, payload, qosLevel, false);
        }

        /// <summary>
        /// Publish an object as a JSON string with QoS level and retain flag
        /// </summary>
        public ushort PublishJson<T>(string topic, T payload, byte qosLevel, bool retain)
        {
            string json = JsonConvert.SerializeObject(payload);
            return Publish(topic, json, qosLevel, retain);
        }

        /// <summary>
        /// Deserialize a JSON message payload
        /// </summary>
        public T DeserializeJson<T>(byte[] payload)
        {
            string json = GetMessageAsString(payload);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Publish a protobuf byte message (default QoS 0, not retained)
        /// </summary>
        public ushort PublishProto(string topic, byte[] protoBytes)
        {
            return PublishProto(topic, protoBytes, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }

        /// <summary>
        /// Publish a protobuf byte message with QoS level
        /// </summary>
        public ushort PublishProto(string topic, byte[] protoBytes, byte qosLevel)
        {
            return PublishProto(topic, protoBytes, qosLevel, false);
        }

        /// <summary>
        /// Publish a protobuf byte message with QoS level and retain flag
        /// </summary>
        public ushort PublishProto(string topic, byte[] protoBytes, byte qosLevel, bool retain)
        {
            return base.Publish(topic, protoBytes, qosLevel, retain);
        }

        /// <summary>
        /// Publish an object as a Protobuf message (default QoS 0, not retained)
        /// </summary>
        public ushort PublishProto<T>(string topic, T payload)
        {
            return PublishProto(topic, payload, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }

        /// <summary>
        /// Publish an object as a Protobuf message with QoS level
        /// </summary>
        public ushort PublishProto<T>(string topic, T payload, byte qosLevel)
        {
            return PublishProto(topic, payload, qosLevel, false);
        }

        /// <summary>
        /// Publish an object as a Protobuf message with QoS level and retain flag
        /// </summary>
        public ushort PublishProto<T>(string topic, T payload, byte qosLevel, bool retain)
        {
            byte[] bytes = _serializer.Pack(payload);
            return base.Publish(topic, bytes, qosLevel, retain);
        }

        /// <summary>
        /// Deserialize a Protobuf message payload
        /// </summary>
        public T DeserializeProto<T>(byte[] payload)
        {
            return (T)_serializer.Unpack(payload);
        }
    }
}
