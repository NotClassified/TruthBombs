using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;
using Unity.VisualScripting;
using Unity.Collections;
using UnityEngine.Events;


namespace UIState
{
    public class SetupQuestionCards : StateBase
    {
        //========================================================================
        public Button startGameButton;
        public TextMeshProUGUI joinCodeText;

        //========================================================================
        public Transform questionCardParent;
        public GameObject questionCardPrefab;
        List<Button> m_questionCardObjects = new();

        //========================================================================
        public Transform playersParent;
        public GameObject playersPrefab;
        List<Transform> m_playerItems = new();

        //========================================================================
        public TMP_InputField customQuestionInput;
        string m_currentCustomInput = "";
        public Button customQuestionButton;
        public Button cancelEditModeButton;
        int m_currentSelectedCard;

        UnityAction m_EditModeRequest;

        //========================================================================
        public override void OnEnter()
        {
            base.OnEnter();

            GameManager.singleton.QuestionCardsUpdated += UpdateQuestionCards;
            GameManager.singleton.PlayerNameConfirmed += UpdateQuestionCards;

            joinCodeText.SetText("Join Code: " + GameManager.singleton.currentJoinCode);

            startGameButton.gameObject.SetActive(Player.owningPlayer.IsOwnedByServer);

            {
                GameManager.singleton.NewCustomEditingPlayer += CustomQuestionEditMode;

                m_EditModeRequest = () => { GameManager.singleton.RequestCustomQuestionEditMode_ServerRpc(Player.owningPlayer.playerIndex); };
                customQuestionButton.onClick.AddListener(m_EditModeRequest);

                cancelEditModeButton.gameObject.SetActive(false);
                cancelEditModeButton.onClick.AddListener(GameManager.singleton.CancelCustomQuestionEdit_Rpc);

                customQuestionInput.gameObject.SetActive(GameManager.singleton.customEditModeInUse);
                customQuestionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Customize Questions");
            }

            if (Player.owningPlayer.IsOwnedByServer)
            {
                startGameButton.interactable = false;
                startGameButton.onClick.AddListener(GameManager.singleton.StartAnswering_Rpc);
                UpdateQuestionCards();
            }
            else
            {
                GameManager.singleton.SyncCurrentQuestionCards_ServerRpc();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            GameManager.singleton.QuestionCardsUpdated -= UpdateQuestionCards;
            GameManager.singleton.PlayerNameConfirmed -= UpdateQuestionCards;

            customQuestionButton.onClick.RemoveAllListeners();

            if (Player.owningPlayer.IsOwnedByServer)
            {
                startGameButton.onClick.RemoveListener(GameManager.singleton.StartAnswering_Rpc);
            }
        }

        //========================================================================
        void UpdateQuestionCards()
        {
            foreach (Button card in m_questionCardObjects)
            {
                Destroy(card.gameObject);
            }
            m_questionCardObjects.Clear();

            List<FixedString128Bytes> questionCards = GameManager.singleton.GetCurrentQuestionCards();
            for (int i = 0; i < questionCards.Count; i++)
            {
                Button cardObject = Instantiate(questionCardPrefab, questionCardParent).GetComponent<Button>();
                m_questionCardObjects.Add(cardObject);

                int cardIndex = i;
                m_questionCardObjects[i].onClick.AddListener(
                    () => { GameManager.singleton.ChangeQuestionCard_ServerRpc(cardIndex); }
                );
                m_questionCardObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(questionCards[i].ToString());
            }


            foreach (Transform button in m_playerItems)
            {
                Destroy(button.gameObject);
            }
            m_playerItems.Clear();

            foreach (Player player in PlayerManager.singleton.allPlayers)
            {
                Transform selectButton = Instantiate(playersPrefab, playersParent).transform;
                m_playerItems.Add(selectButton);

                TextMeshProUGUI playerNameText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
                playerNameText.text = player.playerName.ToString();
            }

            int playerCount = PlayerManager.singleton.playerCount;
            int maxPlayers = GameManager.singleton.maxPlayers;
            joinCodeText.SetText("Join Code: " + GameManager.singleton.currentJoinCode + " (" + playerCount + "/" + maxPlayers + ")");
            startGameButton.interactable = GameManager.singleton.confirmedPlayerNameCount == playerCount;


            customQuestionInput.gameObject.SetActive(false);
            cancelEditModeButton.gameObject.SetActive(false);
            customQuestionButton.onClick.RemoveAllListeners();
            customQuestionButton.onClick.AddListener(m_EditModeRequest);
            customQuestionButton.interactable = true;
            customQuestionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Customize Questions");
        }

        //========================================================================
        void CustomQuestionEditMode(int editingPlayerIndex)
        {
            startGameButton.interactable = false;
            customQuestionButton.onClick.RemoveListener(m_EditModeRequest);

            //this is the editing player?
            if (editingPlayerIndex == Player.owningPlayer.playerIndex)
            {
                for (int i = 0; i < m_questionCardObjects.Count; i++)
                {
                    m_questionCardObjects[i].image.color = UIManager.singleton.unselectedUIColor;

                    m_questionCardObjects[i].onClick.RemoveAllListeners();
                    m_questionCardObjects[i].interactable = true;

                    int cardIndex = i;
                    m_questionCardObjects[i].onClick.AddListener(
                        () => { SelectCard(cardIndex); }
                    );
                }

                m_currentSelectedCard = -1;
                customQuestionInput.gameObject.SetActive(true);
                cancelEditModeButton.gameObject.SetActive(true);

                customQuestionButton.interactable = false;
                customQuestionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select Card");
            }
            else
            {
                for (int i = 0; i < m_questionCardObjects.Count; i++)
                {
                    m_questionCardObjects[i].image.color = UIManager.singleton.defaultUIColor;

                    m_questionCardObjects[i].onClick.RemoveAllListeners();
                    m_questionCardObjects[i].interactable = false;
                }

                customQuestionButton.interactable = false;
                customQuestionButton.GetComponentInChildren<TextMeshProUGUI>().SetText(PlayerManager.GetPlayerName(editingPlayerIndex) + " is Editing");
            }
        }

        void SelectCard(int cardIndex)
        {
            UnselectCard(m_currentSelectedCard); //reset previous selected card
            m_currentSelectedCard = cardIndex;

            m_questionCardObjects[cardIndex].image.color = UIManager.singleton.selectedUIColor;

            customQuestionButton.onClick.RemoveListener(ConfirmCustomQuestion);
            customQuestionButton.onClick.AddListener(ConfirmCustomQuestion);
            customQuestionButton.interactable = true;
            customQuestionButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Confirm Edit");
        }
        void UnselectCard(int cardIndex)
        {
            if (cardIndex == -1)
                return; //there wasn't a previous selected card

            m_questionCardObjects[cardIndex].image.color = UIManager.singleton.unselectedUIColor;
        }

        public void CustomQuewstionInput(string newInput)
        {
            if (newInput.Length > 128 - 3)
            {
                customQuestionInput.text = m_currentCustomInput;
                return; //prevent input that exceeds the "FixedString128Bytes" size
            }

            m_currentCustomInput = newInput;
        }
        void ConfirmCustomQuestion()
        {
            GameManager.singleton.SetCustomQuestionCard_ServerRpc(m_currentSelectedCard, m_currentCustomInput);
        }

        //========================================================================
    }
}