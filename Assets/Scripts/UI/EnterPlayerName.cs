using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Collections;


namespace UIState
{
    public class EnterPlayerName : StateBase
    {
        bool m_confirmingName;

        string m_currentNameInput = "";
        public TMP_InputField nameInput;

        public TextMeshProUGUI joinCodeText;

        public override void OnEnter()
        {
            base.OnEnter();
            m_confirmingName = false;

            joinCodeText.SetText("Join Code: " + GameManager.singleton.currentJoinCode);
        }

        public void NewTextInput(string newText)
        {
            if (newText.Length > 32 - 3)
            {
                nameInput.text = m_currentNameInput;
                return; //prevent a name that exceeds the "FixedString32Bytes" size
            }

            m_currentNameInput = newText;
        }

        public void ConfirmName()
        {
            if (m_confirmingName)
                return;
            m_confirmingName = true;

            if (m_currentNameInput == "")
                m_currentNameInput = "Player " + Player.owningPlayer.playerIndex.ToString();

            GameManager.singleton.ChangePlayerName_ServerRpc(Player.owningPlayer.playerIndex, m_currentNameInput);
            Player.NameConfirmed?.Invoke(m_currentNameInput);
        }

    }
}