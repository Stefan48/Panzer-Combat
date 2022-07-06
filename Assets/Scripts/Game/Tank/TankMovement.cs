using Photon.Pun;
using UnityEngine;

public class TankMovement : MonoBehaviour, IPunObservable
{
    private PhotonView _photonView;
    private TankInfo _tankInfo;
    private Rigidbody _rigidbody;
    private SphereCollider _sphereCollider;
    [SerializeField] private AudioSource _engineAudioSource;
    [SerializeField] private AudioClip _engineIdleAudioClip;
    [SerializeField] private AudioClip _engineDrivingAudioClip;
    private float _engineOriginalPitch;
    private const float _enginePitchRange = 0.2f;
    public bool EscPanelIsActive = false;
    [SerializeField] private Transform _orientation;
    [SerializeField] private LayerMask _groundLayerMask;
    public bool IsMoving { get; private set; } = false;
    private Vector3 _movementDirection;
    private int _collisionsLayerMask;
    [SerializeField] private Transform _movementRaycastDirection;
    [SerializeField] private Transform _movementRaycastInnerPoint;
    private const float _movementRaycastAngleStep = 10f;
    private const float _movementRaycastMagnitude = 1f;


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(IsMoving);
        }
        else if (stream.IsReading)
        {
            IsMoving = (bool)stream.ReceiveNext();
        }
    }

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _tankInfo = GetComponent<TankInfo>();
        _rigidbody = GetComponent<Rigidbody>();
        _sphereCollider = GetComponent<SphereCollider>();
        _collisionsLayerMask = LayerMask.GetMask("Default", "Tanks");
    }

    private void Start()
    {
        _engineOriginalPitch = _engineAudioSource.pitch;
    }

    private void Update()
    {
        if (_photonView.IsMine)
        {
            if (!EscPanelIsActive)
            {
                if (_tankInfo.IsSelected)
                {
                    ProcessMovementInput();
                }
            }
        }        
        PlayEngineAudio();
    }

    private void FixedUpdate()
    {
        if (_photonView.IsMine)
        {
            if (!EscPanelIsActive)
            {
                if (_tankInfo.IsSelected)
                {
                    ApplyMovement();
                }
            }
        }
    }

    private void ProcessMovementInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            IsMoving = true;
            _movementDirection = transform.forward;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            IsMoving = true;
            _movementDirection = -transform.forward;
        }
        else
        {
            IsMoving = false;
        }
    }

    private void ApplyMovement()
    {
        // The tank is always oriented towards the player's cursor
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayerMask, QueryTriggerInteraction.Collide))
        {            
            _orientation.LookAt(hit.point);
            _rigidbody.MoveRotation(_orientation.rotation);
        }

        if (IsMoving)
        {
            Vector3 movement = _movementDirection * _tankInfo.Speed * Time.fixedDeltaTime;
            Vector3 desiredPosition = _rigidbody.position + movement;
            // Check if the tank would hit any colliders
            // Not using Physics.OverlapSphere since it doesn't work with the objects instantiated with PhotonNetwork.Instantiate
            bool wouldHitColliders = false;
            Vector3 currentRaycastPosition = _movementRaycastDirection.position;
            Quaternion currentRaycastRotation = _movementRaycastDirection.rotation;
            _movementRaycastDirection.position += movement;
            for (float angle = 0f; angle < 360f; angle += _movementRaycastAngleStep)
            {
                _movementRaycastDirection.eulerAngles = new Vector3(0f, angle, 0f);
                // Query ignores triggers (like the CameraRig, the collider for the level's boundaries or the shells)
                if (Physics.Raycast(_movementRaycastInnerPoint.position, _movementRaycastDirection.forward, out hit, _movementRaycastMagnitude,
                    _collisionsLayerMask, QueryTriggerInteraction.Ignore))
                {
                    wouldHitColliders = true;
                    break;
                }
            }
            _movementRaycastDirection.position = currentRaycastPosition;
            _movementRaycastDirection.rotation = currentRaycastRotation;
            if (!wouldHitColliders)
            {
                _rigidbody.MovePosition(desiredPosition);
            }
        }
    }

    private void PlayEngineAudio()
    {
        if (IsMoving && _engineAudioSource.clip == _engineIdleAudioClip)
        {
            _engineAudioSource.clip = _engineDrivingAudioClip;
            _engineAudioSource.pitch = Random.Range(_engineOriginalPitch - _enginePitchRange, _engineOriginalPitch + _enginePitchRange);
            _engineAudioSource.Play();
        }
        else if (!IsMoving && _engineAudioSource.clip == _engineDrivingAudioClip)
        {
            _engineAudioSource.clip = _engineIdleAudioClip;
            _engineAudioSource.pitch = Random.Range(_engineOriginalPitch - _enginePitchRange, _engineOriginalPitch + _enginePitchRange);
            _engineAudioSource.Play();
        }
    }
}
