using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerManager
{
    public int PlayerNumber { get; private set; }
    public Color PlayerColor { get; private set; }
    public Transform SpawnPoint { get; private set; }
    private readonly GameObject tankPrefab;
    public List<GameObject> Tanks { get; private set; }
    public int RoundsWon { get; set; }
    

    public PlayerManager(int playerNumber, Color playerColor, Transform spawnPoint, GameObject tankPrefab)
    {
        PlayerNumber = playerNumber;
        PlayerColor = playerColor;
        SpawnPoint = spawnPoint;
        this.tankPrefab = tankPrefab;
    }

    public void Setup()
    {
        Tanks = new List<GameObject>();
        GameObject tank = GameObject.Instantiate(tankPrefab, SpawnPoint.position, Quaternion.identity);
        Tanks.Add(tank);
        tank.GetComponent<TankMovement>().playerNumber = PlayerNumber;
        
        MeshRenderer[] renderers = tank.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            renderers[i].material.color = PlayerColor;
        }
    }

    public void DisableControl()
    {
        for (int i = 0; i < Tanks.Count; ++i)
        {
            Tanks[i].GetComponent<TankMovement>().enabled = false;
            Tanks[i].GetComponent<TankShooting>().enabled = false;
            Tanks[i].GetComponentInChildren<Canvas>(true).gameObject.SetActive(false);
        }
    }

    public void EnableControl()
    {
        for (int i = 0; i < Tanks.Count; ++i)
        {
            Tanks[i].GetComponent<TankMovement>().enabled = true;
            Tanks[i].GetComponent<TankShooting>().enabled = true;
            Tanks[i].GetComponentInChildren<Canvas>(true).gameObject.SetActive(true);
        }
    }

    public void Reset()
    {
        while (Tanks.Count > 1)
        {
            GameObject.Destroy(Tanks[1]);
        }
        Tanks[0].transform.position = SpawnPoint.position;
        Tanks[0].transform.rotation = Quaternion.identity;
        Tanks[0].SetActive(false);
        Tanks[0].SetActive(true);
    }

}
