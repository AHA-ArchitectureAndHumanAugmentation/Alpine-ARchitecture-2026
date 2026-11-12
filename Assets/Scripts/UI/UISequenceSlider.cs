using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// Slider that steps through the received geometry in its clicked order.
    /// </summary>
    public class UISequenceSlider : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField]
        [Tooltip("Optional. Shows the current step, e.g. \"3 / 12\".")]
        private TMP_Text _stepLabel;
        [SerializeField] private Canvas _canvas;

        private int _count;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            _slider.onValueChanged.AddListener(OnSliderChanged);
            GeometrySequence sequence = GeoViewerHub.Instance.Sequence;
            sequence.OnSequenceReady.AddListener(OnSequenceReady);
            sequence.OnStepShown.AddListener(OnStepShown);

            _slider.wholeNumbers = true;
            UpdateSlider();
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
            GeometrySequence sequence = GeoViewerHub.Instance.Sequence;
            sequence.OnSequenceReady.RemoveListener(OnSequenceReady);
            sequence.OnStepShown.RemoveListener(OnStepShown);
        }

        private void SetVisible(bool isVisible)
        {
            _canvas.enabled = isVisible;
            _slider.interactable = isVisible;
        }

        private void OnSequenceReady(int count)
        {
            _count = count;
            SetVisible(_count > 0);

            _slider.SetValueWithoutNotify(0);
            UpdateSlider();

            if (_count > 0)
            {
                GeoViewerHub.Instance.Sequence.ShowStep(0);
            }
        }

        private void OnSliderChanged(float value)
        {
            GeoViewerHub.Instance.Sequence.ShowStep(Mathf.RoundToInt(value));
        }

        private void OnStepShown(int step)
        {
            _slider.SetValueWithoutNotify(step);
            UpdateLabel(step);
        }

        private void UpdateSlider()
        {
            _slider.minValue = 0;
            _slider.maxValue = Mathf.Max(0, _count - 1);
            _slider.SetValueWithoutNotify(Mathf.Clamp(_slider.value, _slider.minValue, _slider.maxValue));
            _slider.interactable = _count > 1;
            UpdateLabel((int)_slider.value);
        }

        private void UpdateLabel(int index)
        {
            if (_stepLabel == null)
            {
                return;
            }
            _stepLabel.text = _count == 0 ? "-" : $"{index + 1} / {_count}";
        }
    }
}
