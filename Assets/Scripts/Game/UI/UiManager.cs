using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections;

public class UiManager : MonoBehaviourPunCallbacks
{
    private bool _gameUiIsEnabled = true;
    // TODO - Options in the Esc panel; More info in the Tab panel
    [SerializeField] private GameObject _escPanel;
    [SerializeField] private GameObject _leaveConfirmationModal;
    private bool _leaveYesButtonClicked = false;
    [SerializeField] private GameObject _tabPanel;
    [SerializeField] private Text _currentRoundText;
    private bool _panelsInitialized = false;
    [SerializeField] private Transform _playerInfoListingsContent;
    [SerializeField] private PlayerInfoListing _playerInfoListingPrefab;
    private List<PlayerInfoListing> _playerInfoListings = new List<PlayerInfoListing>();
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Text _infoText;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private LayerMask _visionLayerMask;
    private List<GameObject> _selectedAlliedTanks = new List<GameObject>();
    private GameObject _selectedEnemyTank = null;
    [SerializeField] private GameObject _tankInfoPanel;
    [SerializeField] private Text _tankInfoPanelUsernameText;
    [SerializeField] private Text _tankInfoPanelHealthText;
    [SerializeField] private Text _tankInfoPanelAmmoText;
    [SerializeField] private Text _tankInfoPanelDamageText;
    [SerializeField] private Text _tankInfoPanelArmorText;
    [SerializeField] private Text _tankInfoPanelSpeedText;
    [SerializeField] private Text _tankInfoPanelRangeText;
    [SerializeField] private GameObject _multipleTanksSelectedPanel;
    [SerializeField] private Text _multipleTanksSelectedPanelUsernameText;
    [SerializeField] private Text _numTanksSelectedText;
    [SerializeField] private GameObject _minimapPanel;
    private const int _maxGameLogs = 4;
    [SerializeField] private Text[] _gameLogsTexts = new Text[_maxGameLogs];
    [SerializeField] private Color _gameLogsColor;
    private (PlayerInfo, PlayerInfo)[] _gameLogsPlayers = new (PlayerInfo, PlayerInfo)[_maxGameLogs];
    private const float _gameLogsAlphaDecreaseStep = 0.1f;
    private IEnumerator _updateGameLogsCoroutine = null;
    [SerializeField] private GameObject[] _placeholderAbilityPanels = new GameObject[TankAbilities.MaxAbilities];
    [SerializeField] private GameObject[] _activeAbilityPanels = new GameObject[TankAbilities.MaxAbilities];
    [SerializeField] private RawImage[] _abilityIcons = new RawImage[TankAbilities.MaxAbilities];
    [SerializeField] private Texture _tripleShellsAbilityIcon;
    [SerializeField] private Texture _deflectShellsAbilityIcon;
    [SerializeField] private Texture _laserBeamAbilityIcon;
    [SerializeField] private Texture _mineAbilityIcon;
    private Dictionary<AbilityType, Texture> _abilityIconsTextures = new Dictionary<AbilityType, Texture>();
    [SerializeField] private Color _abilityNotActiveColor;
    [SerializeField] private Color _abilityActiveColor;
    public static readonly float AbilityPanelShrinkTime = 1f;

    public static event Action<bool> EscPanelToggledEvent;

    // TODO - Have a set of predefined screen resolutions; make sure ability panels don't overlap with the tank info panel or the minimap


    private void Awake()
    {
        GameManager.RoundStartingEvent += OnRoundStarting;
        GameManager.RoundPlayingEvent += OnRoundPlaying;
        GameManager.RoundEndingEvent += OnRoundEnding;
        TankHealth.AlliedTankGotDestroyedEvent += OnAlliedTankGotDestroyed;
        GameManager.PlayerGotDefeatedEvent += OnPlayerGotDefeated;
        GameManager.PlayerDisconnectedEvent += OnPlayerDisconnect;

        _abilityIconsTextures.Add(AbilityType.TripleShells, _tripleShellsAbilityIcon);
        _abilityIconsTextures.Add(AbilityType.DeflectShells, _deflectShellsAbilityIcon);
        _abilityIconsTextures.Add(AbilityType.LaserBeam, _laserBeamAbilityIcon);
        _abilityIconsTextures.Add(AbilityType.Mine, _mineAbilityIcon);
    }

