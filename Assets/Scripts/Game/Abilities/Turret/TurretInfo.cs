using Photon.Pun;
using UnityEngine;

public class TurretInfo : MonoBehaviour
{
    private PhotonView _photonView;
    [SerializeField] private TextMesh _usernameTextMesh;
    public int ActorNumber { get; private set; } = -1; // initializing is for testing only
    public string Username { get; private set; }
    [SerializeField] private Color _color;
    public Color Color => _color;
    public int MaxHealth = 500;
    public int Health;
    public int Damage { get; private set; } = 20;
    public int Armor { get; private set; } = 0;
    public int ShellSpeed { get; private set; } = 20; // TODO - Higher shell speed (have a static constant for the difference)
    public int Range { get; private set; } = 10;
    private static readonly int s_defaultRange = 10;
    [SerializeField] private GameObject _vision;
    private const float _visionPerRange = 0.065f;

    // public static event Action<int> TurretRangeIncreasedEvent; // TODO?


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        Health = MaxHealth;
    }

    public void SetInfo(int actorNumber, int damage, int armor, int shellSpeed, int range)
    {
        _photonView.RPC("RPC_SetInfo", RpcTarget.AllViaServer, actorNumber, damage, armor, shellSpeed, range);
        // TODO - if (actorNumber == local actor number &&) range > s_defaultRange, increase vision scale
    }

    [PunRPC]
    private void RPC_SetInfo(int actorNumber, int damage, int armor, int shellSpeed, int range)
    {
        ActorNumber = actorNumber;
        Username = PhotonNetwork.CurrentRoom.GetPlayer(ActorNumber).NickName;
        _usernameTextMesh.text = Username;
        Damage = damage;
        Armor = armor;
        ShellSpeed = shellSpeed;
        Range = range;
    }

    
    /*[PunRPC]
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
    }*/
}
