using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// Shows the MQTT connection state as a colour, and optionally as a label.
    /// </summary>
    public class UIMqttStatusIndicator : MonoBehaviour
    {
        [SerializeField] private Image _statusCircle;
        [SerializeField] private TMP_Text _statusLabel;

        private void Reset() => _statusCircle = GetComponent<Image>();

        private void OnEnable()
        {
            GeoViewerHub.Instance.OnConnectionStateChanged.AddListener(ShowConnectionState);
            ShowConnectionState();
        }

        private void OnDisable()
        {
            GeoViewerHub.Instance.OnConnectionStateChanged.RemoveListener(ShowConnectionState);
        }

        private void ShowConnectionState()
        {
            UpdateMqttStatus(GeoViewerHub.Instance.ConnectionState);
        }

        private void UpdateMqttStatus(MqttConnectionStateUI connectionState)
        {
            Color newColor = Color.white;
            switch (connectionState)
            {
                case MqttConnectionStateUI.Connected:
                    newColor = Color.green;
                    break;
                case MqttConnectionStateUI.AttemptingConnection:
                    newColor = Color.yellow;
                    break;
                case MqttConnectionStateUI.Disconnected:
                case MqttConnectionStateUI.ConnectionFailed:
                    newColor = Color.red;
                    break;
            }

            if (_statusCircle != null)
            {
                _statusCircle.color = newColor;
            }

            if (_statusLabel != null)
            {
                _statusLabel.color = newColor;
                _statusLabel.text = connectionState.ToString();
            }
        }
    }
}
