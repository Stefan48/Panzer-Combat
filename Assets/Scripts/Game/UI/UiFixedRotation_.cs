using UnityEngine;

public class UiFixedRotation : MonoBehaviour
{
    private Quaternion _initialRotation;

    private void Start()
    {
        _initialRotation = transform.rotation;
    }

    private void Update()
    {
        transform.rotation = _initialRotation;
    }
}
