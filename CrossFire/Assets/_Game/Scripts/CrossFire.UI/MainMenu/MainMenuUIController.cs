using CrossFire.App;
using FactorialFun.Core.UI;
using UnityEngine;

namespace CrossFire.App.UI
{
    // Scene-specific UI controller for the main menu.
    // Registers MainMenuPanel with the global UIRoot so it shares the same canvas and
    // priority stack as all other panels — no separate canvas needed.
    //
    // Place this MonoBehaviour in the MainMenu scene (not on a DontDestroyOnLoad object).
    // When the scene unloads, SceneUIController.OnDestroy automatically removes and
    // destroys the panel from UIRoot.
    public class MainMenuUIController : SceneUIController
    {
        [SerializeField]
        private MainMenuPanel _mainMenuPanelPrefab;

        [SerializeField]
        private string _nextSceneName = "HexMap";

        private MainMenuPanel _mainMenuPanel;

        protected override void RegisterPanels()
        {
            _mainMenuPanel = RegisterPanel(_mainMenuPanelPrefab);
            _mainMenuPanel.PlayButtonClicked += HandlePlayButtonClicked;
            _mainMenuPanel.ExitButtonClicked += HandleExitButtonClicked;

            UIRoot.Instance.Show<MainMenuPanel>();
        }

        protected override void OnDestroy()
        {
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.PlayButtonClicked -= HandlePlayButtonClicked;
                _mainMenuPanel.ExitButtonClicked -= HandleExitButtonClicked;
            }

            base.OnDestroy();
        }

        private void HandlePlayButtonClicked()
        {
            LevelLoader.Instance.LoadLevel(_nextSceneName);
        }

        private void HandleExitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
