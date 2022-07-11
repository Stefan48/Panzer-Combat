using Photon.Pun;
using System;
using UnityEngine;

public class CrateRestoreHp : Crate
{
    private static readonly int s_minHealthToRestore = 50;
    private static readonly int s_maxHealthToRestore = 100;
    private int _healthToRestore;


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

    public override void Init(float lifetime)
    {
        int healthToRestore;
        if (UnityEngine.Random.Range(0, 100) < 10)
        {
            // 10% chance to restore all the missing health
            healthToRestore = int.MaxValue;
        }
        else
        {
            healthToRestore = UnityEngine.Random.Range(s_minHealthToRestore, s_maxHealthToRestore + 1);
        }
        _photonView.RPC("RPC_InitWithHealthToRestore", RpcTarget.AllViaServer, lifetime, healthToRestore);
    }

    [PunRPC]
    private void RPC_InitWithHealthToRestore(float lifetime, int healthToRestore)
    {
        _healthToRestore = healthToRestore;
        StartCoroutine(Shatter(lifetime));
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        TankInfo tankInfo = tank.GetComponent<TankInfo>();
        _healthToRestore = Math.Min(_healthToRestore, tankInfo.MaxHealth - tankInfo.Health);
        return $"+ {_healthToRestore} health";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankHealth>().RestoreHealth(_healthToRestore);
    }
}
