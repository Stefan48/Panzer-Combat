using Photon.Pun;
using System;
using UnityEngine;

public class TankInfo : MonoBehaviour, IPunInstantiateMagicCallback
{
    private PhotonView _photonView;
    [SerializeField] private MeshRenderer[] _coloredMeshRenderers;
    [SerializeField] private TextMesh _usernameTextMesh;
    [SerializeField] private SpriteRenderer _minimapIconSpriteRenderer;
    private TankShooting _tankShooting;
    public int ActorNumber { get; private set; } = -1;
    public static readonly int TankNumberMultiplier = 100;
    // Unique identifier for tanks, equal to ActorNumber * TankNumberMultiplier + the index in the Tanks list of the PlayerManager
    // (could get reused after the tank gets destroyed)
    public int TankNumber { get; private set; }
    public string Username { get; private set; }
    public Color Color { get; private set; }
    public bool IsSelected = false;
    public int Speed { get; private set; } = 12;
    public int ShellSpeed { get; private set; } = 20;
    public int MaxHealth = 100;
    public int Health;
    public int Armor { get; private set; } = 0;
    public int Damage { get; private set; } = 20;
    public int Ammo = 30;
    public int Range { get; private set; } = 10;
    private static readonly int s_speedShellSpeedDifference = 8;
    private static readonly int s_defaultRange = 10;
    [SerializeField] private GameObject _vision;
    private const float _visionPerRange = 0.065f;

    public static event Action<int> TankRangeIncreasedEvent;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _tankShooting = GetComponent<TankShooting>();
        Health = MaxHealth;
    }

    // This is called after Awake and before Start
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        TankNumber = (int)instantiationData[0];
        ActorNumber = TankNumber / TankNumberMultiplier;
        _tankShooting.CurrentShellId = TankNumber * TankShooting.ShellIdMultiplier;
        Username = PhotonNetwork.CurrentRoom.GetPlayer(ActorNumber).NickName;
        _usernameTextMesh.text = Username;

        Vector3 color = (Vector3)instantiationData[1];
        Color = new Color(color.x, color.y, color.z);
        foreach (MeshRenderer renderer in _coloredMeshRenderers)
        {
            renderer.material.color = Color;
        }
        _usernameTextMesh.color = Color;
        _minimapIconSpriteRenderer.color = Color;

        bool tankHasDefaultStats = (bool)instantiationData[2];
        if (!tankHasDefaultStats)
        {
            int health = (int)instantiationData[3];
            int maxHealth = (int)instantiationData[4];
            GetComponent<TankHealth>().SetHealthAndMaxHealth(health, maxHealth);
            Ammo = (int)instantiationData[5];
            Damage = (int)instantiationData[6];
            Armor = (int)instantiationData[7];
            Speed = (int)instantiationData[8];
            ShellSpeed = Speed + s_speedShellSpeedDifference;
            int range = (int)instantiationData[9];
            if (range > s_defaultRange)
            {
                RPC_IncreaseRange(range - s_defaultRange);
            }
        }
    }

    public void IncreaseArmor(int extraArmor)
    {
        _photonView.RPC("RPC_IncreaseArmor", RpcTarget.AllViaServer, extraArmor);
    }

    [PunRPC]
    private void RPC_IncreaseArmor(int extraArmor)
    {
        Armor += extraArmor;
    }

    public void IncreaseDamage(int extraDamage)
    {
        _photonView.RPC("RPC_IncreaseDamage", RpcTarget.AllViaServer, extraDamage);
    }

    [PunRPC]
    private void RPC_IncreaseDamage(int extraDamage)
    {
        Damage += extraDamage;
    }

    public void IncreaseSpeed(int extraSpeed)
    {
        _photonView.RPC("RPC_IncreaseSpeed", RpcTarget.AllViaServer, extraSpeed);
    }

    [PunRPC]
    private void RPC_IncreaseSpeed(int extraSpeed)
    {
        Speed += extraSpeed;
        ShellSpeed += extraSpeed;
    }

    public void IncreaseAmmo(int extraAmmo)
    {
        _photonView.RPC("RPC_IncreaseAmmo", RpcTarget.AllViaServer, extraAmmo);
    }

    [PunRPC]
    private void RPC_IncreaseAmmo(int extraAmmo)
    {
        Ammo += extraAmmo;
    }

    public void IncreaseRange(int extraRange)
    {
        _photonView.RPC("RPC_IncreaseRange", RpcTarget.AllViaServer, extraRange);
    }

    [PunRPC]
    private void RPC_IncreaseRange(int extraRange)
    {
        Range += extraRange;
        if (_photonView.IsMine)
        {
            Vector3 visionCurrentScale = _vision.transform.localScale;
            _vision.transform.localScale = new Vector3(visionCurrentScale.x + _visionPerRange * extraRange,
                visionCurrentScale.y + _visionPerRange * extraRange, visionCurrentScale.z);
            TankRangeIncreasedEvent?.Invoke(Range);
        }
    }
}
