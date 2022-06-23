using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListing : MonoBehaviour
{
    public Player Player { get; private set; }
    [SerializeField] private Text _playerListingText;
    [SerializeField] private GameObject _hostImage;
    [SerializeField] private GameObject _kickButton;

    public void SetPlayerInfo(Player player)
    {
        Player = player;
        _playerListingText.text = player.NickName + (player.IsLocal ? " •" : string.Empty);
        _hostImage.SetActive(player.IsMasterClient);
        _kickButton.SetActive(PhotonNetwork.IsMasterClient && player != PhotonNetwork.LocalPlayer);
    }

    public void KickPlayer()
    {
        if (PhotonNetwork.IsMasterClient && Player != PhotonNetwork.LocalPlayer)
        {
            PhotonNetwork.CloseConnection(Player);
        }
    }
}
