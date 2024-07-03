using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerManager : MonoBehaviour
{
    //========================================================================
    public event System.Action PlayerAdded;

    public static PlayerManager singleton;

    //========================================================================
    public int playerCount = 0; 
    public List<Player> allPlayers = new();

    int m_disconnectedPlayerIndex = -1;
    FixedString32Bytes m_disconnectedPlayerName;

    //========================================================================
    private void Awake()
    {
        singleton = this;
        Player.Disconnected += DiconnectPlayer;
        Player.Reconnected += ReconnectPlayer;
    }
    private void OnDestroy()
    {
        Player.Disconnected -= DiconnectPlayer;
        Player.Reconnected -= ReconnectPlayer;
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
    public void ReconnectPlayer(Player reconnectPlayer)
    {
        if (m_disconnectedPlayerIndex == -1)
        {
            Debug.LogError("There is no player to reconnect");
            return;
        }

        allPlayers[m_disconnectedPlayerIndex] = reconnectPlayer;
        reconnectPlayer.gameObject.name = "Player" + m_disconnectedPlayerIndex;
        reconnectPlayer.playerIndex = m_disconnectedPlayerIndex;
        reconnectPlayer.playerName = m_disconnectedPlayerName;

        m_disconnectedPlayerIndex = -1;
    }
    public void ConnectClient(ulong clientId, int playerIndex, FixedString32Bytes playerName)
    {
        foreach (Player player in allPlayers)
        {
            if (player.OwnerClientId == clientId)
            {
                player.gameObject.name = "Player" + playerIndex;
                player.playerIndex = playerIndex;
                player.playerName = playerName;
            }
        }
    }

    //========================================================================
    void DiconnectPlayer(int disconnectedPlayerIndex)
    {
        m_disconnectedPlayerIndex = disconnectedPlayerIndex;
        m_disconnectedPlayerName = allPlayers[disconnectedPlayerIndex].playerName;
    }
    public void ReinitializePlayers()
    {
        //remove null references
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i] == null)
                allPlayers.RemoveAt(i--);
        }

        for (int i = 0; i < allPlayers.Count; i++)
        {
            allPlayers[i].playerIndex = i;
        }
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
    public FixedString32Bytes GetPlayerName(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerCount)
        {
            Debug.LogError("player index out of range " + playerIndex);
            return null;
        }

        return allPlayers[playerIndex].playerName;
    }
    public int GetPlayerIndex(int playerIndex, int increment)
    {
        if (playerIndex + increment < 0) //below valid range
            return playerCount - 1; //last player index

        if (playerIndex + increment >= playerCount) //above valid range
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
