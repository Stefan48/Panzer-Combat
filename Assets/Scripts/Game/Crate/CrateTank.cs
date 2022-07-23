using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CrateTank : Crate
{
    private static readonly int s_duplicateTankChance = 20;
    private float _positionX;
    private float _positionZ;
    private bool _defaultTank;
    private int _health;
    private int _maxHealth;
    private int _ammo;
    private int _damage;
    private int _armor;
    private int _speed;
    private int _range;


    [PunRPC]
    protected override void RPC_OnCollect(string onCollectText)
    {
        base.RPC_OnCollect(onCollectText);
    }

    [PunRPC]
    protected override void RPC_Shatter()
    {
        base.RPC_Shatter();
    }

    protected override void Awake()
    {
        base.Awake();
        SetRandomTankType();
    }

    private void SetRandomTankType()
    {
        if (UnityEngine.Random.Range(0, 100) < s_duplicateTankChance)
        {
            _defaultTank = false;
        }
        else
        {
            _defaultTank = true;
        }
    }

    protected override string GetOnCollectText(GameObject tank)
    {
        Vector3 extraTankPosition = 2f * transform.position - tank.transform.position;
        _positionX = extraTankPosition.x;
        _positionZ = extraTankPosition.z;
        if (!_defaultTank)
        {
            TankInfo tankInfo = tank.GetComponent<TankInfo>();
            _health = tankInfo.Health;
            _maxHealth = tankInfo.MaxHealth;
            _ammo = tankInfo.Ammo;
            _damage = tankInfo.Damage;
            _armor = tankInfo.Armor;
            _speed = tankInfo.Speed;
            _range = tankInfo.Range;
        }
        return "Extra Tank";
    }

    protected override void RewardPlayer(GameObject tank)
    {
        int rewardedPlayer = tank.GetComponent<TankInfo>().ActorNumber;
        if (_defaultTank)
        {
            PhotonNetwork.RaiseEvent(PlayerManager.TankCrateCollectedNetworkEvent, new float[] { _positionX, _positionZ, 1f },
                new RaiseEventOptions { TargetActors = new int[] { rewardedPlayer } }, SendOptions.SendReliable);
        }
        else
        {
            float[] eventContent = new float[10];
            eventContent[0] = _positionX;
            eventContent[1] = _positionZ;
            eventContent[2] = 0f;
            eventContent[3] = _health;
            eventContent[4] = _maxHealth;
            eventContent[5] = _ammo;
            eventContent[6] = _damage;
            eventContent[7] = _armor;
            eventContent[8] = _speed;
            eventContent[9] = _range;
            PhotonNetwork.RaiseEvent(PlayerManager.TankCrateCollectedNetworkEvent, eventContent,
                new RaiseEventOptions { TargetActors = new int[] { rewardedPlayer } }, SendOptions.SendReliable);
        }
    }
}
