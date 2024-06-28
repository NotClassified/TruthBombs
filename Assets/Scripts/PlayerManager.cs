using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static event System.Action PlayerAdded;

    public static PlayerManager singleton;

    public int playerCount = 0;
    public List<GameObject> allPlayerObjects = new List<GameObject>();
    public List<Player> allPlayers = new List<Player>();

    private void Awake()
    {
        singleton = this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newPlayer"></param>
    /// <returns>the new player index</returns>
    public int AddPlayer(GameObject newPlayer)
    {
        newPlayer.name = "Player" + allPlayers.Count.ToString();
        allPlayerObjects.Add(newPlayer);
        allPlayers.Add(newPlayer.GetComponent<Player>());
        playerCount++;

        PlayerAdded?.Invoke();

        return allPlayers.Count - 1;
    }

}
