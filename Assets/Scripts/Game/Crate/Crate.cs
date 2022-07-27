using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class Crate : MonoBehaviour
{
    protected PhotonView _photonView;
    private const float _lifetime = 7f;
    private const float _destroyDelay = 4f;
    [SerializeField] private MeshRenderer _wholeCrateMeshRenderer;
    [SerializeField] private BoxCollider _boxCollider;
    [SerializeField] private GameObject _fracturedCrate;
    [SerializeField] private GameObject _minimapIcon;
    [SerializeField] private LayerMask _tanksLayerMask;
    [SerializeField] protected TextMesh _onCollectText;
    [SerializeField] private Animator _onCollectTextAnimator;
    [SerializeField] private AudioSource _crateAudioSource;
    private readonly Quaternion _onCollectTextRotation = Quaternion.Euler(0f, 60f, 0f);
    private bool _collected = false;
    private bool _shatterPending = false;


    protected virtual void Awake()
    {
        GameManager.RoundEndingEvent += OnRoundEnding;
        _photonView = GetComponent<PhotonView>();
        transform.Find("OnCollectText").rotation = _onCollectTextRotation;
        StartCoroutine(Shatter(_lifetime));
    }

    private void OnDestroy()
    {
        GameManager.RoundEndingEvent -= OnRoundEnding;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Crate collisions are checked only by the Master Client
        if (!PhotonNetwork.IsMasterClient || _collected)
        {
            return;
        }
        if (((1 << other.gameObject.layer) & _tanksLayerMask.value) > 0)
        {
            _collected = true;
            OnCollect(other.gameObject);
            StartCoroutine(Shatter(0f));
        }
    }

    private void OnCollect(GameObject tank)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(tank.GetComponent<TankInfo>().ActorNumber);
        if (player != null)
        {
            _photonView.RPC("RPC_OnCollect", player, GetOnCollectText(tank));
            RewardPlayer(tank);
        }
    }

    [PunRPC]
    protected virtual void RPC_OnCollect(string onCollectText)
    {
        _onCollectText.text = onCollectText;
        _onCollectTextAnimator.SetTrigger("OnCollectTrigger");
        _crateAudioSource.Play();
    }

    protected virtual string GetOnCollectText(GameObject tank)
    {
        return "+ bonus";
    }

    protected virtual void RewardPlayer(GameObject tank) { }

    protected IEnumerator Shatter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient)
        {
            if (!_shatterPending)
            {
                _shatterPending = true;
                _photonView.RPC("RPC_Shatter", RpcTarget.AllViaServer);
            }
        }
    }

    [PunRPC]
    protected virtual void RPC_Shatter()
    {
        _wholeCrateMeshRenderer.enabled = false;
        _boxCollider.enabled = false;
        _fracturedCrate.SetActive(true);
        _minimapIcon.SetActive(false);
        // PhotonNetwork.Destroy does not have an overload for destroying the GameObject after a delay
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnRoundEnding(PlayerInfo roundWinner, bool isGameWinner)
    {
        StartCoroutine(Shatter(0f));
    }
}


public enum CrateType
{
    Ability,
    Ammo,
    Armor,
    Damage,
    MaxHealth,
    Range,
    RestoreHealth,
    Speed,
    Tank
}
