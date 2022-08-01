using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CrateTank : Crate
{
    private static readonly int s_duplicateTankChance = 20;
    private bool _defaultTank;
    private float _positionX;
    private float _positionZ;
    private int _originalTankNumber = -1;


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
        SetRandomTankType();
    }

    private void SetRandomTankType()
    {
        if (UnityEngine.Random.Range(0, 100) < s_duplicateTankChance)
        {
            _defaultTank = false;
        }
        else
        {
            _defaultTank = true;
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        Vector3 extraTankPosition = 2f * transform.position - tank.transform.position;
        _positionX = extraTankPosition.x;
        _positionZ = extraTankPosition.z;
        if (!_defaultTank)
        {
            _originalTankNumber = tank.GetComponent<TankInfo>().TankNumber;
        }
        return "Extra Tank";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        int rewardedPlayer = tank.GetComponent<TankInfo>().ActorNumber;
        PhotonNetwork.RaiseEvent(PlayerManager.TankCrateCollectedNetworkEvent, new float[] { _positionX, _positionZ, _originalTankNumber },
            new RaiseEventOptions { TargetActors = new int[] { rewardedPlayer } }, SendOptions.SendReliable);
    }
}
