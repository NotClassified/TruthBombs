using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    //private readonly NetworkVariable<FixedString32Bytes> m_netPlayerName = new(writePerm: NetworkVariableWritePermission.Owner);
    public static Player owningPlayer;
    public FixedString32Bytes playerName;

    public int playerIndex;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            owningPlayer = this;


        }

        playerIndex = PlayerManager.singleton.AddPlayer(gameObject);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {

        }
    }



    public void Owner_ChangeName(string newName)
    {
        if (IsServer)
        {
            print(gameObject.name + ", " + newName + ", ownerserver");
            ChangeName_ClientRpc(newName);
        }
        else
        {
            print(gameObject.name + ", " + newName + ", ownerclient");
            ChangeName_ServerRpc(newName);
        }
    }
    [Rpc(SendTo.Server)]
    void ChangeName_ServerRpc(FixedString32Bytes newName)
    {
        ChangeName_ClientRpc(newName);
    }
    [Rpc(SendTo.NotServer)]
    void ChangeName_ClientRpc(FixedString32Bytes newName)
    {
        playerName = newName;
        print(gameObject.name + ", " + playerName + ", ClientRpc");
    }


    [Rpc(SendTo.NotServer)]
    public void StartGame_ClientRpc()
    {
        UIManager.singleton.ChangeUIState<State_AnswerSheet>();
    }


    [ServerRpc]
    public void AddAsnwer_ServerRpc(int playerIndex, int answerSheetIndex, int cardIndex, FixedString128Bytes newAnswer)
    {

    }
}
