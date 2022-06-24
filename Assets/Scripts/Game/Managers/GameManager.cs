using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int NumberOfPlayers { get; private set; } = 2;
    [SerializeField] private Color[] playerColors;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private GameObject tankPrefab;
    public PlayerManager[] playerManagers { get; private set; } = new PlayerManager[2]; // TODO
    public int TotalRoundsToWin { get; private set; } = 2;
    [SerializeField] public Transform[] SpawnPoints { get; private set; }
    private readonly float startDelay = 1f;
    private readonly float endDelay = 1f;
    private WaitForSeconds startWait;
    private WaitForSeconds endWait;
    private int currentRound;
    private bool matchEnded;


    public event Action<int> RoundStartingEvent;
    public event Action RoundPlayingEvent;
    public event Action<PlayerManager, bool> RoundEndingEvent;


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
        currentRound++;
        RoundStartingEvent?.Invoke(currentRound);


        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            playerManagers[i].Reset();
            playerManagers[i].DisableControl();
        }

        // TODO - Set camera initial position
        yield return startWait;
    }

    private IEnumerator RoundPlaying()
    {
        RoundPlayingEvent?.Invoke();

        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            playerManagers[i].EnableControl();
        }

        while(RoundInProgress())
        {
            yield return null;
        }
    }

    private IEnumerator RoundEnding()
    {
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

        RoundEndingEvent?.Invoke(roundWinner, matchEnded);

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
}
