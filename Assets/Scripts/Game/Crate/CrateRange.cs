using Photon.Pun;
using System;
using UnityEngine;

public class CrateRange : Crate
{
    private static readonly int s_minRange = 1;
    private static readonly int s_maxRange = 3;
    private static readonly int s_infiniteRange = 100;
    private int _range;


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
        SetRandomRange();
    }

    private void SetRandomRange()
    {
        if (UnityEngine.Random.Range(0, 100) < 1)
        {
            // 1% chance for infinite range
            _range = s_infiniteRange;
        }
        else
        {
            _range = UnityEngine.Random.Range(s_minRange, s_maxRange + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        int currentRange = tank.GetComponent<TankInfo>().Range;
        if (_range == s_infiniteRange)
        {
            _range -= currentRange;
            return "Infinite Range";
        }
        if (currentRange == s_infiniteRange)
        {
            // The tank had previously collected the Infinite Range power-up
            _range = 0;
        }
        return $"+ {_range} Range";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        if (_range != 0)
        {
            tank.GetComponent<TankInfo>().IncreaseRange(_range);
        }
    }
}
