using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomListing : MonoBehaviourPunCallbacks
{
    public RoomInfo RoomInfo { get; private set; }
    [SerializeField] private Text _roomListingText;
    [SerializeField] private GameObject _passwordRequiredImage;
    private InputField _usernameInputField;
    private GameObject _passwordModal;
    private Text _passwordModalRoomNameText;
    private Text _errorText;
    private bool _joinedRoom = false;


    private void Start()
    {
        Transform canvasTransform = GameObject.Find("Canvas").transform;
        _usernameInputField = canvasTransform.Find("UsernameInputField").GetComponent<InputField>();
        _passwordModal = canvasTransform.Find("PasswordModal").gameObject;
        _passwordModalRoomNameText = _passwordModal.transform.Find("PasswordBox").Find("RoomNameText").GetComponent<Text>();
        _errorText = canvasTransform.Find("ErrorText").GetComponent<Text>();
    }

    public void SetRoomInfo(RoomInfo info)
    {
        RoomInfo = info;
        _roomListingText.text = info.Name + " (" + info.PlayerCount + "/" + info.MaxPlayers + ")";
        _passwordRequiredImage.SetActive(info.CustomProperties.ContainsKey(LobbyManager.PasswordPropertyKey));
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(_usernameInputField.text))
        {
            _errorText.text = "Choose a username first";
            return;
        }
        _errorText.text = string.Empty;
        if (RoomInfo.CustomProperties.ContainsKey(LobbyManager.PasswordPropertyKey))
        {
            _passwordModal.SetActive(true);
            _passwordModalRoomNameText.text = RoomInfo.Name;
        }
        else
        {
            if (_joinedRoom)
            {
                return;
            }
            _joinedRoom = true;
            PhotonNetwork.JoinRoom(RoomInfo.Name);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        _joinedRoom = false;
    }
}
