using UnityEngine;

public class TankShooting : MonoBehaviour
{
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private AudioSource shotFiredAudioSource;
    private float shotShellSpeed = 20f;
    private Transform muzzleTransform;
    private TankMovement tankMovementComponent;

    private void Awake()
    {
        muzzleTransform = transform.Find("Muzzle");
        tankMovementComponent = GetComponent<TankMovement>();
    }

    private void OnEnable()
    {
        // TODO - reset stats to default values
    }

    private void Update()
    {
        // Tank may shoot only when selected
        if (tankMovementComponent.isSelectedByOwner)
        {
            if (Input.GetMouseButtonDown(1))
            {
                // TODO - direction doesn't exactly follow the mouse's cursor
                // TODO - object pooling
                GameObject shell = Instantiate(shellPrefab, muzzleTransform.position, muzzleTransform.rotation);
                shell.GetComponent<ShellMovement>().speed = shotShellSpeed;
                shotFiredAudioSource.Play();
            }
        }
    }
}
