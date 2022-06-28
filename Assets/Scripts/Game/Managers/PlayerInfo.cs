using UnityEngine;

public class PlayerInfo
{
    public int PlayerNumber { get; private set; }
    public string Username { get; private set; }
    public Color Color { get; private set; }
    public int RoundsWon { get; private set; }

    public PlayerInfo(int playerNumber, string username, Color color)
    {
        PlayerNumber = playerNumber;
        Username = username;
        Color = color;
        RoundsWon = 0;
    }
}
