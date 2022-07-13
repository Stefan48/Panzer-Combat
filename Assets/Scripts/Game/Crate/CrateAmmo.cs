using Photon.Pun;
using UnityEngine;

public class CrateAmmo : Crate
{
    private static readonly int s_minAmmo = 20;
    private static readonly int s_maxAmmo = 60;
    private int _ammo;


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
        SetRandomAmmo();
    }

    private void SetRandomAmmo()
    {
        if (UnityEngine.Random.Range(0, 100) < 1)
        {
            // 1% chance for infinite ammo
            _ammo = int.MaxValue;
        }
        else
        {
            _ammo = UnityEngine.Random.Range(s_minAmmo, s_maxAmmo + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        if (_ammo == int.MaxValue)
        {
            _ammo -= tank.GetComponent<TankInfo>().Ammo;
            return "Infinite Ammo";
        }
        return $"+ {_ammo} Ammo";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankInfo>().IncreaseAmmo(_ammo);
    }
}
