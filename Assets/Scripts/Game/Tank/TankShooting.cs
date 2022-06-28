using Photon.Pun;
using UnityEngine;

public class TankShooting : MonoBehaviour
{
    private PhotonView _photonView;
    private TankInfo _tankInfo;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private GameObject _shellPrefab;
    [SerializeField] private AudioSource _shotFiredAudioSource;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (!_photonView.IsMine)
        {
            enabled = false;
        }
        _tankInfo = GetComponent<TankInfo>();
    }

    private void Update()
    {
        if (_tankInfo.IsSelected)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        // TODO - Object pooling
        // TODO - RPC
        GameObject shell = Instantiate(_shellPrefab, _muzzle.position, _muzzle.rotation);
        shell.GetComponent<ShellMovement>().speed = _tankInfo.ShellSpeed;
        _shotFiredAudioSource.Play();
    }
}
