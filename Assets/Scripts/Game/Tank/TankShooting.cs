using UnityEngine;

public class TankShooting : MonoBehaviour
{
    private TankInfo _tankInfo;

    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private AudioSource shotFiredAudioSource;
    private float shotShellSpeed = 20f;
    private Transform muzzleTransform;

    private void Awake()
    {
        muzzleTransform = transform.Find("Muzzle");

        _tankInfo = GetComponent<TankInfo>();
    }

    private void OnEnable()
    {
        // TODO - reset stats to default values
    }

    private void Update()
    {
        // Tank may shoot only when selected
        if (_tankInfo.IsSelected)
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
