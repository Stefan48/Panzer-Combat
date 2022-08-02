using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private UiManager _uiManager;
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
    private const float _cameraSpeed = 20f;
    private int _screenHeight;
    private int _screenWidth;
    private const float _mouseThresholdLowerLimit = 0.05f;
    private const float _mouseThresholdUpperLimit = 0.95f;
    private float _mouseThresholdTop;
    private float _mouseThresholdBottom;
    private float _mouseThresholdRight;
    private float _mouseThresholdLeft;
    private static readonly Vector3 s_cameraUp = (new Vector3(1f, 0f, 0f) * 1.7f + new Vector3(0f, 0f, 1f)).normalized;
    private static readonly Vector3 s_cameraDown = -s_cameraUp;
    private static readonly Vector3 s_cameraRight = (new Vector3(1f, 0f, 0f) - new Vector3(0f, 0f, 1f) * 1.7f).normalized;
    private static readonly Vector3 s_cameraLeft = -s_cameraRight;
    private static readonly Vector3 s_cameraUpRight = (s_cameraUp + s_cameraRight).normalized;
    private static readonly Vector3 s_cameraUpLeft = (s_cameraUp + s_cameraLeft).normalized;
    private static readonly Vector3 s_cameraDownRight = (s_cameraDown + s_cameraRight).normalized;
    private static readonly Vector3 s_cameraDownLeft = (s_cameraDown + s_cameraLeft).normalized;
    private bool _escPanelIsActive = false;
    private Vector3 _cameraMovementDirection;
    [SerializeField] private GameObject _level;
    private bool _insideLevel = true;
    [SerializeField] private Camera _fogCamera;
    [SerializeField] private GameObject _fog;
    [SerializeField] private RenderTexture _fogRenderTexture;
    [SerializeField] private GameObject _fogPlane;
    [SerializeField] private GameObject _globalVision;
    private bool _spectating = false;
    private bool _autoFollowWhileSpectating = false;
    private enum CursorOrientation { Regular, Up, Down, Right, Left, UpRight, UpLeft, DownRight, DownLeft};
    private CursorOrientation _cursorOrientation = CursorOrientation.Regular;
    [SerializeField] private Texture2D _cursorRegular;
    [SerializeField] private Texture2D _cursorUp;
    [SerializeField] private Texture2D _cursorDown;
    [SerializeField] private Texture2D _cursorRight;
    [SerializeField] private Texture2D _cursorLeft;
    [SerializeField] private Texture2D _cursorUpRight;
    [SerializeField] private Texture2D _cursorUpLeft;
    [SerializeField] private Texture2D _cursorDownRight;
    [SerializeField] private Texture2D _cursorDownLeft;


    private void Awake()
    {
        GameManager.RoundStartingEvent += OnRoundStarting;
        GameManager.RoundPlayingEvent += OnRoundPlaying;
        GameManager.RoundEndingEvent += OnRoundEnding;
        UiManager.EscPanelToggledEvent += OnEscPanelToggled;
        PlayerManager.TanksListReducedEvent += OnAlliedTankGotDestroyed;
        TankInfo.TankRangeIncreasedEvent += OnTankRangeIncreased;

        Cursor.SetCursor(_cursorRegular, Vector2.zero, CursorMode.Auto);
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
        if (!_playerManagerIsSetUp || (_escPanelIsActive && !_spectating))
        {
            return;
        }
        if (!_escPanelIsActive)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_spectating)
                {
                    _autoFollowWhileSpectating = !_autoFollowWhileSpectating;
                }
                else
                {
                    _switchViewedTankPending = true;
                }
            }
            ProcessScrollInput();
        }
        if (_spectating)
        {
            // Spectate is called in Update instead of FixedUpdate since the latter option causes jittering
            Spectate();
        }
    }

    private void FixedUpdate()
    {
        // Use FixedUpdate for the camera movement in order to sync with the players' movement and avoid jittering
        if (!_playerManagerIsSetUp || _escPanelIsActive)
        {
            return;
        }
        ApplyZoom();
        if (_spectating)
        {
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

    private void Spectate()
    {
        SetCursorOrientation(CursorOrientation.Regular);
        if (_autoFollowWhileSpectating)
        {
            if (_uiManager.SelectedEnemyTank != null)
            {
                transform.position = _uiManager.SelectedEnemyTank.transform.position;
            }
            else if (_uiManager.SelectedEnemyTurret != null)
            {
                transform.position = _uiManager.SelectedEnemyTurret.transform.position;
            }
            else if (_uiManager.SelectedAlliedTurret != null)
            {
                transform.position = _uiManager.SelectedAlliedTurret.transform.position;
            }
            return;
        }
        GlideCamera();
    }

    private void GlideCamera()
    {
        _cameraMovementDirection = Vector3.zero;
        if (Input.mousePosition.y >= _mouseThresholdTop)
        {
            _cameraMovementDirection = s_cameraUp;
            SetCursorOrientation(CursorOrientation.Up);
        }
        else if (Input.mousePosition.y <= _mouseThresholdBottom)
        {
            _cameraMovementDirection = s_cameraDown;
            SetCursorOrientation(CursorOrientation.Down);
        }
        if (Input.mousePosition.x >= _mouseThresholdRight)
        {
            if (_cameraMovementDirection == Vector3.zero)
            {
                _cameraMovementDirection = s_cameraRight;
                SetCursorOrientation(CursorOrientation.Right);
            }
            else if (_cameraMovementDirection == s_cameraUp)
            {
                _cameraMovementDirection = s_cameraUpRight;
                SetCursorOrientation(CursorOrientation.UpRight);
            }
            else if (_cameraMovementDirection == s_cameraDown)
            {
                _cameraMovementDirection = s_cameraDownRight;
                SetCursorOrientation(CursorOrientation.DownRight);
            }
        }
        else if (Input.mousePosition.x <= _mouseThresholdLeft)
        {
            if (_cameraMovementDirection == Vector3.zero)
            {
                _cameraMovementDirection = s_cameraLeft;
                SetCursorOrientation(CursorOrientation.Left);
            }
            else if (_cameraMovementDirection == s_cameraUp)
            {
                _cameraMovementDirection = s_cameraUpLeft;
                SetCursorOrientation(CursorOrientation.UpLeft);
            }
            else if (_cameraMovementDirection == s_cameraDown)
            {
                _cameraMovementDirection = s_cameraDownLeft;
                SetCursorOrientation(CursorOrientation.DownLeft);
            }
        }
        if (_cameraMovementDirection != Vector3.zero)
        {
            if (_insideLevel)
            {
                transform.Translate(_cameraMovementDirection * _cameraSpeed * Time.fixedDeltaTime, Space.World);
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

    private void SetCursorOrientation(CursorOrientation orientation)
    {
        if (_cursorOrientation == orientation)
        {
            return;
        }
        _cursorOrientation = orientation;
        switch (orientation)
        {
            case CursorOrientation.Regular:
                Cursor.SetCursor(_cursorRegular, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.Up:
                Cursor.SetCursor(_cursorUp, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.Down:
                Cursor.SetCursor(_cursorDown, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.Right:
                Cursor.SetCursor(_cursorRight, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.Left:
                Cursor.SetCursor(_cursorLeft, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.UpRight:
                Cursor.SetCursor(_cursorUpRight, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.UpLeft:
                Cursor.SetCursor(_cursorUpLeft, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.DownRight:
                Cursor.SetCursor(_cursorDownRight, Vector2.zero, CursorMode.Auto);
                break;
            case CursorOrientation.DownLeft:
                Cursor.SetCursor(_cursorDownLeft, Vector2.zero, CursorMode.Auto);
                break;
            default:
                break;
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
        _spectating = false;
        _fog.SetActive(true);
        _globalVision.SetActive(false);
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
        SetCursorOrientation(CursorOrientation.Regular);
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
            _spectating = true;
            _viewedTank = null;
            _fog.SetActive(false);
            _globalVision.SetActive(true);
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
