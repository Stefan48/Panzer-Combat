using UnityEngine;
using System.Collections;
using System;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ShellExplosion : MonoBehaviourPunCallbacks
{
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private LayerMask _noCollisionsLayerMask;
    public int Id; // unique shell identifier
    public float Damage;
    public float Lifetime;
    private bool _explosionPending = false;

    private static readonly byte s_shellExplosionEvent = 0;


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

    private void OnTriggerEnter(Collider other)
    {
        // Shell collisions are checked only by the Master Client
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        if (((1 << other.gameObject.layer) & _tanksLayerMask.value) > 0)
        {
            // The shell hit a tank
            other.gameObject.GetComponent<TankHealth>().TakeDamage(Damage);
            StartCoroutine(Explosion(0f));
        }
        else if (((1 << other.gameObject.layer) & _noCollisionsLayerMask.value) == 0)
        {
            // The shell hit the environment
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
