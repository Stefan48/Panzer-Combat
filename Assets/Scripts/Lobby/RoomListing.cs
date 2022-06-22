using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomListing : MonoBehaviour
{
    public RoomInfo RoomInfo { get; private set; }
    [SerializeField] private Text _roomListingText;
    [SerializeField] private GameObject _passwordRequiredImage;
    private GameObject _enterPasswordModal;
    private Text _roomNameText;

    private void Start()
    {
        _enterPasswordModal = GameObject.Find("LobbyManager").GetComponent<LobbyManager>().PasswordModal;
        _roomNameText = _enterPasswordModal.transform.Find("PasswordBox").Find("RoomNameText").GetComponent<Text>();
    }

    public void SetRoomInfo(RoomInfo info)
    {
        RoomInfo = info;
        _roomListingText.text = info.Name + " (" + info.PlayerCount + "/" + info.MaxPlayers + ")";
        _passwordRequiredImage.SetActive(info.CustomProperties.ContainsKey(LobbyManager.PasswordPropertyKey));
    }

    public void JoinRoom()
    {
        if (RoomInfo.CustomProperties.ContainsKey(LobbyManager.PasswordPropertyKey))
        {
            _enterPasswordModal.SetActive(true);
            _roomNameText.text = RoomInfo.Name;
        }
        else
        {
            PhotonNetwork.JoinRoom(RoomInfo.Name);
        }
    }
}
