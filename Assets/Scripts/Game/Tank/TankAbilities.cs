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
    public bool DeflectShellsAbilityActive { get; private set; } = false;
    public bool LaserBeamAbilityActive { get; private set; } = false;
    [SerializeField] private LaserBeam _laserBeam;


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
                    UseAbility(0);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    UseAbility(1);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    UseAbility(2);
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    UseAbility(3);
                }
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    UseAbility(4);
                }
            }
        }
        AdvanceAbilityTimers();
    }

    private void UseAbility(int index)
    {
        Ability ability = Abilities[index];
        if (ability == null || ability.IsActive)
        {
            return;
        }
        ability.IsActive = true;
        if (ability.Type == AbilityType.TripleShells)
        {
            if (!ActiveAbilitiesOfTypeBesidesIndex(AbilityType.TripleShells, index))
            {
                SetTripleShellsAbilityActive(true);
            }
        }
        else if (ability.Type == AbilityType.DeflectShells)
        {
            if (!ActiveAbilitiesOfTypeBesidesIndex(AbilityType.DeflectShells, index))
            {
                SetDeflectShellsAbilityActive(true);
            }
        }
        else if (ability.Type == AbilityType.LaserBeam)
        {
            if (!ActiveAbilitiesOfTypeBesidesIndex(AbilityType.LaserBeam, index))
            {
                SetLaserBeamAbilityActive(true);
            }
        }
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
                    else if (ability.Type == AbilityType.DeflectShells)
                    {
                        if (!ActiveAbilitiesOfTypeBesidesIndex(AbilityType.DeflectShells, i))
                        {
                            SetDeflectShellsAbilityActive(false);
                        }
                    }
                    else if (ability.Type == AbilityType.LaserBeam)
                    {
                        if (!ActiveAbilitiesOfTypeBesidesIndex(AbilityType.LaserBeam, i))
                        {
                            SetLaserBeamAbilityActive(false);
                        }
                    }
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
                return;
            }
        }
        for (int i = 0; i < MaxAbilities; ++i)
        {
            if (Abilities[i].Type == AbilityType.Mine && Abilities[i].IsActive)
            {
                Abilities[i] = new Ability(abilityType);
                return;
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

    private void SetDeflectShellsAbilityActive(bool active)
    {
        _photonView.RPC("RPC_SetDeflectShellsAbilityActive", RpcTarget.AllViaServer, active);
    }

    [PunRPC]
    private void RPC_SetDeflectShellsAbilityActive(bool active)
    {
        DeflectShellsAbilityActive = active;
    }

    private void SetLaserBeamAbilityActive(bool active)
    {
        _photonView.RPC("RPC_SetLaserBeamAbilityActive", RpcTarget.AllViaServer, active);
    }

    [PunRPC]
    private void RPC_SetLaserBeamAbilityActive(bool active)
    {
        LaserBeamAbilityActive = active;
        if (active)
        {
            _laserBeam.Activate();
        }
        else
        {
            _laserBeam.Deactivate();
        }
    }
}
