using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    public static Player owningPlayer;
    public static int disconnectingPlayerIndex = -1;

    public static event System.Action OwnerSpawned;
    /// <summary>(int disconnectedPlayerIndex)</summary>
    public static event System.Action<int> Disconnected;
    public static event System.Action<Player> Reconnected;
    public static event System.Action Reconnecting;

    public int playerIndex;
    public FixedString32Bytes playerName;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        disconnectingPlayerIndex = -1;

        if (IsOwnedByServer)
        {
            Initialization();
        }
        else if (IsServer)
        {
            if (GameManager.singleton.isWaitingForPlayerReconnection)
            {
                Reconnected?.Invoke(this);
            }
            else
            {
                Initialization();
            }
        }
        else //client
        {
            if (GameManager.singleton.hasConnected) //owner has already connected, this a new client
            {
                if (GameManager.singleton.isWaitingForPlayerReconnection)
                    Reconnected?.Invoke(this);
                else
                    Initialization();
            }
            else if (IsOwner) //owner hasn't been connected and will request reconnection status
            {
                Initialization();

                GameManager.RespondReconnectionStatus += ReconnectionStatusResponse;
                GameManager.singleton.RequestReconnectionStatus_OwnerClient();
            }
            else //other clients where the owner hasn't been connected
            {
                Initialization();
            }
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (owningPlayer.playerIndex != disconnectingPlayerIndex)
            Disconnected?.Invoke(playerIndex); //don't call on the owner client that is disconnecting
    }

    void ReconnectionStatusResponse(bool status)
    {
        print("ReconnectionStatusResponse");
        GameManager.RespondReconnectionStatus -= ReconnectionStatusResponse;

        if (status)
        {
            Reconnecting?.Invoke();
            GameManager.singleton.ReconnectPlayers_OwnerClient();
        }
    }
    void Initialization()
    {
        if (IsOwner)
        {
            owningPlayer = this;
            GameManager.singleton.hasConnected = true;

            if (IsOwnedByServer)
                GameManager.singleton.SubscribeEventsForServer();

            OwnerSpawned?.Invoke();
        }

        PlayerManager.singleton.AddPlayer(this);
    }
}
