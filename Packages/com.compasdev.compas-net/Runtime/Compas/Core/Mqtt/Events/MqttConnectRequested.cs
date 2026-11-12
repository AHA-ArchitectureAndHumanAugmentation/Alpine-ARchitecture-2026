using Compas.Events;

namespace Compas.Core.Mqtt
{
    /// <summary>
    /// Command: something wants an MQTT connection opened. Raised by whatever expresses the
    /// intent - a UI button, a scanned QR code, saved settings, a deep link - and handled by a
    /// single binder that owns the MQTT client reference.
    ///
    /// <para>The whole connection is described by <see cref="connectPath"/>, so neither the
    /// sender nor the MQTT layer needs to know about the other.</para>
    /// </summary>
    public readonly struct MqttConnectRequested : IEvent
    {
        /// <summary>An <c>mqtt://host[:port][?topic=...]</c> URI.</summary>
        public readonly string connectPath;

        public MqttConnectRequested(string connectPath)
        {
            this.connectPath = connectPath;
        }
    }
}
