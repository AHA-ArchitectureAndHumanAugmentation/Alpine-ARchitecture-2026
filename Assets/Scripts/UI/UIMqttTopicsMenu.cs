using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Compas.GeoViewerXR.UI
{
    public class UIMqttTopicsMenu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _brokerAddressField;
        [SerializeField] private UIMqttTopicField _subscribeTopicField;
        [SerializeField] private UIMqttTopicField _publishTopicField;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _connectBrokerButton;
        [SerializeField] private Button _connectWithQRCodeButton;

        private void Awake()
        {
            _saveButton.onClick.AddListener(OnSaveClicked);
            _cancelButton.onClick.AddListener(Close);
            _connectBrokerButton.onClick.AddListener(OnConnectClicked);
            _connectWithQRCodeButton.onClick.AddListener(OnConnectWithQRCodeClicked);
        }

        private void OnDestroy()
        {
            _saveButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.RemoveAllListeners();
            _connectBrokerButton.onClick.RemoveAllListeners();
            _connectWithQRCodeButton.onClick.RemoveAllListeners();
        }

        private void OnConnectWithQRCodeClicked()
        {
            GeoViewerHub.Instance.StartQRScan();
        }

        // The panel shows what MQTT is set to right now, so it is filled when it opens rather than
        // kept up to date while nobody is looking at it.
        private void OnEnable()
        {
            GeoViewerHub hub = GeoViewerHub.Instance;

            if (hub.SubscribeTopic != null)
            {
                _subscribeTopicField.Text = hub.SubscribeTopic;
            }

            if (hub.PublishTopic != null)
            {
                _publishTopicField.Text = hub.PublishTopic;
            }

            _brokerAddressField.text = hub.BrokerAddress;
        }

        private void OnSaveClicked()
        {
            _subscribeTopicField.Submit();
            _publishTopicField.Submit();
        }

        private void OnConnectClicked()
        {
            GeoViewerHub.Instance.Connect();
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }
    }
}