using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Text _infoText;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private GameObject _selectionRingPrefab;
    private const int _numAlliedSelectionRingsPooled = 10;
    private List<GameObject> _alliedSelectionRings = new List<GameObject>();
    private GameObject _enemySelectionRing;
    private List<GameObject> _selectedAlliedTanks = new List<GameObject>();
    private GameObject _selectedEnemyTank = null;


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

    private void Start()
    {
        // Pool selection rings
        for (int i = 0; i < _numAlliedSelectionRingsPooled; ++i)
        {
            GameObject ring = Instantiate(_selectionRingPrefab);
            ring.name = "AlliedSelectionRing" + (i + 1);
            ring.SetActive(false);
            _alliedSelectionRings.Add(ring);
        }
        _enemySelectionRing = Instantiate(_selectionRingPrefab);
        _enemySelectionRing.transform.Find("Slider").Find("Fill Area").Find("Fill").GetComponent<Image>().color = new Color32(255, 0, 0, 180);
        _enemySelectionRing.name = "EnemySelectionRing";
        _enemySelectionRing.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UpdateSelectedTanks();
        }
    }

    private void FixedUpdate()
    {
        // This is called in FixedUpdate so the rings get synced with the players' movement
        UpdateSelectionRingsPositions();
    }

    private void DeselectEnemyTank()
    {
        if (_selectedEnemyTank != null)
        {
            _selectedEnemyTank = null;
            _enemySelectionRing.SetActive(false);
        }
    }

    private void DeselectAlliedTanks()
    {
        if (_selectedAlliedTanks.Count > 0)
        {
            for (int i = 0; i < _selectedAlliedTanks.Count; ++i)
            {
                _alliedSelectionRings[i].SetActive(false);
                _selectedAlliedTanks[i].GetComponent<TankInfo>().IsSelected = false;
            }
            _selectedAlliedTanks.Clear();
        }
    }

    private void UpdateSelectedTanks()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // TODO - No hit if user clicked on UI
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _tanksLayerMask, QueryTriggerInteraction.Ignore))
        {
            GameObject tank = hit.transform.gameObject;
            TankInfo tankInfo = tank.GetComponent<TankInfo>();
            if (tankInfo.PlayerNumber == _gameManager.ActorNumber)
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
                            tankInfo.IsSelected = false;
                            _alliedSelectionRings[_selectedAlliedTanks.Count - 1].SetActive(false);
                            _selectedAlliedTanks.RemoveAt(i);
                            break;
                        }
                    }
                    if (selectedNewTank)
                    {
                        tankInfo.IsSelected = true;
                        _selectedAlliedTanks.Add(tank);
                        _alliedSelectionRings[_selectedAlliedTanks.Count - 1].SetActive(true);
                    }
                }
                else
                {
                    // Player selected a single allied tank
                    DeselectAlliedTanks();
                    tankInfo.IsSelected = true;
                    _selectedAlliedTanks.Add(tank);
                    _alliedSelectionRings[0].SetActive(true);
                }
            }
            else
            {
                // Player selected an enemy tank
                DeselectAlliedTanks();
                _selectedEnemyTank = tank;
                _enemySelectionRing.SetActive(true);
            }
        }
        else
        {
            // Left-clicking  outside of any tank deselects the currently selected tanks (if any)
            DeselectAlliedTanks();
            DeselectEnemyTank();
        }
    }

    private void UpdateSelectionRingsPositions()
    {
        if (_selectedAlliedTanks.Count > 0)
        {
            for (int i = 0; i < _selectedAlliedTanks.Count; ++i)
            {
                // TODO - Null error?
                if (!_selectedAlliedTanks[i].activeSelf)
                {
                    // Selected allied tank got destroyed
                    _alliedSelectionRings[_selectedAlliedTanks.Count - 1].SetActive(false);
                    _selectedAlliedTanks.RemoveAt(i);
                    i--;
                }
                else
                {
                    Vector3 tankPosition = _selectedAlliedTanks[i].transform.position;
                    _alliedSelectionRings[i].transform.position = new Vector3(tankPosition.x, _alliedSelectionRings[i].transform.position.y, tankPosition.z);
                }
            }
        }
        else if (_selectedEnemyTank != null)
        {
            if (!_selectedEnemyTank.activeSelf)
            {
                // The selected enemy tank got destroyed
                _selectedEnemyTank = null;
                _enemySelectionRing.SetActive(false);
            }
            else
            {
                Vector3 tankPosition = _selectedEnemyTank.transform.position;
                _enemySelectionRing.transform.position = new Vector3(tankPosition.x, _enemySelectionRing.transform.position.y, tankPosition.z);
            }
        }
    }

    private void OnRoundStarting(int round)
    {
        _infoText.text = "ROUND " + round;
        enabled = false;
    }

    private void OnRoundPlaying()
    {
        enabled = true;
        _infoText.text = string.Empty;
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        Reset();
        _infoText.text = GetRoundEndText(roundWinner, isGameWinner);
        enabled = false;
    }

    private void Reset()
    {
        for (int i = 0; i < _selectedAlliedTanks.Count; ++i)
        {
            //_selectedAlliedTanks[i].GetComponent<TankInfo>().IsSelected = false;
            _alliedSelectionRings[i].SetActive(false);
        }
        _selectedAlliedTanks.Clear();
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
        for (int i = 0; i < _gameManager.NumberOfPlayers; ++i)
        {
            text += GetColoredPlayerText(_gameManager.PlayersInfo[i]) + ": " + _gameManager.PlayersInfo[i].RoundsWon + "\n";
        }
        return text;
    }

    private string GetColoredPlayerText(PlayerInfo playerInfo)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(playerInfo.Color) + ">" + playerInfo.Username + "</color>";
    }
}
