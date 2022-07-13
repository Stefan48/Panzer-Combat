using Photon.Pun;
using UnityEngine;

public class CrateMaxHealth : Crate
{
    private static readonly int s_minHealthToGain = 50;
    private static readonly int s_maxHealthToGain = 100;
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
        if (UnityEngine.Random.Range(0, 100) < 10)
        {
            // 10% chance to gain double health
            _healthToGain = UnityEngine.Random.Range(2 * s_minHealthToGain, 2 * s_maxHealthToGain + 1);
        }
        else
        {
            _healthToGain = UnityEngine.Random.Range(s_minHealthToGain, s_maxHealthToGain + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        return $"+ {_healthToGain} max health";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankHealth>().GainMaxHealth(_healthToGain);
    }
}
