using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Collections;


namespace UIState
{
    public class EnterPlayerName : StateBase
    {
        bool confirmingName;

        string currentNameInput = "";

        public TextMeshProUGUI joinCodeText;

        public override void OnEnter()
        {
            base.OnEnter();
            confirmingName = false;

            joinCodeText.SetText("Join Code: " + GameManager.singleton.currentJoinCode);
        }

        public void NewTextInput(string newText)
        {
            if (newText.Length > 32)
                return; //prevent a name that exceeds the "FixedString32Bytes" size

            currentNameInput = newText;
        }

        public void ConfirmName()
        {
            if (confirmingName)
                return;
            confirmingName = true;

            if (currentNameInput == "")
                currentNameInput = "Player " + Player.owningPlayer.playerIndex.ToString();

            GameManager.singleton.ChangePlayerName_ServerRpc(Player.owningPlayer.playerIndex, currentNameInput);
            Player.NameConfirmed?.Invoke(currentNameInput);
        }

    }
}