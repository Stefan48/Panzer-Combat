using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerManager
{
    private readonly GameManager _gameManager;
    private readonly int _playerNumber;
    private readonly Color _playerColor;
    private readonly Vector3 _spawnPosition;
    private readonly GameObject _tankPrefab;
    public List<GameObject> Tanks { get; private set; } = new List<GameObject>();
    

    public PlayerManager(GameManager gameManager, int playerNumber, Color playerColor, Vector3 spawnPosition, GameObject tankPrefab)
    {
        _gameManager = gameManager;
        _playerNumber = playerNumber;
        _playerColor = playerColor;
        _spawnPosition = spawnPosition;
        _tankPrefab = tankPrefab;

        TankHealth.TankGotDestroyedEvent += OnTankGotDestroyed;
    }

    ~PlayerManager()
    {
        TankHealth.TankGotDestroyedEvent -= OnTankGotDestroyed;
    }

    public void Setup()
    {
        // TODO - Have user-defined initial number of tanks
        GameObject tank = PhotonNetwork.Instantiate(_tankPrefab.name, _spawnPosition, Quaternion.identity);
        TankInfo tankInfo = tank.GetComponent<TankInfo>();
        tankInfo.SetPlayerNumber(_playerNumber);
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
            Debug.LogWarning("Could not remove destroyed tank from Tanks list");
        }

        if (Tanks.Count == 0)
        {
            _gameManager.LocalPlayerLost();
        }
    }
}
