using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerManager : NetworkBehaviour
{
    //========================================================================
    public static event System.Action PlayerAdded;

    public static PlayerManager singleton;

    //========================================================================
    public int playerCount = 0;
    public List<GameObject> allPlayerObjects = new List<GameObject>();
    public List<Player> allPlayers = new List<Player>();

    //========================================================================
    private void Awake()
    {
        singleton = this;
    }

    //========================================================================
    /// <summary>
    /// 
    /// </summary>
    /// <param name="newPlayer"></param>
    /// <returns>the new player index</returns>
    public void AddPlayer(GameObject newPlayer)
    {
        newPlayer.name = "Player" + allPlayers.Count.ToString();

        allPlayerObjects.Add(newPlayer);
        allPlayers.Add(newPlayer.GetComponent<Player>());

        newPlayer.GetComponent<Player>().playerIndex = playerCount;
        playerCount++;

        PlayerAdded?.Invoke();
    }

    //========================================================================
    [Rpc(SendTo.Server)]
    public void ChangePlayerName_Rpc(int playerIndex, FixedString32Bytes newName)
    {
        allPlayers[playerIndex].playerName = newName;
        SyncPlayerNames_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    void SyncPlayerNames_ServerRpc()
    {
        for (int i = 0; i < allPlayers.Count; i++)
        {
            SyncPlayerNames_ClientRpc(i, allPlayers[i].playerName);
        }
    }
    [Rpc(SendTo.NotServer)]
    void SyncPlayerNames_ClientRpc(int playerIndex, FixedString32Bytes playerName)
    {
        allPlayers[playerIndex].playerName = playerName;
    }
}
