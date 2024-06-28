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

            State_EnterPlayerName.NameConfirmed += Owner_ChangeName;
        }

        playerIndex = PlayerManager.singleton.AddPlayer(gameObject);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {
            State_EnterPlayerName.NameConfirmed -= Owner_ChangeName;
        }
    }



    void Owner_ChangeName(string newName)
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
    [ServerRpc]
    void ChangeName_ServerRpc(FixedString32Bytes newName)
    {
        ChangeName_ClientRpc(newName);
    }
    [ClientRpc]
    void ChangeName_ClientRpc(FixedString32Bytes newName)
    {
        playerName = newName;
        print(gameObject.name + ", " + playerName + ", ClientRpc");
    }


    [Rpc(SendTo.Server)]
    public int GetQuestionCards_Rpc()
    {
        return 1;
    }
    [Rpc(SendTo.NotServer)]
    public void UpdateQuestionCard_Rpc(int cardIndex, FixedString128Bytes newNum)
    {
        print("update one NotServer");
        State_SetupQuestionCards.singleton.UpdateQuestionCard_NotServer(cardIndex, newNum);
    }
    [Rpc(SendTo.Server)]
    public void UpdateAllQuestionCards_Rpc()
    {
        print("update all server player");
        State_SetupQuestionCards.singleton.UpdateAllQuestionCards_Server();
    }


    [ClientRpc]
    public void StartGame_ClientRpc()
    {
        UIManager.singleton.ChangeUIState<State_AnswerSheet>();
    }


    [ServerRpc]
    public void AddAsnwer_ServerRpc(int playerIndex, int answerSheetIndex, int cardIndex, FixedString128Bytes newAnswer)
    {

    }
}
