using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private const float cameraSpeed = 15f;
    private float mouseThresholdTop;
    private float mouseThresholdBottom;
    private float mouseThresholdRight;
    private float mouseThresholdLeft;
    private Vector3 cameraUp = Vector3.Normalize(new Vector3(1f, 0f, 0f) * 1.7f + new Vector3(0f, 0f, 1f));
    private Vector3 cameraRight = Vector3.Normalize(new Vector3(1f, 0f, 0f) - new Vector3(0f, 0f, 1f) * 1.7f);
    private Vector3 cameraMovementDirection;
    [SerializeField] private GameObject level;
    private bool insideLevel = true;


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

    private void FixedUpdate()
    {
        // Use FixedUpdate for the camera movement in order to sync with the players' movement and avoid jittering
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
                // We normalize the direction so the camera's speed won't increase when scrolling diagonally
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

    // TODO - camera bounds (shouldn't go over the map's boundaries)
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
}
