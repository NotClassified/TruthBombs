using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class State_EnterPlayerName : StateBase
{
    public static State_EnterPlayerName singleton;

    string currentNameInput = "";

    private void Awake()
    {
        singleton = this;
    }

    public void NewTextInput(string newText)
    {
        if (newText.Length > 32)
            return; //prevent a name that exceeds the "FixedString32Bytes" size

        currentNameInput = newText;
    }

    public void ConfirmName()
    {
        PlayerManager.singleton.ChangePlayerName_Rpc(Player.owningPlayer.playerIndex, currentNameInput);

        UIManager.singleton.ChangeUIState<State_SetupQuestionCards>();
    }
}
