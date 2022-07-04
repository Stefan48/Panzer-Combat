using UnityEngine;

public class PlayerInfo
{
    public int ActorNumber { get; private set; }
    public string Username { get; private set; }
    public Color Color { get; private set; }
    public int RoundsWon { get; private set; }
    // TODO - More statistics (tanks spawned, tanks destroyed, max tanks owned, players defeated, shells shot etc.)

    
    public PlayerInfo(int actorNumber, string username, Color color)
    {
        ActorNumber = actorNumber;
        Username = username;
        Color = color;
        RoundsWon = 0;
    }

    public void WonRound()
    {
        RoundsWon++;
    }
}
