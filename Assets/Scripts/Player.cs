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

    public int playerIndex;
    public FixedString32Bytes playerName;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            owningPlayer = this;

            if (IsOwnedByServer)
                GameManager.singleton.SubscribeEventsForServer();

            OwnerSpawned?.Invoke();
        }

        PlayerManager.singleton.AddPlayer(this);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (owningPlayer.playerIndex != disconnectingPlayerIndex)
            Disconnected?.Invoke(playerIndex); //don't call on the owner client that is disconnecting
    }
}
