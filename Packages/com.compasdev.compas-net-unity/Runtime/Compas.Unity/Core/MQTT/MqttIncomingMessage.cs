namespace Compas.Unity.Core.Mqtt
{
    /// <summary>
    /// A decoded MQTT message: the topic it arrived on plus the deserialized payload object.
    /// The topic is what lets the visualization layer key geometry (one anchored object per
    /// topic), so it travels with the payload rather than being dropped at decode time.
    /// </summary>
    public readonly struct MqttIncomingMessage
    {
        public string Topic { get; }
        public object Decoded { get; }

        public MqttIncomingMessage(string topic, object decoded)
        {
            Topic = topic;
            Decoded = decoded;
        }
    }
}
