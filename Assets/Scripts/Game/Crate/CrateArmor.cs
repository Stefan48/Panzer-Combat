using Photon.Pun;
using UnityEngine;

public class CrateArmor : Crate
{
    private static readonly int s_minArmor = 1;
    private static readonly int s_maxArmor = 10;
    private static readonly int s_doubleExtraArmorChance = 10;
    private int _armor;


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
        SetRandomArmor();
    }

    private void SetRandomArmor()
    {
        if (UnityEngine.Random.Range(0, 100) < s_doubleExtraArmorChance)
        {
            _armor = UnityEngine.Random.Range(2 * s_minArmor, 2 * s_maxArmor + 1);
        }
        else
        {
            _armor = UnityEngine.Random.Range(s_minArmor, s_maxArmor + 1);
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        return $"+ {_armor} Armor";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankInfo>().IncreaseArmor(_armor);
    }
}
