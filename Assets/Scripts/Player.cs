using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    public static Player owningPlayer;
    public FixedString32Bytes playerName;

    public int playerIndex;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            owningPlayer = this;

            if (IsServer)
                GameManager.singleton.SubscribeEventsForServer();
        }

        PlayerManager.singleton.AddPlayer(gameObject);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {

        }
    }
}
