using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerManager
{
    private readonly GameManager _gameManager;
    public readonly Color PlayerColor;
    private readonly Vector3 _spawnPosition;
    private readonly GameObject _tankPrefab;
    // Reinstantiating this would cause errors in the CameraControl script
    public List<GameObject> Tanks { get; private set; } = new List<GameObject>();

    public static event Action<GameObject> TanksListReducedEvent;
    public static readonly byte TankCrateCollectedNetworkEvent = 1;


    public PlayerManager(GameManager gameManager, Color playerColor, Vector3 spawnPosition, GameObject tankPrefab)
    {
        _gameManager = gameManager;
        PlayerColor = playerColor;
        _spawnPosition = spawnPosition;
        _tankPrefab = tankPrefab;

        TankHealth.AlliedTankGotDestroyedEvent += OnAlliedTankGotDestroyed;
        UiManager.EscPanelToggledEvent += OnEscPanelToggled;
        PhotonNetwork.NetworkingClient.EventReceived += NetworkingClient_EventReceived;
    }

    public void UnsubscribeFromEvents()
    {
        TankHealth.AlliedTankGotDestroyedEvent -= OnAlliedTankGotDestroyed;
        UiManager.EscPanelToggledEvent -= OnEscPanelToggled;
        PhotonNetwork.NetworkingClient.EventReceived -= NetworkingClient_EventReceived;
    }

    public void Setup()
    {
        // TODO - Have user-defined initial number of tanks
        // If there are multiple tanks, make sure they don't overlap
        // Rotate them towards the map's center?
        for (int i = 0; i < 2; ++i)
        {
            InstantiateDefaultTank(_spawnPosition);

            /*if (_gameManager.ActorNumber > 1) // this is for testing only
            {
                break;
            }*/
        }
    }

    private GameObject InstantiateDefaultTank(Vector3 position)
    {
        GameObject tank = PhotonNetwork.Instantiate(_tankPrefab.name, position, Quaternion.identity, 0,
            new object[] { _gameManager.ActorNumber * TankInfo.TankNumberMultiplier + Tanks.Count,
                new Vector3(PlayerColor.r,  PlayerColor.g, PlayerColor.b) });
        tank.transform.Find("Vision").gameObject.SetActive(true);
        Tanks.Add(tank);
        return tank;
    }

    public void SetControlEnabled(bool enabled)
    {
        foreach (GameObject tank in Tanks)
        {
            tank.GetComponent<TankMovement>().enabled = enabled;
            tank.GetComponent<TankShooting>().enabled = enabled;
            tank.transform.Find("HealthBar").gameObject.SetActive(enabled);
            tank.transform.Find("OwnerText").gameObject.SetActive(enabled);
        }
    }

    public void Reset()
    {
        foreach (GameObject tank in Tanks)
        {
            PhotonNetwork.Destroy(tank);
        }
        Tanks.Clear();
    }

    private void OnAlliedTankGotDestroyed(GameObject tank, int defeaterPlayerActorNumber)
    {
        if (!Tanks.Remove(tank))
        {
            Debug.LogWarning("[PlayerManager] Could not remove destroyed tank from the Tanks list");
        }
        TanksListReducedEvent?.Invoke(tank);
        if (Tanks.Count == 0)
        {
            _gameManager.LocalPlayerLost(defeaterPlayerActorNumber);
        }
    }

    private void OnEscPanelToggled(bool active)
    {
        // If Esc panel is active, then controls are not
        foreach (GameObject tank in Tanks)
        {
            tank.GetComponent<TankMovement>().EscPanelIsActive = active;
            tank.GetComponent<TankShooting>().EscPanelIsActive = active;
            tank.GetComponent<TankAbilities>().EscPanelIsActive = active;
        }
    }

    private void NetworkingClient_EventReceived(EventData obj)
    {
        if (obj.Code == TankCrateCollectedNetworkEvent)
        {
            float[] eventData = (float[])obj.CustomData;
            Vector3 position = new Vector3(eventData[0], 0f, eventData[1]);
            // TODO - Use instantiation data for stats as well (to avoid RPC call)
            GameObject tank = InstantiateDefaultTank(position);
            // The third float tells if the tank should be a default one or if it has duplicated stats instead
            if (eventData[2] == 0f)
            {
                int health = (int)eventData[3];
                int maxHealth = (int)eventData[4];
                int ammo = (int)eventData[5];
                int damage = (int)eventData[6];
                int armor = (int)eventData[7];
                int speed = (int)eventData[8];
                int range = (int)eventData[9];
                tank.GetComponent<TankInfo>().SetStats(health, maxHealth, ammo, damage, armor, speed, range);
            }
        }
    }
}
