using UnityEngine;
using UnityEngine.XR.Templates.AR;

namespace Compas.GeoViewerXR.UI
{
    /// <summary>
    /// Shows the greeting and the AR tutorial cards on the app's first launch only.
    /// </summary>
    [RequireComponent(typeof(GoalManager))]
    public class UICoaching : MonoBehaviour
    {
        private GoalManager _goalManager;

        /// <summary>
        /// Starts the tutorial cards and remembers that they were shown. Called by the greeting's Continue button.
        /// </summary>
        public void StartCoaching()
        {
            PlayerPrefs.SetInt(PlayerPrefsKeys.CoachingShown, 1);
            PlayerPrefs.Save();
            _goalManager.StartCoaching();
        }

        private void Awake()
        {
            _goalManager = GetComponent<GoalManager>();
        }

        private void Start()
        {
            bool firstLaunch = PlayerPrefs.GetInt(PlayerPrefsKeys.CoachingShown, 0) == 0;

            // The Continue button brings the hidden buttons back when it starts the cards.
            _goalManager.greetingPrompt.SetActive(firstLaunch);
            _goalManager.createButton.SetActive(!firstLaunch);
            _goalManager.optionsButton.SetActive(!firstLaunch);
        }
    }
}
