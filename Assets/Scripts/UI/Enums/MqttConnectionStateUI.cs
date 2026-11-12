namespace Compas.GeoViewerXR
{
    /// <summary>
    /// The state of the MQTT broker. For UI only, no actual logic.
    /// </summary>
    public enum MqttConnectionStateUI
    {
        Disconnected,
        Connected,
        AttemptingConnection,
        ConnectionFailed
    }
}
