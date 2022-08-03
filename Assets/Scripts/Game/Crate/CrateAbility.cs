using Photon.Pun;
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
        int roll = UnityEngine.Random.Range(0, 100);
        // TODO - Probabilities
        if (roll < 80)
        {
            _abilityType = AbilityType.Turret;
        }
        else if (roll < 85)
        {
            _abilityType = AbilityType.Mine;
        }
        else if (roll < 90)
        {
            _abilityType = AbilityType.TripleShells;
        }
        else if (roll < 95)
        {
            _abilityType = AbilityType.LaserBeam;
        }
        else
        {
            _abilityType = AbilityType.DeflectShells;
        }
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
