using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class UiManager : MonoBehaviourPunCallbacks
{
    private bool _gameUiIsEnabled = true;
    // TODO - Options in the Esc panel; More info in the Tab panel
    [SerializeField] private GameObject _escPanel;
    [SerializeField] private GameObject _leaveConfirmationModal;
    private bool _leaveYesButtonClicked = false;
    [SerializeField] private GameObject _tabPanel;
    [SerializeField] private Text _currentRoundText;
    private bool _tabPanelInitialized = false;
    [SerializeField] private Transform _playerInfoListingsContent;
    [SerializeField] private PlayerInfoListing _playerInfoListingPrefab;
    private List<PlayerInfoListing> _playerInfoListings = new List<PlayerInfoListing>();
    // TODO - Console for game events

    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Text _infoText;
    [SerializeField] private LayerMask _tanksLayerMask;
    private List<GameObject> _selectedAlliedTanks = new List<GameObject>();
    private GameObject _selectedEnemyTank = null;

    public static event Action<bool> EscPanelToggledEvent;


    private void Awake()
    {
        _gameManager.RoundStartingEvent += OnRoundStarting;
        _gameManager.RoundPlayingEvent += OnRoundPlaying;
        _gameManager.RoundEndingEvent += OnRoundEnding;
    }

    private void OnDestroy()
    {
        _gameManager.RoundStartingEvent -= OnRoundStarting;
        _gameManager.RoundPlayingEvent -= OnRoundPlaying;
        _gameManager.RoundEndingEvent -= OnRoundEnding;
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
                }
            }
        }
    }

    private void ToggleEscPanel()
    {
        if (_escPanel.activeSelf && _leaveConfirmationModal.activeSelf)
        {
            _leaveConfirmationModal.SetActive(false);
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
            if (_selectedAlliedTanks[i] == null)
            {
                _selectedAlliedTanks.RemoveAt(i);
            }
            else
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
        // TODO - No hit if user clicked on UI
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
                // Player selected an enemy tank
                DeselectAlliedTanks();
                SetTankSelectionRingEnabled(tank, false, true);
                _selectedEnemyTank = tank;
            }
        }
        else
        {
            // Left-clicking  outside of any tank deselects the currently selected tanks (if any)
            DeselectAlliedTanks();
            DeselectEnemyTank();
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

    private void OnRoundStarting(int round)
    {
        _currentRoundText.text = "Round " + round;
        _infoText.text = "ROUND " + round;
        _gameUiIsEnabled = false;
        if (!_tabPanelInitialized)
        {
            _tabPanelInitialized = true;
            InitializeTabPanel();
        }
    }

    private void OnRoundPlaying()
    {
        _gameUiIsEnabled = true;
        _infoText.text = string.Empty;
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
        // TODO - If game ended, display additional stats?
    }

    private void Reset()
    {
        DeselectAlliedTanks();
        DeselectEnemyTank();
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
}
