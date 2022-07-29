using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TurretLifetime : MonoBehaviour
{
    private PhotonView _photonView;
    private TurretInfo _turretInfo;
    [SerializeField] private Slider _healthBarSlider;
    [SerializeField] private Image _healthBarFillImage;
    [SerializeField] private Color _maxHealthColor = new Color32(0, 255, 0, 180);
    [SerializeField] private Color _minHealthColor = new Color32(255, 0, 0, 180);
    private const float _lifetime = 1000f; // TODO - 10f
    private float _timeRemaining = _lifetime;
    [SerializeField] private Slider _timeBarSlider;
    [SerializeField] private GameObject _turretExplosionPrefab;
    private bool _destructionPending = false;

    // TODO - TurretGotDestroyedEvent for camera size?
    

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _turretInfo = GetComponent<TurretInfo>();
    }

    private void Start()
    {
        _healthBarSlider.maxValue = _turretInfo.MaxHealth;
        UpdateHealthBar();
        _timeBarSlider.maxValue = _lifetime;
        UpdateTimeBar();
    }

    private void Update()
    {
        if (_destructionPending)
        {
            return;
        }
        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            Explode();
            return;
        }
        UpdateTimeBar();
    }

    public void TakeDamage(int damage, bool trueDamage)
    {
        if (_destructionPending)
        {
            return;
        }
        // Due to the latency, this might still get called on a turret that is soon going to be destroyed,
        // if multiple RPC_TakeDamage have already been called but not executed yet
        // So by the time the new RPC call is received, the GameObject/PhotonView might not exist anymore
        // (RPC gets lost and a warning is logged)
        _photonView.RPC("RPC_TakeDamage", RpcTarget.AllViaServer, damage, trueDamage);
    }

    [PunRPC]
    private void RPC_TakeDamage(int damage, bool trueDamage)
    {
        if (_destructionPending)
        {
            return;
        }
        if (!trueDamage)
        {
            // Armor acts as a flat damage reduction
            damage -= _turretInfo.Armor;
            if (damage <= 0)
            {
                return;
            }
        }
        _turretInfo.Health = Math.Max(0, _turretInfo.Health - damage);
        UpdateHealthBar();
        if (_turretInfo.Health == 0)
        {
            Explode();
        }
    }

    private void UpdateHealthBar()
    {
        _healthBarSlider.value = _turretInfo.Health;
        _healthBarFillImage.color = Color.Lerp(_minHealthColor, _maxHealthColor, (float)_turretInfo.Health / _turretInfo.MaxHealth);
    }

    private void UpdateTimeBar()
    {
        _timeBarSlider.value = _timeRemaining;
    }

    private void Explode()
    {
        _destructionPending = true;
        PlayExplosionEffects();
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void PlayExplosionEffects()
    {
        GameObject turretExplosion = Instantiate(_turretExplosionPrefab, transform.position + _turretExplosionPrefab.transform.position, transform.rotation);
        ParticleSystem turretExplosionParticleSystem = turretExplosion.GetComponent<ParticleSystem>();
        AudioSource turretExplosionAudioSource = turretExplosion.GetComponent<AudioSource>();
        turretExplosionParticleSystem.Play();
        turretExplosionAudioSource.Play();
        Destroy(turretExplosion, Math.Max(turretExplosionParticleSystem.main.duration, turretExplosionAudioSource.clip.length));
    }
}
