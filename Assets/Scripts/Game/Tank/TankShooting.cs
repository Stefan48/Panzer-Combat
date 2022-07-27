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
    [SerializeField] private Transform _muzzleCenter;
    [SerializeField] private Transform _muzzleLeft;
    [SerializeField] private Transform _muzzleRight;
    [SerializeField] private GameObject _shellPrefab;
    [SerializeField] private AudioSource _shotFiredAudioSource;
    [SerializeField] private AudioSource _noAmmoAudioSource;


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

    /*
    // for testing
    private void Start()
    {
        StartCoroutine(ShootCoroutine());
    }
    private IEnumerator ShootCoroutine()
    {
        for(;;)
        {
            yield return new WaitForSeconds(1f);
            Shoot();
        }
    }
    */

    private void Shoot()
    {
        if (_tankAbilities.LaserBeamAbilityActive)
        {
            return;
        }
        if (_tankInfo.Ammo == 0)
        {
            _noAmmoAudioSource.Play();
            return;
        }
        _photonView.RPC("RPC_Shoot", RpcTarget.AllViaServer, CurrentShellId);
        CurrentShellId += s_maxShellsShotAtOnce;
    }

    [PunRPC]
    private void RPC_Shoot(int shellId)
    {
        // Due to the latency, the ammo might already be 0
        if (_tankInfo.Ammo == 0)
        {
            return;
        }
        _tankInfo.Ammo--;        
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
    }
}
