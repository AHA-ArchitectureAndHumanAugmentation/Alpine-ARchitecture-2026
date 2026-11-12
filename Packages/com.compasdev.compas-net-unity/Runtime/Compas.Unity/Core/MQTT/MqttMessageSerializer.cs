using Compas.Unity.Data;
using CompasPb;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Compas.Unity.Core.Mqtt
{
    /// <summary>
    /// Serialization protocol for MQTT message payloads.
    /// </summary>
    public enum SerializationProtocol
    {
        /// <summary>JSON serialization (default).</summary>
        Json = 0,
        /// <summary>Protocol Buffers serialization.</summary>
        ProtoBuf = 1
    }

    /// <summary>
    /// Translates MQTT payloads between raw bytes and objects, both directions, choosing the
    /// wire format by <see cref="SerializationProtocol"/>. <see cref="Decode"/> is used on the
    /// way in, and <see cref="Encode"/> is its mirror, used when publishing.
    ///
    /// The conversion mapping lives in <see cref="UnityProtoConversions"/>.
    /// </summary>
    public static class MqttMessageSerializer
    {
        /// <summary>Object -> bytes, for publishing.</summary>
        public static byte[] Encode(SerializationProtocol protocol, object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            switch (protocol)
            {
                case SerializationProtocol.ProtoBuf:
                    return new CompasPbSerializer().Pack(message);
                case SerializationProtocol.Json:
                    return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                default:
                    return Array.Empty<byte>();
            }
        }

        /// <summary>Bytes -> object (plus a string form for logging/history), for receiving.</summary>
        public static (object decoded, string text) Decode(SerializationProtocol protocol, byte[] message)
        {
            object decoded = null;
            string text = string.Empty;
            switch (protocol)
            {
                case SerializationProtocol.ProtoBuf:
                    decoded = new CompasPbSerializer().Unpack(message);

                    text = decoded?.ToString() ?? string.Empty;
                    break;
                case SerializationProtocol.Json:
                    text = Encoding.UTF8.GetString(message);
                    decoded = JsonConvert.DeserializeObject(text);
                    break;
            }

            return (decoded, text);
        }
    }
}
