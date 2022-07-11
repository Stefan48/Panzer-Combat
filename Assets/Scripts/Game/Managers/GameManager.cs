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
    [SerializeField] private List<Transform> _crateSpawnPoints = new List<Transform>();
    [SerializeField] private readonly int _initialTankCount = 1; // TODO - Creator of the room should set this in the UI
    [SerializeField] private readonly int _totalRoundsToWin = 2; // TODO - Creator of the room should set this in the UI
    public Dictionary<Player, PlayerInfo> PlayersInfo = new Dictionary<Player, PlayerInfo>();
    [SerializeField] private GameObject _tankPrefab;
    [SerializeField] private GameObject _crateAbilityPrefab;
    [SerializeField] private GameObject _crateAmmunitionPrefab;
    [SerializeField] private GameObject _crateArmorPrefab;
    [SerializeField] private GameObject _crateDamagePrefab;
    [SerializeField] private GameObject _crateMaxHpPrefab;
    [SerializeField] private GameObject _crateRangePrefab;
    [SerializeField] private GameObject _crateRestoreHpPrefab;
    [SerializeField] private GameObject _crateSpeedPrefab;
    [SerializeField] private GameObject _crateTankPrefab;
    private Dictionary<CrateType, GameObject> _cratePrefabs = new Dictionary<CrateType, GameObject>();
    public PlayerManager PlayerManager { get; private set; } = null;
    private const float _startDelay = 2f;
    private const float _endDelay = 2f;
    private const float _potentialDrawDelay = 0.3f;
    // TODO - Creator of the room should set the frequency of the crates in the UI
    private const float _crateSpawnDelay = 10f;
    private const float _crateLifetime = 7f;
    private readonly WaitForSeconds _startWait = new WaitForSeconds(_startDelay);
    private readonly WaitForSeconds _endWait = new WaitForSeconds(_endDelay);
    private readonly WaitForSeconds _potentialDrawWait = new WaitForSeconds(_potentialDrawDelay);
    private readonly WaitForSeconds _crateSpawnWait = new WaitForSeconds(_crateSpawnDelay);
    private IEnumerator _crateSpawnCoroutine = null;
    private int _currentRound = 0;
    private List<Player> _playersRemaining = new List<Player>();
    private bool _roundInProgress = false;
    private bool _gameEnded = false;

    public static event Action<int> RoundStartingEvent;
    public static event Action RoundPlayingEvent;
    public static event Action<PlayerInfo, bool> RoundEndingEvent;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        ActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        _players = PhotonNetwork.PlayerList;

        _cratePrefabs.Add(CrateType.Ability, _crateAbilityPrefab);
        _cratePrefabs.Add(CrateType.Ammunition, _crateAmmunitionPrefab);
        _cratePrefabs.Add(CrateType.Armor, _crateArmorPrefab);
        _cratePrefabs.Add(CrateType.Damage, _crateDamagePrefab);
        _cratePrefabs.Add(CrateType.MaxHp, _crateMaxHpPrefab);
        _cratePrefabs.Add(CrateType.Range, _crateRangePrefab);
        _cratePrefabs.Add(CrateType.RestoreHp, _crateRestoreHpPrefab);
        _cratePrefabs.Add(CrateType.Speed, _crateSpeedPrefab);
        _cratePrefabs.Add(CrateType.Tank, _crateTankPrefab);
    }

    private void OnDestroy()
    {
        if (PlayerManager != null)
        {
            PlayerManager.UnsubscribeFromEvents();
        }
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
        if (_gameEnded)
        {
            // The game ended due to disconnects
            return;
        }
        StartCoroutine(StartRound());
        StartSpawningCrates();
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
        if (_gameEnded)
        {
            // The game ended due to disconnects
            yield break;
        }
        RoundPlayingEvent?.Invoke();
        PlayerManager.SetControlEnabled(true);
    }

    private void StartSpawningCrates()
    {
        _crateSpawnCoroutine = SpawnCrates();
        StartCoroutine(_crateSpawnCoroutine);
    }

    private void StopSpawningCrates()
    {
        if (_crateSpawnCoroutine != null)
        {
            StopCoroutine(_crateSpawnCoroutine);
            _crateSpawnCoroutine = null;
        }
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
        if (_gameEnded)
        {
            // The game ended due to disconnects
            return;
        }
        StartCoroutine(EndRound());
        StopSpawningCrates();
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
        if(PhotonNetwork.IsMasterClient)
        {
            _photonView.RPC("RPC_NewRound", RpcTarget.AllViaServer);
        }
    }

    private CrateType GetRandomCrateType()
    {
        int roll = UnityEngine.Random.Range(0, 100);
        // TODO - Probabilities
        if (roll < 100)
        {
            return CrateType.RestoreHp;
        }
        return CrateType.Tank;
    }

    private IEnumerator SpawnCrates()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // TODO - Pooling? (might require too much overhead since every client would need references to all the pooled crates; also more RPCs)
            foreach (Transform spawnPoint in _crateSpawnPoints)
            {
                GameObject cratePrefab = _cratePrefabs[GetRandomCrateType()];
                Quaternion crateRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                GameObject crate = PhotonNetwork.Instantiate(cratePrefab.name, spawnPoint.position, crateRotation);
                crate.GetComponent<Crate>().Init(_crateLifetime);
            }
        }
        yield return _crateSpawnWait;
        StartSpawningCrates();
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
                _roundInProgress = false;
                RoundEndingEvent?.Invoke(PlayersInfo[_players[0]], true);
                StopSpawningCrates();
            }
        }
        else if (_roundInProgress)
        {
            // Call the RPC only locally, since all clients receive the OnPlayerLeftRoom event anyway
            RPC_PlayerLost(new PhotonMessageInfo(player, 0, null));
        }
    }
}
