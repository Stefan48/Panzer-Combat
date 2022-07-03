using Photon.Pun;
using UnityEngine;

public class TankInfo : MonoBehaviour
{
    private PhotonView _photonView;
    [SerializeField] private TextMesh _usernameTextMesh;
    public int ActorNumber { get; private set; } //= -1;//
    public string Username { get; private set; }
    public bool IsSelected = false;
    public float Speed { get; private set; } = 12f;
    public float MaxHealth { get; private set; } = 100f;
    public float Health;
    public float Damage { get; private set; } = 25f;
    public float ShellSpeed { get; private set; } = 20f;
    public float ShellLifetime { get; private set; } = 10f;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        Health = MaxHealth;

        //ActorNumber = Random.Range(0, 100) % 2 == 0 ? 0 : -1;//
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
}
