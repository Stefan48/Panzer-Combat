using UnityEngine;

public class ShellMovement : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private ShellExplosion _shellExplosion;
    private float _speed = 20f;
    private int _range = 10;
    private float _distanceTraveled = 0f;
    

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _shellExplosion = GetComponent<ShellExplosion>();
    }

    public void Init(float speed, int range)
    {
        _speed = speed;
        _range = range;
    }

    private void FixedUpdate()
    {
        float distance = _speed * Time.fixedDeltaTime;
        Vector3 movement = transform.forward * distance;
        _rigidbody.MovePosition(_rigidbody.position + movement);
        _distanceTraveled += distance;
        if (_distanceTraveled >= _range)
        {
            _shellExplosion.OnRangeReached();
        }
    }
}
