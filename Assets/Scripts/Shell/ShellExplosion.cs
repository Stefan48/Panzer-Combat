using UnityEngine;
using System.Collections;

public class ShellExplosion : MonoBehaviour
{
    [SerializeField] private LayerMask playersLayerMask;
    [SerializeField] private LayerMask noCollisionsLayerMask;
    private ParticleSystem shellExplosionParticleSystem;
    private AudioSource shellExplosionAudioSource;
    private float shellDamage = 25f;
    private float shellLifetime = 10f;

    private void Awake()
    {
        shellExplosionParticleSystem = transform.Find("ShellExplosion").GetComponent<ParticleSystem>();
        shellExplosionAudioSource = shellExplosionParticleSystem.GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(Explode(shellLifetime));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playersLayerMask.value) > 0)
        {
            // The shell hit a tank
            GameObject tank = other.gameObject;
            TankHealth tankHealthComponent = tank.GetComponent<TankHealth>();
            tankHealthComponent.TakeDamage(shellDamage);
            StartCoroutine(Explode(0f));
        }
        else if (((1 << other.gameObject.layer) & noCollisionsLayerMask.value) == 0)
        {
            // The shell hit the environment
            Debug.Log(other.name);
            StartCoroutine(Explode(0f));
        }
    }

    private IEnumerator Explode(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Detach the particle system from the shell GameObject
        shellExplosionParticleSystem.transform.parent = null;
        shellExplosionParticleSystem.Play();
        shellExplosionAudioSource.Play();
        Destroy(shellExplosionParticleSystem, shellExplosionParticleSystem.main.duration);
        Destroy(gameObject);
    }
}
