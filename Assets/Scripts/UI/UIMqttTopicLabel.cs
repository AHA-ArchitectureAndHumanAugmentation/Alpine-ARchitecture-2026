using TMPro;
using UnityEngine;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// Shows the MQTT topic the app listens on.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class UIMqttTopicLabel : MonoBehaviour
    {
        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            GeoViewerHub hub = GeoViewerHub.Instance;
            hub.OnSubscribeTopicChanged.AddListener(ShowTopic);
            hub.OnTopicSubscribed.AddListener(ShowTopic);
            ShowTopic(hub.SubscribeTopic);
        }

        private void OnDisable()
        {
            GeoViewerHub hub = GeoViewerHub.Instance;
            hub.OnSubscribeTopicChanged.RemoveListener(ShowTopic);
            hub.OnTopicSubscribed.RemoveListener(ShowTopic);
        }

        private void ShowTopic(string topic)
        {
            _label.text = topic;
        }
    }
}
