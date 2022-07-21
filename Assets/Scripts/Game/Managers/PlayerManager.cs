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


    public PlayerManager(GameManager gameManager, Color playerColor, Vector3 spawnPosition, GameObject tankPrefab)
    {
        _gameManager = gameManager;
        PlayerColor = playerColor;
        _spawnPosition = spawnPosition;
        _tankPrefab = tankPrefab;

        TankHealth.AlliedTankGotDestroyedEvent += OnAlliedTankGotDestroyed;
        UiManager.EscPanelToggledEvent += OnEscPanelToggled;
    }

    public void UnsubscribeFromEvents()
    {
        TankHealth.AlliedTankGotDestroyedEvent -= OnAlliedTankGotDestroyed;
        UiManager.EscPanelToggledEvent -= OnEscPanelToggled;
    }

    public void Setup()
    {
        // TODO - Have user-defined initial number of tanks
        // If there are multiple tanks, make sure they don't overlap
        // Rotate them towards the map's center?
        for (int i = 0; i < 1; ++i)
        {
            GameObject tank = PhotonNetwork.Instantiate(_tankPrefab.name, _spawnPosition, Quaternion.identity);
            TankInfo tankInfo = tank.GetComponent<TankInfo>();
            tankInfo.SetActorNumber(_gameManager.ActorNumber);
            tankInfo.SetUsername(PhotonNetwork.LocalPlayer.NickName);
            tankInfo.SetColor(PlayerColor);
            tank.transform.Find("Vision").gameObject.SetActive(true);
            Tanks.Add(tank);

            /*if (_gameManager.ActorNumber > 1) // this is for testing only
            {
                break;
            }*/
        }
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

    private void OnAlliedTankGotDestroyed(GameObject tank)
    {
        if (!Tanks.Remove(tank))
        {
            Debug.LogWarning("[PlayerManager] Could not remove destroyed tank from the Tanks list");
        }
        TanksListReducedEvent?.Invoke(tank);
        if (Tanks.Count == 0)
        {
            _gameManager.LocalPlayerLost();
        }
    }

    private void OnEscPanelToggled(bool active)
    {
        // If Esc panel is active, then controls are not
        foreach (GameObject tank in Tanks)
        {
            tank.GetComponent<TankMovement>().EscPanelIsActive = active;
            tank.GetComponent<TankShooting>().EscPanelIsActive = active;
        }
    }
}
