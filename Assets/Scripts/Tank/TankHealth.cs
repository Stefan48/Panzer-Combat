using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color maxHealthColor = new Color32(0, 255, 0, 180);
    [SerializeField] private Color minHealthColor = new Color32(255, 0, 0, 180);
    [SerializeField] private GameObject tankExplosionPrefab;
    private AudioSource tankExplosionAudioSource;
    private ParticleSystem tankExplosionParticleSystem;
    private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    private void Awake()
    {
        tankExplosionParticleSystem = Instantiate(tankExplosionPrefab).GetComponent<ParticleSystem>();
        tankExplosionAudioSource = tankExplosionParticleSystem.GetComponent<AudioSource>();
        tankExplosionParticleSystem.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        slider.maxValue = maxHealth;
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHealthBar();
        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            PlayDeathEffects();
        }
    }

    private void UpdateHealthBar()
    {
        slider.value = currentHealth;
        fillImage.color = Color.Lerp(minHealthColor, maxHealthColor, currentHealth / maxHealth);
    }

    private void PlayDeathEffects()
    {
        tankExplosionParticleSystem.transform.position = transform.position;
        tankExplosionParticleSystem.gameObject.SetActive(true);
        tankExplosionParticleSystem.Play();
        tankExplosionAudioSource.Play();
        gameObject.SetActive(false);
    }
}
