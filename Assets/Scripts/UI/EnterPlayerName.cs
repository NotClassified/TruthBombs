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
        bool confirmingName;

        string currentNameInput = "";

        private void Awake()
        {
            singleton = this;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            confirmingName = false;
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

            GameManager.singleton.ChangePlayerName_ServerRpc(Player.owningPlayer.playerIndex, currentNameInput);
            NameConfirmed?.Invoke(currentNameInput);
        }
    }

}