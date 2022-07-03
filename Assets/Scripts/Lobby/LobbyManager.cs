using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private const string _usernamePrefKey = "Username";
    public static string PasswordPropertyKey = "p";
    private bool _mainMenuButtonClicked = false;
    private bool _joinedRoom = false;
    [SerializeField] private InputField _usernameInputField;
    [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private InputField _passwordInputField;
    private const byte _maxPlayersPerRoom = 4;
    [SerializeField] private Text _errorText;
    [SerializeField] private Transform _roomListingsContent;
    [SerializeField] private RoomListing _roomListingPrefab;
    private List<RoomListing> _roomListings = new List<RoomListing>();
    [SerializeField] private GameObject _passwordModal;
    public GameObject PasswordModal => _passwordModal;
    [SerializeField] private Text _passwordModalRoomNameText;
    [SerializeField] private InputField _passwordModalPasswordInputField;
    [SerializeField] private GameObject _passwordModalIncorrectPasswordText;


    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        string username = string.Empty;
        if (PlayerPrefs.HasKey(_usernamePrefKey))
        {
            username = PlayerPrefs.GetString(_usernamePrefKey);
            _usernameInputField.text = username;
        }
        PhotonNetwork.NickName = username;
    }

    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public void LoadMainMenu()
    {
        if (_mainMenuButtonClicked)
        {
            return;
        }
        _mainMenuButtonClicked = true;
        if (PhotonNetwork.IsConnected)
        {
            Disconnect();
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            Debug.LogWarning("Disconnected: " + cause);
            _errorText.text = "Connection lost";
        }
        SceneManager.LoadScene("MainMenuScene");
    }

    public void UpdateUsername()
    {
        PlayerPrefs.SetString(_usernamePrefKey, _usernameInputField.text);
        PhotonNetwork.NickName = _usernameInputField.text;
    }

    public void CreateRoom()
    {
        if (_joinedRoom)
        {
            return;
        }
        if (string.IsNullOrEmpty(_usernameInputField.text))
        {
            _errorText.text = "Choose a username first";
            return;
        }
        if (string.IsNullOrEmpty(_roomNameInputField.text))
        {
            _errorText.text = "Room name cannot be empty";
            return;
        }
        _errorText.text = string.Empty;
        _joinedRoom = true;
        RoomOptions options = new RoomOptions { MaxPlayers = _maxPlayersPerRoom };
        if (!string.IsNullOrEmpty(_passwordInputField.text))
        {
            options.CustomRoomPropertiesForLobby = new string[1] { PasswordPropertyKey };
            options.CustomRoomProperties = new Hashtable { [PasswordPropertyKey] = _passwordInputField.text };
        }
        PhotonNetwork.CreateRoom(_roomNameInputField.text, options);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Creating room failed: " + message);
        _errorText.text = "Couldn't create room";
        _joinedRoom = false;
    }

    public void JoinRoom()
    {
        if (_joinedRoom)
        {
            return;
        }
        if (string.IsNullOrEmpty(_usernameInputField.text))
        {
            _errorText.text = "Choose a username first";
            return;
        }
        if (string.IsNullOrEmpty(_roomNameInputField.text))
        {
            _errorText.text = "Room name cannot be empty";
            return;
        }
        int index = _roomListings.FindIndex(listing => listing.RoomInfo.Name == _roomNameInputField.text);
        if (index == -1)
        {
            _errorText.text = "This room does not exist";
            return;
        }
        RoomInfo info = _roomListings[index].RoomInfo;
        if (info.CustomProperties.ContainsKey(PasswordPropertyKey))
        {
            if (_passwordInputField.text != (string)info.CustomProperties[PasswordPropertyKey])
            {
                _errorText.text = "Password is incorrect";
                return;
            }
        }
        _errorText.text = string.Empty;
        _joinedRoom = true;
        PhotonNetwork.JoinRoom(info.Name);
    }

    public void JoinRoomFromPasswordModal()
    {
        if (_joinedRoom)
        {
            return;
        }
        int index = _roomListings.FindIndex(listing => listing.RoomInfo.Name == _passwordModalRoomNameText.text);
        if (index == -1)
        {
            _errorText.text = "This room does not exist anymore";
            ClosePasswordModal();
            return;
        }
        if (_roomListings[index].RoomInfo.CustomProperties.ContainsKey(PasswordPropertyKey)
            && _passwordModalPasswordInputField.text != (string)_roomListings[index].RoomInfo.CustomProperties[PasswordPropertyKey])
        {
            _passwordModalIncorrectPasswordText.SetActive(true);
            return;
        }
        _passwordModalIncorrectPasswordText.SetActive(false);
        _joinedRoom = true;
        PhotonNetwork.JoinRoom(_passwordModalRoomNameText.text);
    }

    public void ClosePasswordModal()
    {
        _passwordModalPasswordInputField.text = string.Empty;
        _passwordModalIncorrectPasswordText.SetActive(false);
        _passwordModal.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        SceneManager.LoadScene("RoomScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Joining room failed: " + message);
        _errorText.text = "Couldn't join room";
        _joinedRoom = false;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo roomInfo in roomList)
        {
            int index = _roomListings.FindIndex(listing => listing.RoomInfo.Name == roomInfo.Name);
            if (index != -1)
            {
                if (roomInfo.RemovedFromList)
                {
                    // Remove listing
                    Destroy(_roomListings[index].gameObject);
                    _roomListings.RemoveAt(index);
                }
                else
                {
                    // Update listing
                    _roomListings[index].SetRoomInfo(roomInfo);
                }
            }
            else
            {
                if (!roomInfo.RemovedFromList)
                {
                    // Add listing
                    RoomListing listing = Instantiate(_roomListingPrefab, _roomListingsContent);
                    listing.SetRoomInfo(roomInfo);
                    _roomListings.Add(listing);
                }
            }
        }
    }
}
