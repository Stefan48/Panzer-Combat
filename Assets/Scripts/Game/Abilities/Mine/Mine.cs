using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;

public class Mine : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    private PhotonView _photonView;
    private BoxCollider _boxCollider;
    private const float _timeToActivate = 2f;
    private const int _damage = int.MaxValue;
    private int _deployedByActorNumber;
    private bool _detonated = false;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] private LayerMask _turretsLayerMask;
    [SerializeField] private AudioClip[] _mineExplosionAudioClips;


    private void Awake()
    {
        GameManager.RoundStartingEvent += OnRoundStarting;

        _photonView = GetComponent<PhotonView>();
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void OnDestroy()
    {
        GameManager.RoundStartingEvent -= OnRoundStarting;
    }

    // This is called after Awake and before Start
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        _deployedByActorNumber = (int)instantiationData[0];
    }

    private void Start()
    {
        GetComponent<AudioSource>().Play();
        StartCoroutine(Activate());
    }

    private void OnTriggerEnter(Collider other)
    {
        // Collisions are checked only by the Master Client
        if (!PhotonNetwork.IsMasterClient || _detonated)
        {
            return;
        }
        GameObject otherGameObject = other.gameObject;
        if (((1 << otherGameObject.layer) & _tanksLayerMask.value) > 0)
        {
            // A tank triggered the mine
            Detonate();
            otherGameObject.GetComponent<TankHealth>().TakeDamage(_damage, true, _deployedByActorNumber);
        }
        else if (((1 << otherGameObject.layer) & _turretsLayerMask.value) > 0)
        {
            // A turret was placed on the mine
            Detonate();
            otherGameObject.GetComponent<TurretLifetime>().TakeDamage(_damage, true);
        }
    }

    private IEnumerator Activate()
    {
        yield return new WaitForSeconds(_timeToActivate);
        _boxCollider.enabled = true;
    }

    private void Detonate()
    {
        _detonated = true;
        int audioClipIndex = UnityEngine.Random.Range(0, _mineExplosionAudioClips.Length);
        _photonView.RPC("RPC_Detonate", RpcTarget.AllViaServer, audioClipIndex);
    }

    [PunRPC]
    private void RPC_Detonate(int audioClipIndex)
    {
        GetComponent<MeshRenderer>().enabled = false;
        _boxCollider.enabled = false;
        GameObject mineExplosion = transform.Find("MineExplosion").gameObject;
        ParticleSystem mineExplosionParticleSystem = mineExplosion.GetComponent<ParticleSystem>();
        AudioSource mineExplosionAudioSource = mineExplosion.GetComponent<AudioSource>();
        mineExplosionAudioSource.clip = _mineExplosionAudioClips[audioClipIndex];
        mineExplosionParticleSystem.Play();
        mineExplosionAudioSource.Play();
        StartCoroutine(DestroyAfterDelay(Math.Max(mineExplosionParticleSystem.main.duration, mineExplosionAudioSource.clip.length)));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnRoundStarting(int round)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
