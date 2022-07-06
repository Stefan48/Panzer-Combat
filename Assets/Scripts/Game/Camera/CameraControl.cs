using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    private bool _playerManagerIsSetUp = false;
    private List<GameObject> _tanks = null;
    private GameObject _followedTank = null;
    private bool _cameraRepositionPending = false;
    private bool _switchFollowedTankPending = false;
    // TODO - Adjustable camera speed in settings
    private const float _cameraSpeed = 15f;
    /*private readonly float _mouseThresholdTop = Screen.height * 0.95f;
    private readonly float _mouseThresholdBottom = Screen.height * 0.05f;
    private readonly float _mouseThresholdRight = Screen.width * 0.95f;
    private readonly float _mouseThresholdLeft = Screen.width * 0.05f;*/

    private readonly float[] _mouseThresholdsTop = { Screen.height * 0.95f, Screen.height * 0.99999f };
    private readonly float[] _mouseThresholdsBottom = { Screen.height * 0.00001f, Screen.height * 0.05f };
    private readonly float[] _mouseThresholdsRight = { Screen.width * 0.95f, Screen.width * 0.99999f };
    private readonly float[] _mouseThresholdsLeft = { Screen.width * 0.00001f, Screen.width * 0.05f };

    private readonly Vector3 _cameraUp = Vector3.Normalize(new Vector3(1f, 0f, 0f) * 1.7f + new Vector3(0f, 0f, 1f));
    private readonly Vector3 _cameraRight = Vector3.Normalize(new Vector3(1f, 0f, 0f) - new Vector3(0f, 0f, 1f) * 1.7f);
    private bool _escPanelIsActive = false;
    private Vector3 _cameraMovementDirection;
    [SerializeField] private GameObject _level;
    private bool _insideLevel = true;


    private void Awake()
    {
        _gameManager.RoundStartingEvent += OnRoundStarting;
        _gameManager.RoundPlayingEvent += OnRoundPlaying;
        _gameManager.RoundEndingEvent += OnRoundEnding;
        UiManager.EscPanelToggledEvent += OnEscPanelToggled;
        TankHealth.TankGotDestroyedEvent += OnTankGotDestroyed;
    }

    private void OnDestroy()
    {
        _gameManager.RoundStartingEvent -= OnRoundStarting;
        _gameManager.RoundPlayingEvent -= OnRoundPlaying;
        _gameManager.RoundEndingEvent -= OnRoundEnding;
        UiManager.EscPanelToggledEvent -= OnEscPanelToggled;
        TankHealth.TankGotDestroyedEvent -= OnTankGotDestroyed;
    }

    private void Update()
    {
        if (!_playerManagerIsSetUp || _escPanelIsActive)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_followedTank == null || (transform.position != _followedTank.transform.position))
            {
                _cameraRepositionPending = true;
                return;
            }
            else
            {
                _switchFollowedTankPending = true;
            }
        }
    }

    private void FixedUpdate()
    {
        // Use FixedUpdate for the camera movement in order to sync with the players' movement and avoid jittering
        if (!_playerManagerIsSetUp || _escPanelIsActive)
        {
            return;
        }
        if (_cameraRepositionPending)
        {
            _cameraRepositionPending = false;
            RepositionCamera();
            return;
        }
        if (_switchFollowedTankPending)
        {
            _switchFollowedTankPending = false;
            SwitchFollowedTank();
            return;
        }
        if (FollowTanks())
        {
            return;
        }
        GlideCamera();
    }

    private void SetCameraInitialPosition()
    {
        Vector3 medianPosition = Vector3.zero;
        foreach (GameObject tank in _tanks)
        {
            medianPosition += tank.transform.position;
        }
        medianPosition /= _tanks.Count;
        transform.position = medianPosition;
    }

    private void RepositionCamera()
    {
        if (_followedTank == null)
        {
            if (_tanks.Count > 0)
            {
                int index = _tanks.FindIndex(tank => tank.transform.position == transform.position);
                if (index == -1)
                {
                    // Reposition to the nearest tank
                    float minDistance = float.PositiveInfinity;
                    GameObject tankToRepositionTo = null;
                    foreach (GameObject tank in _tanks)
                    {
                        float distance = Vector3.Distance(transform.position, tank.transform.position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            tankToRepositionTo = tank;
                        }
                    }
                    transform.position = tankToRepositionTo.transform.position;
                }
                else
                {
                    // If already at a tank's location when repositioning, switch between tanks even if none of them are selected
                    transform.position = _tanks[(index + 1) % _tanks.Count].transform.position;
                }
            }
        }
        else
        {
            transform.position = _followedTank.transform.position;
        }
    }

    private void SwitchFollowedTank()
    {
        int index = _tanks.FindIndex(tank => tank == _followedTank);
        for (int i = 1; i < _tanks.Count; ++i)
        {
            GameObject tank = _tanks[(index + i) % _tanks.Count];
            if (tank.GetComponent<TankInfo>().IsSelected)
            {
                _followedTank = tank;
                break;
            }
        }
        transform.position = _followedTank.transform.position;
    }

    private bool FollowTanks()
    {
        if (_followedTank == null)
        {
            foreach (GameObject tank in _tanks)
            {
                if (tank.GetComponent<TankInfo>().IsSelected)
                {
                    _followedTank = tank;
                    break;
                }
            }
        }
        else if (!_followedTank.GetComponent<TankInfo>().IsSelected)
        {
            _followedTank = null;
            foreach (GameObject tank in _tanks)
            {
                if (tank.GetComponent<TankInfo>().IsSelected)
                {
                    _followedTank = tank;
                    break;
                }
            }
        }
        if (_followedTank != null && _followedTank.GetComponent<TankMovement>().IsMoving)
        {
            transform.position = _followedTank.transform.position;
            return true;
        }
        return false;
    }

    private void GlideCamera()
    {
        _cameraMovementDirection = Vector3.zero;
        //if (Input.mousePosition.y >= _mouseThresholdTop)
        if (Input.mousePosition.y >= _mouseThresholdsTop[0] && Input.mousePosition.y <= _mouseThresholdsTop[1])
        {
            _cameraMovementDirection += _cameraUp;
        }
        //else if (Input.mousePosition.y <= _mouseThresholdBottom)
        if (Input.mousePosition.y >= _mouseThresholdsBottom[0] && Input.mousePosition.y <= _mouseThresholdsBottom[1])
        {
            _cameraMovementDirection -= _cameraUp;
        }
        //if (Input.mousePosition.x >= _mouseThresholdRight)
        if (Input.mousePosition.x >= _mouseThresholdsRight[0] && Input.mousePosition.x <= _mouseThresholdsRight[1])
        {
            _cameraMovementDirection += _cameraRight;
        }
        //else if (Input.mousePosition.x <= _mouseThresholdLeft)
        if (Input.mousePosition.x >= _mouseThresholdsLeft[0] && Input.mousePosition.x <= _mouseThresholdsLeft[1])
        {
            _cameraMovementDirection -= _cameraRight;
        }
        if (_cameraMovementDirection != Vector3.zero)
        {
            if (_insideLevel)
            {
                // Normalize the direction so the camera's speed won't increase when scrolling diagonally
                transform.Translate(Vector3.Normalize(_cameraMovementDirection) * _cameraSpeed * Time.fixedDeltaTime, Space.World);
            }
            else
            {
                // Prevent the camera from moving further away from the level's center
                float currentDistance = Vector3.Distance(transform.position, _level.transform.position);
                Vector3 newPosition = transform.position + Vector3.Normalize(_cameraMovementDirection) * _cameraSpeed * Time.fixedDeltaTime;
                float newDistance = Vector3.Distance(newPosition, _level.transform.position);
                if (newDistance <= currentDistance)
                {
                    transform.position = newPosition;
                }
            }
        }
    }

    // Have camera bounds so that the camera doesn't go over the map's boundaries
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Level")
        {
            // Camera Rig goes beyond the level's borders
            _insideLevel = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Level")
        {
            // Camera Rig goes back inside the level's borders
            _insideLevel = true;
        }
    }

    private void OnRoundStarting(int round)
    {
        if (!_playerManagerIsSetUp)
        {
            _playerManagerIsSetUp = true;
            // This is the only place where the _tanks reference to the PlayerManager's Tanks list is assigned
            // So the PlayerManager should avoid reinstantiating the list
            _tanks = _gameManager.PlayerManager.Tanks;
        }
        SetCameraInitialPosition();
        enabled = false;
    }

    private void OnRoundPlaying()
    {
        enabled = true;
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        // TODO - Play round end/game end sounds
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank");
        // Disable engine sounds
        foreach (GameObject tank in tanks)
        {
            tank.GetComponent<AudioSource>().enabled = false;
        }
        if (isGameWinner)
        {
            // Go to the winner's location
            if (tanks.Length > 0)
            {
                transform.position = tanks[0].transform.position;
            }
        }
        enabled = false;
    }

    private void OnTankGotDestroyed(GameObject tank)
    {
        if (ReferenceEquals(tank, _followedTank))
        {
            _followedTank = null;
        }
    }

    private void OnEscPanelToggled(bool active)
    {
        _escPanelIsActive = active;
    }
}
