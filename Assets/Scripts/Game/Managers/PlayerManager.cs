using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerManager
{
    private readonly GameManager _gameManager;
    private readonly Color _playerColor;
    private readonly Vector3 _spawnPosition;
    private readonly GameObject _tankPrefab;
    public List<GameObject> Tanks { get; private set; } = new List<GameObject>();


    public PlayerManager(GameManager gameManager, Color playerColor, Vector3 spawnPosition, GameObject tankPrefab)
    {
        _gameManager = gameManager;
        _playerColor = playerColor;
        _spawnPosition = spawnPosition;
        _tankPrefab = tankPrefab;

        TankHealth.TankGotDestroyedEvent += OnTankGotDestroyed;
        UiManager.EscPanelToggledEvent += OnEscPanelToggled;
    }

    public void UnsubscribeFromEvents()
    {
        TankHealth.TankGotDestroyedEvent -= OnTankGotDestroyed;
        UiManager.EscPanelToggledEvent -= OnEscPanelToggled;
    }

    public void Setup()
    {
        // TODO - Have user-defined initial number of tanks
        // If there are multiple tanks, make sure they don't overlap
        // Rotate them towards the map's center?
        GameObject tank = PhotonNetwork.Instantiate(_tankPrefab.name, _spawnPosition, Quaternion.identity);
        TankInfo tankInfo = tank.GetComponent<TankInfo>();
        tankInfo.SetActorNumber(_gameManager.ActorNumber);
        tankInfo.SetUsername(PhotonNetwork.LocalPlayer.NickName);
        tankInfo.SetColor(_playerColor);
        Tanks.Add(tank);
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

    private void OnTankGotDestroyed(GameObject tank)
    {
        if (!Tanks.Remove(tank))
        {
            Debug.LogWarning("[PlayerManager] Could not remove destroyed tank from the Tanks list");
        }
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
