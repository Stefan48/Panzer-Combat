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
    [SerializeField] private LayerMask _defaultTanksAndShellsLayerMask;
    private int _id; // unique shell identifier
    private int _actorNumber;
    private int _damage;
    [SerializeField] private List<Transform> _raycastOrigins = new List<Transform>();
    private const float _raycastMagnitude = 1f;
    private bool _hitSomething = false;
    private bool _explosionPending = false;
    private const float _maxDeflectionRotation = 30f;
    private bool _deflectionPending = false;
    [SerializeField] private AudioSource _shellExplosionAudioSource;
    [SerializeField] private AudioClip _shellDeflectionAudioClip;
    private const float _shellDeflectionAudioVolume = 0.6f;

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

    public void Init(int id, int damage)
    {
        _id = id;
        _actorNumber = TankShooting.GetOwnerActorNumberOfShell(_id);
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
            if (_actorNumber != otherGameObject.GetComponent<TankInfo>().ActorNumber)
            {
                // The shell hit an enemy tank in short range
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
            // Else, the shell could have hit the tank which shot it right after it was shot (or another allied tank in short range)
            // TODO - Unique identifiers for tanks, stored in the shell script as well for extra checks
        }
        else if (((1 << otherGameObject.layer) & _defaultLayerMask.value) > 0)
        {
            // The shell hit the environment in short range
            OnHitWithoutCheck();
        }
        else if (((1 << otherGameObject.layer) & _shellsLayerMask.value) > 0)
        {
            ShellExplosion otherShell = otherGameObject.GetComponent<ShellExplosion>();
            if (_actorNumber != TankShooting.GetOwnerActorNumberOfShell(otherShell._id))
            {
                // The shell hit an enemy shell
                OnHitWithoutCheck();
            }
        }
    }

    // Checks for potential collisions are done using raycasts for lag compensation
    private void CheckForCollision()
    {
        // Theoretically, a (large enough) shell could hit multiple tanks, but in reality (with its current size) it passes right between them
        foreach (Transform origin in _raycastOrigins)
        {
            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, _raycastMagnitude,
                _defaultTanksAndShellsLayerMask, QueryTriggerInteraction.Collide))
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
                    if (_actorNumber != TankShooting.GetOwnerActorNumberOfShell(otherShell._id))
                    {
                        // The shell hit an enemy shell (which did not necessarily detect the collision)
                        _hitSomething = true;
                        otherShell.OnHitWithCheck();
                    }
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

    public void OnRangeReached()
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
                _shellExplosionAudioSource.PlayOneShot(_shellDeflectionAudioClip, _shellDeflectionAudioVolume);
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
