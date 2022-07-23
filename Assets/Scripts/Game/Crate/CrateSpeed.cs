using Photon.Pun;
using UnityEngine;

public class CrateSpeed : Crate
{
    private static readonly int s_speed = 1;


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

    protected override string GetOnCollectText(GameObject tank)
    {
        return $"+ {s_speed} Speed";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankInfo>().IncreaseSpeed(s_speed);
    }
}
