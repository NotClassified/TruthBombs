using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    public static Player owningPlayer;

    public static event System.Action OwnerSpawned;
    /// <summary>(int disconnectedPlayerIndex)</summary>
    public event System.Action<int> ClientDisconnected;

    public static System.Action<string> NameConfirmed;

    public int playerIndex;
    public FixedString32Bytes playerName;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (GameManager.singleton.isPlayingGame)
            return;

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

        ClientDisconnected?.Invoke(playerIndex);
    }
}
