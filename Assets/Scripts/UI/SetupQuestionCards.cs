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
        public static SetupQuestionCards singleton;

        public Button startGameButton;

        public Transform questionCardParent;
        public GameObject questionCardPrefab;
        List<GameObject> m_questionCardObjects = new();

        event UnityAction StartGameCallback = () => { GameManager.singleton.StartAnswering_Rpc(); };

        private void Awake()
        {
            singleton = this;

            if (Player.owningPlayer.IsServer)
            {

                startGameButton.onClick.AddListener(StartGameCallback);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            startGameButton.gameObject.SetActive(Player.owningPlayer.IsServer);

            GameManager.QuestionCardsUpdated += UpdateQuestionCards;

            if (Player.owningPlayer.IsServer)
            {
                UpdateQuestionCards(); //should have the latest version of cards
            }
            else
            {
                GameManager.singleton.SyncCurrentQuestionCards_TargetClient();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            GameManager.QuestionCardsUpdated -= UpdateQuestionCards;
        }

        void UpdateQuestionCards()
        {
            List<FixedString128Bytes> questionCards = GameManager.singleton.GetCurrentQuestionCards();
            for (int i = 0; i < Mathf.Max(questionCards.Count, m_questionCardObjects.Count); i++)
            {
                if (i >= questionCards.Count)
                {
                    Destroy(m_questionCardObjects[i]);
                    m_questionCardObjects.RemoveAt(i);
                    continue; //destroy the rest of the card objects
                }

                if (i >= m_questionCardObjects.Count)
                {
                    GameObject cardObject = Instantiate(questionCardPrefab, questionCardParent);
                    m_questionCardObjects.Add(cardObject);

                    if (Player.owningPlayer.IsServer)
                    {
                        int cardIndex = i;
                        m_questionCardObjects[i].GetComponent<Button>().onClick.AddListener(
                            () => { GameManager.singleton.ChangeQuestionCard_Host(cardIndex); }
                        );
                    }
                }
                m_questionCardObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(questionCards[i].ToString());

                Button cardButton = m_questionCardObjects[i].GetComponent<Button>();
                cardButton.interactable = Player.owningPlayer.IsServer;
            }
        }
    }

}