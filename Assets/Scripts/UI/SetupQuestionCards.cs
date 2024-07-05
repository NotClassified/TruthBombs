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
        public Button startGameButton;
        public TextMeshProUGUI joinCodeText;

        public Transform questionCardParent;
        public GameObject questionCardPrefab;
        List<GameObject> m_questionCardObjects = new();

        public Transform playersParent;
        public GameObject playersPrefab;
        List<Transform> m_playerItems = new();

        private void OnDestroy()
        {
            GameManager.QuestionCardsUpdated -= UpdateQuestionCards;
            GameManager.PlayerNameConfirmed -= UpdateQuestionCards;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            joinCodeText.SetText("Join Code: " + GameManager.singleton.currentJoinCode);

            startGameButton.gameObject.SetActive(Player.owningPlayer.IsOwnedByServer);

            GameManager.QuestionCardsUpdated += UpdateQuestionCards;
            GameManager.PlayerNameConfirmed += UpdateQuestionCards;

            if (Player.owningPlayer.IsOwnedByServer)
            {
                startGameButton.onClick.AddListener(GameManager.singleton.StartAnswering_Rpc);
                UpdateQuestionCards(); //should have the latest version of cards
            }
            else
            {
                GameManager.singleton.SyncCurrentQuestionCards_OwnerClient();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            GameManager.QuestionCardsUpdated -= UpdateQuestionCards;
            GameManager.PlayerNameConfirmed -= UpdateQuestionCards;

            if (Player.owningPlayer.IsOwnedByServer)
            {
                startGameButton.onClick.RemoveListener(GameManager.singleton.StartAnswering_Rpc);
            }
        }

        void UpdateQuestionCards()
        {
            foreach (GameObject card in m_questionCardObjects)
            {
                Destroy(card);
            }
            m_questionCardObjects.Clear();

            List<FixedString128Bytes> questionCards = GameManager.singleton.GetCurrentQuestionCards();
            for (int i = 0; i < questionCards.Count; i++)
            {
                GameObject cardObject = Instantiate(questionCardPrefab, questionCardParent);
                m_questionCardObjects.Add(cardObject);

                if (Player.owningPlayer.IsOwnedByServer)
                {
                    int cardIndex = i;
                    m_questionCardObjects[i].GetComponent<Button>().onClick.AddListener(
                        () => { GameManager.singleton.ChangeQuestionCard_Host(cardIndex); }
                    );
                }
                m_questionCardObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(questionCards[i].ToString());

                Button cardButton = m_questionCardObjects[i].GetComponent<Button>();
                cardButton.interactable = Player.owningPlayer.IsOwnedByServer;
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
        }
    }

}