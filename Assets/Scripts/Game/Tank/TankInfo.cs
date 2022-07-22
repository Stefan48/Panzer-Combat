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
    public float Speed { get; private set; } = 12f;
    public int MaxHealth = 100;
    public int Health;
    public int Armor { get; private set; } = 0;
    public int Damage { get; private set; } = 20;
    public int Ammo = 30;
    public float ShellSpeed { get; private set; } = 20f;
    public int Range { get; private set; } = 10;
    [SerializeField] private GameObject _vision;
    private const float _visionPerRange = 0.065f;

    public static event Action<int> TankRangeIncreasedEvent;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        Health = MaxHealth;

        //ActorNumber = Random.Range(0, 100) % 2 == 0 ? 0 : -1; // this is for testing only
    }

    public void SetActorNumber(int actorNumber)
    {
        _photonView.RPC("RPC_SetActorNumber", RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void RPC_SetActorNumber(int actorNumber)
    {
        ActorNumber = actorNumber;
    }

    public void SetUsername(string username)
    {
        _photonView.RPC("RPC_SetUsername", RpcTarget.All, username);
    }

    [PunRPC]
    private void RPC_SetUsername(string username)
    {
        Username = username;
        _usernameTextMesh.text = username;
    }

    public void SetColor(Color color)
    {
        _photonView.RPC("RPC_SetColor", RpcTarget.All, new Vector3(color.r, color.g, color.b));
    }

    [PunRPC]
    private void RPC_SetColor(Vector3 color)
    {
        Color = new Color(color.x, color.y, color.z);
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material.color = Color;
        }
        _minimapIconSpriteRenderer.color = Color;
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

    public void IncreaseSpeed(float extraSpeed)
    {
        _photonView.RPC("RPC_IncreaseSpeed", RpcTarget.AllViaServer, extraSpeed);
    }

    [PunRPC]
    private void RPC_IncreaseSpeed(float extraSpeed)
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
