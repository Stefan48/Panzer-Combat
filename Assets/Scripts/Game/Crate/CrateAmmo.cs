using Photon.Pun;
using UnityEngine;

public class CrateAmmo : Crate
{
    private static readonly int s_minAmmo = 20;
    private static readonly int s_maxAmmo = 60;
    private static readonly int s_infiniteAmmoChance = 1;
    public static readonly int InfiniteAmmoThreshold = 1000000000;
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
        if (UnityEngine.Random.Range(0, 100) < s_infiniteAmmoChance)
        {
            _ammo = int.MaxValue;
        }
        else
        {
            _ammo = UnityEngine.Random.Range(s_minAmmo, s_maxAmmo + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        // A tank which had previously collected the Infinite Ammo power-up will have (close to) int.MaxValue ammo already. Don't overflow
        int currentAmmo = tank.GetComponent<TankInfo>().Ammo;
        if (_ammo == int.MaxValue)
        {
            _ammo -= currentAmmo;
            return "Infinite Ammo";
        }
        if (currentAmmo > InfiniteAmmoThreshold)
        {
            // The tank had previously collected the Infinite Ammo power-up
            _ammo = int.MaxValue - currentAmmo;
            return "+ 0 Ammo";
        }
        return $"+ {_ammo} Ammo";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankInfo>().IncreaseAmmo(_ammo);
    }
}
