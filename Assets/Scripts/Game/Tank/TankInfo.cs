using Photon.Pun;
using UnityEngine;

public class TankInfo : MonoBehaviour
{
    private PhotonView _photonView;
    [SerializeField] private TextMesh _usernameTextMesh;
    public int ActorNumber { get; private set; } = -1; // initializing is for testing only
    public string Username { get; private set; }
    public bool IsSelected = false;
    public float Speed { get; private set; } = 12f;
    public int MaxHealth = 100;
    public int Health;
    public int Armor { get; private set; } = 0;
    public int Damage { get; private set; } = 20;
    public float ShellSpeed { get; private set; } = 20f;
    public float ShellLifetime { get; private set; } = 10f;


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
        Color c = new Color(color.x, color.y, color.z);
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material.color = c;
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
}
