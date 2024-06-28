using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using static AnswerSheet;
using static UnityEngine.GraphicsBuffer;

public class GameManager : NetworkBehaviour
{
    //========================================================================
    public static GameManager singleton;

    public List<AnswerSheet> answerSheets = new();

    //========================================================================
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
    }

    public void SubscribeEventsForServer()
    {
        PlayerManager.PlayerAdded += AddQuestionCard;
        PlayerManager.PlayerAdded += AddPlayerAnswerSheet;
    }

    //========================================================================
    void AddQuestionCard()
    {
        //add the amount of question cards that a player can answer for others but not themself
        if (PlayerManager.singleton.playerCount == 1)
            return; //do not create a question card for the first player

        currentQuestionCards.Add(currentQuestionCards.Count);

        QuestionCardsUpdated?.Invoke();

        SyncCurrentQuestionCardsOnClients_Host();
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
    public void ChangeQuestionCard_Host(int cardIndex)
    {
        if (++currentQuestionCards[cardIndex] >= possibleQuestionCards.Length)
        {
            currentQuestionCards[cardIndex] = 0;
        }

        QuestionCardsUpdated?.Invoke();

        SyncCurrentQuestionCardsOnClients_Host();
    }
    void ChangeQuestionCard_Client(int cardIndex, int card)
    {
        if (cardIndex == currentQuestionCards.Count)
            currentQuestionCards.Add(card);
        else
            currentQuestionCards[cardIndex] = card;
    }

    //========================================================================
    void SyncCurrentQuestionCardsOnClients_Host()
    {
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            SyncSingleQuestionCard_ClientRpc(i, currentQuestionCards[i]);
        }
        FinishedQuestionCardSync_ClientRpc();
    }
    [Rpc(SendTo.NotServer)]
    void SyncSingleQuestionCard_ClientRpc(int cardIndex, int card)
    {
        print("any client syncing a question card");
        if (cardIndex > currentQuestionCards.Count)
            Debug.LogError("question cards not updating in order");

        ChangeQuestionCard_Client(cardIndex, card);
    }
    [Rpc(SendTo.NotServer)]
    void FinishedQuestionCardSync_ClientRpc()
    {
        print("any client question card sync finished");
        QuestionCardsUpdated?.Invoke();
    }

    //========================================================================
    public void SyncCurrentQuestionCards_TargetClient()
    {
        SyncCurrentQuestionCards_TargetRpc(RpcTarget.Owner);
    }
    [Rpc(SendTo.Server, AllowTargetOverride = true)]
    void SyncCurrentQuestionCards_TargetRpc(RpcParams targetRpc)
    {
        print("server syncing question cards");

        RpcParams target = RpcTarget.Single(targetRpc.Receive.SenderClientId, RpcTargetUse.Temp);
        for (int i = 0; i < currentQuestionCards.Count; i++)
        {
            SyncSingleQuestionCard_TargetRpc(i, currentQuestionCards[i], target);
        }
        FinishedQuestionCardSync_TargetRpc(target);
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void SyncSingleQuestionCard_TargetRpc(int cardIndex, int card, RpcParams targetRPC)
    {
        print("target client syncing a question card");
        if (cardIndex > currentQuestionCards.Count)
            Debug.LogError("question cards not updating in order");

        ChangeQuestionCard_Client(cardIndex, card);
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void FinishedQuestionCardSync_TargetRpc(RpcParams targetRPC)
    {
        print("target client question card sync finished");
        QuestionCardsUpdated?.Invoke();
    }

    //========================================================================
    [Rpc(SendTo.Everyone)]
    public void StartAnswering_Rpc()
    {
        UIManager.singleton.ChangeUIState<State_AnswerSheet>();
    }

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
