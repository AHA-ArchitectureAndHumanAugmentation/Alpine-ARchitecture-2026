using Compas.Events;

namespace Compas.Core.Mqtt
{
    /// <summary>
    /// Notification: an attempt to reach the broker did not succeed. The counterpart to
    /// <see cref="MqttConnectionEstablished"/>, so a UI can react to both outcomes of an
    /// <see cref="MqttConnectRequested"/> without holding a reference to the MQTT client.
    ///
    /// <para>No error text: the underlying client exposes failure as a bare notification, and
    /// the message it logs is not carried on the public event.</para>
    /// </summary>
    public readonly struct MqttConnectionFailed : IEvent
    {
        public readonly string brokerAddress;
        public readonly int port;

        public MqttConnectionFailed(string brokerAddress, int port)
        {
            this.brokerAddress = brokerAddress;
            this.port = port;
        }
    }
}
