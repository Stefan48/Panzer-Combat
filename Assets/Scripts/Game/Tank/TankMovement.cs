using Photon.Pun;
using UnityEngine;

public class TankMovement : MonoBehaviour, IPunObservable
{
    private PhotonView _photonView;
    private TankInfo _tankInfo;
    private Rigidbody _rigidbody;
    [SerializeField] private AudioSource _engineAudioSource;
    [SerializeField] private AudioClip _engineIdleAudioClip;
    [SerializeField] private AudioClip _engineDrivingAudioClip;
    private float _engineOriginalPitch;
    private const float _enginePitchRange = 0.2f;
    public bool EscPanelIsActive = false;
    [SerializeField] private Transform _orientation;
    [SerializeField] private LayerMask _groundLayerMask;
    private bool _isMoving = false;
    private bool _movingForward;
    [SerializeField] private LayerMask _defaultTanksAndTurretsLayerMask;
    [SerializeField] private Transform _movementRaycastDirection;
    [SerializeField] private Transform _movementRaycastInnerPoint;
    [SerializeField] private Transform _movementRaycastLowerPoint;
    private const float _movementRaycastAngleStep = 10f;
    private const float _movementRaycastMagnitude = 1f;


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_isMoving);
        }
        else if (stream.IsReading)
        {
            _isMoving = (bool)stream.ReceiveNext();
        }
    }

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _tankInfo = GetComponent<TankInfo>();
        _rigidbody = GetComponent<Rigidbody>();

        _engineOriginalPitch = _engineAudioSource.pitch;
        // Tanks are initially rotated towards the map's center
        _orientation.LookAt(Vector3.zero);
        _rigidbody.MoveRotation(_orientation.rotation);
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
            _isMoving = true;
            _movingForward = true;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            _isMoving = true;
            _movingForward = false;
        }
        else
        {
            _isMoving = false;
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

        if (_isMoving)
        {
            Vector3 movement = (_movingForward ?  transform.forward : -transform.forward) * _tankInfo.Speed * Time.fixedDeltaTime;
            // Check if the tank would hit any colliders
            bool wouldHitColliders = false;
            Vector3 currentRaycastPosition = _movementRaycastDirection.position;
            Quaternion currentRaycastRotation = _movementRaycastDirection.rotation;
            _movementRaycastDirection.position += movement;
            float angleStart = _movingForward ? -90f : 90f;
            float angleStop = angleStart + 180f;
            for (float angle = angleStart; angle <= angleStop; angle += _movementRaycastAngleStep)
            {
                _movementRaycastDirection.localEulerAngles = new Vector3(0f, angle, 0f);
                if (Physics.Raycast(_movementRaycastInnerPoint.position, _movementRaycastDirection.forward, out hit, _movementRaycastMagnitude,
                    _defaultTanksAndTurretsLayerMask, QueryTriggerInteraction.Ignore))
                {
                    wouldHitColliders = true;
                    break;
                }
                else if (Physics.Raycast(_movementRaycastLowerPoint.position, _movementRaycastDirection.forward, out hit, _movementRaycastMagnitude,
                    _defaultTanksAndTurretsLayerMask, QueryTriggerInteraction.Ignore))
                {
                    wouldHitColliders = true;
                    break;
                }
            }
            _movementRaycastDirection.position = currentRaycastPosition;
            _movementRaycastDirection.rotation = currentRaycastRotation;
            if (!wouldHitColliders)
            {
                Vector3 desiredPosition = _rigidbody.position + movement;
                _rigidbody.MovePosition(desiredPosition);
            }
        }
    }

    private void PlayEngineAudio()
    {
        if (!_engineAudioSource.enabled)
        {
            return;
        }
        if (_isMoving && _engineAudioSource.clip == _engineIdleAudioClip)
        {
            _engineAudioSource.clip = _engineDrivingAudioClip;
            _engineAudioSource.pitch = Random.Range(_engineOriginalPitch - _enginePitchRange, _engineOriginalPitch + _enginePitchRange);
            _engineAudioSource.Play();
        }
        else if (!_isMoving && _engineAudioSource.clip == _engineDrivingAudioClip)
        {
            _engineAudioSource.clip = _engineIdleAudioClip;
            _engineAudioSource.pitch = Random.Range(_engineOriginalPitch - _enginePitchRange, _engineOriginalPitch + _enginePitchRange);
            _engineAudioSource.Play();
        }
    }
}
