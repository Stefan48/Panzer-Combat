using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using VolumetricLines;

public class LaserBeam : MonoBehaviour
{
    private TankInfo _tankInfo;
    private VolumetricLineBehavior _volumetricLineBehavior;
    private const float _maxVolumetricLineStartPos = 100f;
    private const float _initialLengthPercentage = 0.05f;
    private float _currentLengthPercentage = _initialLengthPercentage;
    private const float _lengthPercentageIncreaseStep = 0.1f;
    [SerializeField] private LayerMask _defaultTanksShellsAndTurretsLayerMask;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private LayerMask _turretsLayerMask;
    private const float _raycastMaxMagnitude = 10f;
    private bool _deactivationPending = false;
    [SerializeField] private AudioSource _audioSource;
    // The dictionary maps damaged tanks and turrets to timestamps, in order to avoid calling RPC_TakeDamage every frame
    private Dictionary<GameObject, float> _damagedTanksAndTurretsTimestamps = new Dictionary<GameObject, float>();
    private const float _minTimeBetweenTicks = 0.25f;
    private const int _damagePerTick = 20;


    private void Awake()
    {
        _tankInfo = transform.parent.GetComponent<TankInfo>();
        _volumetricLineBehavior = GetComponent<VolumetricLineBehavior>();
    }

    private void Update()
    {
        if (_deactivationPending)
        {
            _currentLengthPercentage = Math.Max(_initialLengthPercentage, _currentLengthPercentage - _lengthPercentageIncreaseStep);
            if (_currentLengthPercentage == _initialLengthPercentage)
            {
                _deactivationPending = false;
                ScaleLaserBeamToPercentage(_currentLengthPercentage);
                gameObject.SetActive(false);
                return;
            }
        }
        else if (_currentLengthPercentage < 1f)
        {
            _currentLengthPercentage = Math.Min(1f, _currentLengthPercentage + _lengthPercentageIncreaseStep);
        }
        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hit, _currentLengthPercentage * _raycastMaxMagnitude,
                _defaultTanksShellsAndTurretsLayerMask, QueryTriggerInteraction.Collide))
        {
            // Collisions are checked only by the Master Client
            if (PhotonNetwork.IsMasterClient)
            {
                GameObject hitGameObject = hit.collider.gameObject;
                if (((1 << hitGameObject.layer) & _tanksLayerMask.value) > 0)
                {
                    // The laser hit a tank
                    ApplyDamageBasedOnTimestamps(hitGameObject, true);
                }
                else if (((1 << hitGameObject.layer) & _turretsLayerMask.value) > 0)
                {
                    // The laser hit a turret
                    ApplyDamageBasedOnTimestamps(hitGameObject, false);
                }
            }
            float newLengthPercentage = hit.distance / _raycastMaxMagnitude;
            ScaleLaserBeamToPercentage(newLengthPercentage);
        }
        else
        {
            ScaleLaserBeamToPercentage(_currentLengthPercentage);
        }
    }

    private void ApplyDamageBasedOnTimestamps(GameObject hitGameObject, bool isTank)
    {
        float currentTime = Time.time;
        if (_damagedTanksAndTurretsTimestamps.ContainsKey(hitGameObject))
        {
            if (currentTime - _damagedTanksAndTurretsTimestamps[hitGameObject] >= _minTimeBetweenTicks)
            {
                ApplyDamageTick(hitGameObject, isTank);
                _damagedTanksAndTurretsTimestamps[hitGameObject] = currentTime;
            }
        }
        else
        {
            ApplyDamageTick(hitGameObject, isTank);
            _damagedTanksAndTurretsTimestamps.Add(hitGameObject, currentTime);
        }
    }

    private void ApplyDamageTick(GameObject hitGameObject, bool isTank)
    {
        if (isTank)
        {
            hitGameObject.GetComponent<TankHealth>().TakeDamage(_damagePerTick, true, _tankInfo.ActorNumber);
        }
        else
        {
            hitGameObject.GetComponent<TurretLifetime>().TakeDamage(_damagePerTick, true);
        }
    }

    private void ScaleLaserBeamToPercentage(float percentage)
    {
        _volumetricLineBehavior.StartPos = new Vector3(0f, percentage * _maxVolumetricLineStartPos, 0f);
    }

    public void Activate()
    {
        _deactivationPending = false;
        gameObject.SetActive(true);
        _audioSource.Play();
        _damagedTanksAndTurretsTimestamps.Clear();
    }

    public void Deactivate()
    {
        _deactivationPending = true;
    }
}
