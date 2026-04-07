using System;
using Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CrossFire.App.UI
{
    // Main menu panel. Scene-owned: registered and destroyed by MainMenuUIController.
    // Exposes button events so MainMenuUIController can wire game logic without coupling the view.
    public class MainMenuPanel : PanelBase
    {
        [SerializeField]
        private Button _playButton;

        [SerializeField]
        private Button _exitButton;

        // Raised when the player clicks Play.
        public event Action PlayButtonClicked;

        // Raised when the player clicks Exit.
        public event Action ExitButtonClicked;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(HandlePlayButtonClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(HandleExitButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(HandleExitButtonClicked);
            }
        }

        private void HandlePlayButtonClicked()
        {
            PlayButtonClicked?.Invoke();
        }

        private void HandleExitButtonClicked()
        {
            ExitButtonClicked?.Invoke();
        }
    }
}
