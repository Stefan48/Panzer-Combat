using Photon.Pun;
using System;
using UnityEngine;

public class CrateRestoreHealth : Crate
{
    private static readonly int s_minHealthToRestore = 50;
    private static readonly int s_maxHealthToRestore = 100;
    private static readonly int s_restoreAllHealthChance = 20;
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
        if (UnityEngine.Random.Range(0, 100) < s_restoreAllHealthChance)
        {
            _healthToRestore = int.MaxValue;
        }
        else
        {
            _healthToRestore = UnityEngine.Random.Range(s_minHealthToRestore, s_maxHealthToRestore + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        string onCollectText = (_healthToRestore == int.MaxValue) ? "Full Health" : string.Empty;
        TankInfo tankInfo = tank.GetComponent<TankInfo>();
        _healthToRestore = Math.Min(_healthToRestore, tankInfo.MaxHealth - tankInfo.Health);
        if (onCollectText == string.Empty)
        {
            onCollectText = $"+ {_healthToRestore} Health";
        }
        return onCollectText;
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankHealth>().RestoreHealth(_healthToRestore);
    }
}
