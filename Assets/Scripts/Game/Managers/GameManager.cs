using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static int NumberOfPlayers { get; private set; } = 2;
    [SerializeField] private Color[] playerColors;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private GameObject tankPrefab;
    private PlayerManager[] playerManagers = new PlayerManager[NumberOfPlayers];
    public int TotalRoundsToWin { get; private set; } = 2;
    [SerializeField] public Transform[] SpawnPoints { get; private set; }
    private readonly float startDelay = 1f;
    private readonly float endDelay = 1f;
    private WaitForSeconds startWait;
    private WaitForSeconds endWait;
    [SerializeField] private CameraControl cameraControl;
    [SerializeField] private UiManager uiManager;
    [SerializeField] private Text infoText;
    private int currentRound;
    private bool matchEnded;



    private void Start()
    {
        //Physics.defaultMaxDepenetrationVelocity = float.PositiveInfinity;

        startWait = new WaitForSeconds(startDelay);
        endWait = new WaitForSeconds(endDelay);

        SetupPlayerManagers();

        StartCoroutine(GameLoop());
    }

    private void SetupPlayerManagers()
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {            
            playerManagers[i] = new PlayerManager(i + 1, playerColors[i], playerSpawnPoints[i], tankPrefab);
            playerManagers[i].Setup();
        }
    }

    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());

        if (matchEnded)
        {
            // If there is a game winner, restart the level
            //SceneManager.LoadScene(0);
        }
        else
        {
            // If there isn't a winner yet, restart this coroutine so the loop continues
            // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end
            StartCoroutine(GameLoop());
        }
    }

    private IEnumerator RoundStarting()
    {
        cameraControl.enabled = false;
        uiManager.enabled = false;

        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            playerManagers[i].Reset();
            playerManagers[i].DisableControl();
        }

        // TODO - Set camera initial position

        currentRound++;
        infoText.text = "ROUND " + currentRound;
        yield return startWait;
    }

    private IEnumerator RoundPlaying()
    {
        cameraControl.enabled = true;
        uiManager.enabled = true;

        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            playerManagers[i].EnableControl();
        }

        infoText.text = string.Empty;

        while(RoundInProgress())
        {
            yield return null;
        }
    }

    private IEnumerator RoundEnding()
    {
        cameraControl.enabled = false;
        uiManager.Reset();
        uiManager.enabled = false;

        PlayerManager roundWinner = null;
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            playerManagers[i].DisableControl();
            if (playerManagers[i].Tanks[0].activeSelf)
            {
                roundWinner = playerManagers[i];
                break;
            }
        }
        if (roundWinner != null)
        {
            roundWinner.RoundsWon++;
        }

        if (roundWinner.RoundsWon == TotalRoundsToWin)
        {
            matchEnded = true;
        }

        infoText.text = GetRoundEndText(roundWinner);

        yield return endWait;
    }

    private bool RoundInProgress()
    {
        int playersRemaining = 0;
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            if (playerManagers[i].Tanks.Count > 1 || playerManagers[i].Tanks[0].activeSelf)
            {
                playersRemaining++;
                if (playersRemaining > 1)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private string GetRoundEndText(PlayerManager roundWinner)
    {
        if (roundWinner == null)
        {
            return "DRAW!";
        }
        string text;
        string coloredPlayerText = GetColoredPlayerText(roundWinner.PlayerColor, roundWinner.PlayerNumber);
        if (roundWinner.RoundsWon == TotalRoundsToWin)
        {
            text = coloredPlayerText + " WON THE GAME!";
        }
        else
        {
            text = coloredPlayerText + " WON THE ROUND!";
        }
        text += "\n\n";
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            text += GetColoredPlayerText(playerManagers[i].PlayerColor, playerManagers[i].PlayerNumber) + ": " + playerManagers[i].RoundsWon + "\n";
        }
        return text;
    }

    private string GetColoredPlayerText(Color playerColor, int playerNumber)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(playerColor) + ">PLAYER " + playerNumber + "</color>";
    }
}
