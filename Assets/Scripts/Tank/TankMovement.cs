using UnityEngine;

public class TankMovement : MonoBehaviour
{
    public int playerNumber;
    public bool isSelectedByOwner = false;
    [SerializeField] private float speed = 12f;
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioClip engineIdleAudioClip;
    [SerializeField] private AudioClip engineDrivingAudioClip;
    private float engineOriginalPitch;
    private float enginePitchRange = 0.2f;
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private bool isMoving = false;
    private Vector3 movementDirection;
    private const float viewAngleOffset = 60f;
    private float newRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {
        isMoving = false;
    }

    private void Start()
    {
        engineOriginalPitch = engineAudioSource.pitch;
    }

    private void Update()
    {
        // Tank may move only when selected
        if (isSelectedByOwner)
        {
            // Process input
            if (Input.GetKey(KeyCode.W))
            {
                isMoving = true;
                movementDirection = transform.forward;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                isMoving = true;
                movementDirection = -transform.forward;
            }
            else
            {
                isMoving = false;
            }

            // Tank is always oriented towards the player's cursor
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
            screenPosition.z = 0f;
            Vector3 mousePosition = Input.mousePosition;
            float angle = Vector3.Angle(mousePosition - screenPosition, new Vector3(0f, 1f, 0f));
            if (mousePosition.x < screenPosition.x)
            {
                angle = 360.0f - angle;
            }
            newRotation = angle + viewAngleOffset;
        }

        PlayEngineAudio();
    }

    private void FixedUpdate()
    {
        // Tank may move only when selected
        if (isSelectedByOwner)
        {
            rb.MoveRotation(Quaternion.Euler(0f, newRotation, 0f));

            if (isMoving)
            {
                Vector3 movement = movementDirection * speed * Time.fixedDeltaTime;
                Vector3 desiredPosition = rb.position + movement;
                bool wouldHitColliders = false;
                // Query ignores triggers (like the Camera Rig, the collider for the level's boundaries or the shells)
                Collider[] collidersThatWouldBeHit = Physics.OverlapSphere(desiredPosition + sphereCollider.center, sphereCollider.radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < collidersThatWouldBeHit.Length; ++i)
                {
                    if (collidersThatWouldBeHit[i].name != transform.name)
                    {
                        //Debug.Log(collidersThatWouldBeHit[i].name);
                        wouldHitColliders = true;
                        break;
                    }
                }
                if (!wouldHitColliders)
                {
                    rb.MovePosition(desiredPosition);
                }
            }
        }
    }

    private void PlayEngineAudio()
    {
        if (isMoving && engineAudioSource.clip == engineIdleAudioClip)
        {
            engineAudioSource.clip = engineDrivingAudioClip;
            engineAudioSource.pitch = Random.Range(engineOriginalPitch - enginePitchRange, engineOriginalPitch + enginePitchRange);
            engineAudioSource.Play();
        }
        else if (!isMoving && engineAudioSource.clip == engineDrivingAudioClip)
        {
            engineAudioSource.clip = engineIdleAudioClip;
            engineAudioSource.pitch = Random.Range(engineOriginalPitch - enginePitchRange, engineOriginalPitch + enginePitchRange);
            engineAudioSource.Play();
        }
    }
}
