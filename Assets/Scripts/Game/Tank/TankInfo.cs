using Photon.Pun;
using UnityEngine;

public class TankInfo : MonoBehaviour
{
    private PhotonView _photonView;
    // TODO - Have the username of the owner (and display it in the UI)
    // Then, the player number might not be necessary (maybe yes though, for the camera and the UiManager)
    public int PlayerNumber { get; private set; } //= -1;//
    public bool IsSelected = false;
    public float Speed { get; private set; } = 12f;
    public float ShellSpeed { get; private set; } = 20f;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();

        //PlayerNumber = Random.Range(0, 100) % 2 == 0 ? 0 : -1;//
    }

    public void SetPlayerNumber(int playerNumber)
    {
        _photonView.RPC("RPC_SetPlayerNumber", RpcTarget.All, playerNumber);
    }

    [PunRPC]
    private void RPC_SetPlayerNumber(int playerNumber)
    {
        PlayerNumber = playerNumber;
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
