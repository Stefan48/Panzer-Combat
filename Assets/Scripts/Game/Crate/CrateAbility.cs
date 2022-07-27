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
        if (roll < 70)
        {
            _abilityType = AbilityType.Mine;
        }
        else if (roll < 80)
        {
            _abilityType = AbilityType.DeflectShells;
        }
        else if (roll < 90)
        {
            _abilityType = AbilityType.TripleShells;
        }
        else
        {
            _abilityType = AbilityType.LaserBeam;
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
