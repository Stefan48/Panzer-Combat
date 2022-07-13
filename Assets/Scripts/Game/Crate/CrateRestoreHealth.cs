using Photon.Pun;
using System;
using UnityEngine;

public class CrateRestoreHealth : Crate
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

    protected override void Awake()
    {
        base.Awake();
        SetRandomHealthToRestore();
    }

    private void SetRandomHealthToRestore()
    {
        if (UnityEngine.Random.Range(0, 100) < 10)
        {
            // 10% chance to restore all the missing health
            _healthToRestore = int.MaxValue;
        }
        else
        {
            _healthToRestore = UnityEngine.Random.Range(s_minHealthToRestore, s_maxHealthToRestore + 1);
        }
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
