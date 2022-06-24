using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Text _infoText;


    [SerializeField] private int playerNumber = 1;
    [SerializeField] private LayerMask playersLayerMask;
    [SerializeField] private GameObject selectionRingPrefab;
    private const int numAlliedSelectionRingsStored = 10;
    private List<GameObject> alliedSelectionRings = new List<GameObject>();
    private GameObject enemySelectionRing;
    private List<GameObject> selectedAlliedTanks = new List<GameObject>();
    private GameObject selectedEnemyTank = null;

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
        // Instantiate selection rings prefabs
        for (int i = 0; i < numAlliedSelectionRingsStored; ++i)
        {
            GameObject ring = Instantiate(selectionRingPrefab);
            ring.name = "AlliedSelectionRing" + (i + 1);
            ring.SetActive(false);
            alliedSelectionRings.Add(ring);
        }
        enemySelectionRing = Instantiate(selectionRingPrefab, selectionRingPrefab.transform.position, selectionRingPrefab.transform.rotation);
        enemySelectionRing.transform.Find("Slider").Find("Fill Area").Find("Fill").GetComponent<Image>().color = new Color32(255, 0, 0, 180);
        enemySelectionRing.name = "EnemySelectionRing";
        enemySelectionRing.SetActive(false);
    }

    private void Update()
    {
        // Process input
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, playersLayerMask, QueryTriggerInteraction.Ignore))
            {
                GameObject tank = hit.transform.gameObject;
                TankMovement tankMovementComponent = tank.GetComponent<TankMovement>();
                if (tankMovementComponent.playerNumber == playerNumber)
                {
                    // Player selected an allied tank
                    if (selectedEnemyTank != null)
                    {
                        selectedEnemyTank = null;
                        enemySelectionRing.SetActive(false);
                    }
                    if (Input.GetKey(KeyCode.LeftAlt))
                    {
                        // Player might have selected multiple allied tanks
                        bool selectedNewTank = true;
                        for (int i = 0; i < selectedAlliedTanks.Count; ++i)
                        {
                            if (ReferenceEquals(tank, selectedAlliedTanks[i]))
                            {
                                selectedNewTank = false;
                            }
                        }
                        if (selectedNewTank)
                        {
                            tankMovementComponent.isSelectedByOwner = true;
                            selectedAlliedTanks.Add(tank);
                            alliedSelectionRings[selectedAlliedTanks.Count - 1].SetActive(true);
                        }
                    }
                    else
                    {
                        // Player selected a single allied tank
                        if (selectedAlliedTanks.Count > 0)
                        {
                            for (int i = 1; i < selectedAlliedTanks.Count; ++i)
                            {
                                alliedSelectionRings[i].SetActive(false);
                                selectedAlliedTanks[i].GetComponent<TankMovement>().isSelectedByOwner = false;
                            }
                            selectedAlliedTanks[0].GetComponent<TankMovement>().isSelectedByOwner = false;
                            selectedAlliedTanks.Clear();
                        }
                        tankMovementComponent.isSelectedByOwner = true;
                        selectedAlliedTanks.Add(tank);
                        alliedSelectionRings[0].SetActive(true);
                    }
                }
                else
                {
                    // Player selected an enemy tank
                    if (selectedAlliedTanks.Count > 0)
                    {
                        for (int i = 0; i < selectedAlliedTanks.Count; ++i)
                        {
                            alliedSelectionRings[i].SetActive(false);
                            selectedAlliedTanks[i].GetComponent<TankMovement>().isSelectedByOwner = false;
                        }
                        selectedAlliedTanks.Clear();
                    }
                    selectedEnemyTank = tank;
                    enemySelectionRing.SetActive(true);
                }
            }
            else
            {
                // Left-clicking  outside of any tank deselects the currently selected tanks (if any)
                if (selectedAlliedTanks.Count > 0)
                {
                    for (int i = 0; i < selectedAlliedTanks.Count; ++i)
                    {
                        alliedSelectionRings[i].SetActive(false);
                        selectedAlliedTanks[i].GetComponent<TankMovement>().isSelectedByOwner = false;
                    }
                    selectedAlliedTanks.Clear();
                }
                else if (selectedEnemyTank != null)
                {
                    selectedEnemyTank = null;
                    enemySelectionRing.SetActive(false);
                }
            }
        }
    }
    private void FixedUpdate()
    {
        // Update selection rings' positions (use FixedUpdate to sync with the players' movement)
        if (selectedAlliedTanks.Count > 0)
        {
            for (int i = 0; i < selectedAlliedTanks.Count; ++i)
            {
                if (!selectedAlliedTanks[i].activeSelf)
                {
                    // Selected allied tank got destroyed
                    alliedSelectionRings[selectedAlliedTanks.Count - 1].SetActive(false);
                    selectedAlliedTanks.RemoveAt(i);
                    i--;
                }
                else
                {
                    Vector3 tankPosition = selectedAlliedTanks[i].transform.position;
                    alliedSelectionRings[i].transform.position = new Vector3(tankPosition.x, alliedSelectionRings[i].transform.position.y, tankPosition.z);
                }
            }
        }
        else if (selectedEnemyTank != null)
        {
            if (!selectedEnemyTank.activeSelf)
            {
                // The selected enemy tank got destroyed
                selectedEnemyTank = null;
                enemySelectionRing.SetActive(false);
            }
            else
            {
                Vector3 tankPosition = selectedEnemyTank.transform.position;
                enemySelectionRing.transform.position = new Vector3(tankPosition.x, enemySelectionRing.transform.position.y, tankPosition.z);
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

    private void OnRoundEnding(PlayerManager roundWinner, bool isGameWinner)
    {
        Reset();
        _infoText.text = GetRoundEndText(roundWinner, isGameWinner);
        enabled = false;
    }

    public void Reset()
    {
        for (int i = 0; i < selectedAlliedTanks.Count; ++i)
        {
            selectedAlliedTanks[i].GetComponent<TankMovement>().isSelectedByOwner = false;
            alliedSelectionRings[i].SetActive(false);
        }
        selectedAlliedTanks.Clear();

        if (selectedEnemyTank != null)
        {
            selectedEnemyTank = null;
            enemySelectionRing.SetActive(false);
        }
    }

    private string GetRoundEndText(PlayerManager roundWinner, bool isGameWinner)
    {
        if (roundWinner == null)
        {
            return "DRAW!";
        }
        string text;
        string coloredPlayerText = GetColoredPlayerText(roundWinner.PlayerColor, roundWinner.PlayerNumber);
        if (isGameWinner)
        {
            text = coloredPlayerText + " WON THE GAME!";
        }
        else
        {
            text = coloredPlayerText + " WON THE ROUND!";
        }
        text += "\n\n";
        for (int i = 0; i < _gameManager.NumberOfPlayers; ++i)
        {
            text += GetColoredPlayerText(_gameManager.playerManagers[i].PlayerColor, _gameManager.playerManagers[i].PlayerNumber)
                + ": " + _gameManager.playerManagers[i].RoundsWon + "\n";
        }
        return text;
    }

    private string GetColoredPlayerText(Color playerColor, int playerNumber)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(playerColor) + ">PLAYER " + playerNumber + "</color>";
    }
}
