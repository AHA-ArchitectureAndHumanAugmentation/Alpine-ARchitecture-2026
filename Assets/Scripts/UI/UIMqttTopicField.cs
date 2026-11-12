using TMPro;
using UnityEngine;

namespace Compas.GeoViewerXR.UI
{
    public enum TopicType
    {
        None,
        Subscribe,
        Publish
    }

    public class UIMqttTopicField : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _topicInput;
        [SerializeField] private GameObject _topicSavedIcon;
        [SerializeField] private TopicType _topicType;
        private string _oldTopicValue;

        public string Text
        {
            get => _topicInput.text;
            set => _topicInput.text = value;
        }

        private void Awake()
        {
            _topicInput.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnEnable()
        {
            _oldTopicValue = _topicInput.text;
            _topicSavedIcon.SetActive(false);
        }

        private void OnValueChanged(string newText)
        {
            _topicSavedIcon.SetActive(false);
        }

        public void Submit()
        {
            _topicSavedIcon.SetActive(true);

            if (_topicInput.text == _oldTopicValue)
            {
                return;
            }

            if (_topicType == TopicType.Publish)
            {
                GeoViewerHub.Instance.SetPublishTopic(_topicInput.text);
            }
            else if (_topicType == TopicType.Subscribe)
            {
                GeoViewerHub.Instance.SetSubscribeTopic(_topicInput.text);
            }
        }
    }
}