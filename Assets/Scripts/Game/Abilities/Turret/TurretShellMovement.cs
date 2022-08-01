using Photon.Pun;
using UnityEngine;

public class TurretShellMovement : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private ShellExplosion _shellExplosion;
    [SerializeField] private Transform _orientation;
    // Turret shells are faster than regular shells
    private int _speed = 25;
    private GameObject _target = null;
    
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _shellExplosion = GetComponent<ShellExplosion>();
    }

    public void Init(int speed, int targetPhotonViewId)
    {
        _speed = speed;
        PhotonView photonView = PhotonView.Find(targetPhotonViewId);
        if (photonView == null)
        {
            _shellExplosion.OnRangeReachedOrTargetGotDestroyed();
        }
        else
        {
            _target = photonView.gameObject;
        }
    }

    private void FixedUpdate()
    {
        if (_target != null)
        {
            Vector3 targetPosition = _target.transform.position;
            _orientation.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
            _rigidbody.MoveRotation(_orientation.rotation);
        }
        else if (!ReferenceEquals(_target, null))
        {
            _shellExplosion.OnRangeReachedOrTargetGotDestroyed();
        }
        // Else, the target has not been set yet
        Vector3 movement = transform.forward * _speed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }
}
