using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerManager : MonoBehaviour
{
    //========================================================================
    public static PlayerManager singleton;

    public event System.Action PlayerAdded;

    //========================================================================
    public int playerCount = 0; 
    public List<Player> allPlayers = new();

    //========================================================================
    private void Awake()
    {
        singleton = this;
    }
    private void OnDestroy()
    {
    }

    //========================================================================
    /// <summary>
    /// 
    /// </summary>
    /// <param name="newPlayer"></param>
    /// <returns>the new player index</returns>
    public void AddPlayer(Player newPlayer)
    {
        newPlayer.gameObject.name = "Player" + allPlayers.Count.ToString();

        allPlayers.Add(newPlayer.GetComponent<Player>());

        newPlayer.GetComponent<Player>().playerIndex = playerCount;
        playerCount++;

        PlayerAdded?.Invoke();
    }

    //========================================================================
    public RpcParams GetPlayerRpcParams(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerCount)
        {
            Debug.LogError("player index out of range " + playerIndex);
            return null;
        }

        return Player.owningPlayer.RpcTarget.Single(allPlayers[playerIndex].OwnerClientId, RpcTargetUse.Temp);
    }

    //========================================================================
    public static FixedString32Bytes GetPlayerName(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= singleton.playerCount)
        {
            Debug.LogError("player index out of range " + playerIndex);
            return null;
        }

        return singleton.allPlayers[playerIndex].playerName;
    }
    public static int GetPlayerIndex(int playerIndex, int increment)
    {
        if (playerIndex + increment < 0) //below valid range
            return singleton.playerCount - 1; //last player index

        if (playerIndex + increment >= singleton.playerCount) //above valid range
            return 0;

        return playerIndex + increment;
    }
    public FixedString128Bytes GetHostName()
    {
        foreach (Player player in allPlayers)
        {
            if (player.IsOwnedByServer)
                return player.playerName;
        }
        Debug.LogError("couldn't get host's name");
        return "";
    }

    //========================================================================
}
