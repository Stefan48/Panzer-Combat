using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _initialTanksText;
    [SerializeField] private Text _roundsToWinText;
    [SerializeField] private GameObject _startGameButton;
    private bool _startGameButtonClicked = false;
    private bool _leftRoomIntentionally = false;
    [SerializeField] private GameObject _disconnectedModal;
    [SerializeField] private Text _disconnectedText;
    [SerializeField] private Transform _playerListingsContent;
    [SerializeField] private PlayerListing _playerListingPrefab;
    private List<PlayerListing> _playerListings = new List<PlayerListing>();
    [SerializeField] private GameObject _notEnoughPlayersText;
    // Hold a reference to the CurrentSessionData ScriptableObject so it doesn't get reset when the scene loads
    [SerializeField] private CurrentSessionData _currentSessionData;


    private void Start()
    {
        _roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        byte initialTanks = (byte)PhotonNetwork.CurrentRoom.CustomProperties[LobbyManager.InitialTanksPropertyKey];
        _initialTanksText.text = initialTanks + " initial " + (initialTanks > 1 ? "tanks" : "tank");
        int roundsToWin = (int)PhotonNetwork.CurrentRoom.CustomProperties[LobbyManager.RoundsToWinPropertyKey];
        _roundsToWinText.text = roundsToWin + (roundsToWin > 1 ? " rounds" : " round") + " to win";
        _startGameButton.SetActive(PhotonNetwork.IsMasterClient);

        Player[] players = PhotonNetwork.PlayerList;
        foreach (Player player in players)
        {
            PlayerListing listing = Instantiate(_playerListingPrefab, _playerListingsContent);
            listing.SetPlayerInfo(player);
            _playerListings.Add(listing);
        }
    }

    public void Leave()
    {
        if (_leftRoomIntentionally)
        {
            return;
        }
        _leftRoomIntentionally = true;
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        if (_leftRoomIntentionally)
        {
            LoadLobby();
        }
        else
        {
            _disconnectedModal.SetActive(true);
            _disconnectedText.text = "You've been kicked out of the room";
        }
    }

    public void LoadLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected: " + cause);
        _disconnectedModal.SetActive(true);
        _disconnectedText.text = "You've been disconnected";
    }

    public void StartGame()
    {
        if (_startGameButtonClicked)
        {
            return;
        }
        // Commenting this and/or disabling 'Force Single Instance' in the Project Settings might be necessary for local testing
        // Also, enabling 'Development Build' in the Build Settings enables a useful console for logging warnings and errors
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            _notEnoughPlayersText.SetActive(true);
            return;
        }
        _startGameButtonClicked = true;
        _notEnoughPlayersText.SetActive(false);
        // Load the scene at the same time for everyone in the room
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    public override void OnPlayerEnteredRoom(Player player)
    {
        int index = _playerListings.FindIndex(listing => listing.Player == player);
        if (index == -1)
        {
            PlayerListing listing = Instantiate(_playerListingPrefab, _playerListingsContent);
            listing.SetPlayerInfo(player);
            _playerListings.Add(listing);
        }
    }

    public override void OnPlayerLeftRoom(Player player)
    {
        int index = _playerListings.FindIndex(listing => listing.Player == player);
        if (index != -1)
        {
            Destroy(_playerListings[index].gameObject);
            _playerListings.RemoveAt(index);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            _startGameButton.gameObject.SetActive(true);
            for (int i = 0; i < _playerListings.Count; ++i)
            {
                _playerListings[i].SetPlayerInfo(_playerListings[i].Player);
            }
        }
        else
        {
            int index = _playerListings.FindIndex(listing => listing.Player.IsMasterClient);
            _playerListings[index].SetPlayerInfo(_playerListings[index].Player);
        }
    }
}
