using Compas.Core.Mqtt;
using Compas.Events;
using UnityEngine;

/// <summary>
/// Turns a scanned QR code  into a request. For the moment, it is used for reading and creating an
/// MQTT connection request. Raises <see cref="MqttConnectRequested"/>
/// rather than talking to the MQTT layer directly, so scanning a code is just one of several
/// interchangeable ways to ask for a connection - a UI button or saved settings raise the same
/// event and take the same path.
///
/// The scanned text is passed through untouched: validation belongs with the parser that knows
/// what a broker URI looks like, and it reports malformed input instead of throwing.
/// </summary>
public class QRCodeProcessor : MonoBehaviour
{
    private void OnEnable()
    {
        SimpleQRScanner.QRScanned += OnQRScanned;
    }

    private void OnDisable()
    {
        SimpleQRScanner.QRScanned -= OnQRScanned;
    }

    private void OnQRScanned(string qrText)
    {
        if (qrText.StartsWith("mqtt"))
        {
            EventBus<MqttConnectRequested>.Raise(new MqttConnectRequested(qrText));
        }
    }
}
