using Photon.Pun;
using UnityEngine;

public class TankMovement : MonoBehaviour
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
    [SerializeField] private Transform _orientation;
    [SerializeField] private LayerMask _groundLayerMask;
    private bool _isMoving = false;
    private Vector3 _movementDirection;
    

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (!_photonView.IsMine)
        {
            enabled = false;
        }
        _tankInfo = GetComponent<TankInfo>();
        _rigidbody = GetComponent<Rigidbody>();
        _sphereCollider = GetComponent<SphereCollider>();

    }

    private void Start()
    {
        _engineOriginalPitch = _engineAudioSource.pitch;
    }

    private void Update()
    {
        if (_tankInfo.IsSelected)
        {
            ProcessMovementInput();
        }
        PlayEngineAudio();
    }

    private void FixedUpdate()
    {
        if (_tankInfo.IsSelected)
        {
            ApplyMovement();
        }
    }

    private void ProcessMovementInput()
    {
        _isMoving = false;
        if (Input.GetKey(KeyCode.W))
        {
            _isMoving = true;
            _movementDirection = transform.forward;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            _isMoving = true;
            _movementDirection = -transform.forward;
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
            Vector3 movement = _movementDirection * _tankInfo.Speed * Time.fixedDeltaTime;
            Vector3 desiredPosition = _rigidbody.position + movement;
            bool wouldHitColliders = false;
            // Query ignores triggers (like the Camera Rig, the collider for the level's boundaries or the shells)
            Collider[] collidersThatWouldBeHit = Physics.OverlapSphere(desiredPosition + _sphereCollider.center, _sphereCollider.radius,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < collidersThatWouldBeHit.Length; ++i)
            {
                if (collidersThatWouldBeHit[i].name != transform.name)
                {
                    wouldHitColliders = true;
                    break;
                }
            }
            if (!wouldHitColliders)
            {
                _rigidbody.MovePosition(desiredPosition);
            }
        }
    }

    private void PlayEngineAudio()
    {
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
