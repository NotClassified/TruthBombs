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

public class State_SetupQuestionCards : StateBase
{
    public static State_SetupQuestionCards singleton;

    public Button startGameButton;

    public Transform questionCardParent;
    public GameObject questionCardPrefab;
    List<GameObject> m_questionCardObjects = new List<GameObject>();

    event UnityAction StartGameCallback = () => { Player.owningPlayer.StartGame_ClientRpc(); };

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
        }
        else
        {
            print("update all client state");
            GameManager.singleton.SyncCurrentQuestionCardIndexes();
        }
    }

    public override void OnExit()
    {
        base.OnExit();

        GameManager.QuestionCardsUpdated -= UpdateQuestionCards;
    }

    void CreateQuestionCards_Server()
    {
        m_questionCardObjects.Clear();

        //amount of question cards that a player can answer for others but not themself
        for (int i = 0; i < PlayerManager.singleton.allPlayers.Count - 1; i++)
        {
            GameObject cardObject;

            if (i < questionCardParent.childCount)
            {
                cardObject = questionCardParent.GetChild(i).gameObject;
            }
            else
            {
                cardObject = Instantiate(questionCardPrefab, questionCardParent);
            }

            m_questionCardObjects.Add(cardObject.GetComponent<Button>());
        }

        //add click listeners for buttons
        startGameButton.onClick.AddListener(() => { UIManager.singleton.ChangeUIState<State_EnterPlayerName>(); });
        for (int i = 0; i < m_questionCardObjects.Count; i++)
        {
            int index = i;
            m_questionCardObjects[i].onClick.RemoveAllListeners();
            m_questionCardObjects[i].onClick.AddListener(() => { ChangeQuestion(index); });
        }

        UpdateAllQuestionCards_Server();
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
                        () => { GameManager.singleton.ChangeQuestionCard(cardIndex); }
                    );
                }
            }
            m_questionCardObjects[i].GetComponent<TextMeshProUGUI>().SetText(questionCards[i].ToString());

            Button cardButton = m_questionCardObjects[i].GetComponent<Button>();
            cardButton.interactable = Player.owningPlayer.IsServer;
        }
    }

    void ChangeQuestion(int index)
    {
        TextMeshProUGUI cardText = m_questionCardObjects[index].transform.GetComponentInChildren<TextMeshProUGUI>();
        GameManager.singleton.questionCards[index] += "1";
        cardText.text = GameManager.singleton.questionCards[index].ToString();
        Player.owningPlayer.UpdateQuestionCard_Rpc(index, GameManager.singleton.questionCards[index]);
    }

    public void UpdateQuestionCard_NotServer(int cardIndex, FixedString128Bytes newNum)
    {
        if (!isCurrentState)
            return;

        print("update one client state");
        int infiniteStopper = 0;
        while (cardIndex >= questionCardParent.childCount)
        {
            if (infiniteStopper++ > 1000)
            {
                Debug.LogError("Inifinite Loop");
                return;
            }

            GameObject cardObject = Instantiate(questionCardPrefab, questionCardParent);
            cardObject.GetComponent<Button>().interactable = false;
            m_questionCardObjects.Add(cardObject.GetComponent<Button>());
        }
        m_questionCardObjects[cardIndex].transform.GetComponentInChildren<TextMeshProUGUI>().text = newNum.ToString();

    }
    public void UpdateAllQuestionCards_Server()
    {
        print("update all server state");
        for (int i = 0; i < GameManager.singleton.questionCards.Count; i++)
        {
            Player.owningPlayer.UpdateQuestionCard_Rpc(i, GameManager.singleton.questionCards[i]);
        }
    }
}
