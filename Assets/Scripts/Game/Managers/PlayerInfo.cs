using UnityEngine;

public class PlayerInfo
{
    public int PlayerNumber { get; private set; }
    // TODO - Username?
    public Color Color { get; private set; }
    public int RoundsWon { get; private set; }

    public PlayerInfo(int playerNumber, Color color)
    {
        PlayerNumber = playerNumber;
        Color = color;
        RoundsWon = 0;
    }
}
