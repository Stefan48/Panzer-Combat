using Photon.Pun;
using UnityEngine;

public class TurretInfo : MonoBehaviour, IPunInstantiateMagicCallback
{
    private PhotonView _photonView;
    [SerializeField] private TextMesh _usernameTextMesh;
    private readonly Quaternion _uiComponentsRotation = Quaternion.Euler(0f, 60f, 0f);
    public int ActorNumber { get; private set; } = -1;
    public string Username { get; private set; }
    [SerializeField] private Color _color;
    public Color Color => _color;
    public int MaxHealth = 500;
    public int Health;
    public int Damage { get; private set; } = 20;
    public int Armor { get; private set; } = 0;
    // Turret shells are faster than regular shells
    public static readonly int TurretShellsExtraSpeed = 5;
    public int ShellSpeed { get; private set; } = 25;
    public int Range { get; private set; } = 10;
    private static readonly int s_defaultRange = 10;
    [SerializeField] private GameObject _vision;
    [SerializeField] private GameObject _range;
    private const float _visionPerRange = 0.065f;


    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        Health = MaxHealth;
        transform.Find("HealthAndTimeBars").rotation = _uiComponentsRotation;
        transform.Find("OwnerText").rotation = _uiComponentsRotation;
    }

    // This is called after Awake and before Start
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        ActorNumber = (int)instantiationData[0];
        Username = PhotonNetwork.CurrentRoom.GetPlayer(ActorNumber)?.NickName;
        _usernameTextMesh.text = Username;
        Damage = (int)instantiationData[1];
        Armor = (int)instantiationData[2];
        ShellSpeed = (int)instantiationData[3];
        Range = (int)instantiationData[4];
        if (ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            _vision.SetActive(true);
        }
        if (Range > s_defaultRange)
        {
            float newScale = _vision.transform.localScale.x + _visionPerRange * (Range - s_defaultRange);
            // The vision and range scales match
            _vision.transform.localScale = new Vector3(newScale, newScale, 0f);
            // The range scale has to be up to date on all clients so they can check for trigger enters/exits correctly
            _range.transform.localScale = new Vector3(newScale, newScale, newScale);
            // For simplicity and due to the lack of importance, the max camera size is not influenced by the owned turrets' ranges
        }
    }
}
