using System;

namespace Compas.Mqtt
{
    /// <summary>
    /// Broker connection details parsed out of an MQTT URI.
    /// <see cref="Topic"/> is optional and null when the URI carries no <c>topic</c> parameter.
    /// </summary>
    public readonly struct MqttUriInfo
    {
        public string BrokerAddress { get; }
        public int BrokerPort { get; }
        public string Topic { get; }

        public MqttUriInfo(string brokerAddress, int brokerPort, string topic)
        {
            BrokerAddress = brokerAddress;
            BrokerPort = brokerPort;
            Topic = topic;
        }
    }

    /// <summary>
    /// Parses <c>mqtt://host[:port][?topic=...]</c> into <see cref="MqttUriInfo"/>. This is the
    /// form encoded in the QR codes used to point a device at a broker, but nothing here assumes
    /// QR - it is a string in, connection details out.
    ///
    /// The query is split by hand rather than with <c>System.Web.HttpUtility</c>: that type
    /// exists only in the .NET Framework API profile, so it fails to compile against
    /// .NET Standard 2.1 and would prevent this parser moving into the pure-C# package.
    /// </summary>
    public static class MqttUri
    {
        /// <summary>Scheme an MQTT URI must use.</summary>
        public const string Scheme = "mqtt";

        /// <summary>Port assumed when the URI does not specify one.</summary>
        public const int DefaultPort = 1883;

        /// <summary>Query parameter carrying the topic to subscribe to.</summary>
        public const string TopicParameter = "topic";

        /// <summary>
        /// Parses <paramref name="text"/>. Returns false with a human-readable
        /// <paramref name="error"/> rather than throwing, so callers can handle malformed input
        /// (a QR code holding arbitrary text, say) without a try/catch.
        /// </summary>
        public static bool TryParse(string text, out MqttUriInfo info, out string error)
        {
            info = default;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "URI is empty.";
                return false;
            }

            if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri uri))
            {
                error = $"'{text}' is not a valid absolute URI.";
                return false;
            }

            return TryParse(uri, out info, out error);
        }

        /// <summary>
        /// Parses an already-constructed <paramref name="uri"/>. Returns false with a
        /// human-readable <paramref name="error"/> if it is not a usable MQTT URI.
        /// </summary>
        public static bool TryParse(Uri uri, out MqttUriInfo info, out string error)
        {
            info = default;

            if (uri == null)
            {
                error = "URI is null.";
                return false;
            }

            if (!string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Expected scheme '{Scheme}://' but found '{uri.Scheme}://'.";
                return false;
            }

            if (string.IsNullOrEmpty(uri.Host))
            {
                error = "URI has no host.";
                return false;
            }

            // Uri reports -1 when no port is present; it also substitutes a default port for
            // known schemes, which mqtt:// is not, so an explicit check is the reliable route.
            int port = uri.Port > 0 ? uri.Port : DefaultPort;

            info = new MqttUriInfo(uri.Host, port, ReadQueryValue(uri.Query, TopicParameter));
            error = null;
            return true;
        }

        /// <summary>
        /// Reads a single parameter out of a URI query string. Returns null when absent.
        /// </summary>
        private static string ReadQueryValue(string query, string key)
        {
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            foreach (string pair in query.TrimStart('?').Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }

                int separator = pair.IndexOf('=');
                string name = separator < 0 ? pair : pair.Substring(0, separator);

                if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = separator < 0 ? string.Empty : pair.Substring(separator + 1);

                // Topics contain '/', which is legal unescaped in a query but may also arrive
                // percent-encoded depending on what produced the URI.
                return Uri.UnescapeDataString(value);
            }

            return null;
        }
    }
}
