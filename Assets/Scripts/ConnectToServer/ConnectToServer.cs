using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    [SerializeField] private Text _loadingText;
    [SerializeField] private GameObject _mainMenuButton;

    private void Awake()
    {
        // This makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = "1";
    }

    public override void OnConnectedToMaster()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Connection failed: " + cause);
        _loadingText.text = "Connection failed";
        _mainMenuButton.SetActive(true);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
