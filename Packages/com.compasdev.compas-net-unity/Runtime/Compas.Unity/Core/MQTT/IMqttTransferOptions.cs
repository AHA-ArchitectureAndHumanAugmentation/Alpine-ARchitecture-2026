namespace Compas.Unity.Core.Mqtt
{
    public interface IMqttTransferSettings
    {
        QoS QualityOfService { get; }
        SerializationProtocol Protocol { get; }
    }
}