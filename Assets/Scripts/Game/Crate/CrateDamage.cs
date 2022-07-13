using Photon.Pun;
using UnityEngine;

public class CrateDamage : Crate
{
    private static readonly int s_minDamage = 1;
    private static readonly int s_maxDamage = 10;
    private int _damage;


    [PunRPC]
    protected override void RPC_OnCollect(string onCollectText)
    {
        base.RPC_OnCollect(onCollectText);
    }

    [PunRPC]
    protected override void RPC_Shatter()
    {
        base.RPC_Shatter();
    }

    protected override void Awake()
    {
        base.Awake();
        SetRandomDamage();
    }

    private void SetRandomDamage()
    {
        if (UnityEngine.Random.Range(0, 100) < 5)
        {
            // 5% chance for doubling the current damage
            _damage = int.MaxValue;
        }
        else
        {
            _damage = UnityEngine.Random.Range(s_minDamage, s_maxDamage + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        if (_damage == int.MaxValue)
        {
            _damage = tank.GetComponent<TankInfo>().Damage;
            return "2X damage";
        }
        return $"+ {_damage} damage";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankInfo>().IncreaseDamage(_damage);
    }
}
