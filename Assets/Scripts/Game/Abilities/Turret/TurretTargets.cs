using UnityEngine;

public class TurretTargets : MonoBehaviour
{
    [SerializeField] private TurretShooting _turretShooting;


    private void OnTriggerEnter(Collider other)
    {
        _turretShooting.OnGameObjectEnteredRange(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {        
        _turretShooting.OnGameObjectExitedRange(other.gameObject);
    }
}
