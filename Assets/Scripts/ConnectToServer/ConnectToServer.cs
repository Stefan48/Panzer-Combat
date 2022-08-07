using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    [SerializeField] private Text _loadingText;
    [SerializeField] private GameObject _mainMenuButton;
    // Hold a reference to the CurrentSessionData ScriptableObject so it doesn't get reset when the scene loads
    [SerializeField] private CurrentSessionData _currentSessionData;


    private void Awake()
    {
        // This makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        // Players can play only with other players who are on the same game version
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.EnableCloseConnection = true;
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
