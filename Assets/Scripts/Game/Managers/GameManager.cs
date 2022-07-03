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
    [SerializeField] private List<Color> _availablePlayerColors = new List<Color>();
    [SerializeField] private List<Transform> _availablePlayerSpawnPoints = new List<Transform>();
    [SerializeField] private readonly int _initialTankCount = 1; // TODO - Creator of the room should set this in the UI
    [SerializeField] private readonly int _totalRoundsToWin = 2; // TODO - Creator of the room should set this in the UI
    public Dictionary<Player, PlayerInfo> PlayersInfo = new Dictionary<Player, PlayerInfo>();
    [SerializeField] private GameObject _tankPrefab;
    public PlayerManager PlayerManager { get; private set; } = null;
    private const float _startDelay = 2f;
    private const float _endDelay = 2f;
    private const float _potentialDrawDelay = 0.3f;
    private readonly WaitForSeconds _startWait = new WaitForSeconds(_startDelay);
    private readonly WaitForSeconds _endWait = new WaitForSeconds(_endDelay);
    private readonly WaitForSeconds _potentialDrawWait = new WaitForSeconds(_potentialDrawDelay);
    private int _currentRound = 0;
    private List<Player> _playersRemaining = new List<Player>();
    private bool _roundInProgress = false;
    private bool _gameEnded = false;

    public event Action<int> RoundStartingEvent;
    public event Action RoundPlayingEvent;
    public event Action<PlayerInfo, bool> RoundEndingEvent;

    // TODO - The camera should follow the selected tanks, switching the followed tank when Space is pressed


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        ActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        _players = PhotonNetwork.PlayerList;
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

    public List<PlayerInfo> GetSortedPlayersInfo()
    {
        return PlayersInfo.Values.OrderBy(info => info.ActorNumber).ToList();
    }

    private void InitializePlayersInfo()
    {
        foreach (Player player in _players)
        {
            int index = UnityEngine.Random.Range(0, _availablePlayerColors.Count);
            Color color = _availablePlayerColors[index];
            _availablePlayerColors.RemoveAt(index);
            PlayersInfo.Add(player, new PlayerInfo(player.ActorNumber, player.NickName, color));
        }
        _photonView.RPC("RPC_SetPlayersInfo", RpcTarget.Others,
            GetSortedPlayersInfo().Select(info => new Vector3(info.Color.r, info.Color.g, info.Color.b)).ToArray());
    }

    [PunRPC]
    private void RPC_SetPlayersInfo(Vector3[] colors)
    {
        for (int i = 0; i < _players.Length; ++i)
        {
            PlayersInfo.Add(_players[i], new PlayerInfo(_players[i].ActorNumber, _players[i].NickName, new Color(colors[i].x, colors[i].y, colors[i].z)));
        }
    }

    private void InitializePlayersManagers()
    {
        foreach (Player player in _players)
        {
            int index = UnityEngine.Random.Range(0, _availablePlayerSpawnPoints.Count);
            Transform spawnPoint = _availablePlayerSpawnPoints[index];
            _availablePlayerSpawnPoints.RemoveAt(index);
            _photonView.RPC("RPC_SetPlayerManager", player, spawnPoint.position);
        }
    }

    [PunRPC]
    private void RPC_SetPlayerManager(Vector3 spawnPosition)
    {
        PlayerManager = new PlayerManager(this, PlayersInfo[PhotonNetwork.LocalPlayer].Color, spawnPosition, _tankPrefab);
    }

    [PunRPC]
    private void RPC_NewRound()
    {
        StartCoroutine(StartRound());
    }

    private IEnumerator StartRound()
    {
        _currentRound++;
        _playersRemaining = _players.ToList();
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
        _playersRemaining.Remove(info.Sender);
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
        PlayerInfo roundWinner = _playersRemaining.Count > 0 ? PlayersInfo[_playersRemaining[0]] : null;
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
                RoundEndingEvent?.Invoke(PlayersInfo[_players[0]], true);
            }
        }
        else if (_roundInProgress)
        {
            RPC_PlayerLost(new PhotonMessageInfo(player, 0, null));
        }
    }
}
