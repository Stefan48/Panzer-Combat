using UnityEngine;

public class UIFixedRotation : MonoBehaviour
{
    private Quaternion initialRotation;


    private void Start()
    {
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        transform.rotation = initialRotation;
    }
}
