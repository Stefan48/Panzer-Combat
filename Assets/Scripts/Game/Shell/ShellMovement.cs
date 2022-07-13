using UnityEngine;

public class ShellMovement : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private float _speed = 20f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Init(float speed)
    {
        _speed = speed;
    }

    private void FixedUpdate()
    {
        Vector3 movement = transform.forward * _speed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }
}
