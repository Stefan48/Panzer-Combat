using UnityEngine;

public class UiFixedRotation : MonoBehaviour
{
    [SerializeField] private Vector3 _eulerAngles;
    private Quaternion _rotation;


    private void Awake()
    {
        _rotation = Quaternion.Euler(_eulerAngles);
    }

    private void Update()
    {
        transform.rotation = _rotation;
    }
}
