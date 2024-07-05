using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIState
{
    public class WinScreen : StateBase
    {
        //========================================================================
        public TextMeshProUGUI headerMessageText;
        public Button actionButton;

        //========================================================================
        private void OnDestroy()
        {
            GameManager.TieDataSynced -= TieDataSynced;
        }
        public override void OnEnter()
        {
            base.OnEnter();

            if (GameManager.singleton.allPlayersTied)
            {
                headerMessageText.SetText("Everyone Won!");

                SetActionButton(false);
            }
            else if (GameManager.singleton.playersTied)
            {
                headerMessageText.SetText("Calculating Scores...");

                if (!GameManager.singleton.isTieDataSynced)
                {
                    GameManager.TieDataSynced += TieDataSynced;
                }
                else //is already synced
                    TieDataSynced();
            }
            else //one player won
            {
                string playerWinName = PlayerManager.GetPlayerName(GameManager.singleton.playerWinIndex).ToString();
                headerMessageText.SetText("\"" + playerWinName + "\" Won!");

                SetActionButton(false);
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void SetActionButton(bool needsTieBreaker)
        {
            if (!Player.owningPlayer.IsOwnedByServer)
            {
                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Waiting for Host...");
            }
            else //is host
            {
                actionButton.interactable = true;
                if (needsTieBreaker)
                {
                    actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Start Tie Breaker");
                    actionButton.onClick.AddListener(StartTieBreaker);
                }
                else
                {
                    actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Start New Game");
                    actionButton.onClick.AddListener(StartNewGame);
                }
            }
        }

        void TieDataSynced()
        {
            GameManager.TieDataSynced -= TieDataSynced;

            string tieMessage = "";
            for (int i = 0; i < GameManager.singleton.currentTieData.tiePlayers.Count; i++)
            {
                if (i > 0)
                    tieMessage += "and ";

                tieMessage += "\"" + GameManager.singleton.currentTieData.GetPlayerName(i) + "\" ";
            }
            tieMessage += "\nHave Tied!";
            headerMessageText.SetText(tieMessage);

            SetActionButton(true);
        }

        //========================================================================
        void StartNewGame()
        {
            actionButton.interactable = false;
            GameManager.singleton.StartNewGame_Rpc();
        }
        void StartTieBreaker()
        {
            actionButton.interactable = false;
            GameManager.singleton.StartTieBreaker_Rpc();
        }

        //========================================================================
    }
}
