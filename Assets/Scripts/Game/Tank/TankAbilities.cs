using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TankAbilities : MonoBehaviour
{
    private PhotonView _photonView;
    private Player _owner = null;
    private TankInfo _tankInfo;
    public bool EscPanelIsActive = false;
    public static readonly int MaxAbilities = 5;
    public Ability[] Abilities = new Ability[MaxAbilities];
    public bool TripleShellsAbilityActive { get; private set; } = false;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (!_photonView.IsMine)
        {
            enabled = false;
        }
        _tankInfo = GetComponent<TankInfo>();
    }

    private void Update()
    {
        if (!EscPanelIsActive)
        {
            if (_tankInfo.IsSelected)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    UseAbility(Abilities[0]);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    UseAbility(Abilities[1]);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    UseAbility(Abilities[2]);
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    UseAbility(Abilities[3]);
                }
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    UseAbility(Abilities[4]);
                }
            }
        }
        AdvanceAbilityTimers();
    }

    private void UseAbility(Ability ability)
    {
        if (ability == null || ability.IsActive)
        {
            return;
        }
        ability.IsActive = true;
        if (ability.Type == AbilityType.TripleShells)
        {
            if (!TripleShellsAbilityActive)
            {
                SetTripleShellsAbilityActive(true);
            }
        }
        // TODO - If AbilityType.DeflectShells, set DeflectingShells status (via RPC)
        // TODO - If AbilityType.LaserBeam, activate laser (if not already active)
        // TODO - If AbilityType.Mine, place mine
    }

    private void AdvanceAbilityTimers()
    {
        for (int i = 0; i < MaxAbilities; ++i)
        {
            Ability ability = Abilities[i];
            if (ability != null && ability.IsActive)
            {
                ability.Timer += Time.deltaTime;
                if (ability.Timer > ability.Duration)
                {
                    if (ability.Type == AbilityType.TripleShells)
                    {
                        if (!ActiveAbilitiesOfTypeBesidesIndex(AbilityType.TripleShells, i))
                        {
                            SetTripleShellsAbilityActive(false);
                        }
                    }
                    // TODO - If AbilityType.DeflectShells, set DeflectingShells status to false (via RPC) (if there aren't other instances of the same ability active)
                    // TODO - If AbilityType.LaserBeam, deactivate laser (if there aren't any other LaserBeam abilities still active)
                    Abilities[i] = null;
                }
            }
        }
    }

    private bool ActiveAbilitiesOfTypeBesidesIndex(AbilityType type, int index)
    {
        for (int i = 0; i < MaxAbilities; ++i)
        {
            if (i != index && Abilities[i] != null && Abilities[i].Type == type && Abilities[i].IsActive)
            {
                return true;
            }
        }
        return false;
    }

    public void NewAbility(AbilityType abilityType)
    {
        if (_owner == null)
        {
            _owner = PhotonNetwork.CurrentRoom.GetPlayer(_tankInfo.ActorNumber);
        }
        _photonView.RPC("RPC_NewAbility", _owner, abilityType);
    }

    [PunRPC]
    private void RPC_NewAbility(AbilityType abilityType)
    {
        for (int i = 0; i < MaxAbilities; ++i)
        {
            if (Abilities[i] == null)
            {
                Abilities[i] = new Ability(abilityType);
                break;
            }
        }
    }

    private void SetTripleShellsAbilityActive(bool active)
    {
        _photonView.RPC("RPC_SetTripleShellsAbilityActive", RpcTarget.AllViaServer, active);
    }

    [PunRPC]
    private void RPC_SetTripleShellsAbilityActive(bool active)
    {
        TripleShellsAbilityActive = active;
    }
}
