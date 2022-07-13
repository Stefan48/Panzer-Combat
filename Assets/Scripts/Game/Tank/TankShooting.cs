using Photon.Pun;
using System.Collections;
using UnityEngine;

public class TankShooting : MonoBehaviour
{
    private PhotonView _photonView;
    private TankInfo _tankInfo;
    private static readonly int s_shellIdMultiplier = 10000000;
    private static int s_currentShellId = 0;
    public bool EscPanelIsActive = false;
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
        if (s_currentShellId == 0)
        {
            s_currentShellId = PhotonNetwork.LocalPlayer.ActorNumber * s_shellIdMultiplier;
        }
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
        _photonView.RPC("RPC_Shoot", RpcTarget.AllViaServer, ++s_currentShellId);
    }

    [PunRPC]
    private void RPC_Shoot(int shellId)
    {
        // TODO - Object pooling
        GameObject shell = Instantiate(_shellPrefab, _muzzle.position, _muzzle.rotation);
        shell.GetComponent<ShellMovement>().Init(_tankInfo.ShellSpeed);
        shell.GetComponent<ShellExplosion>().Init(shellId, _tankInfo.Damage, _tankInfo.ShellLifetime);
        _shotFiredAudioSource.Play();
    }

    public static int GetOwnerActorNumberOfShell(int shellId)
    {
        return shellId / s_shellIdMultiplier;
    }
}
