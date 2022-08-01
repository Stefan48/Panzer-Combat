using UnityEngine;
using System.Collections;
using System;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class ShellExplosion : MonoBehaviourPunCallbacks
{
    [SerializeField] private LayerMask _defaultLayerMask;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private LayerMask _shellsLayerMask;
    [SerializeField] private LayerMask _turretsLayerMask;
    [SerializeField] private LayerMask _defaultTanksShellsAndTurretsLayerMask;
    private int _id; // unique shell identifier
    private int _actorNumber;
    private int _tankNumber;
    private int _damage;
    [SerializeField] private List<Transform> _raycastOrigins = new List<Transform>();
    private const float _raycastMagnitude = 1f;
    private bool _hitSomething = false;
    private bool _explosionPending = false;
    private const float _maxDeflectionRotation = 30f;
    private bool _deflectionPending = false;
    [SerializeField] private AudioSource _shellExplosionAudioSource;
    [SerializeField] private AudioClip _shellDeflectionAudioClip;
    private const float _shellDeflectionAudioVolumeScale = 0.6f;

    private static readonly byte s_shellExplosionNetworkEvent = 0;
    private static readonly byte s_shellDeflectionNetworkEvent = 2;


    private void Awake()
    {
        GameManager.RoundEndingEvent += OnRoundEnding;
    }

    private void OnDestroy()
    {
        GameManager.RoundEndingEvent -= OnRoundEnding;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += NetworkingClient_EventReceived;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= NetworkingClient_EventReceived;
    }

    public void Init(int id, int actorNumber, int tankNumber, int damage)
    {
        _id = id;
        _actorNumber = actorNumber;
        _tankNumber = tankNumber;
        _damage = damage;
    }

    private void Update()
    {
        // Shell collisions are checked only by the Master Client
        if (!PhotonNetwork.IsMasterClient || _hitSomething)
        {
            return;
        }
        CheckForCollision();
    }

    // TODO - No more collisions between shells? (trying to shoot a turret that is targeting you is pretty awkward)
    private void OnTriggerEnter(Collider other)
    {
        // Shell collisions are checked only by the Master Client
        if (!PhotonNetwork.IsMasterClient || _hitSomething)
        {
            return;
        }
        GameObject otherGameObject = other.gameObject;
        if (((1 << otherGameObject.layer) & _tanksLayerMask.value) > 0)
        {
            if (_tankNumber != otherGameObject.GetComponent<TankInfo>().TankNumber)
            {
                // The shell hit a tank (different from the one which shot it) in short range
                if (otherGameObject.GetComponent<TankAbilities>().DeflectShellsAbilityActive)
                {
                    OnShellGotDeflected();
                }
                else
                {
                    OnHitWithoutCheck();
                    otherGameObject.GetComponent<TankHealth>().TakeDamage(_damage, false, _actorNumber);
                }
            }
            // Else, the shell could have hit the tank which shot it right after it was shot
            // But it also could have been deflected off another tank which was close enough
        }
        else if (((1 << otherGameObject.layer) & _defaultLayerMask.value) > 0)
        {
            // The shell hit the environment in short range
            OnHitWithoutCheck();
        }
        else if (((1 << otherGameObject.layer) & _shellsLayerMask.value) > 0)
        {
            ShellExplosion otherShell = otherGameObject.GetComponent<ShellExplosion>();
            if (_actorNumber != otherShell._actorNumber)
            {
                // The shell hit an enemy shell
                OnHitWithoutCheck();
            }
        }
        else if (((1 << otherGameObject.layer) & _turretsLayerMask.value) > 0)
        {
            // The shell hit a turret in short range
            OnHitWithoutCheck();
            otherGameObject.GetComponent<TurretLifetime>().TakeDamage(_damage, false);
        }
    }

    // Checks for potential collisions are done using raycasts for lag compensation
    private void CheckForCollision()
    {
        // Theoretically, a (large enough) shell could hit multiple tanks, but in reality (with its current size) it passes right between them
        foreach (Transform origin in _raycastOrigins)
        {
            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, _raycastMagnitude,
                _defaultTanksShellsAndTurretsLayerMask, QueryTriggerInteraction.Collide))
            {                
                GameObject hitGameObject = hit.collider.gameObject;
                if (((1 << hitGameObject.layer) & _tanksLayerMask.value) > 0)
                {
                    // The shell hit a tank
                    if (hitGameObject.GetComponent<TankAbilities>().DeflectShellsAbilityActive)
                    {
                        if (!_hitSomething)
                        {
                            OnShellGotDeflected();
                        }
                    }
                    else
                    {
                        _hitSomething = true;
                        hitGameObject.GetComponent<TankHealth>().TakeDamage(_damage, false, _actorNumber);
                    }
                    // Breaking so the tank doesn't take more damage if more raycasts hit
                    break;
                }
                else if (((1 << hitGameObject.layer) & _defaultLayerMask.value) > 0)
                {
                    // The shell hit the environment
                    _hitSomething = true;
                }
                else if (((1 << hitGameObject.layer) & _shellsLayerMask.value) > 0)
                {
                    ShellExplosion otherShell = hitGameObject.GetComponent<ShellExplosion>();
                    if (_actorNumber != otherShell._actorNumber)
                    {
                        // The shell hit an enemy shell (which did not necessarily detect the collision)
                        _hitSomething = true;
                        otherShell.OnHitWithCheck();
                    }
                }
                else if (((1 << hitGameObject.layer) & _turretsLayerMask.value) > 0)
                {
                    // The shell hit a turret
                    _hitSomething = true;
                    hitGameObject.GetComponent<TurretLifetime>().TakeDamage(_damage, false);
                    break;
                }
            }
        }
        if (_hitSomething)
        {
            StartCoroutine(Explosion(0f));
        }
    }

    private void OnShellGotDeflected()
    {
        if (_deflectionPending)
        {
            return;
        }
        _deflectionPending = true;
        float rotation = UnityEngine.Random.Range(-_maxDeflectionRotation, _maxDeflectionRotation);
        PhotonNetwork.RaiseEvent(s_shellDeflectionNetworkEvent, new float[] { _id, rotation },
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }

    private void OnHitWithCheck()
    {
        if (!_hitSomething)
        {
            _hitSomething = true;
            StartCoroutine(Explosion(0f));
        }
    }

    private void OnHitWithoutCheck()
    {
        _hitSomething = true;
        StartCoroutine(Explosion(0f));
    }

    public void OnRangeReachedOrTargetGotDestroyed()
    {
        StartCoroutine(Explosion(0f));
    }

    private IEnumerator Explosion(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient)
        {
            if (!_explosionPending)
            {
                _explosionPending = true;
                // Using events since shells don't have a PhotonView component (they're not instantiated over the network for optimization reasons)
                PhotonNetwork.RaiseEvent(s_shellExplosionNetworkEvent, _id, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            }
        }
    }

    private void NetworkingClient_EventReceived(EventData obj)
    {
        if (obj.Code == s_shellExplosionNetworkEvent)
        {
            if ((int)obj.CustomData == _id)
            {
                Explode();
            }
        }
        else if (obj.Code == s_shellDeflectionNetworkEvent)
        {
            float[] customData = (float[])obj.CustomData;
            if (customData[0] == _id)
            {
                _deflectionPending = false;
                float newRotation = transform.eulerAngles.y + 180f + customData[1];
                transform.eulerAngles = new Vector3(0f, newRotation, 0f);
                // Play the deflection sound using the shell explosion AudioSource so that it can't get canceled by the explosion sound
                _shellExplosionAudioSource.PlayOneShot(_shellDeflectionAudioClip, _shellDeflectionAudioVolumeScale);
            }
        }
    }

    private void Explode()
    {
        GameObject shellExplosion = transform.Find("ShellExplosion").gameObject;
        ParticleSystem shellExplosionParticleSystem = shellExplosion.GetComponent<ParticleSystem>();
        // Detach shellExplosion from the shell GameObject
        shellExplosion.transform.parent = null;
        shellExplosionParticleSystem.Play();
        _shellExplosionAudioSource.PlayOneShot(_shellExplosionAudioSource.clip);
        Destroy(shellExplosion, Math.Max(shellExplosionParticleSystem.main.duration, _shellExplosionAudioSource.clip.length));
        Destroy(gameObject);
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        StartCoroutine(Explosion(0f));
    }
}