    private void OnDestroy()
    {
        GameManager.RoundStartingEvent -= OnRoundStarting;
        GameManager.RoundPlayingEvent -= OnRoundPlaying;
        GameManager.RoundEndingEvent -= OnRoundEnding;
        TankHealth.AlliedTankGotDestroyedEvent -= OnAlliedTankGotDestroyed;
        GameManager.PlayerGotDefeatedEvent -= OnPlayerGotDefeated;
        GameManager.PlayerDisconnectedEvent -= OnPlayerDisconnect;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleEscPanel();
        }
        else if (!_escPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _tabPanel.SetActive(true);
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                _tabPanel.SetActive(false);
            }
            if (_gameUiIsEnabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    UpdateSelectedTanks();
                    return;
                }
            }
        }
        UpdateTankInfoPanel(false);
        UpdateAbilityPanels(false);
    }

    private void ToggleEscPanel()
    {
        if (_escPanel.activeSelf)
        {
            _leaveConfirmationModal.SetActive(false);
            if (Input.GetKey(KeyCode.Tab))
            {
                _tabPanel.SetActive(true);
            }
        }
        else
        {
            _tabPanel.SetActive(false);
        }
        _escPanel.SetActive(!_escPanel.activeSelf);
        EscPanelToggledEvent?.Invoke(_escPanel.activeSelf);
    }

    public void LeaveRoom()
    {
        if (_leaveYesButtonClicked)
        {
            return;
        }
        _leaveYesButtonClicked = true;
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    private void SetTankSelectionRingEnabled(GameObject tank, bool allied, bool enabled)
    {
        tank.transform.Find(allied ? "AlliedSelectionRing" : "EnemySelectionRing").gameObject.SetActive(enabled);
    }

    private void DeselectEnemyTank()
    {
        if (_selectedEnemyTank != null)
        {
            SetTankSelectionRingEnabled(_selectedEnemyTank, false, false);
        }
        _selectedEnemyTank = null;
    }

    private void DeselectAlliedTanks()
    {
        for (int i = _selectedAlliedTanks.Count - 1; i >= 0; --i)
        {
            if (_selectedAlliedTanks[i] != null)
            {
                SetTankSelectionRingEnabled(_selectedAlliedTanks[i], true, false);
                _selectedAlliedTanks[i].GetComponent<TankInfo>().IsSelected = false;
            }
        }
        _selectedAlliedTanks.Clear();
    }

    private void UpdateSelectedTanks()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _tanksLayerMask, QueryTriggerInteraction.Ignore))
        {
            GameObject tank = hit.transform.gameObject;
            TankInfo tankInfo = tank.GetComponent<TankInfo>();
            if (tankInfo.ActorNumber == _gameManager.ActorNumber)
            {
                // Player selected an allied tank
                DeselectEnemyTank();
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    // Player might have selected multiple allied tanks
                    bool selectedNewTank = true;
                    for (int i = 0; i < _selectedAlliedTanks.Count; ++i)
                    {
                        if (ReferenceEquals(tank, _selectedAlliedTanks[i]))
                        {
                            selectedNewTank = false;
                            // Player deselected one of the selected tanks
                            SetTankSelectionRingEnabled(tank, true, false);
                            tankInfo.IsSelected = false;
                            _selectedAlliedTanks.RemoveAt(i);
                            break;
                        }
                    }
                    if (selectedNewTank)
                    {
                        // Player selected one more tank
                        SetTankSelectionRingEnabled(tank, true, true);
                        tankInfo.IsSelected = true;
                        _selectedAlliedTanks.Add(tank);
                    }
                }
                else
                {
                    // Player selected a single allied tank
                    DeselectAlliedTanks();
                    SetTankSelectionRingEnabled(tank, true, true);
                    tankInfo.IsSelected = true;
                    _selectedAlliedTanks.Add(tank);
                }
            }
            else
            {
                SphereCollider sphere = tank.GetComponent<SphereCollider>();
                if (Physics.OverlapSphere(sphere.transform.position, sphere.radius, _visionLayerMask, QueryTriggerInteraction.Collide).Length > 0)
                {
                    // Player selected a (visible) enemy tank
                    DeselectAlliedTanks();
                    if (!ReferenceEquals(tank, _selectedEnemyTank))
                    {
                        DeselectEnemyTank();
                        SetTankSelectionRingEnabled(tank, false, true);
                        _selectedEnemyTank = tank;
                    }
                }
                else
                {
                    // Player clicked on an enemy tank that is not visible
                    DeselectAlliedTanks();
                    DeselectEnemyTank();
                }
            }
        }
        else
        {            
            // Left-clicking  outside of any tank deselects the currently selected tanks (if any)
            DeselectAlliedTanks();
            DeselectEnemyTank();
        }
        UpdateTankInfoPanel(true);
        UpdateAbilityPanels(true);
    }

    private void UpdateTankInfoPanel(bool tankSelectionUpdated)
    {
        if (!tankSelectionUpdated)
        {
            if (_selectedAlliedTanks.Count == 1)
            {
                if (_selectedAlliedTanks[0] == null)
                {
                    _selectedAlliedTanks.Clear();
                    _tankInfoPanel.SetActive(false);
                }
                else
                {
                    SetTankInfoPanelTexts(_selectedAlliedTanks[0].GetComponent<TankInfo>());
                }
            }
            else if (!ReferenceEquals(_selectedEnemyTank, null))
            {
                if (_selectedEnemyTank == null)
                {
                    _selectedEnemyTank = null;
                    _tankInfoPanel.SetActive(false);
                }
                else
                {
                    SetTankInfoPanelTexts(_selectedEnemyTank.GetComponent<TankInfo>());
                }
            }
            return;
        }
        if (_selectedAlliedTanks.Count > 1)
        {
            _tankInfoPanel.SetActive(false);
            _multipleTanksSelectedPanel.SetActive(true);
            _numTanksSelectedText.text = $"{_selectedAlliedTanks.Count} tanks selected";
        }
        else
        {
            _multipleTanksSelectedPanel.SetActive(false);
            if (_selectedAlliedTanks.Count == 1)
            {
                _tankInfoPanel.SetActive(true);
                SetTankInfoPanelTexts(_selectedAlliedTanks[0].GetComponent<TankInfo>());
            }
            else if (_selectedEnemyTank != null)
            {
                _tankInfoPanel.SetActive(true);
                SetTankInfoPanelTexts(_selectedEnemyTank.GetComponent<TankInfo>());
            }
            else
            {
                _tankInfoPanel.SetActive(false);
            }
        }
    }

    private void SetTankInfoPanelTexts(TankInfo tankInfo)
    {
        _tankInfoPanelUsernameText.text = GetColoredText(tankInfo.Username, tankInfo.Color);
        _tankInfoPanelHealthText.text = $"{tankInfo.Health}/{tankInfo.MaxHealth}";
        if (tankInfo.Ammo > CrateAmmo.InfiniteAmmoThreshold)
        {
            _tankInfoPanelAmmoText.text = "∞";
            _tankInfoPanelAmmoText.fontStyle = FontStyle.Bold;
        }
        else
        {
            _tankInfoPanelAmmoText.text = $"{tankInfo.Ammo}";
            _tankInfoPanelAmmoText.fontStyle = FontStyle.Normal;
        }
        _tankInfoPanelDamageText.text = $"{tankInfo.Damage}";
        _tankInfoPanelArmorText.text = $"{tankInfo.Armor}";
        _tankInfoPanelSpeedText.text = $"{tankInfo.Speed}";
        if (tankInfo.Range == CrateRange.InfiniteRange)
        {
            _tankInfoPanelRangeText.text = "∞";
            _tankInfoPanelRangeText.fontStyle = FontStyle.Bold;
        }
        else
        {
            _tankInfoPanelRangeText.text = $"{tankInfo.Range}";
            _tankInfoPanelRangeText.fontStyle = FontStyle.Normal;
        }
    }

    private void UpdateAbilityPanels(bool tankSelectionUpdated)
    {
        if (!tankSelectionUpdated)
        {
            if (_selectedAlliedTanks.Count == 1)
            {
                if (_selectedAlliedTanks[0] == null)
                {
                    _selectedAlliedTanks.Clear();
                    DisableAbilityPanels();
                }
                else
                {
                    UpdateSelectedTankAbilityPanels(_selectedAlliedTanks[0].GetComponent<TankAbilities>());
                }
            }
            return;
        }
        if (_selectedAlliedTanks.Count != 1)
        {
            DisableAbilityPanels();
            return;
        }
        UpdateSelectedTankAbilityPanels(_selectedAlliedTanks[0].GetComponent<TankAbilities>());
    }

    private void UpdateSelectedTankAbilityPanels(TankAbilities tank)
    {
        for (int i = 0; i < TankAbilities.MaxAbilities; ++i)
        {
            _placeholderAbilityPanels[i].SetActive(true);
            Ability ability = tank.Abilities[i];
            if (ability != null)
            {
                _activeAbilityPanels[i].SetActive(true);
                _activeAbilityPanels[i].GetComponent<Image>().color = ability.IsActive ? _abilityActiveColor : _abilityNotActiveColor;
                float scaleReduction = ability.IsActive ?
                    Math.Min(1f, Math.Max(0f, (AbilityPanelShrinkTime - ability.Duration + ability.Timer)) / AbilityPanelShrinkTime) : 0f;
                float scale = 1f - scaleReduction;
                _activeAbilityPanels[i].transform.localScale = new Vector3(scale, scale, 1f);
                _abilityIcons[i].texture = _abilityIconsTextures[ability.Type];
            }
            else
            {
                _activeAbilityPanels[i].SetActive(false);
            }
        }
    }

    private void DisableAbilityPanels()
    {
        foreach (GameObject panel in _placeholderAbilityPanels)
        {
            panel.SetActive(false);
        }
    }

    private void DisableWinnerUi(int actorNumber)
    {
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank") .Where(tank => tank.GetComponent<TankInfo>().ActorNumber == actorNumber).ToArray();
        foreach (GameObject tank in tanks)
        {
            tank.transform.Find("HealthBar").gameObject.SetActive(false);
            tank.transform.Find("OwnerText").gameObject.SetActive(false);
        }
    }

    private void InitializeTabPanel()
    {
        List<PlayerInfo> sortedPlayersInfo = _gameManager.GetSortedPlayersInfo();
        foreach (PlayerInfo playerInfo in sortedPlayersInfo)
        {
            PlayerInfoListing listing = Instantiate(_playerInfoListingPrefab, _playerInfoListingsContent);
            listing.SetPlayerInfo(playerInfo);
            _playerInfoListings.Add(listing);
        }
    }

    private void UpdateTabPanel()
    {
        List<PlayerInfo> sortedPlayersInfo = _gameManager.PlayersInfo.Values.OrderByDescending(info => info.RoundsWon).ToList();
        for (int i = 0; i < sortedPlayersInfo.Count; ++i)
        {
            _playerInfoListings[i].SetPlayerInfo(sortedPlayersInfo[i]);
        }
    }

    private void InitializeMultipleTanksSelectedPanel()
    {
        _multipleTanksSelectedPanelUsernameText.text = GetColoredText(PhotonNetwork.NickName, _gameManager.PlayerManager.PlayerColor);
    }

    private void OnRoundStarting(int round)
    {
        _currentRoundText.text = "Round " + round;
        _infoText.text = "ROUND " + round;
        _gameUiIsEnabled = false;
        if (!_panelsInitialized)
        {
            _panelsInitialized = true;
            InitializeTabPanel();
            InitializeMultipleTanksSelectedPanel();
        }
        _minimapPanel.SetActive(false);
        ClearGameLogs();
    }

    private void OnRoundPlaying()
    {
        _gameUiIsEnabled = true;
        _infoText.text = string.Empty;
        _minimapPanel.SetActive(true);
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        Reset();
        if (roundWinner != null)
        {
            DisableWinnerUi(roundWinner.ActorNumber);
        }
        UpdateTabPanel();
        _infoText.text = GetRoundEndText(roundWinner, isGameWinner);
        _gameUiIsEnabled = false;
        _minimapPanel.SetActive(false);
        // TODO - If game ended, display additional stats?
    }

    private void OnAlliedTankGotDestroyed(GameObject tank, int defeaterPlayerActorNumber)
    {
        if (tank.GetComponent<TankInfo>().IsSelected)
        {
            if (!_selectedAlliedTanks.Remove(tank))
            {
                Debug.LogWarning("[UiManager] Could not remove destroyed tank from the _selectedAlliedTanks list");
            }
            UpdateTankInfoPanel(true);
            UpdateAbilityPanels(true);
        }
    }

    private void OnPlayerGotDefeated(PlayerInfo defeatedPlayer, PlayerInfo defeaterPlayer)
    {
        AddGameLog(defeatedPlayer, defeaterPlayer);
    }

    private void OnPlayerDisconnect(PlayerInfo disconnectedPlayer)
    {
        AddGameLog(disconnectedPlayer);
    }

    private void AddGameLog(PlayerInfo defeatedPlayer, PlayerInfo defeaterPlayer)
    {
        ShiftGameLogs();
        _gameLogsTexts[0].text = GetColoredPlayerText(defeaterPlayer) + " defeated " + GetColoredPlayerText(defeatedPlayer);
        _gameLogsTexts[0].color = _gameLogsColor;
        _gameLogsPlayers[0] = (defeaterPlayer, defeatedPlayer);
        StartUpdatingGameLogs();
    }

    private void AddGameLog(PlayerInfo disconnectedPlayer)
    {
        ShiftGameLogs();
        _gameLogsTexts[0].text = GetColoredPlayerText(disconnectedPlayer) + " disconnected";
        _gameLogsTexts[0].color = _gameLogsColor;
        _gameLogsPlayers[0] = (disconnectedPlayer, null);
        StartUpdatingGameLogs();
    }

    private void ShiftGameLogs()
    {
        for (int i = _maxGameLogs - 1; i > 0; --i)
        {
            _gameLogsTexts[i].text = _gameLogsTexts[i - 1].text;
            _gameLogsTexts[i].color = _gameLogsTexts[i - 1].color;
            _gameLogsPlayers[i] = _gameLogsPlayers[i - 1];
        }
    }

    private void StartUpdatingGameLogs()
    {
        if (_updateGameLogsCoroutine == null)
        {
            _updateGameLogsCoroutine = UpdateGameLogs();
            StartCoroutine(_updateGameLogsCoroutine);
        }
    }

    private IEnumerator UpdateGameLogs()
    {
        bool gameLogsExist;
        while (true)
        {
            gameLogsExist = false;
            for (int i = 0; i < _maxGameLogs; ++i)
            {
                if (_gameLogsTexts[i].text != string.Empty)
                {
                    gameLogsExist = true;
                    Color color = _gameLogsTexts[i].color;
                    float newAlpha = color.a - _gameLogsAlphaDecreaseStep * Time.deltaTime;
                    if (newAlpha <= 0f)
                    {
                        _gameLogsTexts[i].text = string.Empty;
                        _gameLogsPlayers[i] = (null, null);
                    }
                    else
                    {
                        if (_gameLogsPlayers[i].Item2 == null)
                        {
                            _gameLogsTexts[i].text = GetColoredPlayerTextWithTransparency(_gameLogsPlayers[i].Item1, newAlpha) + " disconnected";
                        }
                        else
                        {
                            _gameLogsTexts[i].text = GetColoredPlayerTextWithTransparency(_gameLogsPlayers[i].Item1, newAlpha)
                                + " defeated " + GetColoredPlayerTextWithTransparency(_gameLogsPlayers[i].Item2, newAlpha);
                        }
                        _gameLogsTexts[i].color = new Color(color.r, color.g, color.b, newAlpha);
                    }
                }
            }
            if (gameLogsExist)
            {
                yield return null;
            }
            else
            {
                break;
            }
        }
        _updateGameLogsCoroutine = null;
    }

    private void ClearGameLogs()
    {
        if (_updateGameLogsCoroutine != null)
        {
            StopCoroutine(_updateGameLogsCoroutine);
            _updateGameLogsCoroutine = null;
        }
        for (int i = 0; i < _maxGameLogs; ++i)
        {
            _gameLogsTexts[i].text = string.Empty;
            _gameLogsPlayers[i] = (null, null);
        }
    }

    private void Reset()
    {
        DeselectAlliedTanks();
        DeselectEnemyTank();
        UpdateTankInfoPanel(true);
        UpdateAbilityPanels(true);
    }

    private string GetRoundEndText(PlayerInfo roundWinner, bool isGameWinner)
    {
        if (roundWinner == null)
        {
            return "DRAW!";
        }
        string text;
        string coloredPlayerText = GetColoredPlayerText(roundWinner);
        text = coloredPlayerText + (isGameWinner ? " WON THE GAME!" : " WON THE ROUND!") + "\n\n";
        List<PlayerInfo> sortedPlayersInfo = _gameManager.PlayersInfo.Values.OrderByDescending(info => info.RoundsWon).ToList();
        foreach (PlayerInfo playerInfo in sortedPlayersInfo)
        {
            text += GetColoredPlayerText(playerInfo) + ": " + playerInfo.RoundsWon + "\n";
        }
        return text;
    }

    public static string GetColoredPlayerText(PlayerInfo playerInfo)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(playerInfo.Color) + ">" + playerInfo.Username + "</color>";
    }

    private string GetColoredPlayerTextWithTransparency(PlayerInfo playerInfo, float alpha)
    {
        Color color = new Color(playerInfo.Color.r, playerInfo.Color.g, playerInfo.Color.b, alpha);
        return "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + playerInfo.Username + "</color>";
    }

    private string GetColoredText(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }
}
