using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private PhotonView _photonView;
    private int _actorNumber;
    private Player[] _players;
    public int NumberOfPlayers { get; private set; }
    [SerializeField] private List<Color> _availablePlayerColors = new List<Color>();
    [SerializeField] private List<Transform> _availablePlayerSpawnPoints = new List<Transform>();
    [SerializeField] private readonly int _initialTankCount = 1; // TODO - Creator of the room should set this in the UI
    [SerializeField] private readonly int _totalRoundsToWin = 2; // TODO - Creator of the room should set this in the UI
    public List<PlayerInfo> PlayersInfo = new List<PlayerInfo>();
    [SerializeField] private GameObject _tankPrefab;
    private PlayerManager _playerManager;
    private const float _startDelay = 1f;
    private const float _endDelay = 1f;
    private readonly WaitForSeconds _startWait = new WaitForSeconds(_startDelay);
    private readonly WaitForSeconds _endWait = new WaitForSeconds(_endDelay);
    private int _currentRound = 0;
    private int _playersRemaining;

    public event Action<int> RoundStartingEvent;
    public event Action RoundPlayingEvent;
    public event Action<PlayerInfo, bool> RoundEndingEvent;



    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        _players = PhotonNetwork.PlayerList;
        NumberOfPlayers = _players.Length;
    }

    private void Start()
    {
        //Physics.defaultMaxDepenetrationVelocity = float.PositiveInfinity;

        if (PhotonNetwork.IsMasterClient)
        {
            InitializePlayersInfo();
            InitializePlayersManagers();
            _photonView.RPC("RPC_NewRound", RpcTarget.AllViaServer);
        }

        //_playerManager.Setup(); // This gets called before InitializePlayersManagers finished
    }

    private void InitializePlayersInfo()
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            int index = UnityEngine.Random.Range(0, _availablePlayerColors.Count - 1);
            Color color = _availablePlayerColors[index];
            _availablePlayerColors.RemoveAt(index);
            PlayersInfo.Add(new PlayerInfo(_players[i].ActorNumber, color));
        }
        _photonView.RPC("RPC_SetPlayersInfo", RpcTarget.Others, PlayersInfo.Select(info => new Vector3(info.Color.r, info.Color.g, info.Color.b)).ToArray());
    }

    [PunRPC]
    private void RPC_SetPlayersInfo(Vector3[] colors)
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            PlayersInfo.Add(new PlayerInfo(_players[i].ActorNumber, new Color(colors[i].x, colors[i].y, colors[i].z)));
        }
    }

    private void InitializePlayersManagers()
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            int index = UnityEngine.Random.Range(0, _availablePlayerSpawnPoints.Count - 1);
            Transform spawnPoint = _availablePlayerSpawnPoints[index];
            _availablePlayerSpawnPoints.RemoveAt(index);
            _photonView.RPC("RPC_SetPlayerManager", _players[i], spawnPoint.position);
            
        }
    }

    [PunRPC]
    private void RPC_SetPlayerManager(Vector3 spawnPosition)
    {
        _playerManager = new PlayerManager(_actorNumber, PlayersInfo[_actorNumber-1].Color, spawnPosition, _tankPrefab);
    }

    [PunRPC]
    private void RPC_NewRound()
    {
        StartCoroutine(StartRound());
    }

    private IEnumerator StartRound()
    {
        _currentRound++;
        _playerManager.Reset();
        _playerManager.Setup();
        _playerManager.SetControlEnabled(false);
        RoundStartingEvent?.Invoke(_currentRound);
        yield return _startWait;
        RoundPlayingEvent?.Invoke();
        _playerManager.SetControlEnabled(true);
    }

    // TODO - Callbacks for OnPlayerJoin/Leave


#if false
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
            // TODO - Display game end modal
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

#endif
}
