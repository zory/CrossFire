using UnityEngine;

public class SimpleMainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        // Load the main game scene
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Gameplay");
	}

    public void ExitGame()
    {
        // Quit the application
        Application.Quit();
	}
}
