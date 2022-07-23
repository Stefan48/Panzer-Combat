using Photon.Pun;
using UnityEngine;

public class CrateMaxHealth : Crate
{
    private static readonly int s_minHealthToGain = 25;
    private static readonly int s_maxHealthToGain = 75;
    private static readonly int s_doubleMaxHealthChance = 5;
    private int _healthToGain;


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
        SetRandomHealthToGain();
    }

    private void SetRandomHealthToGain()
    {
        if (UnityEngine.Random.Range(0, 100) < s_doubleMaxHealthChance)
        {
            _healthToGain = int.MaxValue;
        }
        else
        {
            _healthToGain = UnityEngine.Random.Range(s_minHealthToGain, s_maxHealthToGain + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        if (_healthToGain == int.MaxValue)
        {
            _healthToGain = tank.GetComponent<TankInfo>().MaxHealth;
            return "2X Max Health";
        }
        return $"+ {_healthToGain} Max Health";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankHealth>().GainMaxHealth(_healthToGain);
    }
}
