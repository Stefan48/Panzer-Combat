using UnityEngine;

public class TurretShooting : MonoBehaviour
{
    [SerializeField] private Transform _turretTopTransform;
    [SerializeField] private Animator _turretTopAnimator;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private GameObject _shotParticlesPrefab;
    private GameObject _shotParticles = null;
    private ParticleSystem _shotParticleSystem;
    [SerializeField] private AudioSource _shotAudioSource;
    public GameObject Target;


    private void Awake()
    {
        _shotParticles = Instantiate(_shotParticlesPrefab, _muzzle);
        _shotParticleSystem = _shotParticles.GetComponent<ParticleSystem>();
    }

    private void OnDestroy()
    {
        if (!ReferenceEquals(_shotParticles, null))
        {
            Destroy(_shotParticles);
        }
    }

    private void Update()
    {
        _turretTopTransform.LookAt(new Vector3(Target.transform.position.x, transform.position.y, Target.transform.position.z));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (Target == null)
        {
            return;
        }
        _turretTopAnimator.SetTrigger("Shot");
        _shotParticleSystem.Play();
        _shotAudioSource.Play();
    }


    // TODO - Add script to the Cylinder of the Vision and implement OnTriggerEnter, which calls OnTargetsUpdated method of this script
    // TODO - Network sync
    // TODO - Separate scripts for turret info, turret lifetime (including health) and turret shooting (this script) - can turrets target other turrets?
    // Turrets target enemy tanks and turrets (tanks have priority). Turrets never target allied tanks or turrets,
    // but they may hit them if they get in the way
    // TODO - When placing a turret, it can't (Physics.)Overlap with environment objects, tanks or other turrets
    // TODO - Turret shells prefab (smaller and faster than regular shells; they follow the target till they hit it or it gets destroyed)
    // TODO - Minimap icons for turrets (rounded squares?)
    // TODO - Turrets get destroyed on round start
    // TODO - Turrets stop shooting on round end
    // TODO - UI for selected turrets (max 1 turret selected at a time; gets disabled if the selected turret gets destroyed)
}