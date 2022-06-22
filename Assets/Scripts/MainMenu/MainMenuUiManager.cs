using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUiManager : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("ConnectToServerScene");
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
