using Photon.Pun;
using System;
using UnityEngine;

public class TankInfo : MonoBehaviour
{
    private PhotonView _photonView;
    [SerializeField] private TextMesh _usernameTextMesh;
    [SerializeField] private SpriteRenderer _minimapIconSpriteRenderer;
    public int ActorNumber { get; private set; } = -1; // initializing is for testing only
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
        Health = MaxHealth;

        //ActorNumber = Random.Range(0, 100) % 2 == 0 ? 0 : -1; // this is for testing only
    }

    public void SetInitialInfo(int actorNumber, Color color)
    {
        _photonView.RPC("RPC_SetInitialInfo", RpcTarget.All, actorNumber, new Vector3(color.r, color.g, color.b));
    }

    [PunRPC]
    private void RPC_SetInitialInfo(int actorNumber, Vector3 color)
    {
        ActorNumber = actorNumber;
        Username = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber).NickName;
        _usernameTextMesh.text = Username;

        Color = new Color(color.x, color.y, color.z);
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material.color = Color;
        }
        _minimapIconSpriteRenderer.color = Color;
    }

    public void SetStats(int health, int maxHealth, int ammo, int damage, int armor, int speed, int range)
    {
        _photonView.RPC("RPC_SetStats", RpcTarget.AllViaServer, health, maxHealth, ammo, damage, armor, speed, range);
    }

    [PunRPC]
    private void RPC_SetStats(int health, int maxHealth, int ammo, int damage, int armor, int speed, int range)
    {
        GetComponent<TankHealth>().SetHealthAndMaxHealth(health, maxHealth);
        Ammo = ammo;
        Damage = damage;
        Armor = armor;
        Speed = speed;
        ShellSpeed = Speed + s_speedShellSpeedDifference;
        int extraRange = range - s_defaultRange;
        if (extraRange > 0)
        {
            RPC_IncreaseRange(extraRange);
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
