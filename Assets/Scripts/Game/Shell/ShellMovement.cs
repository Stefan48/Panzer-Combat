using UnityEngine;

public class ShellMovement : MonoBehaviour
{
    public float Speed;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Init(float speed)
    {
        Speed = speed;
    }

    private void FixedUpdate()
    {
        Vector3 movement = transform.forward * Speed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }
}
