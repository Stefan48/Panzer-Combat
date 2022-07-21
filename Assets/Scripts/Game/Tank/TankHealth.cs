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

    public static event Action<GameObject> AlliedTankGotDestroyedEvent;
    

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

    public void TakeDamage(int damage)
    {
        // It's possible that the tank's health dropped to 0 but PhotonNetwork.Destroy has not been called yet
        // In this case, avoid calling the RPC
        if (_tankInfo.Health <= 0)
        {
            return;
        }
        // Due to the latency, this might still get called on a tank that is soon going to be destroyed,
        // if multiple RPC_TakeDamage have already been called but not executed yet
        // So by the time the new RPC call is received, the GameObject/PhotonView might not exist anymore
        // (RPC gets lost and a warning is logged)
        _photonView.RPC("RPC_TakeDamage", RpcTarget.AllViaServer, damage);
    }

    [PunRPC]
    private void RPC_TakeDamage(int damage)
    {
        // Due to the latency, this might get called on a tank whose health has already dropped below 0
        // So make sure to not execute the code below multiple times
        if (_tankInfo.Health <= 0)
        {
            return;
        }
        // Armor acts as a flat damage reduction
        damage -= _tankInfo.Armor;
        if (damage <= 0)
        {
            return;
        }
        _tankInfo.Health -= damage;
        UpdateHealthBar();
        if (_tankInfo.Health <= 0)
        {
            _tankInfo.Health = 0;
            PlayDeathEffects();
            if (_photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
                AlliedTankGotDestroyedEvent?.Invoke(gameObject);
            }
        }
    }

    public void RestoreHealth(int health)
    {
        if (_tankInfo.Health >= _tankInfo.MaxHealth)
        {
            return;
        }
        _photonView.RPC("RPC_RestoreHealth", RpcTarget.AllViaServer, health);
    }

    [PunRPC]
    private void RPC_RestoreHealth(int health)
    {
        if (_tankInfo.Health >= _tankInfo.MaxHealth)
        {
            return;
        }
        _tankInfo.Health += Math.Min(health, _tankInfo.MaxHealth - _tankInfo.Health);
        UpdateHealthBar();
    }

    public void GainMaxHealth(int health)
    {
        _photonView.RPC("RPC_GainMaxHealth", RpcTarget.AllViaServer, health);
    }

    [PunRPC]
    private void RPC_GainMaxHealth(int health)
    {
        _tankInfo.MaxHealth += health;
        _tankInfo.Health += health;
        _healthBarSlider.maxValue = _tankInfo.MaxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        _healthBarSlider.value = _tankInfo.Health;
        _healthBarFillImage.color = Color.Lerp(_minHealthColor, _maxHealthColor, (float)_tankInfo.Health / _tankInfo.MaxHealth);
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
