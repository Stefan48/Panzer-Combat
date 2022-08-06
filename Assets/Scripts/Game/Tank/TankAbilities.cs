using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class TankAbilities : MonoBehaviour
{
    private PhotonView _photonView;
    private GameManager _gameManager;
    private Player _owner = null;
    private TankInfo _tankInfo;
    public bool EscPanelIsActive = false;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _defaultTanksAndTurretsLayerMask;
    private const float _tankColliderRadius = 1f;
    private const float _turretColliderRadius = 0.8f;
    private const float _turretPlacementOffset = _tankColliderRadius + _turretColliderRadius + 1f;
    private readonly Vector3 _turretColliderPoint0 = Vector3.zero;
    private readonly Vector3 _turretColliderPoint1 = new Vector3(0f, 2f, 0f);
    [SerializeField] private AudioSource _warningAudioSource;
    [SerializeField] private AudioClip _cannotUseAbilityAudioClip;
    private const float _cannotUseAbilityAudioClipVolumeScale = 0.3f;
    public static readonly int MaxAbilities = 5;
    public Ability[] Abilities = new Ability[MaxAbilities];
    public bool TripleShellsAbilityActive { get; private set; } = false;
    public bool DeflectShellsAbilityActive { get; private set; } = false;
    public bool LaserBeamAbilityActive { get; private set; } = false;
    [SerializeField] private LaserBeam _laserBeam;
    [SerializeField] private GameObject _minePrefab;
    [SerializeField] private GameObject[] _turretPrefabs;
    [SerializeField] private AudioSource _hornAudioSource;
    [SerializeField] private AudioClip _hornAudioClip;
    private KeyCode _1stAbilityKey = KeyCode.Alpha1;
    private KeyCode _2ndAbilityKey = KeyCode.Alpha2;
    private KeyCode _3rdAbilityKey = KeyCode.Alpha3;
    private KeyCode _4thAbilityKey = KeyCode.Alpha4;
    private KeyCode _5thAbilityKey = KeyCode.Alpha5;
    private KeyCode _hornKey = KeyCode.H;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        if (!_photonView.IsMine)
        {
            enabled = false;
        }
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _tankInfo = GetComponent<TankInfo>();

        OnControlsUpdated();
    }

    private void Update()
    {
        if (!EscPanelIsActive)
        {
            if (_tankInfo.IsSelected)
            {
                if (Input.GetKeyDown(_1stAbilityKey))
                {
                    UseAbility(0);
                }
                if (Input.GetKeyDown(_2ndAbilityKey))
                {
                    UseAbility(1);
                }
                if (Input.GetKeyDown(_3rdAbilityKey))
                {
                    UseAbility(2);
                }
                if (Input.GetKeyDown(_4thAbilityKey))
                {
                    UseAbility(3);
                }
                if (Input.GetKeyDown(_5thAbilityKey))
                {
                    UseAbility(4);
                }
                if (Input.GetKeyDown(_hornKey))
                {
                    Honk();
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
        if (!CanUseAbility(ability.Type))
        {
            _warningAudioSource.PlayOneShot(_cannotUseAbilityAudioClip, _cannotUseAbilityAudioClipVolumeScale);
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
        else if (ability.Type == AbilityType.Mine)
        {
            PlaceMine();
        }
        else if (ability.Type == AbilityType.Turret)
        {
            PlaceTurret();
        }
    }

    private bool CanUseAbility(AbilityType type)
    {
        switch (type)
        {
            case AbilityType.TripleShells:
            case AbilityType.DeflectShells:
            case AbilityType.LaserBeam:
            case AbilityType.Mine:
                return true;
            case AbilityType.Turret:
                Vector3 placementPosition = transform.position + transform.forward * _turretPlacementOffset;
                return Physics.OverlapCapsule(_turretColliderPoint0 + placementPosition, _turretColliderPoint1 + placementPosition,
                    _turretColliderRadius, _defaultTanksAndTurretsLayerMask, QueryTriggerInteraction.Ignore).Length == 0;
            default:
                return false;
        }
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
            if ((Abilities[i].Type == AbilityType.Mine || Abilities[i].Type == AbilityType.Turret) && Abilities[i].IsActive)
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

    private void PlaceMine()
    {
        // Only the Master Client can use PhotonNetwork.InstantiateRoomObject (necessary so that the mines don't get destroyed if the player leaves)
        _photonView.RPC("RPC_PlaceMine", RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_PlaceMine()
    {
        PhotonNetwork.InstantiateRoomObject(_minePrefab.name, transform.position + _minePrefab.transform.position, transform.rotation,
            0, new object[] { _tankInfo.ActorNumber });
    }

    private void PlaceTurret()
    {
        // Only the Master Client can use PhotonNetwork.InstantiateRoomObject (necessary so that the turrets don't get destroyed if the player leaves)
        _photonView.RPC("RPC_PlaceTurret", RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_PlaceTurret()
    {
        int index = _gameManager.AvailablePlayerColors.FindIndex(color => color == _tankInfo.Color);
        Vector3 position = transform.position + transform.forward * _turretPlacementOffset;
        int turretShellSpeed = _tankInfo.ShellSpeed + TurretInfo.TurretShellsExtraSpeed;
        GameObject turret = PhotonNetwork.InstantiateRoomObject(_turretPrefabs[index].name, position, transform.rotation, 0,
            new object[] { _tankInfo.ActorNumber, _tankInfo.Damage, _tankInfo.Armor, turretShellSpeed, _tankInfo.Range });
    }

    private void Honk()
    {
        _photonView.RPC("RPC_Honk", RpcTarget.AllViaServer);
    }

    [PunRPC]
    private void RPC_Honk()
    {
        _hornAudioSource.PlayOneShot(_hornAudioClip);
    }

    private void OnControlsUpdated()
    {
        if (PlayerPrefs.HasKey(MainMenuManager.SelectUnitsControlPrefKey))
        {
            // If a custom control has been saved, then all controls have been saved
            _1stAbilityKey = (KeyCode)PlayerPrefs.GetInt(MainMenuManager.FirstAbilityControlPrefKey);
            _2ndAbilityKey = (KeyCode)PlayerPrefs.GetInt(MainMenuManager.SecondAbilityControlPrefKey);
            _3rdAbilityKey = (KeyCode)PlayerPrefs.GetInt(MainMenuManager.ThirdAbilityControlPrefKey);
            _4thAbilityKey = (KeyCode)PlayerPrefs.GetInt(MainMenuManager.FourthAbilityControlPrefKey);
            _5thAbilityKey = (KeyCode)PlayerPrefs.GetInt(MainMenuManager.FifthAbilityControlPrefKey);
            _hornKey = (KeyCode)PlayerPrefs.GetInt(MainMenuManager.HornControlPrefKey);
        }
    }
}
