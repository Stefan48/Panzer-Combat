using UnityEngine;
using System.Collections;
using System;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class ShellExplosion : MonoBehaviourPunCallbacks
{
    [SerializeField] private LayerMask _tanksLayerMask;
    public int Id; // unique shell identifier
    public float Damage;
    public float Lifetime;
    [SerializeField] private List<Transform> _raycastOrigins = new List<Transform>();
    private const float _raycastMagnitude = 1f;
    private static int s_collisionsLayerMask = 0;
    private bool _hitSomething = false;
    private bool _explosionPending = false;

    private static readonly byte s_shellExplosionEvent = 0;


    private void Awake()
    {
        if (s_collisionsLayerMask == 0)
        {
            s_collisionsLayerMask = LayerMask.GetMask("Default", "Tanks");
        }
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

    public void Init(int id, float damage, float lifetime)
    {
        Id = id;
        Damage = damage;
        Lifetime = lifetime;
        StartCoroutine(Explosion(Lifetime));
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

    // Checks for potential collisions using raycasts (instead of implementing OnTriggerEnter) for lag compensation
    private void CheckForCollision()
    {
        bool hitTank = false;
        GameObject tankHit = null;
        foreach (Transform origin in _raycastOrigins)
        {
            // Query ignores triggers (like the CameraRig, the collider for the level's boundaries or the shells)
            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, _raycastMagnitude,
                s_collisionsLayerMask, QueryTriggerInteraction.Ignore))
            {
                _hitSomething = true;
                if (((1 << hit.collider.gameObject.layer) & _tanksLayerMask.value) > 0)
                {
                    hitTank = true;
                    tankHit = hit.collider.gameObject;
                    break;
                }
            }
        }
        if (hitTank)
        {
            tankHit.GetComponent<TankHealth>().TakeDamage(Damage);
            StartCoroutine(Explosion(0f));
        }
        else if (_hitSomething)
        {
            StartCoroutine(Explosion(0f));
        }
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
                PhotonNetwork.RaiseEvent(s_shellExplosionEvent, Id, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            }
        }
    }

    private void NetworkingClient_EventReceived(EventData obj)
    {
        if (obj.Code == s_shellExplosionEvent)
        {
            if ((int)obj.CustomData == Id)
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
}
