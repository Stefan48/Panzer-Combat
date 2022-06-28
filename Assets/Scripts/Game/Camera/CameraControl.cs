using System.Linq;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
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
    private Vector3 _cameraMovementDirection;
    private bool _cameraTeleportationPending = false;
    [SerializeField] private GameObject _level;
    private bool _insideLevel = true;


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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _cameraTeleportationPending = true;
        }
    }

    private void FixedUpdate()
    {
        // Use FixedUpdate for the camera movement in order to sync with the players' movement and avoid jittering
        if (_cameraTeleportationPending)
        {
            _cameraTeleportationPending = false;
            TeleportCamera();
        }
        GlideCamera();
    }

    private void TeleportCamera()
    {
        // Teleport to the first selected tank, or the first owned tank if none are selected
        if (_gameManager.PlayerManager.Tanks.Count == 0)
        {
            return;
        }
        GameObject firstTank = _gameManager.PlayerManager.Tanks.Find(tank => tank.GetComponent<TankInfo>().IsSelected);
        if (firstTank == null)
        {
            firstTank = _gameManager.PlayerManager.Tanks[0];
        }
        transform.position = firstTank.transform.position;
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
        TeleportCamera();
        enabled = false;
    }

    private void OnRoundPlaying()
    {
        enabled = true;
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        if (roundWinner != null && isGameWinner)
        {
            // Go to the winner's location
            GameObject[] tanks = GameObject.FindGameObjectsWithTag("Tank")
                .Where(tank => tank.GetComponent<TankInfo>().PlayerNumber == roundWinner.PlayerNumber).ToArray();
            if (tanks.Length > 0)
            {
                transform.position = tanks[0].transform.position;
            }
        }
        enabled = false;
    }
}
