using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class State_EnterPlayerName : StateBase
{
    public static State_EnterPlayerName singleton;
    public static event System.Action<string> NameConfirmed;

    string currentNameInput = "";

    private void Awake()
    {
        singleton = this;
    }

    public void NewTextInput(string newText)
    {
        currentNameInput = newText;
    }

    public void ConfirmName()
    {
        NameConfirmed?.Invoke(currentNameInput);
        UIManager.singleton.ChangeUIState<State_SetupQuestionCards>();
    }
}
