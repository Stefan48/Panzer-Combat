using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    private PhotonView _photonView;
    private TankInfo _tankInfo;
    [SerializeField] private Slider _healthBarSlider;
    [SerializeField] private Image _healthBarFillImage;
    [SerializeField] private Color _maxHealthColor = new Color32(0, 255, 0, 180);
    [SerializeField] private Color _minHealthColor = new Color32(255, 0, 0, 180);
    [SerializeField] private GameObject _tankExplosionPrefab;

    public static event Action<GameObject> TankGotDestroyedEvent;
    

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _tankInfo = GetComponent<TankInfo>();
    }

    private void Start()
    {
        _healthBarSlider.maxValue = _tankInfo.MaxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        _photonView.RPC("RPC_TakeDamage", RpcTarget.AllViaServer, damage);
        //RPC_TakeDamage(damage); // this is for testing only
    }

    [PunRPC]
    private void RPC_TakeDamage(float damage)
    {
        // TODO - Armor
        _tankInfo.Health -= damage;
        UpdateHealthBar();
        if (_tankInfo.Health <= 0f)
        {
            PlayDeathEffects();
            if (_photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
                TankGotDestroyedEvent?.Invoke(gameObject);
            }
        }
    }

    private void UpdateHealthBar()
    {
        _healthBarSlider.value = _tankInfo.Health;
        _healthBarFillImage.color = Color.Lerp(_minHealthColor, _maxHealthColor, _tankInfo.Health / _tankInfo.MaxHealth);
    }

    private void PlayDeathEffects()
    {
        GameObject tankExplosion = Instantiate(_tankExplosionPrefab, transform.position, transform.rotation);
        ParticleSystem tankExplosionParticleSystem = tankExplosion.GetComponent<ParticleSystem>();
        AudioSource tankExplosionAudioSource = tankExplosion.GetComponent<AudioSource>();
        tankExplosionParticleSystem.Play();
        tankExplosionAudioSource.Play();
        Destroy(tankExplosion, Math.Max(tankExplosionParticleSystem.main.duration, tankExplosionAudioSource.clip.length));
    }
}
