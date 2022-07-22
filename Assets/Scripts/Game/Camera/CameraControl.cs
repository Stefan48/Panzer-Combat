using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private GameManager _gameManager;
    private bool _playerManagerIsSetUp = false;
    private List<GameObject> _tanks = null;
    private GameObject _viewedTank = null;
    private bool _switchViewedTankPending = false;
    private const float _defaultCameraSize = 10f;
    private const float _minCameraSize = 4f;
    private float _maxCameraSize = 10f;
    private const float _cameraSizeUpperLimit = 21f;
    private int _cameraZoomPending = 0;
    // TODO - Adjustable camera speed in settings (useful only in spectator mode)
    private const float _cameraSpeed = 15f;

    private int _screenHeight;
    private int _screenWidth;
    private const float _mouseThresholdLowerLimit = 0.05f;
    private const float _mouseThresholdUpperLimit = 0.95f;
    private float _mouseThresholdTop;
    private float _mouseThresholdBottom;
    private float _mouseThresholdRight;
    private float _mouseThresholdLeft;

    /* private readonly float[] _mouseThresholdsTop = { Screen.height * 0.95f, Screen.height * 0.99999f };
    private readonly float[] _mouseThresholdsBottom = { Screen.height * 0.00001f, Screen.height * 0.05f };
    private readonly float[] _mouseThresholdsRight = { Screen.width * 0.95f, Screen.width * 0.99999f };
    private readonly float[] _mouseThresholdsLeft = { Screen.width * 0.00001f, Screen.width * 0.05f }; */

    private readonly Vector3 _cameraUp = Vector3.Normalize(new Vector3(1f, 0f, 0f) * 1.7f + new Vector3(0f, 0f, 1f));
    private readonly Vector3 _cameraRight = Vector3.Normalize(new Vector3(1f, 0f, 0f) - new Vector3(0f, 0f, 1f) * 1.7f);
    private bool _escPanelIsActive = false;
    private Vector3 _cameraMovementDirection;
    [SerializeField] private GameObject _level;
    private bool _insideLevel = true;

    [SerializeField] private Camera _fogCamera;
    [SerializeField] private GameObject _fog;
    [SerializeField] private RenderTexture _fogRenderTexture;
    [SerializeField] private GameObject _fogPlane;


    private void Awake()
    {
        GameManager.RoundStartingEvent += OnRoundStarting;
        GameManager.RoundPlayingEvent += OnRoundPlaying;
        GameManager.RoundEndingEvent += OnRoundEnding;
        UiManager.EscPanelToggledEvent += OnEscPanelToggled;
        PlayerManager.TanksListReducedEvent += OnAlliedTankGotDestroyed;
        TankInfo.TankRangeIncreasedEvent += OnTankRangeIncreased;

        OnScreenResized();
    }

    private void OnDestroy()
    {
        GameManager.RoundStartingEvent -= OnRoundStarting;
        GameManager.RoundPlayingEvent -= OnRoundPlaying;
        GameManager.RoundEndingEvent -= OnRoundEnding;
        UiManager.EscPanelToggledEvent -= OnEscPanelToggled;
        PlayerManager.TanksListReducedEvent -= OnAlliedTankGotDestroyed;
        TankInfo.TankRangeIncreasedEvent -= OnTankRangeIncreased;
    }

    private void Update()
    {
        if (_screenHeight != Screen.height || _screenWidth != Screen.width)
        {
            OnScreenResized();
        }
        if (!_playerManagerIsSetUp || _escPanelIsActive)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_tanks.Count > 0)
            {
                _switchViewedTankPending = true;
            }
        }
        ProcessScrollInput();
    }

    private void FixedUpdate()
    {
        // Use FixedUpdate for the camera movement in order to sync with the players' movement and avoid jittering
        if (!_playerManagerIsSetUp || _escPanelIsActive)
        {
            return;
        }
        ApplyZoom();
        if (_tanks.Count == 0)
        {
            GlideCamera();
            return;
        }
        if (_switchViewedTankPending)
        {
            _switchViewedTankPending = false;
            SwitchViewedTank();
            return;
        }
        FollowViewedTank();        
    }

    private void SetCameraInitialPosition()
    {
        _viewedTank = _tanks[0];
        transform.position = _viewedTank.transform.position;
    }

    private void SetCameraInitialSize()
    {
        _camera.orthographicSize = _defaultCameraSize;
        _maxCameraSize = _defaultCameraSize;
    }

    private void SwitchViewedTank()
    {
        if (_viewedTank == null)
        {
            _viewedTank = _tanks[0];
        }
        else
        {
            int index = _tanks.FindIndex(tank => tank == _viewedTank);
            if (_viewedTank.GetComponent<TankInfo>().IsSelected)
            {
                // View the next selected tank
                for (int i = 1; i < _tanks.Count; ++i)
                {
                    GameObject tank = _tanks[(index + i) % _tanks.Count];
                    if (tank.GetComponent<TankInfo>().IsSelected)
                    {
                        _viewedTank = tank;
                        break;
                    }
                }
            }
            else
            {
                // View the first selected tank or the next unselected tank if none are selected
                bool foundSelectedTank = false;
                foreach (GameObject tank in _tanks)
                {
                    if (tank.GetComponent<TankInfo>().IsSelected)
                    {
                        foundSelectedTank = true;
                        _viewedTank = tank;
                        break;
                    }
                }
                if (!foundSelectedTank)
                {
                    _viewedTank = _tanks[(index + 1) % _tanks.Count];
                }
            }
        }
        transform.position = _viewedTank.transform.position;
    }

    private void FollowViewedTank()
    {
        if (_viewedTank.GetComponent<TankInfo>().IsSelected)
        {
            // Update the position even if (_viewedTank.GetComponent<TankMovement>().IsMoving == false), since
            // Rigidbody.MovePosition updates the tank's position over multiple frames, so the camera's position might not be up to date
            transform.position = _viewedTank.transform.position;
        }
    }

    private void ProcessScrollInput()
    {
        float mouseScroll = Input.mouseScrollDelta.y;
        if (mouseScroll > 0f)
        {
            _cameraZoomPending++;
        }
        else if (mouseScroll < 0f)
        {
            _cameraZoomPending--;
        }
    }

    private void ApplyZoom()
    {
        _camera.orthographicSize -= _cameraZoomPending;
        if (_camera.orthographicSize < _minCameraSize)
        {
            _camera.orthographicSize = _minCameraSize;
        }
        else if (_camera.orthographicSize > _maxCameraSize)
        {
            _camera.orthographicSize = _maxCameraSize;
        }
        _fogCamera.orthographicSize = _camera.orthographicSize;
        _cameraZoomPending = 0;
        float fogPlaneScaleZ = _camera.orthographicSize * 0.2f;
        float fogPlanceScaleX = (float)_screenWidth / _screenHeight * fogPlaneScaleZ;
        _fogPlane.transform.localScale = new Vector3(fogPlanceScaleX, 1f, fogPlaneScaleZ);
    }

    private void GlideCamera()
    {
        _cameraMovementDirection = Vector3.zero;
        if (Input.mousePosition.y >= _mouseThresholdTop)
        //if (Input.mousePosition.y >= _mouseThresholdsTop[0] && Input.mousePosition.y <= _mouseThresholdsTop[1])
        {
            _cameraMovementDirection += _cameraUp;
        }
        else if (Input.mousePosition.y <= _mouseThresholdBottom)
        //else if (Input.mousePosition.y >= _mouseThresholdsBottom[0] && Input.mousePosition.y <= _mouseThresholdsBottom[1])
        {
            _cameraMovementDirection -= _cameraUp;
        }
        if (Input.mousePosition.x >= _mouseThresholdRight)
        //if (Input.mousePosition.x >= _mouseThresholdsRight[0] && Input.mousePosition.x <= _mouseThresholdsRight[1])
        {
            _cameraMovementDirection += _cameraRight;
        }
        else if (Input.mousePosition.x <= _mouseThresholdLeft)
        //else if (Input.mousePosition.x >= _mouseThresholdsLeft[0] && Input.mousePosition.x <= _mouseThresholdsLeft[1])
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
        _fog.SetActive(true);
        SetCameraInitialPosition();
        SetCameraInitialSize();
        enabled = false;
    }

    private void OnRoundPlaying()
    {
        enabled = true;
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        // TODO - Play round end/game end sounds
        _fog.SetActive(false);
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

    private void OnAlliedTankGotDestroyed(GameObject tank)
    {
        if (_tanks.Count == 0)
        {
            // After losing his last tank, the player is in spectator mode until a new round starts
            _viewedTank = null;
            _fog.SetActive(false);
            _maxCameraSize = _cameraSizeUpperLimit;
            return;
        }
        if (ReferenceEquals(tank, _viewedTank))
        {
            // View the first selected tank or the first tank if none are selected
            bool foundSelectedTank = false;
            foreach (GameObject t in _tanks)
            {
                if (t.GetComponent<TankInfo>().IsSelected)
                {
                    foundSelectedTank = true;
                    _viewedTank = t;
                    break;
                }
            }
            if (!foundSelectedTank)
            {
                _viewedTank = _tanks[0];
            }
        }
        transform.position = _viewedTank.transform.position;
        _maxCameraSize = _tanks.Max(t => t.GetComponent<TankInfo>().Range);
        if (_camera.orthographicSize > _maxCameraSize)
        {
            _cameraZoomPending = (int)(_camera.orthographicSize - _maxCameraSize);
            ApplyZoom();
        }
    }

    private void OnTankRangeIncreased(int newRange)
    {
        if (newRange > _maxCameraSize)
        {
            _maxCameraSize = Math.Min(_cameraSizeUpperLimit, newRange);
        }
    }

    private void OnEscPanelToggled(bool active)
    {
        _escPanelIsActive = active;
    }

    private void OnScreenResized()
    {
        _screenHeight = Screen.height;
        _screenWidth = Screen.width;

        _mouseThresholdTop = _screenHeight * _mouseThresholdUpperLimit;
        _mouseThresholdBottom = _screenHeight * _mouseThresholdLowerLimit;
        _mouseThresholdRight = _screenWidth * _mouseThresholdUpperLimit;
        _mouseThresholdLeft = _screenWidth * _mouseThresholdLowerLimit;

        _fogRenderTexture.Release();
        _fogRenderTexture.height = _screenHeight;
        _fogRenderTexture.width = _screenWidth;
        _fogRenderTexture.Create();
        float fogPlaneScaleZ = _camera.orthographicSize * 0.2f;
        float fogPlaneScaleX = (float)_screenWidth / _screenHeight * fogPlaneScaleZ;
        _fogPlane.transform.localScale = new Vector3(fogPlaneScaleX, 1f, fogPlaneScaleZ);
        // Resetting the fog GameObject is required for the changes to take place
        _fog.SetActive(!_fog.activeSelf);
        _fog.SetActive(!_fog.activeSelf);
    }
}
