using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    private PhotonView _photonView;
    public int ActorNumber { get; private set; }
    private Player[] _players;
    public int NumberOfPlayers { get; private set; }
    [SerializeField] private List<Color> _availablePlayerColors = new List<Color>();
    [SerializeField] private List<Transform> _availablePlayerSpawnPoints = new List<Transform>();
    [SerializeField] private readonly int _initialTankCount = 1; // TODO - Creator of the room should set this in the UI
    [SerializeField] private readonly int _totalRoundsToWin = 2; // TODO - Creator of the room should set this in the UI
    public List<PlayerInfo> PlayersInfo = new List<PlayerInfo>();
    [SerializeField] private GameObject _tankPrefab;
    public PlayerManager PlayerManager { get; private set; } = null;
    private const float _startDelay = 2f;
    private const float _endDelay = 2f;
    private const float _potentialDrawDelay = 0.3f;
    private readonly WaitForSeconds _startWait = new WaitForSeconds(_startDelay);
    private readonly WaitForSeconds _endWait = new WaitForSeconds(_endDelay);
    private readonly WaitForSeconds _potentialDrawWait = new WaitForSeconds(_potentialDrawDelay);
    private int _currentRound = 0;
    private List<int> _playersRemaining = new List<int>();
    private bool _roundInProgress = false;
    private bool _gameEnded = false;

    public event Action<int> RoundStartingEvent;
    public event Action RoundPlayingEvent;
    public event Action<PlayerInfo, bool> RoundEndingEvent;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        ActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        _players = PhotonNetwork.PlayerList;
        NumberOfPlayers = _players.Length;
    }

    private void OnDestroy()
    {
        PlayerManager.UnsubscribeFromEvents();
        PlayerManager = null;
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
    }

    private void InitializePlayersInfo()
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            int index = UnityEngine.Random.Range(0, _availablePlayerColors.Count);
            Color color = _availablePlayerColors[index];
            _availablePlayerColors.RemoveAt(index);
            PlayersInfo.Add(new PlayerInfo(_players[i].ActorNumber, _players[i].NickName, color));
        }
        _photonView.RPC("RPC_SetPlayersInfo", RpcTarget.Others, PlayersInfo.Select(info => new Vector3(info.Color.r, info.Color.g, info.Color.b)).ToArray());
    }

    [PunRPC]
    private void RPC_SetPlayersInfo(Vector3[] colors)
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            PlayersInfo.Add(new PlayerInfo(_players[i].ActorNumber, _players[i].NickName, new Color(colors[i].x, colors[i].y, colors[i].z)));
        }
    }

    private void InitializePlayersManagers()
    {
        for (int i = 0; i < NumberOfPlayers; ++i)
        {
            int index = UnityEngine.Random.Range(0, _availablePlayerSpawnPoints.Count);
            Transform spawnPoint = _availablePlayerSpawnPoints[index];
            _availablePlayerSpawnPoints.RemoveAt(index);
            _photonView.RPC("RPC_SetPlayerManager", _players[i], spawnPoint.position);
        }
    }

    [PunRPC]
    private void RPC_SetPlayerManager(Vector3 spawnPosition)
    {
        // TODO - Error => Stop using _players[i].ActorNumber?
        Debug.Log("PlayersInfo.Count=" + PlayersInfo.Count + "; ActorNumber=" + ActorNumber); // Output: 2 3

        // TODO - Also, double clicking some (?) buttons results in errors
        // TODO - Also, the camera should follow the selected tank(s)?

        PlayerManager = new PlayerManager(this, PlayersInfo[ActorNumber-1].Color, spawnPosition, _tankPrefab);
    }

    [PunRPC]
    private void RPC_NewRound()
    {
        StartCoroutine(StartRound());
    }

    private IEnumerator StartRound()
    {
        _currentRound++;
        _playersRemaining = _players.Select(p => p.ActorNumber).ToList();
        _roundInProgress = true;
        PlayerManager.Reset();
        PlayerManager.Setup();
        PlayerManager.SetControlEnabled(false);
        RoundStartingEvent?.Invoke(_currentRound);
        yield return _startWait;
        RoundPlayingEvent?.Invoke();
        PlayerManager.SetControlEnabled(true);
    }

    public void LocalPlayerLost()
    {
        _photonView.RPC("RPC_PlayerLost", RpcTarget.AllViaServer);
    }

    [PunRPC]
    private void RPC_PlayerLost(PhotonMessageInfo info)
    {
        _playersRemaining.Remove(info.Sender.ActorNumber);
        // There should be exactly 1 player remaining so we don't start the coroutine more than once
        if (PhotonNetwork.IsMasterClient && _playersRemaining.Count == 1)
        {
            StartCoroutine(AnnounceRoundEnd());
        }
    }

    private IEnumerator AnnounceRoundEnd()
    {
        yield return _potentialDrawWait;
        _photonView.RPC("RPC_RoundEnded", RpcTarget.AllViaServer);
    }

    [PunRPC]
    private void RPC_RoundEnded()
    {
        StartCoroutine(EndRound());
    }

    private IEnumerator EndRound()
    {
        _roundInProgress = false;
        PlayerManager.SetControlEnabled(false);
        PlayerInfo roundWinner = _playersRemaining.Count > 0 ? PlayersInfo[_playersRemaining[0] - 1] : null;
        if (roundWinner != null)
        {
            roundWinner.WonRound();
            _gameEnded = (roundWinner.RoundsWon == _totalRoundsToWin);
        }
        RoundEndingEvent?.Invoke(roundWinner, _gameEnded);
        yield return _endWait;
        if (!_gameEnded)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC("RPC_NewRound", RpcTarget.AllViaServer);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player player)
    {
        _players = _players.Where(p => p != player).ToArray();
        if (_players.Length == 1)
        {
            // If there's only 1 player left in the room (and the game had not ended already), end the game
            if (!_gameEnded)
            {
                _gameEnded = true;
                RoundEndingEvent?.Invoke(PlayersInfo[_players[0].ActorNumber - 1], true);
            }
        }
        else if (_roundInProgress)
        {
            RPC_PlayerLost(new PhotonMessageInfo(player, 0, null));
        }
    }
}
