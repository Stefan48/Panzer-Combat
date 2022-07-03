using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private bool _playButtonClicked = false;

    public void Play()
    {
        if (_playButtonClicked)
        {
            return;
        }
        _playButtonClicked = true;
        SceneManager.LoadScene("ConnectToServerScene");
    }

    public void ExitApplication()
    {
        Application.Quit();
    }

    // TODO - Options section for controls (saved in PlayerPrefs)
}
