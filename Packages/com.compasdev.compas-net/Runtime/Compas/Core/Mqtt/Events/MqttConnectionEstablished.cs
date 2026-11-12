using Compas.Events;

namespace Compas.Core.Mqtt
{
    public readonly struct MqttConnectionEstablished : IEvent
    {
        public readonly string brokerAddress;
        public readonly int port;

        public MqttConnectionEstablished(string brokerAddress, int port)
        {
            this.brokerAddress = brokerAddress;
            this.port = port;
        }
    }
}
