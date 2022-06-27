using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;


    [SerializeField] private const float cameraSpeed = 15f;
    private float mouseThresholdTop;
    private float mouseThresholdBottom;
    private float mouseThresholdRight;
    private float mouseThresholdLeft;
    private bool cameraTeleportationPending;
    private Vector3 cameraUp = Vector3.Normalize(new Vector3(1f, 0f, 0f) * 1.7f + new Vector3(0f, 0f, 1f));
    private Vector3 cameraRight = Vector3.Normalize(new Vector3(1f, 0f, 0f) - new Vector3(0f, 0f, 1f) * 1.7f);
    private Vector3 cameraMovementDirection;
    [SerializeField] private GameObject level;
    private bool insideLevel = true;

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
        mouseThresholdTop = Screen.height * 0.95f;
        mouseThresholdBottom = Screen.height * 0.05f;
        mouseThresholdRight = Screen.width * 0.95f;
        mouseThresholdLeft = Screen.width * 0.05f;

        /*Debug.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f), Color.red, 30f);
        Debug.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 5f), Color.blue, 30f);
        Debug.DrawLine(new Vector3(0f, 0f, 0f), 5f * cameraUp, Color.green, 30f);
        Debug.DrawLine(new Vector3(0f, 0f, 0f), 5f * cameraRight, Color.green, 30f);*/
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Use space bar to teleport the camera to the first selected tank, or the first owned tank if none are selected
            cameraTeleportationPending = true;
        }
    }

    private void FixedUpdate()
    {
        // Use FixedUpdate for the camera movement in order to sync with the players' movement and avoid jittering
        if (cameraTeleportationPending)
        {
            cameraTeleportationPending = false;
            GameObject firstSelectedTank = null;
            GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < tanks.Length; ++i)
            {
                TankInfo tankInfo = tanks[i].GetComponent<TankInfo>();
                // TODO - use game manager when checking the player number
                // TODO - Actually, use the list of tanks from PlayerManager
                if (tankInfo.PlayerNumber == 1)
                {
                    if (firstSelectedTank == null)
                    {
                        firstSelectedTank = tanks[i];
                    }
                    if (tankInfo.IsSelected)
                    {
                        firstSelectedTank = tanks[i];
                        break;
                    }
                }
            }
            if (firstSelectedTank != null)
            {
                transform.position = firstSelectedTank.transform.position;
                return;
            }
        }

        cameraMovementDirection = Vector3.zero;
        if (Input.mousePosition.y >= mouseThresholdTop)
        {
            cameraMovementDirection += cameraUp;
        }
        else if (Input.mousePosition.y <= mouseThresholdBottom)
        {
            cameraMovementDirection -= cameraUp;
        }
        if (Input.mousePosition.x >= mouseThresholdRight)
        {
            cameraMovementDirection += cameraRight;
        }
        else if (Input.mousePosition.x <= mouseThresholdLeft)
        {
            cameraMovementDirection -= cameraRight;
        }
        if (cameraMovementDirection != Vector3.zero)
        {
            if (insideLevel)
            {
                // Normalize the direction so the camera's speed won't increase when scrolling diagonally
                transform.Translate(Vector3.Normalize(cameraMovementDirection) * cameraSpeed * Time.fixedDeltaTime, Space.World);
            }
            else
            {
                // Prevent the camera from moving further away from the level's center
                float currentDistance = Vector3.Distance(transform.position, level.transform.position);
                Vector3 newPosition = transform.position + Vector3.Normalize(cameraMovementDirection) * cameraSpeed * Time.fixedDeltaTime;
                float newDistance = Vector3.Distance(newPosition, level.transform.position);
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
        if (other.name.Contains("Level"))
        {
            // Camera Rig goes beyond the level's borders
            insideLevel = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Level"))
        {
            // Camera Rig goes back inside the level's borders
            insideLevel = true;
        }
    }

    private void OnRoundStarting(int round)
    {
        // TODO - Go to player's spawn position
        enabled = false;
    }

    private void OnRoundPlaying()
    {
        enabled = true;
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        // TODO - Go to winner's location (if isGameWinner ?)
        enabled = false;
    }
}
