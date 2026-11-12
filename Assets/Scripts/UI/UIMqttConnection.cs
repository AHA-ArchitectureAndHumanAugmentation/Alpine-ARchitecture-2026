using UnityEngine;
using UnityEngine.UI;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// Connects to MQTT at startup and shows the connection popup when connecting fails.
    /// </summary>
    public class UIMqttConnection : MonoBehaviour
    {
        [SerializeField] private GameObject _connectionPopup;
        [SerializeField] private Button _testOfflineButton;

        private void OnEnable()
        {
            _testOfflineButton.onClick.AddListener(OnTestOfflineClicked);

            GeoViewerHub hub = GeoViewerHub.Instance;
            hub.OnConnected.AddListener(OnConnected);
            hub.OnConnectionFailed.AddListener(OnConnectionFailed);
        }

        private void Start()
        {
            GeoViewerHub.Instance.Connect();
        }

        private void OnDisable()
        {
            _testOfflineButton.onClick.RemoveAllListeners();

            GeoViewerHub hub = GeoViewerHub.Instance;
            hub.OnConnected.RemoveListener(OnConnected);
            hub.OnConnectionFailed.RemoveListener(OnConnectionFailed);
        }

        private void OnTestOfflineClicked()
        {
            _connectionPopup.SetActive(false);
        }

        private void OnConnected()
        {
            GeoViewerHub hub = GeoViewerHub.Instance;
            Debug.Log($"MQTT connected to {hub.BrokerAddress}:{hub.BrokerPort}");
            _connectionPopup.SetActive(false);
        }

        private void OnConnectionFailed()
        {
            GeoViewerHub hub = GeoViewerHub.Instance;
            Debug.LogError($"MQTT could not connect to {hub.BrokerAddress}:{hub.BrokerPort}.");
            _connectionPopup.SetActive(true);
        }
    }
}
