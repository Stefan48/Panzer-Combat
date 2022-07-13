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

    private static readonly byte s_shellExplosionEvent = 0;


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

    public void Init(int id, int damage, float lifetime)
    {
        _id = id;
        _actorNumber = TankShooting.GetOwnerActorNumberOfShell(_id);
        _damage = damage;
        StartCoroutine(Explosion(lifetime));
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
                OnHitWithoutCheck();
                otherGameObject.GetComponent<TankHealth>().TakeDamage(_damage);
            }
            // Else, the shell would have hit the tank which shot it right after it was shot
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
                    _hitSomething = true;
                    hitGameObject.GetComponent<TankHealth>().TakeDamage(_damage);
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

    private IEnumerator Explosion(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient)
        {
            if (!_explosionPending)
            {
                _explosionPending = true;
                // Using events since shells don't have a PhotonView component (they're not instantiated over the network for optimization reasons)
                PhotonNetwork.RaiseEvent(s_shellExplosionEvent, _id, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            }
        }
    }

    private void NetworkingClient_EventReceived(EventData obj)
    {
        if (obj.Code == s_shellExplosionEvent)
        {
            if ((int)obj.CustomData == _id)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        GameObject shellExplosion = transform.Find("ShellExplosion").gameObject;
        ParticleSystem shellExplosionParticleSystem = shellExplosion.GetComponent<ParticleSystem>();
        AudioSource shellExplosionAudioSource = shellExplosion.GetComponent<AudioSource>();
        // Detach shellExplosion from the shell GameObject
        shellExplosion.transform.parent = null;
        shellExplosionParticleSystem.Play();
        shellExplosionAudioSource.Play();
        Destroy(shellExplosion, Math.Max(shellExplosionParticleSystem.main.duration, shellExplosionAudioSource.clip.length));
        Destroy(gameObject);
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        StartCoroutine(Explosion(0f));
    }
}
