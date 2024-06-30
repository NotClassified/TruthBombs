using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace UIState
{
    public class GuessPlayer : StateBase
    {
        //========================================================================
        public TextMeshProUGUI targetPlayerNameText;

        public Transform playerSelectParent;
        public GameObject playerSelectPrefab;
        List<Transform> m_playerSelectButtons = new();

        int m_selectePlayerIndex;

        public Button actionButton;

        //========================================================================
        public override void OnEnter()
        {
            base.OnEnter();

            m_selectePlayerIndex = -1;

            //is this the target player that will be guessing who made their favorite answer?
            if (GameManager.singleton.GetPresentingSheetIndex() == Player.owningPlayer.playerIndex)
            {
                actionButton.interactable = false;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select a Player");
                actionButton.onClick.AddListener(ConfirmPlayerGuess);
            }
            else //not the target player
            {
                actionButton.interactable = false;
                string targetPlayerName = GameManager.singleton.GetPresentingTargetPlayerName();
                actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText(targetPlayerName + " is Guessing");
            }

            //set player select buttons
            {
                foreach (Transform button in m_playerSelectButtons)
                {
                    Destroy(button.gameObject);
                }
                m_playerSelectButtons.Clear();


                foreach (Player player in PlayerManager.singleton.allPlayers)
                {
                    Transform selectButton = Instantiate(playerSelectPrefab, playerSelectParent).transform;
                    m_playerSelectButtons.Add(selectButton);

                    SetPlayerColor(selectButton, UIManager.singleton.unselectedUIColor);

                    TextMeshProUGUI questionCardText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
                    questionCardText.text = player.playerName.ToString();
                }
            }
        }
        public override void OnExit()
        {
            base.OnExit();

            actionButton.onClick.RemoveAllListeners();
        }

        //========================================================================
        void SelectPlayer(int playerIndex)
        {
            UnselectPlayer(m_selectePlayerIndex); //reset previous selected card

            m_selectePlayerIndex = playerIndex;
            SetPlayerColor(m_playerSelectButtons[playerIndex], UIManager.singleton.selectedUIColor);

            actionButton.interactable = true;
            actionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Guess");
        }
        void UnselectPlayer(int playerIndex)
        {
            if (m_selectePlayerIndex == -1)
                return; //there wasn't a previous selected Player

            SetPlayerColor(m_playerSelectButtons[playerIndex], UIManager.singleton.unselectedUIColor);
        }

        void SetPlayerColor(Transform player, Color newColor)
        {
            player.GetComponent<Button>().image.color = newColor;
        }

        //========================================================================
        void ConfirmPlayerGuess()
        {
            GameManager.singleton.ConfirmPlayerGuess_Rpc(m_selectePlayerIndex);
        }

        //========================================================================
    }

}