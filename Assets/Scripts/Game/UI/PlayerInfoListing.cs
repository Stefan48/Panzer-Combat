using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoListing : MonoBehaviour
{
    [SerializeField] private Text _usernameText;
    [SerializeField] private Text _roundsWonText;

    public void SetPlayerInfo(PlayerInfo playerInfo)
    {
        _usernameText.text = UiManager.GetColoredPlayerText(playerInfo);
        _roundsWonText.text = playerInfo.RoundsWon.ToString();
    }
}
