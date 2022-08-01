using Photon.Pun;
using System.Collections;
using System.Linq;
using UnityEngine;

public class TurretShooting : MonoBehaviour
{
    private PhotonView _photonView;
    private TurretInfo _turretInfo;
    [SerializeField] private Transform _turretTopTransform;
    [SerializeField] private Animator _turretTopAnimator;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private GameObject _turretShellPrefab;
    [SerializeField] private ParticleSystem _shotParticleSystem;
    [SerializeField] private AudioSource _shotAudioSource;
    [SerializeField] private GameObject _range;
    [SerializeField] private SphereCollider _rangeCollider;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private LayerMask _turretsLayerMask;
    private GameObject _currentTarget = null;
    private bool _currentTargetIsTank;
    private const float _timeBetweenShots = 0.9f;
    private readonly WaitForSeconds _waitBetweenShots = new WaitForSeconds(_timeBetweenShots);
    private static int s_currentTurretShellId = -1000000000;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _turretInfo = GetComponent<TurretInfo>();
    }

    private void Start()
    {
        UpdateTarget();
        StartCoroutine(Shoot());
    }

    private void Update()
    {
        if (!ReferenceEquals(_currentTarget, null))
        {
            if (_currentTarget == null)
            {
                // Target got destroyed
                UpdateTarget();
                if (!ReferenceEquals(_currentTarget, null))
                {
                    _turretTopTransform.LookAt(_currentTarget.transform.position);
                }
            }
            else
            {
                _turretTopTransform.LookAt(_currentTarget.transform.position);
            }
        }
    }

    public void OnGameObjectEnteredRange(GameObject gameObj)
    {
        if (((1 << gameObj.layer) & _tanksLayerMask.value) > 0)
        {
            if (gameObj.GetComponent<TankInfo>().ActorNumber != _turretInfo.ActorNumber)
            {
                // An enemy tank entered the turret's range
                if (_currentTarget == null || !_currentTargetIsTank)
                {
                    _currentTarget = gameObj;
                    _currentTargetIsTank = true;
                }
            }
        }
        else if (((1 << gameObj.layer) & _turretsLayerMask.value) > 0)
        {
            if (gameObj.GetComponent<TurretInfo>().ActorNumber != _turretInfo.ActorNumber)
            {
                // An enemy turret was placed in the turret's range
                if (_currentTarget == null)
                {
                    _currentTarget = gameObj;
                    _currentTargetIsTank = false;
                }
            }
        }
    }

    public void OnGameObjectExitedRange(GameObject gameObj)
    {
        if (ReferenceEquals(gameObj, _currentTarget))
        {
            UpdateTarget();
        }
    }

    private void UpdateTarget()
    {
        GameObject[] enemyTanksInRange = Physics.OverlapSphere(_rangeCollider.transform.position,
            _rangeCollider.radius * _range.transform.localScale.x, _tanksLayerMask, QueryTriggerInteraction.Ignore)
            .Where(tank => tank.GetComponent<TankInfo>().ActorNumber != _turretInfo.ActorNumber)
            .Select(collider => collider.gameObject)
            .ToArray();
        if (enemyTanksInRange.Length > 0)
        {
            _currentTarget = GetNearestEnemyInRange(enemyTanksInRange);
            _currentTargetIsTank = true;
            return;
        }
        GameObject[] enemyTurretsInRange = Physics.OverlapSphere(_rangeCollider.transform.position,
            _rangeCollider.radius * _range.transform.localScale.x, _turretsLayerMask, QueryTriggerInteraction.Ignore)
            .Where(turret => turret.GetComponent<TurretInfo>().ActorNumber != _turretInfo.ActorNumber)
            .Select(collider => collider.gameObject)
            .ToArray();
        if (enemyTurretsInRange.Length > 0)
        {
            _currentTarget = GetNearestEnemyInRange(enemyTurretsInRange);
            _currentTargetIsTank = false;
            return;
        }
        _currentTarget = null;
    }

    private GameObject GetNearestEnemyInRange(GameObject[] enemiesInRange)
    {
        GameObject nearestEnemyInRange = null;
        float minSqrDistance = float.MaxValue;
        foreach (GameObject enemy in enemiesInRange)
        {
            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearestEnemyInRange = enemy;
            }
        }
        return nearestEnemyInRange;
    }

    private IEnumerator Shoot()
    {
        yield return _waitBetweenShots;
        if (!ReferenceEquals(_currentTarget, null))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _photonView.RPC("RPC_Shoot", RpcTarget.AllViaServer, s_currentTurretShellId++, _currentTarget.GetComponent<PhotonView>().ViewID);
            }
        }
        StartCoroutine(Shoot());
    }

    [PunRPC]
    private void RPC_Shoot(int turretShellId, int targetPhotonViewId)
    {        
        // TODO - Object pooling
        GameObject turretShell = Instantiate(_turretShellPrefab, _muzzle.position, _muzzle.rotation);
        turretShell.GetComponent<TurretShellMovement>().Init(_turretInfo.ShellSpeed, targetPhotonViewId);
        turretShell.GetComponent<ShellExplosion>().Init(turretShellId, _turretInfo.ActorNumber, -1, _turretInfo.Damage);
        _turretTopAnimator.SetTrigger("Shot");
        _shotParticleSystem.Play();
        _shotAudioSource.Play();
    }


    // TODO - UI for selected turrets (max 1 turret selected at a time; gets disabled if the selected turret gets destroyed)
    // TODO - Test turret stats if not default (including vision and range)
    // TODO - Shouldn't be able to start the game if there's only 1 player in the room
}