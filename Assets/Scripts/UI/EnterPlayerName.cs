using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Collections;


namespace UIState
{
    public class EnterPlayerName : StateBase
    {
        public static EnterPlayerName singleton;
        public static event System.Action<string> NameConfirmed;

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

            NameConfirmed?.Invoke(currentNameInput);
        }
    }

}