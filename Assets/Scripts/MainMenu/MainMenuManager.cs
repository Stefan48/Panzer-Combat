using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("ConnectToServerScene");
    }

    public void ExitApplication()
    {
        Application.Quit();
    }

    // TODO - Options section for controls (saved in PlayerPrefs)
}
