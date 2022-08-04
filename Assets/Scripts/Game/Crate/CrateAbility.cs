using Photon.Pun;
using System;
using UnityEngine;

public class CrateAbility : Crate
{
    private AbilityType _abilityType;


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
        SetRandomAbilityType();
    }

    private void SetRandomAbilityType()
    {
        // All ability types are equiprobable
        _abilityType = (AbilityType)UnityEngine.Random.Range(0, Enum.GetNames(typeof(AbilityType)).Length);
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        return "New Ability";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        tank.GetComponent<TankAbilities>().NewAbility(_abilityType);
    }
}
