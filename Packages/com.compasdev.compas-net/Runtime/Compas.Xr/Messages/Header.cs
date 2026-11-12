using Pb = CompasXr.V1;

namespace Compas.Xr.Messages
{
    /// <summary>
    /// Coordination information carried by every COMPAS XR message.
    /// </summary>
    /// <remarks>
    /// Devices use the header to correlate requests with responses and to ignore messages they
    /// sent themselves. Mirrors <c>compas_xr.mqtt.messages.Header</c> on the Python side.
    /// </remarks>
    public class Header
    {
        public Header(long sequenceId, long responseId, string deviceId, string timeStamp)
        {
            SequenceId = sequenceId;
            ResponseId = responseId;
            DeviceId = deviceId;
            TimeStamp = timeStamp;
        }

        /// <summary>Atomic counter, incremented once per message sent by a device.</summary>
        public long SequenceId { get; }

        /// <summary>Incremented once per request/response routine.</summary>
        public long ResponseId { get; }

        /// <summary>Unique identifier of the device that sent the message.</summary>
        public string DeviceId { get; }

        /// <summary>Time the message was sent.</summary>
        public string TimeStamp { get; }

        public Pb.Header ToProto()
        {
            return new Pb.Header
            {
                SequenceId = SequenceId,
                ResponseId = ResponseId,
                DeviceId = DeviceId ?? string.Empty,
                TimeStamp = TimeStamp ?? string.Empty,
            };
        }

        public static Header FromProto(Pb.Header proto)
        {
            return new Header(proto.SequenceId, proto.ResponseId, proto.DeviceId, proto.TimeStamp);
        }
    }
}
