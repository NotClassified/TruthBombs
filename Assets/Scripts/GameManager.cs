using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using static AnswerSheet;

public class GameManager : NetworkBehaviour
{
    public static GameManager singleton;

    public List<AnswerSheet> answerSheets = new();

    public static event System.Action QuestionCardsUpdated;
    public List<int> currentQuestionCards = new();
    public FixedString128Bytes[] possibleQuestionCards = {
        "one",
        "two",
        "three",
        "four",
        "five",
        "six",
        "seven"
    };

    //========================================================================
    private void Awake()
    {
        singleton = this;

        PlayerManager.PlayerAdded += AddQuestionCard;
        PlayerManager.PlayerAdded += AddPlayerAnswerSheet;
    }

    //========================================================================
    public void AddQuestionCard()
    {
        //add the amount of question cards that a player can answer for others but not themself
        if (PlayerManager.singleton.playerCount == 1)
            return; //do not create a question card for the first player

        currentQuestionCards.Add(currentQuestionCards.Count);

        QuestionCardsUpdated?.Invoke();
    }
    public List<FixedString128Bytes> GetCurrentQuestionCards()
    {
        List<FixedString128Bytes> questionCards = new List<FixedString128Bytes>();
        foreach (int cardIndex in currentQuestionCards)
        {
            questionCards.Add(possibleQuestionCards[cardIndex]);
        }
        return questionCards;
    }
    public void ChangeQuestionCard(int cardIndex)
    {
        if (++currentQuestionCards[cardIndex] >= possibleQuestionCards.Length)
        {
            currentQuestionCards[cardIndex] = 0;
        }

        QuestionCardsUpdated?.Invoke();
    }

    //========================================================================
    public void SyncCurrentQuestionCards()
    {
        SyncCurrentQuestionCards_Rpc(RpcTarget.Owner);
    }
    [Rpc(SendTo.Server)]
    void SyncCurrentQuestionCards_Rpc(RpcParams targetRPC)
    {
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            SyncSingleQuestionCard_Rpc(i, currentQuestionCards[i], targetRPC);
        }
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void SyncSingleQuestionCard_Rpc(int cardIndex, int card, RpcParams targetRPC)
    {
        if (cardIndex > currentQuestionCards.Count)
            Debug.LogError("question cards not updating in order");

        if (cardIndex == currentQuestionCards.Count)
            currentQuestionCards.Add(card);
        else
            currentQuestionCards[cardIndex] = card;

        QuestionCardsUpdated?.Invoke();
    }

    //========================================================================
    void AddPlayerAnswerSheet()
    {
        answerSheets.Add(new AnswerSheet());
    }

    public void AddAnswer(int answerSheetIndex, int cardIndex, CardAnswer newAnswer)
    {
        answerSheets[answerSheetIndex].cardAnswers[cardIndex] = newAnswer;
    }
}

public class AnswerSheet
{
    public class CardAnswer
    {
        public int answeringPlayerIndex = -1;
        public FixedString128Bytes answerString;
    }
    public List<CardAnswer> cardAnswers = new List<CardAnswer>();
}
