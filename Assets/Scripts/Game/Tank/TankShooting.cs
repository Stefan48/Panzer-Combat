using Photon.Pun;
using UnityEngine;

public class TankShooting : MonoBehaviour
{
    private PhotonView _photonView;
    private TankInfo _tankInfo;
    private TankAbilities _tankAbilities;
    public bool EscPanelIsActive = false;
    public static readonly int ShellIdMultiplier = 1000000;
    public int CurrentShellId = -1; // this gets set in the TankInfo script
    private static readonly int s_maxShellsShotAtOnce = 3;
    [SerializeField] private Transform _muzzles;
    [SerializeField] private Transform _muzzleCenter;
    [SerializeField] private Transform _muzzleLeft;
    [SerializeField] private Transform _muzzleRight;
    [SerializeField] private GameObject _shellPrefab;
    [SerializeField] private AudioSource _shotFiredAudioSource;
    [SerializeField] private AudioSource _warningAudioSource;
    [SerializeField] private AudioClip _noAmmoAudioClip;
    private const float _noAmmoAudioClipVolumeScale = 0.5f;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (!_photonView.IsMine)
        {
            enabled = false;
        }
        _tankInfo = GetComponent<TankInfo>();
        _tankAbilities = GetComponent<TankAbilities>();
    }

    private void Update()
    {
        if (!EscPanelIsActive)
        {
            if (_tankInfo.IsSelected)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    Shoot();
                }
            }
        }
    }

    private void Shoot()
    {
        if (_tankAbilities.LaserBeamAbilityActive)
        {
            return;
        }
        if (_tankInfo.Ammo == 0)
        {
            _warningAudioSource.PlayOneShot(_noAmmoAudioClip, _noAmmoAudioClipVolumeScale);
            return;
        }
        // Send the position and rotation of the muzzle so that all clients can shoot in sync
        _photonView.RPC("RPC_Shoot", RpcTarget.AllViaServer, CurrentShellId, _muzzles.position.x, _muzzles.position.z, _muzzles.eulerAngles.y);
        CurrentShellId += s_maxShellsShotAtOnce;
    }

    [PunRPC]
    private void RPC_Shoot(int shellId, float muzzlesPositionX, float muzzlesPositionZ, float muzzlesRotationY)
    {
        // Due to the latency, the ammo might already be 0
        if (_tankInfo.Ammo == 0)
        {
            return;
        }
        _tankInfo.Ammo--;
        // Set the position and rotation of the muzzle to match the tank owner's
        _muzzles.position = new Vector3(muzzlesPositionX, _muzzles.position.y, muzzlesPositionZ);
        _muzzles.rotation = Quaternion.Euler(0f, muzzlesRotationY, 0f);
        // TODO - Object pooling
        GameObject shell = Instantiate(_shellPrefab, _muzzleCenter.position, _muzzleCenter.rotation);
        shell.GetComponent<ShellMovement>().Init(_tankInfo.ShellSpeed, _tankInfo.Range);
        shell.GetComponent<ShellExplosion>().Init(shellId, _tankInfo.ActorNumber, _tankInfo.TankNumber, _tankInfo.Damage);
        if (_tankAbilities.TripleShellsAbilityActive)
        {
            shell = Instantiate(_shellPrefab, _muzzleLeft.position, _muzzleLeft.rotation);
            shell.GetComponent<ShellMovement>().Init(_tankInfo.ShellSpeed, _tankInfo.Range);
            shell.GetComponent<ShellExplosion>().Init(shellId + 1, _tankInfo.ActorNumber, _tankInfo.TankNumber, _tankInfo.Damage);
            shell = Instantiate(_shellPrefab, _muzzleRight.position, _muzzleRight.rotation);
            shell.GetComponent<ShellMovement>().Init(_tankInfo.ShellSpeed, _tankInfo.Range);
            shell.GetComponent<ShellExplosion>().Init(shellId + 2, _tankInfo.ActorNumber, _tankInfo.TankNumber, _tankInfo.Damage);
        }
        _shotFiredAudioSource.Play();
        // Reset the position and rotation of the muzzle
        _muzzles.position = transform.position;
        _muzzles.rotation = transform.rotation;
    }
}
